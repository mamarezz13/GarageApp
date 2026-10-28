using Microsoft.Data.Sqlite;
namespace GarageApp.Data;
public static class Database
{
    private static readonly string PathToDb = Path.Combine(AppContext.BaseDirectory, "garage.db");
    public static SqliteConnection Open()
    {
        var c = new SqliteConnection($"Data Source={PathToDb}");
        c.Open();
        using var p = c.CreateCommand();
        p.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        p.ExecuteNonQuery();
        return c;
    }
    public static void Initialize()
    {
        using var c = Open();
        using var q = c.CreateCommand();
        q.CommandText = """
        CREATE TABLE IF NOT EXISTS Categories(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE);
        CREATE TABLE IF NOT EXISTS Products(
          Id INTEGER PRIMARY KEY AUTOINCREMENT, Code TEXT NOT NULL UNIQUE, Name TEXT NOT NULL,
          CategoryId INTEGER NULL, PurchasePrice INTEGER NOT NULL DEFAULT 0,
          SalePrice INTEGER NOT NULL DEFAULT 0, StockQuantity INTEGER NOT NULL DEFAULT 0, ImagePath TEXT,
          ProfitPercent REAL NOT NULL DEFAULT 30,
          IsActive INTEGER NOT NULL DEFAULT 1,
          FOREIGN KEY(CategoryId) REFERENCES Categories(Id));
        CREATE TABLE IF NOT EXISTS Customers(Id INTEGER PRIMARY KEY AUTOINCREMENT, FullName TEXT NOT NULL, Phone TEXT);
        CREATE TABLE IF NOT EXISTS Vehicles(
          Id INTEGER PRIMARY KEY AUTOINCREMENT, CustomerId INTEGER NOT NULL, Model TEXT NOT NULL, PlateNumber TEXT,
          FOREIGN KEY(CustomerId) REFERENCES Customers(Id));
        CREATE TABLE IF NOT EXISTS Employees(
          Id INTEGER PRIMARY KEY AUTOINCREMENT, FullName TEXT NOT NULL,
          DefaultLaborPercent REAL NOT NULL DEFAULT 100, IsActive INTEGER NOT NULL DEFAULT 1);
        CREATE TABLE IF NOT EXISTS Repairs(
          Id INTEGER PRIMARY KEY AUTOINCREMENT, VehicleId INTEGER NOT NULL, RepairDate TEXT NOT NULL,
          Description TEXT, LaborTotal INTEGER NOT NULL DEFAULT 0, PartsTotal INTEGER NOT NULL DEFAULT 0,
          Status TEXT NOT NULL DEFAULT 'Open', DiscountAmount INTEGER NOT NULL DEFAULT 0, FOREIGN KEY(VehicleId) REFERENCES Vehicles(Id));
        CREATE TABLE IF NOT EXISTS RepairItems(
          Id INTEGER PRIMARY KEY AUTOINCREMENT, RepairId INTEGER NOT NULL, ProductId INTEGER NOT NULL,
          ProductNameAtTime TEXT NOT NULL, Quantity INTEGER NOT NULL,
          PurchasePriceAtTime INTEGER NOT NULL, SalePriceAtTime INTEGER NOT NULL,
          FOREIGN KEY(RepairId) REFERENCES Repairs(Id), FOREIGN KEY(ProductId) REFERENCES Products(Id));
        CREATE TABLE IF NOT EXISTS RepairWorkers(
          Id INTEGER PRIMARY KEY AUTOINCREMENT, RepairId INTEGER NOT NULL, EmployeeId INTEGER NOT NULL,
          EmployeeNameAtTime TEXT NOT NULL, LaborPercent REAL NOT NULL, LaborShare INTEGER NOT NULL,
          FOREIGN KEY(RepairId) REFERENCES Repairs(Id), FOREIGN KEY(EmployeeId) REFERENCES Employees(Id),
          UNIQUE(RepairId,EmployeeId));
        CREATE TABLE IF NOT EXISTS Payments(
          Id INTEGER PRIMARY KEY AUTOINCREMENT, RepairId INTEGER NOT NULL, Amount INTEGER NOT NULL,
          PaymentDate TEXT NOT NULL, Note TEXT, FOREIGN KEY(RepairId) REFERENCES Repairs(Id));
        CREATE TABLE IF NOT EXISTS EmployeeTransactions(
          Id INTEGER PRIMARY KEY AUTOINCREMENT, EmployeeId INTEGER NOT NULL, RepairId INTEGER NULL,
          TransactionType TEXT NOT NULL, Amount INTEGER NOT NULL, TransactionDate TEXT NOT NULL,
          Description TEXT, FOREIGN KEY(EmployeeId) REFERENCES Employees(Id),
          FOREIGN KEY(RepairId) REFERENCES Repairs(Id));
        CREATE TABLE IF NOT EXISTS StockMovements(
          Id INTEGER PRIMARY KEY AUTOINCREMENT, ProductId INTEGER NOT NULL, RepairId INTEGER NULL,
          QuantityChange INTEGER NOT NULL, MovementDate TEXT NOT NULL, Description TEXT,
          FOREIGN KEY(ProductId) REFERENCES Products(Id), FOREIGN KEY(RepairId) REFERENCES Repairs(Id));
        CREATE TABLE IF NOT EXISTS AppSettings([Key] TEXT PRIMARY KEY, [Value] TEXT NOT NULL);
        CREATE TABLE IF NOT EXISTS EmployeeLostTools(
          Id INTEGER PRIMARY KEY AUTOINCREMENT,
          EmployeeId INTEGER NOT NULL,
          ToolName TEXT NOT NULL,
          Quantity INTEGER NOT NULL DEFAULT 1,
          EstimatedValue INTEGER NOT NULL DEFAULT 0,
          LossDate TEXT NOT NULL,
          Description TEXT,
          FOREIGN KEY(EmployeeId) REFERENCES Employees(Id));
        INSERT OR IGNORE INTO AppSettings([Key],[Value]) VALUES('ProfitPercent','30');
        """;
        q.ExecuteNonQuery();
        // Upgrade databases created by earlier versions.
        try
        {
            using var migration = c.CreateCommand();
            migration.CommandText = "ALTER TABLE Products ADD COLUMN ProfitPercent REAL NOT NULL DEFAULT 30;";
            migration.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            // Column already exists; safe to continue.
        }
        try
        {
            using var migration = c.CreateCommand();
            migration.CommandText = "ALTER TABLE Repairs ADD COLUMN DiscountAmount INTEGER NOT NULL DEFAULT 0;";
            migration.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            // Column already exists; safe to continue.
        }
        try
        {
            using var migration = c.CreateCommand();
            migration.CommandText = "ALTER TABLE Products ADD COLUMN ImagePath TEXT;";
            migration.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            // Column already exists; safe to continue.
        }
    }
}
