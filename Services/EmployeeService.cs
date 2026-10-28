using GarageApp.Data;
using GarageApp.Models;
namespace GarageApp.Services;
public sealed class EmployeeService
{
    public List<Employee> GetAll()
    {
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="SELECT Id,FullName,DefaultLaborPercent FROM Employees WHERE IsActive=1 ORDER BY FullName;";
        using var r=q.ExecuteReader(); var list=new List<Employee>();
        while(r.Read()) list.Add(new(r.GetInt32(0),r.GetString(1),r.GetDouble(2)));
        return list;
    }
    public void Save(int? id,string name,double percent)
    {
        if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("نام کارمند الزامی است.");
        if(percent<0||percent>100) throw new ArgumentException("درصد باید بین صفر تا ۱۰۰ باشد.");
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText=id is null
          ? "INSERT INTO Employees(FullName,DefaultLaborPercent) VALUES($n,$p);"
          : "UPDATE Employees SET FullName=$n,DefaultLaborPercent=$p WHERE Id=$id;";
        if(id is not null) q.Parameters.AddWithValue("$id",id.Value);
        q.Parameters.AddWithValue("$n",name.Trim()); q.Parameters.AddWithValue("$p",percent); q.ExecuteNonQuery();
    }
    public void Delete(int id)
    {
        using var c=Database.Open(); using var q=c.CreateCommand();
        q.CommandText="UPDATE Employees SET IsActive=0 WHERE Id=$id;"; q.Parameters.AddWithValue("$id",id); q.ExecuteNonQuery();
    }
}
