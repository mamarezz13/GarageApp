using GarageApp.Data;
using System.Data;
namespace GarageApp.Services;
public sealed class EmployeeLedgerService
{
    public void Add(int employeeId,string type,long amount,string description)
    {
        if(amount<=0) throw new ArgumentException("مبلغ باید بیشتر از صفر باشد.");
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="INSERT INTO EmployeeTransactions(EmployeeId,TransactionType,Amount,TransactionDate,Description) VALUES($e,$t,$a,$d,$x);";
        q.Parameters.AddWithValue("$e",employeeId); q.Parameters.AddWithValue("$t",type); q.Parameters.AddWithValue("$a",amount);
        q.Parameters.AddWithValue("$d",DateTime.Now.ToString("yyyy-MM-dd")); q.Parameters.AddWithValue("$x",description); q.ExecuteNonQuery();
    }
    public DataTable Transactions()
    {
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText = "SELECT t.Id, e.FullName AS Employee, t.TransactionType, t.Amount, t.TransactionDate, COALESCE(t.Description,'') AS Description FROM EmployeeTransactions t JOIN Employees e ON e.Id=t.EmployeeId ORDER BY t.TransactionDate DESC,t.Id DESC;";
        var table=new DataTable(); using var r=q.ExecuteReader(); table.Load(r); return table;
    }

    public void UpdateTransaction(int id, long amount, string date, string description, string type)
    {
        if(amount<=0) throw new ArgumentException("مبلغ باید بیشتر از صفر باشد.");
        if(!DateTime.TryParseExact(date,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out _)) throw new ArgumentException("تاریخ باید به شکل YYYY-MM-DD باشد.");
        if(type=="LaborEarned") throw new ArgumentException("سهم دستمزد متصل به فاکتور را از طریق ویرایش تعمیر اصلاح کن.");
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="UPDATE EmployeeTransactions SET Amount=$a,TransactionDate=$d,Description=$x,TransactionType=$t WHERE Id=$id;";
        q.Parameters.AddWithValue("$a",amount);q.Parameters.AddWithValue("$d",date);q.Parameters.AddWithValue("$x",description);q.Parameters.AddWithValue("$t",type);q.Parameters.AddWithValue("$id",id);
        if(q.ExecuteNonQuery()==0) throw new InvalidOperationException("تراکنش پیدا نشد.");
    }

    public void DeleteTransaction(int id)
    {
        using var c = Database.Open();
        using var q = c.CreateCommand();
        q.CommandText = "SELECT RepairId, TransactionType FROM EmployeeTransactions WHERE Id=$id;";
        q.Parameters.AddWithValue("$id", id);
        using var reader = q.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("تراکنش پیدا نشد.");
        bool linkedToRepair = !reader.IsDBNull(0);
        string type = reader.GetString(1);
        reader.Close();
        if (linkedToRepair || type == "LaborEarned")
            throw new InvalidOperationException("این تراکنش به تعمیر یا فاکتور متصل است؛ برای جلوگیری از به‌هم‌خوردن حسابداری، آن را از طریق ویرایش همان تعمیر اصلاح کن.");
        using var delete = c.CreateCommand();
        delete.CommandText = "DELETE FROM EmployeeTransactions WHERE Id=$id;";
        delete.Parameters.AddWithValue("$id", id);
        if (delete.ExecuteNonQuery() == 0) throw new InvalidOperationException("تراکنش پیدا نشد.");
    }

    public DataTable Balances(string month="")
    {
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="""
        SELECT e.Id,e.FullName,
        COALESCE(SUM(CASE WHEN t.TransactionType='LaborEarned' THEN t.Amount ELSE 0 END),0) Earned,
        COALESCE(SUM(CASE WHEN t.TransactionType='Payment' THEN t.Amount ELSE 0 END),0) Paid,
        COALESCE(SUM(CASE WHEN t.TransactionType='Advance' THEN t.Amount ELSE 0 END),0) Advance,
        COALESCE(SUM(CASE WHEN t.TransactionType='Debt' THEN t.Amount ELSE 0 END),0) Debt,
        COALESCE(SUM(CASE WHEN t.TransactionType='Credit' THEN t.Amount ELSE 0 END),0) Credit,
        COALESCE((SELECT SUM(r.PartsTotal+r.LaborTotal-COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.RepairId=r.Id),0))
          FROM Repairs r JOIN RepairWorkers rw ON rw.Id=(SELECT MIN(rw2.Id) FROM RepairWorkers rw2 WHERE rw2.RepairId=r.Id)
          WHERE rw.EmployeeId=e.Id AND r.PartsTotal+r.LaborTotal>COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.RepairId=r.Id),0)
          AND ($m='' OR substr(r.RepairDate,1,7)=$m)),0) CustomerInvoiceDebt
        FROM Employees e LEFT JOIN EmployeeTransactions t ON t.EmployeeId=e.Id
        AND ($m='' OR substr(t.TransactionDate,1,7)=$m)
        WHERE e.IsActive=1 GROUP BY e.Id,e.FullName ORDER BY e.FullName;
        """;
        q.Parameters.AddWithValue("$m",month); var table=new DataTable(); using var r=q.ExecuteReader(); table.Load(r);
        table.Columns.Add("NetBalance",typeof(long));
        foreach(DataRow row in table.Rows)
            row["NetBalance"]=Convert.ToInt64(row["Earned"])-Convert.ToInt64(row["Paid"])-Convert.ToInt64(row["Advance"])-Convert.ToInt64(row["Debt"])+Convert.ToInt64(row["Credit"]);
        return table;
    }
}
