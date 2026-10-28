using GarageApp.Models;
using GarageApp.Services;
using System.IO;

namespace GarageApp.Forms;

public sealed class ProductEditorForm : Form
{
    readonly ProductService service;
    readonly Product? current;
    readonly TextBox code = new() { Width = 220 };
    readonly TextBox name = new() { Width = 220 };
    readonly TextBox category = new() { Width = 220 };
    readonly NumericUpDown purchase = Money();
    readonly NumericUpDown sale = Money();
    readonly NumericUpDown profitPercent = new() { Width = 220, Minimum = 0, Maximum = 1000, DecimalPlaces = 2, Increment = 1, ThousandsSeparator = false };
    readonly NumericUpDown stock = new() { Width = 220, Minimum = 0, Maximum = 1000000 };
    readonly PictureBox picture = new() { Width = 180, Height = 140, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
    string imagePath = "";

    public ProductEditorForm(ProductService service, Product? product = null)
    {
        this.service = service; current = product;
        Text = product is null ? "افزودن قطعه" : "ویرایش قطعه";
        Width = 470; Height = 620; StartPosition = FormStartPosition.CenterParent;
        RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; Font = new Font("Segoe UI", 9);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, RowCount = 9, AutoSize = false };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(root, 0, "کد قطعه", code); AddRow(root, 1, "نام قطعه", name); AddRow(root, 2, "دسته‌بندی", category);
        AddRow(root, 3, "قیمت خرید", purchase); AddRow(root, 4, "درصد سود (%)", profitPercent); AddRow(root, 5, "قیمت فروش", sale); AddRow(root, 6, "موجودی", stock);
        sale.ReadOnly = true;
        profitPercent.Value = (decimal)Math.Clamp(service.GetProfitPercent(), 0, 1000);
        purchase.ValueChanged += (_, _) => UpdateSalePrice();
        profitPercent.ValueChanged += (_, _) => UpdateSalePrice();
        var photoPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        photoPanel.Controls.Add(picture); var choose = new Button { Text = "انتخاب عکس", AutoSize = true }; choose.Click += (_, _) => ChooseImage(); photoPanel.Controls.Add(choose);
        root.Controls.Add(new Label { Text = "عکس قطعه", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, 0, 7); root.Controls.Add(photoPanel, 1, 7);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = new Button { Text = "ذخیره قطعه", AutoSize = true }; save.Click += (_, _) => SaveProduct();
        var cancel = new Button { Text = "انصراف", AutoSize = true }; cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        buttons.Controls.Add(save); buttons.Controls.Add(cancel); root.Controls.Add(buttons, 0, 8); root.SetColumnSpan(buttons, 2);
        Controls.Add(root);
        if (product is not null) { code.Text = product.Code; name.Text = product.Name; category.Text = product.Category; purchase.Value = Math.Min(purchase.Maximum, product.PurchasePrice); profitPercent.Value = Math.Clamp((decimal)product.ProfitPercent, profitPercent.Minimum, profitPercent.Maximum); sale.Value = Math.Min(sale.Maximum, product.SalePrice); stock.Value = product.StockQuantity; imagePath = product.ImagePath; ShowImage(imagePath); }
        UpdateSalePrice();
    }
    void UpdateSalePrice()
    {
        decimal calculated = purchase.Value * (1 + profitPercent.Value / 100m);
        sale.Value = Math.Min(sale.Maximum, Math.Max(sale.Minimum, Math.Round(calculated, 0, MidpointRounding.AwayFromZero)));
    }
    static NumericUpDown Money() => new() { Width = 220, Maximum = 9_000_000_000_000, Minimum = 0, ThousandsSeparator = true };
    static void AddRow(TableLayoutPanel root, int row, string label, Control control) { root.Controls.Add(new Label { Text = label, AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, row); root.Controls.Add(control, 1, row); }
    void ChooseImage()
    {
        using var dialog = new OpenFileDialog { Title = "انتخاب عکس قطعه", Filter = "تصاویر|*.jpg;*.jpeg;*.png;*.bmp;*.webp" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        imagePath = dialog.FileName; ShowImage(imagePath);
    }
    void ShowImage(string path) { picture.Image?.Dispose(); picture.Image = null; try { if (File.Exists(path)) { using var fs = File.OpenRead(path); using var img = Image.FromStream(fs); picture.Image = new Bitmap(img); } } catch { picture.Image = null; } }
    void SaveProduct()
    {
        if (string.IsNullOrWhiteSpace(code.Text) || string.IsNullOrWhiteSpace(name.Text)) { MessageBox.Show("کد و نام قطعه الزامی است."); return; }
        string stored = imagePath;
        if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath) && !imagePath.StartsWith(Path.Combine(AppContext.BaseDirectory, "ProductImages"), StringComparison.OrdinalIgnoreCase))
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "ProductImages"); Directory.CreateDirectory(folder);
            string dest = Path.Combine(folder, $"product_{Guid.NewGuid():N}{Path.GetExtension(imagePath)}"); File.Copy(imagePath, dest, true); stored = dest;
        }
        try
        {
            service.Save(current?.Id, code.Text, name.Text, (long)purchase.Value, (long)sale.Value, (int)stock.Value, category.Text, stored, (double)profitPercent.Value);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex)
        {
            // SQLite extended error codes can vary; identify UNIQUE errors by code/message.
            if (ex.SqliteErrorCode == 19 || ex.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this,
                    "کد قطعه تکراری است یا قبلاً در انبار ثبت شده. لطفاً کد دیگری وارد کنید. اگر این قطعه قبلاً حذف شده، کد قبلی همچنان در دیتابیس رزرو است.",
                    "کد قطعه تکراری", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show(this, "خطای دیتابیس هنگام ذخیره قطعه: " + ex.Message, "خطای دیتابیس", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "اطلاعات نامعتبر", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "ذخیره قطعه انجام نشد: " + ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
