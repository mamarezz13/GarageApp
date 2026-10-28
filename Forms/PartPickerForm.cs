using GarageApp.Models;
using GarageApp.Services;
using System.Drawing;
using System.IO;

namespace GarageApp.Forms;

public sealed class PartPickerForm : Form
{
    private readonly ProductService products;
    private readonly TextBox search = new() { Dock = DockStyle.Fill, PlaceholderText = "نام قطعه یا کد قطعه را تایپ کنید…", Font = new Font("Segoe UI", 12), Margin = new Padding(6) };
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false, RowHeadersVisible = false, BackgroundColor = Color.White,
        Font = new Font("Segoe UI", 11), RowTemplate = { Height = 34 },
        ColumnHeadersHeight = 42, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
    };
    private readonly PictureBox picture = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
    private readonly Label imageCaption = new() { Dock = DockStyle.Bottom, Height = 42, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10, FontStyle.Bold), Text = "برای مشاهده عکس، قطعه را انتخاب کنید" };
    private readonly Label resultCount = new() { AutoSize = true, Padding = new Padding(8, 10, 8, 0), Font = new Font("Segoe UI", 10) };
    private readonly NumericUpDown quantity = new() { Minimum = 1, Maximum = 10000, Value = 1, Width = 90, Font = new Font("Segoe UI", 11) };

    public Product? SelectedProduct { get; private set; }
    public int SelectedQuantity { get; private set; } = 1;

    public PartPickerForm(ProductService products)
    {
        this.products = products;
        Text = "جست‌وجو و انتخاب قطعه برای ثبت تعمیر";
        Width = 1180; Height = 760; MinimumSize = new Size(980, 620);
        StartPosition = FormStartPosition.CenterParent;
        RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; Font = new Font("Segoe UI", 10);

        var searchBar = new TableLayoutPanel { Dock = DockStyle.Top, Height = 58, ColumnCount = 2, Padding = new Padding(8), BackColor = SystemColors.Control };
        searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var searchLabel = new Label { Text = "جست‌وجوی قطعه:", AutoSize = true, Anchor = AnchorStyles.Right, Padding = new Padding(4, 10, 4, 0), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        searchBar.Controls.Add(search, 0, 0); searchBar.Controls.Add(searchLabel, 1, 0);

        var right = new Panel { Dock = DockStyle.Right, Width = 285, Padding = new Padding(10) };
        right.Controls.Add(picture); right.Controls.Add(imageCaption);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 66, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10), WrapContents = false };
        var add = new Button { Text = "افزودن قطعه انتخاب‌شده", AutoSize = true, Height = 38, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        var cancel = new Button { Text = "انصراف", AutoSize = true, Height = 38, DialogResult = DialogResult.Cancel };
        add.Click += (_, _) => ConfirmSelection();
        bottom.Controls.Add(add); bottom.Controls.Add(new Label { Text = "تعداد:", AutoSize = true, Padding = new Padding(4, 10, 4, 0) }); bottom.Controls.Add(quantity); bottom.Controls.Add(cancel); bottom.Controls.Add(resultCount);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        content.Controls.Add(grid);
        Controls.Add(content); Controls.Add(right); Controls.Add(bottom); Controls.Add(searchBar);
        CancelButton = cancel; AcceptButton = add;

        search.TextChanged += (_, _) => LoadParts();
        grid.SelectionChanged += (_, _) => ShowSelectedImage();
        grid.CellDoubleClick += (_, _) => ConfirmSelection();
        LoadParts();
        search.Focus();
    }

    private void LoadParts()
    {
        var term = search.Text.Trim();
        grid.DataSource = products.Search(term);
        var headers = new Dictionary<string, string>
        {
            ["Id"] = "شناسه", ["Code"] = "کد قطعه", ["Name"] = "نام قطعه",
            ["PurchasePrice"] = "قیمت خرید", ["SalePrice"] = "قیمت فروش",
            ["StockQuantity"] = "موجودی", ["Category"] = "دسته‌بندی"
        };
        foreach (var pair in headers)
            if (grid.Columns.Contains(pair.Key)) grid.Columns[pair.Key]!.HeaderText = pair.Value;
        if (grid.Columns.Contains("Id")) grid.Columns["Id"]!.Visible = false;
        if (grid.Columns.Contains("ImagePath")) grid.Columns["ImagePath"]!.Visible = false;
        if (grid.Columns.Contains("ProfitPercent")) grid.Columns["ProfitPercent"]!.Visible = false;
        if (grid.Columns.Contains("Code")) grid.Columns["Code"]!.FillWeight = 70;
        if (grid.Columns.Contains("Name")) grid.Columns["Name"]!.FillWeight = 150;
        if (grid.Columns.Contains("StockQuantity")) grid.Columns["StockQuantity"]!.FillWeight = 55;
        if (grid.Columns.Contains("Category")) grid.Columns["Category"]!.FillWeight = 80;
        resultCount.Text = $"تعداد نتایج: {grid.Rows.Count}";
        if (grid.Rows.Count == 0) imageCaption.Text = "قطعه‌ای با این مشخصات پیدا نشد";
        else ShowSelectedImage();
    }

    private void ShowSelectedImage()
    {
        picture.Image?.Dispose(); picture.Image = null;
        if (grid.CurrentRow?.DataBoundItem is not Product p) return;
        imageCaption.Text = $"{p.Name}\nموجودی: {p.StockQuantity} عدد";
        if (string.IsNullOrWhiteSpace(p.ImagePath) || !File.Exists(p.ImagePath)) return;
        try { using var stream = File.OpenRead(p.ImagePath); using var image = Image.FromStream(stream); picture.Image = new Bitmap(image); }
        catch { picture.Image = null; }
    }

    private void ConfirmSelection()
    {
        if (grid.CurrentRow?.DataBoundItem is not Product p) { MessageBox.Show("یک قطعه از فهرست انتخاب کن."); return; }
        if (p.StockQuantity <= 0) { MessageBox.Show("موجودی این قطعه تمام شده است.", "موجودی ناکافی", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (quantity.Value > p.StockQuantity) { MessageBox.Show($"موجودی «{p.Name}» فقط {p.StockQuantity} عدد است.", "موجودی ناکافی", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        SelectedProduct = p; SelectedQuantity = (int)quantity.Value; DialogResult = DialogResult.OK; Close();
    }
}
