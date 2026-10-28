using GarageApp.Services;

namespace GarageApp.Forms;

public sealed class ChangePasswordForm : Form
{
    private readonly TextBox current = PasswordBox();
    private readonly TextBox next = PasswordBox();
    private readonly TextBox confirm = PasswordBox();

    public ChangePasswordForm()
    {
        Text = "تغییر رمز ورود"; Width = 430; Height = 250;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent; RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(layout, 0, "رمز فعلی:", current); AddRow(layout, 1, "رمز جدید:", next); AddRow(layout, 2, "تکرار رمز جدید:", confirm);
        var save = new Button { Text = "ذخیره رمز جدید", AutoSize = true };
        save.Click += (_, _) => SavePassword();
        layout.Controls.Add(save, 1, 3); Controls.Add(layout); AcceptButton = save;
    }

    private static TextBox PasswordBox() => new() { Width = 230, UseSystemPasswordChar = true, MaxLength = 100 };
    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private void SavePassword()
    {
        if (next.Text != confirm.Text)
        {
            MessageBox.Show("رمز جدید و تکرار آن یکسان نیستند.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            confirm.Clear(); confirm.Focus(); return;
        }
        try
        {
            PasswordService.Change(current.Text, next.Text);
            MessageBox.Show("رمز ورود با موفقیت تغییر کرد.", "موفق", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK; Close();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
