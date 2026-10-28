namespace GarageApp.Models;
public sealed record Product(int Id, string Code, string Name, long PurchasePrice, long SalePrice, int StockQuantity, string Category, string ImagePath = "", double ProfitPercent = 30);
public sealed record Employee(int Id, string FullName, double DefaultLaborPercent);
public sealed record Vehicle(int Id, string CustomerName, string Model, string PlateNumber)
{
    public override string ToString() => $"{CustomerName} — {Model} — {PlateNumber}";
}
public sealed record RepairSummary(int Id, string RepairDate, string Customer, string Vehicle, string Plate,
    long PartsTotal, long LaborTotal, long Total, long Paid, long Balance, string Status,
    long EmployeeShareTotal, long OwnerShare, string Mechanics, string PartsUsed);
