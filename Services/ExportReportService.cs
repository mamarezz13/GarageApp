using GarageApp.Data;
using System.Net;
using System.Text;

namespace GarageApp.Services;

public sealed class ExportReportService
{
    static string E(object? value) => WebUtility.HtmlEncode(Convert.ToString(value) ?? "");
    static string Money(object? value) => Convert.ToInt64(value ?? 0).ToString("N0") + " تومان";
    static string Table(string title, string[] headers, IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<h2>").Append(E(title)).Append("</h2><table><thead><tr>");
        foreach (var h in headers) sb.Append("<th>").Append(E(h)).Append("</th>");
        sb.Append("</tr></thead><tbody>");
        bool any = false;
        foreach (var row in rows)
        {
            any = true; sb.Append("<tr>");
            foreach (var cell in row) sb.Append("<td>").Append(E(cell)).Append("</td>");
            sb.Append("</tr>");
        }
        if (!any) sb.Append("<tr><td colspan=\"").Append(headers.Length).Append("\">موردی برای نمایش وجود ندارد.</td></tr>");
        sb.Append("</tbody></table>"); return sb.ToString();
    }
    static string Document(string title, string subtitle, string content) => """
        <!DOCTYPE html><html lang="fa"><head><meta charset="utf-8"><style>
        @page { size: A4; margin: 1.5cm; }
        body { font-family: Tahoma, Arial, sans-serif; direction: rtl; text-align: right; color:#222; font-size:10pt; }
        h1 { text-align:center; font-size:18pt; margin-bottom:6px; color:#17365d; }
        .subtitle { text-align:center; color:#555; margin-bottom:24px; }
        h2 { font-size:13pt; color:#17365d; border-bottom:1px solid #9eafc3; padding-bottom:6px; margin-top:24px; }
        table { border-collapse:collapse; width:100%; margin:10px 0 18px; }
        th,td { border:1px solid #bbb; padding:7px 6px; text-align:right; }
        th { background:#e8eef5; }
        .footer { margin-top:28px; font-size:9pt; color:#666; }
        </style></head><body>
        """ + $"<h1>{E(title)}</h1><div class=\"subtitle\">{E(subtitle)}</div>{content}<div class=\"footer\">این گزارش به‌صورت خودکار توسط نرم‌افزار مدیریت تعمیرگاه تهیه شده است.</div></body></html>";

