using GarageApp.Data;
using GarageApp.Models;
namespace GarageApp.Services;
public sealed class ProductService
{
    public List<Product> Search(string search = "")
    {
        using var c = Database.Open(); using var q = c.CreateCommand();
        q.CommandText = """
        SELECT p.Id,p.Code,p.Name,p.PurchasePrice,p.SalePrice,p.StockQuantity,COALESCE(cat.Name,''),COALESCE(p.ImagePath,''),p.ProfitPercent
        FROM Products p LEFT JOIN Categories cat ON cat.Id=p.CategoryId
        WHERE p.IsActive=1 AND (p.Name LIKE $s OR p.Code LIKE $s) ORDER BY p.Id DESC;
        """;
        q.Parameters.AddWithValue("$s", $"%{search}%");
        using var r = q.ExecuteReader(); var list = new List<Product>();
        while (r.Read()) list.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetInt64(3),r.GetInt64(4),r.GetInt32(5),r.GetString(6),r.GetString(7),r.GetDouble(8)));
        return list;
    }

    public int GetLowStockThreshold()
    {
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="SELECT [Value] FROM AppSettings WHERE [Key]='LowStockThreshold';";
        var value=q.ExecuteScalar();
        if(value is null or DBNull) { using var add=c.CreateCommand(); add.CommandText="INSERT OR IGNORE INTO AppSettings([Key],[Value]) VALUES('LowStockThreshold','2');"; add.ExecuteNonQuery(); return 2; }
        return int.TryParse(Convert.ToString(value),out var n)?Math.Max(0,n):2;
    }
    public void SetLowStockThreshold(int threshold)
    {
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="INSERT INTO AppSettings([Key],[Value]) VALUES('LowStockThreshold',$v) ON CONFLICT([Key]) DO UPDATE SET [Value]=excluded.[Value];";
        q.Parameters.AddWithValue("$v",threshold.ToString()); q.ExecuteNonQuery();
    }
    public List<Product> GetLowStock(int threshold)
    {
        return Search().Where(p=>p.StockQuantity<=threshold).OrderBy(p=>p.StockQuantity).ThenBy(p=>p.Name).ToList();
    }

    public double GetProfitPercent()
    {
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="SELECT [Value] FROM AppSettings WHERE [Key]='ProfitPercent';";
        return double.Parse(Convert.ToString(q.ExecuteScalar() ?? "30")!, System.Globalization.CultureInfo.InvariantCulture);
    }
    public long CalculateSalePrice(long purchase) => (long)Math.Round(purchase*(1+GetProfitPercent()/100),MidpointRounding.AwayFromZero);
    public void SetProfitPercent(double percent)
    {
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="UPDATE AppSettings SET [Value]=$v WHERE [Key]='ProfitPercent';";
        q.Parameters.AddWithValue("$v",percent.ToString(System.Globalization.CultureInfo.InvariantCulture)); q.ExecuteNonQuery();
    }
    public void Save(int? id,string code,string name,long purchase,long sale,int stock,string category,string imagePath,double profitPercent)
    {
        if(string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(name)) throw new ArgumentException("کد و نام قطعه الزامی است.");
        if(purchase<0||sale<0||stock<0||profitPercent<0||profitPercent>1000) throw new ArgumentException("قیمت و موجودی نامعتبر است. درصد سود باید بین صفر تا ۱۰۰۰ باشد.");
        using var c=Database.Open(); using var tx=c.BeginTransaction(); long? catId=null;

        // Validate uniqueness before INSERT/UPDATE to show a useful message.
        using (var duplicate = c.CreateCommand())
        {
            duplicate.Transaction = tx;
            duplicate.CommandText = id is null
                ? "SELECT 1 FROM Products WHERE TRIM(Code) = TRIM($code) COLLATE NOCASE LIMIT 1;"
                : "SELECT 1 FROM Products WHERE TRIM(Code) = TRIM($code) COLLATE NOCASE AND Id <> $id LIMIT 1;";
            duplicate.Parameters.AddWithValue("$code", code.Trim());
            if (id is not null) duplicate.Parameters.AddWithValue("$id", id.Value);
            if (duplicate.ExecuteScalar() is not null)
                throw new ArgumentException("این کد قطعه قبلاً ثبت شده است. لطفاً یک کد متفاوت وارد کنید.");
        }

        if(!string.IsNullOrWhiteSpace(category))
        {
            using var cat=c.CreateCommand(); cat.Transaction=tx;
            cat.CommandText="INSERT OR IGNORE INTO Categories(Name) VALUES($n); SELECT Id FROM Categories WHERE Name=$n;";
            cat.Parameters.AddWithValue("$n",category.Trim()); catId=Convert.ToInt64(cat.ExecuteScalar());
        }
        using var q=c.CreateCommand(); q.Transaction=tx;
        q.CommandText=id is null
          ? "INSERT INTO Products(Code,Name,CategoryId,PurchasePrice,SalePrice,StockQuantity,ImagePath,ProfitPercent) VALUES($c,$n,$cat,$p,$s,$q,$img,$profit);"
          : "UPDATE Products SET Code=$c,Name=$n,CategoryId=$cat,PurchasePrice=$p,SalePrice=$s,StockQuantity=$q,ImagePath=$img,ProfitPercent=$profit WHERE Id=$id;";
        if(id is not null) q.Parameters.AddWithValue("$id",id.Value);
        q.Parameters.AddWithValue("$c",code.Trim()); q.Parameters.AddWithValue("$n",name.Trim());
        q.Parameters.AddWithValue("$cat",catId is null?DBNull.Value:catId.Value);
        q.Parameters.AddWithValue("$p",purchase); q.Parameters.AddWithValue("$s",sale); q.Parameters.AddWithValue("$q",stock); q.Parameters.AddWithValue("$img",string.IsNullOrWhiteSpace(imagePath)?DBNull.Value:imagePath); q.Parameters.AddWithValue("$profit",profitPercent);
        q.ExecuteNonQuery(); tx.Commit();
    }
    public void Delete(int id)
    {
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="UPDATE Products SET IsActive=0 WHERE Id=$id;"; q.Parameters.AddWithValue("$id",id); q.ExecuteNonQuery();
    }
}
