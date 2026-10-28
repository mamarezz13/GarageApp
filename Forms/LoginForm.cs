using GarageApp.Services;
namespace GarageApp.Forms;

/// <summary>صفحه ورود برنامه؛ رمز اولیه در این نسخه 1234 است.</summary>
public sealed class LoginForm : Form
{
    private readonly TextBox passwordBox = new()
    {
        Width = 230,
        UseSystemPasswordChar = true,
        MaxLength = 100
    };
    private int attempts;

    public LoginForm()
    {
        Text = "ورود به برنامه مدیریت تعمیرگاه";
        Width = 390;
        Height = 190;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 2,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "برای ورود، رمز عبور را وارد کنید",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 12)
        };
        layout.Controls.Add(title, 0, 0);
        layout.SetColumnSpan(title, 2);

        layout.Controls.Add(new Label { Text = "رمز عبور:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 1);
        layout.Controls.Add(passwordBox, 1, 1);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 10, 0, 0)
        };
        var loginButton = new Button { Text = "ورود", AutoSize = true };
        loginButton.Click += (_, _) => TryLogin();
        var exitButton = new Button { Text = "خروج", AutoSize = true, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(loginButton);
        buttons.Controls.Add(exitButton);
        layout.Controls.Add(buttons, 0, 2);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
        AcceptButton = loginButton;
        CancelButton = exitButton;
        Shown += (_, _) => passwordBox.Focus();
    }

    private void TryLogin()
    {
        if (PasswordService.Verify(passwordBox.Text))
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        attempts++;
        passwordBox.Clear();
        passwordBox.Focus();
        if (attempts >= 3)
        {
            MessageBox.Show("رمز را سه بار اشتباه وارد کردید. برنامه بسته می‌شود.", "عدم دسترسی", MessageBoxButtons.OK, MessageBoxIcon.Error);
            DialogResult = DialogResult.Abort;
            Close();
            return;
        }

        MessageBox.Show($"رمز عبور اشتباه است. {3 - attempts} فرصت باقی مانده.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