    public string LowStockReport(int threshold)
    {
        using var c = Database.Open(); using var q = c.CreateCommand();
        q.CommandText = "SELECT Code,Name,Category,StockQuantity,PurchasePrice,SalePrice FROM (SELECT p.Code,p.Name,COALESCE(cat.Name,'') Category,p.StockQuantity,p.PurchasePrice,p.SalePrice,p.Id,p.IsActive FROM Products p LEFT JOIN Categories cat ON cat.Id=p.CategoryId) WHERE IsActive=1 AND StockQuantity <= $t ORDER BY StockQuantity,Name;";
        q.Parameters.AddWithValue("$t", threshold);
        using var r = q.ExecuteReader(); var rows = new List<string[]>();
        while (r.Read()) rows.Add(new[] { r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3).ToString("N0"), Money(r.GetInt64(4)), Money(r.GetInt64(5)) });
        var table = Table("قطعات نیازمند تأمین موجودی", new[] { "کد قطعه", "نام قطعه", "دسته", "موجودی فعلی", "قیمت خرید واحد", "قیمت فروش واحد" }, rows);
        string intro = $"<p>با سلام و احترام، آقای محمد</p><p>به اطلاع می‌رساند بر اساس بررسی موجودی انبار در تاریخ {DateTime.Now:yyyy-MM-dd}، قطعات درج‌شده در جدول زیر به حد هشدار ( {threshold} عدد یا کمتر) رسیده‌اند. خواهشمند است دستور بررسی و تأمین موجودی این اقلام را صادر فرمایید.</p><p>با تشکر<br/>گزارش مدیریت تعمیرگاه</p>";
        return Document("گزارش هشدار موجودی انبار", $"تاریخ تهیه گزارش: {DateTime.Now:yyyy-MM-dd} | حد هشدار: {threshold} عدد", intro + table);
    }

    public string WeeklyReport(DateTime from, DateTime to)
    {
        if (to.Date < from.Date) throw new ArgumentException("تاریخ پایان نباید قبل از تاریخ شروع باشد.");
        string start = from.ToString("yyyy-MM-dd"), end = to.ToString("yyyy-MM-dd");
        using var c = Database.Open();
        var content = new StringBuilder();
        using (var q = c.CreateCommand())
        {
            q.CommandText = "SELECT COUNT(*),COALESCE(SUM(PartsTotal),0),COALESCE(SUM(LaborTotal),0),COALESCE(SUM(PartsTotal+LaborTotal),0),COALESCE(SUM((SELECT SUM(Amount) FROM Payments WHERE Payments.RepairId=Repairs.Id)),0),COALESCE(SUM(PartsTotal+LaborTotal-(SELECT COALESCE(SUM(Amount),0) FROM Payments WHERE Payments.RepairId=Repairs.Id)),0),COALESCE(SUM((SELECT SUM(LaborShare) FROM RepairWorkers WHERE RepairWorkers.RepairId=Repairs.Id)),0),COALESCE(SUM(PartsTotal+LaborTotal-(SELECT COALESCE(SUM(LaborShare),0) FROM RepairWorkers WHERE RepairWorkers.RepairId=Repairs.Id)),0) FROM Repairs WHERE RepairDate >= $s AND RepairDate <= $e;";
            q.Parameters.AddWithValue("$s", start); q.Parameters.AddWithValue("$e", end);
            using var r = q.ExecuteReader(); r.Read();
            content.Append("<h2>خلاصه عملکرد تعمیرگاه</h2><p>تعداد تعمیرهای ثبت‌شده: <b>").Append(r.GetInt32(0).ToString("N0"))
                .Append("</b></p><p>مجموع فروش قطعات: <b>").Append(E(Money(r.GetInt64(1))))
                .Append("</b></p><p>مجموع اجرت: <b>").Append(E(Money(r.GetInt64(2))))
                .Append("</b></p><p>جمع کل تعمیرها: <b>").Append(E(Money(r.GetInt64(3))))
                .Append("</b></p><p>مبلغ دریافت‌شده از مشتریان: <b>").Append(E(Money(r.GetInt64(4))))
                .Append("</b></p><p>بدهی باقی‌مانده مشتریان: <b>").Append(E(Money(r.GetInt64(5))))
                .Append("</b></p><p>مجموع سهم کارکنان از اجرت: <b>").Append(E(Money(r.GetInt64(6))))
                .Append("</b></p><p>مجموع سهم صاحب‌کار (قطعات + اجرت باقی‌مانده پس از سهم کارکنان): <b>").Append(E(Money(r.GetInt64(7)))).Append("</b></p>");
        }
        using (var q = c.CreateCommand())
        {
            q.CommandText = "SELECT r.RepairDate,c.FullName,v.Model,COALESCE(v.PlateNumber,''),r.PartsTotal,r.LaborTotal,r.PartsTotal+r.LaborTotal,COALESCE((SELECT SUM(Amount) FROM Payments WHERE RepairId=r.Id),0),r.PartsTotal+r.LaborTotal-COALESCE((SELECT SUM(Amount) FROM Payments WHERE RepairId=r.Id),0),COALESCE((SELECT SUM(LaborShare) FROM RepairWorkers WHERE RepairId=r.Id),0),r.PartsTotal+r.LaborTotal-COALESCE((SELECT SUM(LaborShare) FROM RepairWorkers WHERE RepairId=r.Id),0) FROM Repairs r JOIN Vehicles v ON v.Id=r.VehicleId JOIN Customers c ON c.Id=v.CustomerId WHERE r.RepairDate >= $s AND r.RepairDate <= $e ORDER BY r.RepairDate,r.Id;";
            q.Parameters.AddWithValue("$s", start); q.Parameters.AddWithValue("$e", end);
            using var r = q.ExecuteReader(); var rows = new List<string[]>();
            while (r.Read()) rows.Add(new[] { r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), Money(r.GetInt64(4)), Money(r.GetInt64(5)), Money(r.GetInt64(6)), Money(r.GetInt64(7)), Money(r.GetInt64(8)), Money(r.GetInt64(9)), Money(r.GetInt64(10)) });
            content.Append(Table("فهرست تعمیرهای ثبت‌شده و سهم‌ها", new[] { "تاریخ", "مشتری", "خودرو", "پلاک", "قطعات", "اجرت", "جمع کل", "دریافتی", "بدهی مانده", "سهم کارکنان", "سهم صاحب‌کار" }, rows));
        }
        using (var q = c.CreateCommand())
        {
            q.CommandText = "SELECT ri.ProductNameAtTime,SUM(ri.Quantity),SUM(ri.Quantity*ri.SalePriceAtTime) FROM RepairItems ri JOIN Repairs r ON r.Id=ri.RepairId WHERE r.RepairDate >= $s AND r.RepairDate <= $e GROUP BY ri.ProductNameAtTime ORDER BY ri.ProductNameAtTime;";
            q.Parameters.AddWithValue("$s", start); q.Parameters.AddWithValue("$e", end);
            using var r = q.ExecuteReader(); var rows = new List<string[]>();
            while (r.Read()) rows.Add(new[] { r.GetString(0), r.GetInt64(1).ToString("N0"), Money(r.GetInt64(2)) });
            content.Append(Table("قطعات مصرف‌شده در بازه گزارش", new[] { "نام قطعه", "مجموع تعداد مصرف‌شده", "ارزش فروش قطعات" }, rows));
        }
        using (var q = c.CreateCommand())
        {
            q.CommandText = "SELECT EmployeeNameAtTime,SUM(LaborShare),COUNT(DISTINCT RepairId) FROM RepairWorkers rw JOIN Repairs r ON r.Id=rw.RepairId WHERE r.RepairDate >= $s AND r.RepairDate <= $e GROUP BY EmployeeNameAtTime ORDER BY EmployeeNameAtTime;";
            q.Parameters.AddWithValue("$s", start); q.Parameters.AddWithValue("$e", end);
            using var r = q.ExecuteReader(); var rows = new List<string[]>();
            while (r.Read()) rows.Add(new[] { r.GetString(0), r.GetInt32(2).ToString("N0"), Money(r.GetInt64(1)) });
            content.Append(Table("عملکرد و سهم اجرت کارکنان", new[] { "نام تعمیرکار", "تعداد تعمیرهای مشارکت‌شده", "سهم اجرت کسب‌شده" }, rows));
        }
        using (var q = c.CreateCommand())
        {
            q.CommandText = "SELECT e.FullName,t.TransactionType,t.Amount,t.TransactionDate,COALESCE(t.Description,'') FROM EmployeeTransactions t JOIN Employees e ON e.Id=t.EmployeeId WHERE t.TransactionDate >= $s AND t.TransactionDate <= $e ORDER BY e.FullName,t.TransactionDate,t.Id;";
            q.Parameters.AddWithValue("$s", start); q.Parameters.AddWithValue("$e", end);
            using var r = q.ExecuteReader(); var rows = new List<string[]>();
            while (r.Read()) rows.Add(new[] { r.GetString(0), TransactionName(r.GetString(1)), Money(r.GetInt64(2)), r.GetString(3), r.GetString(4) });
            content.Append(Table("تراکنش‌های حساب کارکنان در این بازه", new[] { "نام", "نوع تراکنش", "مبلغ", "تاریخ", "شرح" }, rows));
        }
        content.Append("<p>با سلام و احترام،<br/>گزارش هفتگی عملکرد تعمیرگاه جهت بررسی و اطلاع تقدیم می‌گردد.</p><p>با احترام<br/>مدیریت تعمیرگاه</p>");
        return Document("گزارش هفتگی تعمیرگاه", $"بازه گزارش: {start} تا {end} | تاریخ تهیه: {DateTime.Now:yyyy-MM-dd}", content.ToString());
    }

    static string TransactionName(string type) => type switch { "LaborEarned" => "سهم اجرت", "Payment" => "پرداخت دستمزد", "Advance" => "مساعده / قرض", "Debt" => "بدهکاری", "Credit" => "بستانکاری", _ => type };
}
