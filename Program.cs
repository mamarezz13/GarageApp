using GarageApp.Data;
using GarageApp.Forms;
using GarageApp.Services;
namespace GarageApp;
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Database.Initialize();
        PasswordService.EnsureInitialized();
        using var login = new LoginForm();
        if (login.ShowDialog() != DialogResult.OK)
            return;
        Application.Run(new MainForm());
    }
}
