using GarageApp.Data;
using GarageApp.Models;
namespace GarageApp.Services;
public sealed class RepairService
{
    public List<Vehicle> GetVehicles()
    {
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="SELECT v.Id,c.FullName,v.Model,COALESCE(v.PlateNumber,'') FROM Vehicles v JOIN Customers c ON c.Id=v.CustomerId ORDER BY v.Id DESC;";
        using var r=q.ExecuteReader(); var list=new List<Vehicle>();
        while(r.Read()) list.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetString(3)));
        return list;
    }
    public void AddCustomerVehicle(string customer,string phone,string model,string plate)
    {
        if(string.IsNullOrWhiteSpace(customer)||string.IsNullOrWhiteSpace(model)) throw new ArgumentException("نام مشتری و مدل خودرو الزامی است.");
        using var c=Database.Open(); using var tx=c.BeginTransaction();
        using var q=c.CreateCommand(); q.Transaction=tx;
        q.CommandText="INSERT INTO Customers(FullName,Phone) VALUES($n,$p); SELECT last_insert_rowid();";
        q.Parameters.AddWithValue("$n",customer.Trim()); q.Parameters.AddWithValue("$p",phone.Trim());
        long customerId=Convert.ToInt64(q.ExecuteScalar());
        using var v=c.CreateCommand(); v.Transaction=tx; v.CommandText="INSERT INTO Vehicles(CustomerId,Model,PlateNumber) VALUES($c,$m,$p);";
        v.Parameters.AddWithValue("$c",customerId); v.Parameters.AddWithValue("$m",model.Trim()); v.Parameters.AddWithValue("$p",plate.Trim());
        v.ExecuteNonQuery(); tx.Commit();
    }

    public System.Data.DataTable GetEmployeeWorkSummary(string month = "", string day = "")
    {
        using var c = Database.Open();
        using var q = c.CreateCommand();
        q.CommandText = """
            SELECT rw.EmployeeNameAtTime AS EmployeeName,
                   COUNT(DISTINCT rw.RepairId) AS RepairCount,
                   COALESCE(SUM(rw.LaborShare), 0) AS LaborEarned
            FROM RepairWorkers rw
            JOIN Repairs r ON r.Id = rw.RepairId
            WHERE ($m = '' OR substr(r.RepairDate, 1, 7) = $m)
              AND ($d = '' OR r.RepairDate = $d)
            GROUP BY rw.EmployeeNameAtTime
            ORDER BY rw.EmployeeNameAtTime;
            """;
        q.Parameters.AddWithValue("$m", month);
        q.Parameters.AddWithValue("$d", day);
        var table = new System.Data.DataTable();
        using var reader = q.ExecuteReader();
        table.Load(reader);
        return table;
    }

    public List<RepairSummary> GetRepairs(string month="", string day="")
    {
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="""
        SELECT r.Id,r.RepairDate,c.FullName,v.Model,COALESCE(v.PlateNumber,''),r.PartsTotal,r.LaborTotal,
        r.PartsTotal+r.LaborTotal-COALESCE(r.DiscountAmount,0),COALESCE((SELECT SUM(Amount) FROM Payments WHERE RepairId=r.Id),0),
        r.PartsTotal+r.LaborTotal-COALESCE(r.DiscountAmount,0)-COALESCE((SELECT SUM(Amount) FROM Payments WHERE RepairId=r.Id),0),r.Status,
        COALESCE((SELECT SUM(LaborShare) FROM RepairWorkers WHERE RepairId=r.Id),0),
        r.PartsTotal+r.LaborTotal-COALESCE(r.DiscountAmount,0)-COALESCE((SELECT SUM(LaborShare) FROM RepairWorkers WHERE RepairId=r.Id),0),
        COALESCE((SELECT group_concat(EmployeeNameAtTime, ', ') FROM RepairWorkers WHERE RepairId=r.Id),'—'),
        COALESCE((SELECT group_concat(ProductNameAtTime || ' × ' || Quantity, ', ') FROM RepairItems WHERE RepairId=r.Id),'—')
        FROM Repairs r JOIN Vehicles v ON v.Id=r.VehicleId JOIN Customers c ON c.Id=v.CustomerId
        WHERE ($m='' OR substr(r.RepairDate,1,7)=$m) AND ($d='' OR r.RepairDate=$d) ORDER BY r.Id DESC;
        """;
        q.Parameters.AddWithValue("$m",month); q.Parameters.AddWithValue("$d",day); using var r=q.ExecuteReader(); var list=new List<RepairSummary>();
        while(r.Read()) list.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetInt64(5),r.GetInt64(6),r.GetInt64(7),r.GetInt64(8),r.GetInt64(9),r.GetString(10),r.GetInt64(11),r.GetInt64(12),r.GetString(13),r.GetString(14)));
        return list;
    }
    public void CreateRepair(int vehicleId,string description,long labor,
      List<(int id,string name,int qty,long purchase,long sale)> items,
      List<(int id,string name,double percent)> workers, long initialPayment = -1, long discountAmount = 0)
    {
        if(labor<0) throw new ArgumentException("دستمزد نمی‌تواند منفی باشد.");
        if(workers.Count==0) throw new ArgumentException("حداقل یک تعمیرکار انتخاب کنید.");
        if(workers.Any(x=>x.percent<0 || x.percent>100)) throw new ArgumentException("درصد هر تعمیرکار باید بین صفر تا ۱۰۰ باشد.");
        if(workers.Sum(x=>x.percent)>100.001) throw new ArgumentException("مجموع درصد سهم تعمیرکاران نمی‌تواند بیشتر از ۱۰۰٪ باشد؛ باقی دستمزد سهم تعمیرگاه است.");
        if(items.Any(x=>x.qty<=0)) throw new ArgumentException("تعداد قطعه باید بیشتر از صفر باشد.");
        long parts=items.Sum(x=>x.sale*x.qty);
        long subtotal=checked(parts+labor);
        if(discountAmount<0 || discountAmount>subtotal) throw new ArgumentException("مبلغ تخفیف نامعتبر است.");
        long total=subtotal-discountAmount;
        if(initialPayment < 0) initialPayment=total;
        if(initialPayment > total) throw new ArgumentException("مبلغ دریافتی نمی‌تواند بیشتر از جمع کل پس از تخفیف باشد.");
        using var c=Database.Open(); using var tx=c.BeginTransaction();
        using var rq=c.CreateCommand(); rq.Transaction=tx;
        rq.CommandText="INSERT INTO Repairs(VehicleId,RepairDate,Description,LaborTotal,PartsTotal,DiscountAmount,Status) VALUES($v,$d,$x,$l,$p,$discount,'Open'); SELECT last_insert_rowid();";
        rq.Parameters.AddWithValue("$v",vehicleId); rq.Parameters.AddWithValue("$d",DateTime.Now.ToString("yyyy-MM-dd"));
        rq.Parameters.AddWithValue("$x",description); rq.Parameters.AddWithValue("$l",labor); rq.Parameters.AddWithValue("$p",parts); rq.Parameters.AddWithValue("$discount",discountAmount);
        long repairId=Convert.ToInt64(rq.ExecuteScalar());
        if(initialPayment > 0)
        {
            using var payment=c.CreateCommand(); payment.Transaction=tx;
            payment.CommandText="INSERT INTO Payments(RepairId,Amount,PaymentDate,Note) VALUES($r,$a,$d,$n);";
            payment.Parameters.AddWithValue("$r",repairId); payment.Parameters.AddWithValue("$a",initialPayment);
            payment.Parameters.AddWithValue("$d",DateTime.Now.ToString("yyyy-MM-dd"));
            payment.Parameters.AddWithValue("$n",initialPayment==total ? "تسویه هنگام ثبت تعمیر" : "پرداخت بخشی از مبلغ هنگام ثبت تعمیر");
            payment.ExecuteNonQuery();
        }
        foreach(var item in items)
        {
            using var stock=c.CreateCommand(); stock.Transaction=tx; stock.CommandText="SELECT StockQuantity FROM Products WHERE Id=$id AND IsActive=1;";
            stock.Parameters.AddWithValue("$id",item.id); var raw=stock.ExecuteScalar();
            if(raw is null) throw new InvalidOperationException($"قطعه {item.name} پیدا نشد.");
            if(Convert.ToInt32(raw)<item.qty) throw new InvalidOperationException($"موجودی «{item.name}» کافی نیست.");
            using var q=c.CreateCommand(); q.Transaction=tx;
            q.CommandText="""
            INSERT INTO RepairItems(RepairId,ProductId,ProductNameAtTime,Quantity,PurchasePriceAtTime,SalePriceAtTime)
            VALUES($r,$p,$n,$q,$b,$s);
            UPDATE Products SET StockQuantity=StockQuantity-$q WHERE Id=$p;
            INSERT INTO StockMovements(ProductId,RepairId,QuantityChange,MovementDate,Description)
            VALUES($p,$r,-$q,$d,'مصرف در تعمیر');
            """;
            q.Parameters.AddWithValue("$r",repairId); q.Parameters.AddWithValue("$p",item.id); q.Parameters.AddWithValue("$n",item.name);
            q.Parameters.AddWithValue("$q",item.qty); q.Parameters.AddWithValue("$b",item.purchase); q.Parameters.AddWithValue("$s",item.sale);
            q.Parameters.AddWithValue("$d",DateTime.Now.ToString("yyyy-MM-dd")); q.ExecuteNonQuery();
        }
        foreach(var w in workers)
        {
            long share=(long)Math.Round(labor*w.percent/100,MidpointRounding.AwayFromZero);
            using var q=c.CreateCommand(); q.Transaction=tx;
            q.CommandText="""
            INSERT INTO RepairWorkers(RepairId,EmployeeId,EmployeeNameAtTime,LaborPercent,LaborShare) VALUES($r,$e,$n,$p,$s);
            INSERT INTO EmployeeTransactions(EmployeeId,RepairId,TransactionType,Amount,TransactionDate,Description)
            VALUES($e,$r,'LaborEarned',$s,$d,'سهم دستمزد تعمیر');
            """;
            q.Parameters.AddWithValue("$r",repairId); q.Parameters.AddWithValue("$e",w.id); q.Parameters.AddWithValue("$n",w.name);
            q.Parameters.AddWithValue("$p",w.percent); q.Parameters.AddWithValue("$s",share); q.Parameters.AddWithValue("$d",DateTime.Now.ToString("yyyy-MM-dd"));
            q.ExecuteNonQuery();
        }
        tx.Commit();
    }


    public List<(int id, string name, int qty, long purchase, long sale)> GetRepairItems(int repairId)
    {
        using var c = Database.Open(); using var q = c.CreateCommand();
        q.CommandText = "SELECT ProductId,ProductNameAtTime,Quantity,PurchasePriceAtTime,SalePriceAtTime FROM RepairItems WHERE RepairId=$r ORDER BY Id;";
        q.Parameters.AddWithValue("$r", repairId);
        using var reader = q.ExecuteReader();
        var result = new List<(int id, string name, int qty, long purchase, long sale)>();
        while (reader.Read()) result.Add((reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt64(3), reader.GetInt64(4)));
        return result;
    }

    public void UpdateRepairDetails(int repairId, string repairDate, string customerName, string vehicleModel, string plateNumber, long laborTotal, long discountAmount,
        List<(int id, string name, int qty, long purchase, long sale)> updatedItems)
    {
        if (repairId <= 0) throw new ArgumentException("شماره تعمیر نامعتبر است.");
        if (!DateTime.TryParseExact(repairDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _))
            throw new ArgumentException("تاریخ باید به شکل YYYY-MM-DD باشد.");
        if (string.IsNullOrWhiteSpace(customerName)) throw new ArgumentException("نام مشتری الزامی است.");
        if (string.IsNullOrWhiteSpace(vehicleModel)) throw new ArgumentException("مدل خودرو الزامی است.");
        if (laborTotal < 0) throw new ArgumentException("اجرت نمی‌تواند منفی باشد.");
        if (discountAmount < 0) throw new ArgumentException("تخفیف نمی‌تواند منفی باشد.");
        if (updatedItems.Any(x => x.qty <= 0)) throw new ArgumentException("تعداد هر قطعه باید بیشتر از صفر باشد.");
        if (updatedItems.GroupBy(x => x.id).Any(g => g.Count() > 1)) throw new ArgumentException("هر قطعه باید فقط یک ردیف داشته باشد.");

        using var c = Database.Open();
        using var tx = c.BeginTransaction();
        int vehicleId;
        long paid;
        long existingDiscount;
        using (var check = c.CreateCommand())
        {
            check.Transaction = tx;
            check.CommandText = "SELECT r.VehicleId, r.PartsTotal, COALESCE((SELECT SUM(Amount) FROM Payments WHERE RepairId=r.Id),0), COALESCE(r.DiscountAmount,0) FROM Repairs r WHERE r.Id=$r;";
            check.Parameters.AddWithValue("$r", repairId);
            using var reader = check.ExecuteReader();
            if (!reader.Read()) throw new InvalidOperationException("تعمیر موردنظر پیدا نشد.");
            vehicleId = reader.GetInt32(0);
            paid = reader.GetInt64(2);
            existingDiscount = reader.GetInt64(3);
        }
        long partsTotal = updatedItems.Sum(x => checked(x.sale * x.qty));
        if (discountAmount > partsTotal + laborTotal) throw new ArgumentException("تخفیف نمی‌تواند از جمع قطعات و اجرت بیشتر باشد.");
        if (partsTotal + laborTotal - discountAmount < paid)
            throw new ArgumentException($"مبلغ جدید پس از تخفیف ({partsTotal + laborTotal - discountAmount:N0}) از مبلغ دریافتی قبلی ({paid:N0}) کمتر می‌شود.");

        // Restore the previous quantities first, then validate and deduct the edited quantities.
        // All stock and repair-item changes are part of this transaction, so failures roll back together.
        using (var restore = c.CreateCommand())
        {
            restore.Transaction = tx;
            restore.CommandText = "UPDATE Products SET StockQuantity=StockQuantity+COALESCE((SELECT SUM(Quantity) FROM RepairItems WHERE RepairId=$r AND ProductId=Products.Id),0) WHERE Id IN (SELECT ProductId FROM RepairItems WHERE RepairId=$r);";
            restore.Parameters.AddWithValue("$r", repairId); restore.ExecuteNonQuery();
        }
        foreach (var item in updatedItems)
        {
            using var stockCheck = c.CreateCommand(); stockCheck.Transaction = tx;
            stockCheck.CommandText = "SELECT StockQuantity,IsActive FROM Products WHERE Id=$id;";
            stockCheck.Parameters.AddWithValue("$id", item.id);
            using var stockReader = stockCheck.ExecuteReader();
            if (!stockReader.Read()) throw new InvalidOperationException($"قطعه «{item.name}» پیدا نشد.");
            int available = stockReader.GetInt32(0); bool active = stockReader.GetInt32(1) == 1;
            stockReader.Close();
            if (!active) throw new InvalidOperationException($"قطعه «{item.name}» غیرفعال است و نمی‌توان آن را برای تعمیر انتخاب کرد.");
            if (available < item.qty) throw new InvalidOperationException($"موجودی قطعه «{item.name}» کافی نیست. موجودی قابل استفاده: {available} عدد.");
            using var deduct = c.CreateCommand(); deduct.Transaction = tx;
            deduct.CommandText = "UPDATE Products SET StockQuantity=StockQuantity-$q WHERE Id=$id;";
            deduct.Parameters.AddWithValue("$q", item.qty); deduct.Parameters.AddWithValue("$id", item.id); deduct.ExecuteNonQuery();
        }
        using (var clearItems = c.CreateCommand())
        {
            clearItems.Transaction = tx;
            clearItems.CommandText = "DELETE FROM StockMovements WHERE RepairId=$r AND Description='مصرف در تعمیر'; DELETE FROM RepairItems WHERE RepairId=$r;";
            clearItems.Parameters.AddWithValue("$r", repairId); clearItems.ExecuteNonQuery();
        }
        foreach (var item in updatedItems)
        {
            using var addItem = c.CreateCommand(); addItem.Transaction = tx;
            addItem.CommandText = "INSERT INTO RepairItems(RepairId,ProductId,ProductNameAtTime,Quantity,PurchasePriceAtTime,SalePriceAtTime) VALUES($r,$p,$n,$q,$b,$s); INSERT INTO StockMovements(ProductId,RepairId,QuantityChange,MovementDate,Description) VALUES($p,$r,-$q,$d,'مصرف در تعمیر');";
            addItem.Parameters.AddWithValue("$r", repairId); addItem.Parameters.AddWithValue("$p", item.id); addItem.Parameters.AddWithValue("$n", item.name);
            addItem.Parameters.AddWithValue("$q", item.qty); addItem.Parameters.AddWithValue("$b", item.purchase); addItem.Parameters.AddWithValue("$s", item.sale); addItem.Parameters.AddWithValue("$d", repairDate);
            addItem.ExecuteNonQuery();
        }

        using (var customerUpdate = c.CreateCommand())
        {
            customerUpdate.Transaction = tx;
            customerUpdate.CommandText = "UPDATE Customers SET FullName=$name WHERE Id=(SELECT CustomerId FROM Vehicles WHERE Id=$v);";
            customerUpdate.Parameters.AddWithValue("$name", customerName.Trim()); customerUpdate.Parameters.AddWithValue("$v", vehicleId);
            customerUpdate.ExecuteNonQuery();
        }
        using (var vehicleUpdate = c.CreateCommand())
        {
            vehicleUpdate.Transaction = tx;
            vehicleUpdate.CommandText = "UPDATE Vehicles SET Model=$model, PlateNumber=$plate WHERE Id=$v;";
            vehicleUpdate.Parameters.AddWithValue("$model", vehicleModel.Trim()); vehicleUpdate.Parameters.AddWithValue("$plate", plateNumber.Trim()); vehicleUpdate.Parameters.AddWithValue("$v", vehicleId);
            vehicleUpdate.ExecuteNonQuery();
        }

        using (var update = c.CreateCommand())
        {
            update.Transaction = tx;
            update.CommandText = "UPDATE Repairs SET RepairDate=$date, LaborTotal=$labor, PartsTotal=$parts, DiscountAmount=$discount WHERE Id=$r;";
            update.Parameters.AddWithValue("$date", repairDate); update.Parameters.AddWithValue("$labor", laborTotal); update.Parameters.AddWithValue("$parts", partsTotal); update.Parameters.AddWithValue("$discount", discountAmount); update.Parameters.AddWithValue("$r", repairId);
            update.ExecuteNonQuery();
        }

        var shares = new List<(int employeeId, long amount)>();
        using (var q = c.CreateCommand())
        {
            q.Transaction = tx;
            q.CommandText = "SELECT EmployeeId, LaborPercent FROM RepairWorkers WHERE RepairId=$r;";
            q.Parameters.AddWithValue("$r", repairId);
            using var reader = q.ExecuteReader();
            while (reader.Read())
            {
                int employeeId = reader.GetInt32(0);
                double percent = reader.GetDouble(1);
                long share = (long)Math.Round(laborTotal * percent / 100d, MidpointRounding.AwayFromZero);
                shares.Add((employeeId, share));
            }
        }
        foreach (var share in shares)
        {
            using var updateWorker = c.CreateCommand(); updateWorker.Transaction = tx;
            updateWorker.CommandText = "UPDATE RepairWorkers SET LaborShare=$share WHERE RepairId=$r AND EmployeeId=$e;";
            updateWorker.Parameters.AddWithValue("$share", share.amount); updateWorker.Parameters.AddWithValue("$r", repairId); updateWorker.Parameters.AddWithValue("$e", share.employeeId);
            updateWorker.ExecuteNonQuery();
            using var updateLedger = c.CreateCommand(); updateLedger.Transaction = tx;
            updateLedger.CommandText = "UPDATE EmployeeTransactions SET Amount=$share, TransactionDate=$date WHERE RepairId=$r AND EmployeeId=$e AND TransactionType='LaborEarned';";
            updateLedger.Parameters.AddWithValue("$share", share.amount); updateLedger.Parameters.AddWithValue("$date", repairDate); updateLedger.Parameters.AddWithValue("$r", repairId); updateLedger.Parameters.AddWithValue("$e", share.employeeId);
            updateLedger.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public void DeleteRepair(int repairId)
    {
        if (repairId <= 0) throw new ArgumentException("شماره تعمیر نامعتبر است.");

        using var c = Database.Open();
        using var tx = c.BeginTransaction();

        // Restore every part consumed by this repair before removing its history.
        using (var restoreStock = c.CreateCommand())
        {
            restoreStock.Transaction = tx;
            restoreStock.CommandText = """
                UPDATE Products
                SET StockQuantity = StockQuantity + COALESCE((
                    SELECT SUM(ri.Quantity) FROM RepairItems ri
                    WHERE ri.RepairId = $r AND ri.ProductId = Products.Id
                ), 0)
                WHERE Id IN (SELECT ProductId FROM RepairItems WHERE RepairId = $r);
                """;
            restoreStock.Parameters.AddWithValue("$r", repairId);
            restoreStock.ExecuteNonQuery();
        }

        using (var exists = c.CreateCommand())
        {
            exists.Transaction = tx;
            exists.CommandText = "SELECT COUNT(*) FROM Repairs WHERE Id=$r;";
            exists.Parameters.AddWithValue("$r", repairId);
            if (Convert.ToInt32(exists.ExecuteScalar()) == 0)
                throw new InvalidOperationException("تعمیر موردنظر پیدا نشد.");
        }

        // Delete all records linked to this repair in one transaction.
        foreach (string sql in new[]
        {
            "DELETE FROM EmployeeTransactions WHERE RepairId=$r;",
            "DELETE FROM Payments WHERE RepairId=$r;",
            "DELETE FROM StockMovements WHERE RepairId=$r;",
            "DELETE FROM RepairWorkers WHERE RepairId=$r;",
            "DELETE FROM RepairItems WHERE RepairId=$r;",
            "DELETE FROM Repairs WHERE Id=$r;"
        })
        {
            using var command = c.CreateCommand();
            command.Transaction = tx;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$r", repairId);
            command.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public System.Data.DataTable GetMechanicUnpaidInvoices(string month = "")
    {
        using var c = Database.Open(); using var q = c.CreateCommand();
        q.CommandText = """
        SELECT e.FullName AS Mechanic, r.Id AS RepairId, r.RepairDate, c.FullName AS Customer,
               v.Model AS Vehicle, COALESCE(v.PlateNumber,'') AS Plate,
               r.PartsTotal+r.LaborTotal-COALESCE(r.DiscountAmount,0) AS InvoiceTotal,
               COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.RepairId=r.Id),0) AS Collected,
               r.PartsTotal+r.LaborTotal-COALESCE(r.DiscountAmount,0)-COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.RepairId=r.Id),0) AS Outstanding
        FROM Repairs r
        JOIN Vehicles v ON v.Id=r.VehicleId JOIN Customers c ON c.Id=v.CustomerId
        JOIN RepairWorkers rw ON rw.Id=(SELECT MIN(rw2.Id) FROM RepairWorkers rw2 WHERE rw2.RepairId=r.Id)
        JOIN Employees e ON e.Id=rw.EmployeeId
        WHERE r.PartsTotal+r.LaborTotal-COALESCE(r.DiscountAmount,0)>COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.RepairId=r.Id),0)
          AND ($m='' OR substr(r.RepairDate,1,7)=$m)
        ORDER BY r.RepairDate DESC, r.Id DESC;
        """;
        q.Parameters.AddWithValue("$m", month);
        var table = new System.Data.DataTable(); using var reader = q.ExecuteReader(); table.Load(reader); return table;
    }

    public long GetOutstanding(int repairId)
    {
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="SELECT r.PartsTotal+r.LaborTotal-COALESCE(r.DiscountAmount,0)-COALESCE((SELECT SUM(Amount) FROM Payments WHERE RepairId=r.Id),0) FROM Repairs r WHERE r.Id=$r;";
        q.Parameters.AddWithValue("$r",repairId);
        var value=q.ExecuteScalar();
        if(value is null) throw new InvalidOperationException("تعمیر موردنظر پیدا نشد.");
        return Convert.ToInt64(value);
    }

    public void AddPayment(int repairId, long amount, string note)
    {
        if(amount<=0) throw new ArgumentException("مبلغ دریافتی باید بیشتر از صفر باشد.");
        using var c=Database.Open(); using var tx=c.BeginTransaction();
        using var check=c.CreateCommand(); check.Transaction=tx;
        check.CommandText="SELECT r.PartsTotal+r.LaborTotal-COALESCE(r.DiscountAmount,0)-COALESCE((SELECT SUM(Amount) FROM Payments WHERE RepairId=r.Id),0) FROM Repairs r WHERE r.Id=$r;";
        check.Parameters.AddWithValue("$r",repairId); var raw=check.ExecuteScalar();
        if(raw is null) throw new InvalidOperationException("تعمیر موردنظر پیدا نشد.");
        long outstanding=Convert.ToInt64(raw);
        if(outstanding<=0) throw new InvalidOperationException("این تعمیر قبلاً تسویه شده است.");
        if(amount>outstanding) throw new ArgumentException($"مبلغ دریافتی بیشتر از بدهی باقی‌مانده است ({outstanding:N0} تومان).");
        using var q=c.CreateCommand(); q.Transaction=tx;
        q.CommandText="INSERT INTO Payments(RepairId,Amount,PaymentDate,Note) VALUES($r,$a,$d,$n);";
        q.Parameters.AddWithValue("$r",repairId); q.Parameters.AddWithValue("$a",amount);
        q.Parameters.AddWithValue("$d",DateTime.Now.ToString("yyyy-MM-dd")); q.Parameters.AddWithValue("$n",string.IsNullOrWhiteSpace(note)?"دریافت وجه بابت تعمیر":note.Trim());
        q.ExecuteNonQuery(); tx.Commit();
    }

}
