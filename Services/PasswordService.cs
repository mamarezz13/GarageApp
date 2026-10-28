using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using GarageApp.Data;

namespace GarageApp.Services;

public static class PasswordService
{
    private const string SettingKey = "LoginPasswordHash";
    private const string InitialPassword = "1234";

    public static void EnsureInitialized()
    {
        using var c = Database.Open();
        using var q = c.CreateCommand();
        q.CommandText = "INSERT OR IGNORE INTO AppSettings([Key],[Value]) VALUES($key,$value);";
        q.Parameters.AddWithValue("$key", SettingKey);
        q.Parameters.AddWithValue("$value", Hash(InitialPassword));
        q.ExecuteNonQuery();
    }

    public static bool Verify(string password)
    {
        using var c = Database.Open();
        using var q = c.CreateCommand();
        q.CommandText = "SELECT [Value] FROM AppSettings WHERE [Key]=$key;";
        q.Parameters.AddWithValue("$key", SettingKey);
        var stored = q.ExecuteScalar() as string;
        return stored != null && CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(stored), Convert.FromHexString(Hash(password)));
    }

    public static void Change(string currentPassword, string newPassword)
    {
        if (!Verify(currentPassword)) throw new InvalidOperationException("رمز فعلی صحیح نیست.");
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            throw new InvalidOperationException("رمز جدید باید حداقل ۴ کاراکتر داشته باشد.");
        using var c = Database.Open();
        using var q = c.CreateCommand();
        q.CommandText = "INSERT INTO AppSettings([Key],[Value]) VALUES($key,$value) ON CONFLICT([Key]) DO UPDATE SET [Value]=excluded.[Value];";
        q.Parameters.AddWithValue("$key", SettingKey);
        q.Parameters.AddWithValue("$value", Hash(newPassword));
        q.ExecuteNonQuery();
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
