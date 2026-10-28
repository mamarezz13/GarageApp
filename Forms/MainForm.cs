using GarageApp.Models;
using GarageApp.Services;
using System.Data;
using System.IO;
namespace GarageApp.Forms;
public sealed class MainForm : Form
{
    readonly ProductService products=new(); readonly EmployeeService employees=new();
    readonly RepairService repairs=new(); readonly EmployeeLedgerService ledger=new(); readonly ExportReportService exports=new();
    readonly DataGridView productGrid=Grid(), employeeGrid=Grid(), repairGrid=Grid(), ledgerGrid=Grid(), repairItemsGrid=Grid(), workersGrid=Grid(), inventoryGrid=Grid(), customerGrid=Grid();
    readonly Label inventoryProductsValue=new(){AutoSize=true,Font=new Font("Segoe UI",11,FontStyle.Bold)};
    readonly Label inventoryUnitsValue=new(){AutoSize=true,Font=new Font("Segoe UI",11,FontStyle.Bold)};
    readonly Label inventoryPurchaseValue=new(){AutoSize=true,Font=new Font("Segoe UI",11,FontStyle.Bold)};
    readonly Label inventorySaleValue=new(){AutoSize=true,Font=new Font("Segoe UI",11,FontStyle.Bold)};
    readonly Label inventoryProfitValue=new(){AutoSize=true,Font=new Font("Segoe UI",11,FontStyle.Bold)};
    readonly Label monthlyShareSummary=new(){AutoSize=true,Font=new Font("Segoe UI",10,FontStyle.Bold),Padding=new Padding(4)};
    readonly Label repairPartsTotalValue=new(){AutoSize=true,Font=new Font("Segoe UI",10,FontStyle.Bold),ForeColor=Color.DarkSlateBlue,Padding=new Padding(4)};
    readonly Label repairLaborTotalValue=new(){AutoSize=true,Font=new Font("Segoe UI",10,FontStyle.Bold),ForeColor=Color.DarkSlateBlue,Padding=new Padding(4)};
    readonly Label repairGrandTotalValue=new(){AutoSize=true,Font=new Font("Segoe UI",11,FontStyle.Bold),ForeColor=Color.DarkGreen,Padding=new Padding(4)};
    readonly TextBox search=new(){Width=180,PlaceholderText="جست‌وجوی نام یا کد"}, customerSearch=new(){Width=220,PlaceholderText="جست‌وجوی نام یا شماره تماس مشتری"}, code=new(){Width=90}, pname=new(){Width=140}, category=new(){Width=100};
    readonly PictureBox productPicture=new(){Width=90,Height=70,SizeMode=PictureBoxSizeMode.Zoom,BorderStyle=BorderStyle.FixedSingle};
    string selectedImagePath="";
    readonly NumericUpDown lowStockThreshold = new(){Width=65,Minimum=0,Maximum=100000};
    bool lowStockChecked;
    readonly NumericUpDown purchase=Money(), sale=Money(), stock=new(){Width=80,Maximum=1000000}, profit=new(){Width=70,Minimum=0,Maximum=1000,Value=30};
    readonly TextBox empName=new(){Width=140}; readonly NumericUpDown empPercent=new(){Width=80,Minimum=0,Maximum=100,Value=100,DecimalPlaces=2};
    readonly TextBox customer=new(){Width=130}, phone=new(){Width=110}, model=new(){Width=100,Text="پراید"}, plate=new(){Width=100}, description=new(){Width=160};
    readonly ComboBox vehicleCombo=Combo(), productCombo=Combo(), workerCombo=Combo(), ledgerEmployee=Combo(), ledgerType=Combo();
    readonly NumericUpDown labor=Money(), discountAmountInput=Money(), qty=new(){Width=65,Minimum=1,Maximum=10000,Value=1}, workerPercent=new(){Width=75,Minimum=0,Maximum=100,Value=100,DecimalPlaces=2}, ledgerAmount=Money(), paidNow=Money();
    readonly CheckBox fullPayment=new(){Text="تسویه کامل هنگام ثبت",Checked=true,AutoSize=true};
    readonly TextBox ledgerDescription=new(){Width=150}, lostToolName=new(){Width=140}, lostToolDescription=new(){Width=170}, month=new(){Width=90,Text=DateTime.Now.ToString("yyyy-MM")};
    readonly NumericUpDown lostToolQty=new(){Width=65,Minimum=1,Maximum=10000,Value=1}, lostToolValue=Money();
    readonly DateTimePicker lostToolDate=new(){Width=120,Format=DateTimePickerFormat.Short,Value=DateTime.Today};
    readonly DateTimePicker weekFrom=new(){Width=125,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddDays(-6)};
    readonly DateTimePicker weekTo=new(){Width=125,Format=DateTimePickerFormat.Short,Value=DateTime.Today};
    readonly DateTimePicker reportDay=new(){Width=125,Format=DateTimePickerFormat.Short,Value=DateTime.Today};
    readonly ComboBox reportRange=new(){Width=105,DropDownStyle=ComboBoxStyle.DropDownList};
    int? productId, employeeId;
    readonly List<(int id,string name,int qty,long purchase,long sale)> items=new();
    readonly List<(int id,string name,double percent)> workers=new();

    public MainForm()
    {
        Text="مدیریت تعمیرگاه خودرو"; Width=1280; Height=820; StartPosition=FormStartPosition.CenterScreen; KeyPreview=true;
        RightToLeft=RightToLeft.Yes; RightToLeftLayout=true; Font=new Font("Segoe UI",9);
        var tabs=new TabControl{Dock=DockStyle.Fill};
        tabs.TabPages.Add(ProductsTab()); tabs.TabPages.Add(InventoryTab()); tabs.TabPages.Add(EmployeesTab()); tabs.TabPages.Add(RepairTab()); tabs.TabPages.Add(LedgerTab()); tabs.TabPages.Add(CustomersTab()); tabs.TabPages.Add(ReportsTab());
        Controls.Add(tabs);
        KeyDown += (_, e) => HandleGlobalShortcut(e, tabs);
        RefreshAll(); CheckLowStock(false);
    }
    static NumericUpDown Money()=>new(){Width=110,Maximum=9_000_000_000_000,Minimum=0,ThousandsSeparator=true};
    static ComboBox Combo()=>new(){Width=180,DropDownStyle=ComboBoxStyle.DropDownList};
    static DataGridView Grid()=>new(){Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,RowHeadersVisible=false};
    static FlowLayoutPanel Flow()=>new(){Dock=DockStyle.Fill,AutoSize=true,WrapContents=true,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(4)};
    static void Label(Control c,string s)=>c.Controls.Add(new Label{Text=s,AutoSize=true,Padding=new Padding(0,7,0,0)});
    static Button Button(string s,Action a){var b=new Button{Text=s,AutoSize=true}; b.Click+=(_,_)=>Safe(a); return b;}
    static void Safe(Action a){try{a();}catch(Exception ex){MessageBox.Show(ex.Message,"خطا",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
    TabPage ProductsTab()
    {
        var p = new TabPage("قطعات و انبار");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(6) };
        root.RowStyles.Add(new(SizeType.AutoSize)); root.RowStyles.Add(new(SizeType.Percent, 100));
        var bar = Flow(); Label(bar, "جست‌وجو"); bar.Controls.Add(search); bar.Controls.Add(Button("جست‌وجو", LoadProducts));
        bar.Controls.Add(Button("🔐 تغییر رمز ورود", ChangePassword));
        bar.Controls.Add(Button("➕ افزودن قطعه (Ctrl+N)", AddProduct)); bar.Controls.Add(Button("✏️ ویرایش قطعه (Ctrl+E)", EditProduct));
        bar.Controls.Add(Button("📋 کپی فهرست قطعات بدون قیمت خرید", CopyProductsWithoutPurchasePrice));
        bar.Controls.Add(Button("⌨️ میانبرهای صفحه‌کلید", ShowShortcuts));
        bar.Controls.Add(Button("غیرفعال‌کردن قطعه", DeleteProduct));
        Label(bar, "هشدار موجودی در حد یا کمتر از"); lowStockThreshold.Value = products.GetLowStockThreshold(); bar.Controls.Add(lowStockThreshold); Label(bar, "عدد");
        bar.Controls.Add(Button("ذخیره حد هشدار", () => { products.SetLowStockThreshold((int)lowStockThreshold.Value); MessageBox.Show("حد هشدار موجودی ذخیره شد."); CheckLowStock(true); }));
        Label(bar, "سود ٪"); bar.Controls.Add(profit); bar.Controls.Add(Button("ذخیره درصد سود", () => { products.SetProfitPercent((double)profit.Value); MessageBox.Show("درصد ذخیره شد."); }));
        root.Controls.Add(bar, 0, 0); root.Controls.Add(productGrid, 0, 1); p.Controls.Add(root);
        productGrid.SelectionChanged += (_, _) => { if (productGrid.CurrentRow?.DataBoundItem is Product x) productId = x.Id; };
        productGrid.CellDoubleClick += (_, _) => Safe(EditProduct);
        search.TextChanged += (_, _) => LoadProducts(); return p;
    }

    void HandleGlobalShortcut(KeyEventArgs e, TabControl tabs)
    {
        if (e.Control && e.KeyCode == Keys.N) { e.SuppressKeyPress = true; Safe(AddProduct); return; }
        if (e.Control && e.KeyCode == Keys.E) { e.SuppressKeyPress = true; Safe(EditProduct); return; }
        if (e.Control && e.Shift && e.KeyCode == Keys.C) { e.SuppressKeyPress = true; Safe(CopyProductsWithoutPurchasePrice); return; }
        if (e.KeyCode == Keys.F5) { e.SuppressKeyPress = true; Safe(RefreshAll); return; }
        if (e.Control && e.KeyCode == Keys.F) { e.SuppressKeyPress = true; tabs.SelectedIndex = 0; search.Focus(); search.SelectAll(); return; }
        if (e.Control && e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D7) { int index = (int)e.KeyCode - (int)Keys.D1; if (index < tabs.TabPages.Count) { e.SuppressKeyPress = true; tabs.SelectedIndex = index; } }
    }
    void ShowShortcuts()
    {
        MessageBox.Show("میانبرهای برنامه:\n\nCtrl+N : افزودن قطعه جدید\nCtrl+E : ویرایش قطعه انتخاب‌شده\nCtrl+Shift+C : کپی فهرست قطعات بدون قیمت خرید\nCtrl+F : رفتن به جست‌وجوی قطعات\nF5 : تازه‌سازی اطلاعات\nCtrl+1 تا Ctrl+7 : جابه‌جایی بین تب‌ها\nEnter : تأیید فرم‌ها در صورت پشتیبانی\nEsc : بستن یا انصراف از فرم‌های محاوره‌ای", "میانبرهای صفحه‌کلید", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    void CopyProductsWithoutPurchasePrice()
    {
        var missing = products.Search().Where(x => x.PurchasePrice <= 0).OrderBy(x => x.Name).ToList();
        if (missing.Count == 0) { MessageBox.Show("همه قطعات فعال قیمت خرید دارند.", "فهرست قیمت‌های ناقص"); return; }
        var lines = new List<string> { "سلام، لطفاً قیمت خرید قطعات زیر را اعلام کنید:", "", "ردیف\tکد قطعه\tنام قطعه\tدسته\tموجودی\tقیمت خرید (تومان)" };
        int i = 1;
        foreach (var x in missing) lines.Add($"{i++}\t{x.Code}\t{x.Name}\t{x.Category}\t{x.StockQuantity}\t") ;
        lines.Add(""); lines.Add("ممنون می‌شوم قیمت خرید هر مورد را مقابل آن اعلام کنید.");
        var text = string.Join(Environment.NewLine, lines);
        Clipboard.SetText(text);
        MessageBox.Show($"فهرست {missing.Count} قطعه بدون قیمت خرید کپی شد. حالا می‌توانی مستقیم در واتساپ، تلگرام یا پیام‌رسان بچسبانی. برای صاحب‌کار، نام و کد قطعه و موجودی درج شده و ستون قیمت خرید خالی است تا تکمیل شود.", "کپی فهرست قیمت‌های ناقص", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void ChangePassword(){ using var form = new ChangePasswordForm(); form.ShowDialog(this); }

    void AddProduct() { using var editor = new ProductEditorForm(products); if (editor.ShowDialog(this) == DialogResult.OK) { LoadProducts(); LoadInventorySummary(); CheckLowStock(true); } }
    void EditProduct() { if (productGrid.CurrentRow?.DataBoundItem is not Product x) { MessageBox.Show("ابتدا یک قطعه را از فهرست انتخاب کن."); return; } using var editor = new ProductEditorForm(products, x); if (editor.ShowDialog(this) == DialogResult.OK) { LoadProducts(); LoadInventorySummary(); CheckLowStock(true); } }
    void CheckLowStock(bool force)
    {
        if (!force && lowStockChecked) return;
        lowStockChecked = true;
        int threshold = (int)lowStockThreshold.Value;
        var low = products.GetLowStock(threshold);
        if (low.Count == 0) return;
        string details = string.Join(Environment.NewLine, low.Take(15).Select(x => $"{x.Name} ({x.Code}): {x.StockQuantity} عدد"));
        if (low.Count > 15) details += Environment.NewLine + $"و {low.Count - 15} قطعه دیگر...";
        MessageBox.Show($"هشدار موجودی کم! قطعاتی که موجودی آن‌ها {threshold} عدد یا کمتر است:\n\n{details}", "هشدار انبار", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
    TabPage InventoryTab()
    {
        var p = new TabPage("ارزش و موجودی انبار");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(8) };
        root.RowStyles.Add(new(SizeType.AutoSize));
        root.RowStyles.Add(new(SizeType.AutoSize));
        root.RowStyles.Add(new(SizeType.Percent, 100));

        var summary = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(4) };
        AddInventoryMetric(summary, "تعداد انواع قطعه:", inventoryProductsValue);
        AddInventoryMetric(summary, "مجموع تعداد موجودی:", inventoryUnitsValue);
        AddInventoryMetric(summary, "ارزش کل به قیمت خرید:", inventoryPurchaseValue);
        AddInventoryMetric(summary, "ارزش کل به قیمت فروش:", inventorySaleValue);
        AddInventoryMetric(summary, "سود احتمالی کل:", inventoryProfitValue);

        var bar = Flow();
        bar.Controls.Add(Button("🔄 محاسبه مجدد موجودی و ارزش انبار", LoadInventorySummary));
        var note = new Label { AutoSize = true, Text = "ارزش‌ها بر اساس موجودی فعلی هر قطعه محاسبه می‌شوند.", Padding = new Padding(4, 7, 4, 0) };
        bar.Controls.Add(note);
        root.Controls.Add(summary, 0, 0);
        root.Controls.Add(bar, 0, 1);
        inventoryGrid.AutoGenerateColumns = true;
        root.Controls.Add(inventoryGrid, 0, 2);
        p.Controls.Add(root);
        return p;
    }
    static void AddInventoryMetric(FlowLayoutPanel panel, string title, Label value)
    {
        var box = new Panel { AutoSize = true, Padding = new Padding(8), Margin = new Padding(5), BorderStyle = BorderStyle.FixedSingle };
        var layout = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label { Text = title, AutoSize = true, Padding = new Padding(0, 4, 5, 0) });
        value.Text = "0";
        layout.Controls.Add(value);
        box.Controls.Add(layout);
        panel.Controls.Add(box);
    }
    void LoadInventorySummary()
    {
        var all = products.Search();
        decimal totalUnits = all.Sum(x => (decimal)x.StockQuantity);
        decimal purchaseValue = all.Sum(x => (decimal)x.StockQuantity * x.PurchasePrice);
        decimal saleValue = all.Sum(x => (decimal)x.StockQuantity * x.SalePrice);
        inventoryProductsValue.Text = all.Count.ToString("N0");
        inventoryUnitsValue.Text = totalUnits.ToString("N0");
        inventoryPurchaseValue.Text = purchaseValue.ToString("N0") + " تومان";
        inventorySaleValue.Text = saleValue.ToString("N0") + " تومان";
        inventoryProfitValue.Text = (saleValue - purchaseValue).ToString("N0") + " تومان";

        inventoryGrid.DataSource = all.Select(x => new
        {
            کد = x.Code,
            نام_قطعه = x.Name,
            دسته = x.Category,
            موجودی = x.StockQuantity,
            قیمت_خرید_واحد = x.PurchasePrice,
            قیمت_فروش_واحد = x.SalePrice,
            ارزش_خرید_موجودی = (decimal)x.StockQuantity * x.PurchasePrice,
            ارزش_فروش_موجودی = (decimal)x.StockQuantity * x.SalePrice,
            سود_احتمالی = (decimal)x.StockQuantity * (x.SalePrice - x.PurchasePrice)
        }).ToList();
        foreach (DataGridViewColumn column in inventoryGrid.Columns)
        {
            column.DefaultCellStyle.Format = "N0";
            if (column.Name.Contains("کد") || column.Name.Contains("نام") || column.Name.Contains("دسته"))
                column.DefaultCellStyle.Format = "";
        }
    }

    TabPage EmployeesTab()
    {
        var p=new TabPage("کارکنان");var root=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Padding=new Padding(6)};
        root.RowStyles.Add(new(SizeType.AutoSize));root.RowStyles.Add(new(SizeType.Percent,100));
        var f=Flow();Label(f,"نام");f.Controls.Add(empName);Label(f,"درصد پیش‌فرض از اجرت");f.Controls.Add(empPercent);
        f.Controls.Add(Button("ذخیره",SaveEmployee));f.Controls.Add(Button("پاک‌کردن",ClearEmployee));f.Controls.Add(Button("🗑️ حذف حساب کارکن",DeleteEmployee));
        root.Controls.Add(f,0,0);root.Controls.Add(employeeGrid,0,1);p.Controls.Add(root);employeeGrid.SelectionChanged+=(_,_)=>SelectEmployee();return p;
    }
    TabPage RepairTab()
    {
        var p=new TabPage("ثبت تعمیر");var root=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=6,ColumnCount=1,Padding=new Padding(6)};
        root.RowStyles.Add(new(SizeType.AutoSize));root.RowStyles.Add(new(SizeType.AutoSize));root.RowStyles.Add(new(SizeType.Percent,48));root.RowStyles.Add(new(SizeType.AutoSize));root.RowStyles.Add(new(SizeType.AutoSize));root.RowStyles.Add(new(SizeType.Percent,40));
        var v=Flow();Label(v,"مشتری");v.Controls.Add(customer);Label(v,"تلفن");v.Controls.Add(phone);Label(v,"مدل خودرو");v.Controls.Add(model);Label(v,"پلاک");v.Controls.Add(plate);
        v.Controls.Add(Button("ثبت مشتری/خودرو",()=>{repairs.AddCustomerVehicle(customer.Text,phone.Text,model.Text,plate.Text);customer.Clear();phone.Clear();plate.Clear();RefreshVehicles();}));
        Label(v,"خودرو");v.Controls.Add(vehicleCombo);
        var parts=Flow();
        var pickButton=Button("🔎 جست‌وجو و انتخاب قطعه…",PickAndAddItem); pickButton.Height=38; pickButton.Font=new Font("Segoe UI",10,FontStyle.Bold); parts.Controls.Add(pickButton);
        Label(parts,"تعداد انتخاب‌شده");parts.Controls.Add(qty);parts.Controls.Add(Button("حذف قطعه از تعمیر",RemoveItem));
        Label(parts,"دستمزد کل (تومان)");parts.Controls.Add(labor);Label(parts,"تخفیف مشتری (تومان)");parts.Controls.Add(discountAmountInput);Label(parts,"شرح");parts.Controls.Add(description);
        var totals=Flow();
        totals.BorderStyle=BorderStyle.FixedSingle;
        Label(totals,"جمع قطعات:");totals.Controls.Add(repairPartsTotalValue);
        Label(totals,"جمع دستمزد:");totals.Controls.Add(repairLaborTotalValue);
        Label(totals,"جمع کل:");totals.Controls.Add(repairGrandTotalValue);
        labor.ValueChanged += (_,_) => UpdateRepairTotals();
        discountAmountInput.ValueChanged += (_,_) => UpdateRepairTotals();
        var wf=Flow();Label(wf,"تعمیرکار");wf.Controls.Add(workerCombo);Label(wf,"درصد سهم از اجرت");wf.Controls.Add(workerPercent);
        workerCombo.SelectedValueChanged += (_,_) => { if(workerCombo.SelectedItem is Employee selectedWorker) workerPercent.Value=(decimal)selectedWorker.DefaultLaborPercent; };
        wf.Controls.Add(Button("افزودن تعمیرکار",AddWorker));wf.Controls.Add(Button("حذف تعمیرکار",RemoveWorker));
        wf.Controls.Add(fullPayment);Label(wf,"مبلغ دریافتی الان (تومان)");wf.Controls.Add(paidNow);paidNow.Enabled=false;
        fullPayment.CheckedChanged+=(_,_)=>paidNow.Enabled=!fullPayment.Checked;
        wf.Controls.Add(Button("ثبت نهایی تعمیر",SaveRepair));
        root.Controls.Add(v,0,0);root.Controls.Add(parts,0,1);root.Controls.Add(repairItemsGrid,0,2);root.Controls.Add(totals,0,3);root.Controls.Add(wf,0,4);root.Controls.Add(workersGrid,0,5);p.Controls.Add(root);
        UpdateRepairTotals();return p;
    }
    TabPage LedgerTab()
    {
        var p=new TabPage("حساب کارکنان");var root=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,Padding=new Padding(6)};
        root.RowStyles.Add(new(SizeType.AutoSize));root.RowStyles.Add(new(SizeType.AutoSize));root.RowStyles.Add(new(SizeType.Percent,100));
        var f=Flow();
        Label(f,"کارمند");f.Controls.Add(ledgerEmployee);Label(f,"نوع تراکنش");ledgerType.Items.AddRange(new object[]{"پرداخت دستمزد","مساعده / قرض","بدهکاری فرد","بستانکاری فرد"});ledgerType.SelectedIndex=0;f.Controls.Add(ledgerType);
        Label(f,"مبلغ");f.Controls.Add(ledgerAmount);Label(f,"شرح");f.Controls.Add(ledgerDescription);f.Controls.Add(Button("ثبت تراکنش",AddLedger));f.Controls.Add(Button("✏️ ویرایش تراکنش‌ها",EditLedgerTransactions));f.Controls.Add(Button("🗑️ حذف حساب کارمند انتخاب‌شده",DeleteEmployeeFromLedger));f.Controls.Add(Button("تازه‌سازی",LoadLedger));
        var tools=Flow(); tools.Controls.Add(new Label{Text="ثبت ابزار گم‌شده:",AutoSize=true,Font=new Font("Segoe UI",9,FontStyle.Bold),Padding=new Padding(0,7,0,0)});
        Label(tools,"نام ابزار");tools.Controls.Add(lostToolName);Label(tools,"تعداد");tools.Controls.Add(lostToolQty);Label(tools,"ارزش تقریبی (تومان)");tools.Controls.Add(lostToolValue);Label(tools,"تاریخ");tools.Controls.Add(lostToolDate);Label(tools,"توضیحات");tools.Controls.Add(lostToolDescription);
        tools.Controls.Add(Button("ثبت ابزار گم‌شده",AddLostTool));tools.Controls.Add(Button("📋 مشاهده ابزارهای گم‌شده",ShowLostTools));
        root.Controls.Add(f,0,0);root.Controls.Add(tools,0,1);root.Controls.Add(ledgerGrid,0,2);p.Controls.Add(root);return p;
    }
    TabPage CustomersTab()
    {
        var p = new TabPage("مشتریان");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(6) };
        root.RowStyles.Add(new(SizeType.AutoSize));
        root.RowStyles.Add(new(SizeType.Percent, 100));
        var bar = Flow();
        Label(bar, "جست‌وجو");
        bar.Controls.Add(customerSearch);
        bar.Controls.Add(Button("🔎 جست‌وجو", LoadCustomers));
        bar.Controls.Add(Button("✏️ ویرایش مشتری انتخاب‌شده", EditSelectedCustomer));
        bar.Controls.Add(Button("تازه‌سازی", () => { customerSearch.Clear(); LoadCustomers(); }));
        bar.Controls.Add(new Label { Text = "فهرست مشتریان، خودروها و خلاصه حساب هر مشتری", AutoSize = true, Padding = new Padding(8, 7, 0, 0) });
        root.Controls.Add(bar, 0, 0);
        root.Controls.Add(customerGrid, 0, 1);
        p.Controls.Add(root);
        customerGrid.CellDoubleClick += (_, _) => Safe(EditSelectedCustomer);
        p.Enter += (_, _) => LoadCustomers();
        return p;
    }

    void EditSelectedCustomer()
    {
        if (customerGrid.CurrentRow is null || customerGrid.CurrentRow.Cells["Id"].Value is null)
        {
            MessageBox.Show("ابتدا یک مشتری را از فهرست انتخاب کن.");
            return;
        }

        int id = Convert.ToInt32(customerGrid.CurrentRow.Cells["Id"].Value);
        string currentName = Convert.ToString(customerGrid.CurrentRow.Cells["FullName"].Value) ?? "";
        string currentPhone = Convert.ToString(customerGrid.CurrentRow.Cells["Phone"].Value) ?? "";

        using var dialog = new Form { Text = "ویرایش اطلاعات مشتری", Width = 430, Height = 230, StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var nameBox = new TextBox { Dock = DockStyle.Fill, Text = currentName };
        var phoneBox = new TextBox { Dock = DockStyle.Fill, Text = currentPhone };
        layout.Controls.Add(new Label { Text = "نام مشتری", AutoSize = true, Anchor = AnchorStyles.Right }, 0, 0); layout.Controls.Add(nameBox, 1, 0);
        layout.Controls.Add(new Label { Text = "شماره تماس", AutoSize = true, Anchor = AnchorStyles.Right }, 0, 1); layout.Controls.Add(phoneBox, 1, 1);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        var save = new Button { Text = "ذخیره تغییرات", AutoSize = true, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "انصراف", AutoSize = true, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(save); buttons.Controls.Add(cancel); layout.Controls.Add(buttons, 1, 2); dialog.Controls.Add(layout); dialog.AcceptButton = save; dialog.CancelButton = cancel;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(nameBox.Text)) { MessageBox.Show("نام مشتری نمی‌تواند خالی باشد."); return; }

        using var c = GarageApp.Data.Database.Open();
        using var q = c.CreateCommand();
        q.CommandText = "UPDATE Customers SET FullName=$name, Phone=$phone WHERE Id=$id";
        q.Parameters.AddWithValue("$name", nameBox.Text.Trim());
        q.Parameters.AddWithValue("$phone", phoneBox.Text.Trim());
        q.Parameters.AddWithValue("$id", id);
        q.ExecuteNonQuery();
        LoadCustomers();
        RefreshVehicles();
        MessageBox.Show("اطلاعات مشتری ذخیره شد.", "موفق", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void LoadCustomers()
    {
        using var c = GarageApp.Data.Database.Open();
        using var q = c.CreateCommand();
        q.CommandText = """
            SELECT c.Id, c.FullName, COALESCE(c.Phone, '') AS Phone,
                   COUNT(DISTINCT v.Id) AS VehicleCount,
                   COALESCE((SELECT group_concat(x.Model || CASE WHEN COALESCE(x.PlateNumber,'')='' THEN '' ELSE ' (' || x.PlateNumber || ')' END, '، ')
                             FROM Vehicles x WHERE x.CustomerId=c.Id), '') AS Vehicles,
                   COUNT(DISTINCT r.Id) AS RepairCount,
                   COALESCE(SUM(r.PartsTotal + r.LaborTotal - COALESCE(r.DiscountAmount,0)),0) AS TotalBilled,
                   COALESCE(SUM((SELECT COALESCE(SUM(pm.Amount),0) FROM Payments pm WHERE pm.RepairId=r.Id)),0) AS TotalPaid,
                   COALESCE(SUM(r.PartsTotal + r.LaborTotal - COALESCE(r.DiscountAmount,0) -
                       (SELECT COALESCE(SUM(pm.Amount),0) FROM Payments pm WHERE pm.RepairId=r.Id)),0) AS Balance
            FROM Customers c
            LEFT JOIN Vehicles v ON v.CustomerId=c.Id
            LEFT JOIN Repairs r ON r.VehicleId=v.Id
            WHERE ($s='') OR c.FullName LIKE $like OR COALESCE(c.Phone,'') LIKE $like
            GROUP BY c.Id, c.FullName, c.Phone
            ORDER BY c.Id DESC;
            """;
        q.Parameters.AddWithValue("$s", customerSearch.Text.Trim());
        q.Parameters.AddWithValue("$like", "%" + customerSearch.Text.Trim() + "%");
        var table = new DataTable();
        using (var reader = q.ExecuteReader()) table.Load(reader);
        customerGrid.DataSource = table;
        Headers(customerGrid, new Dictionary<string, string> {
            ["Id"] = "شناسه", ["FullName"] = "نام مشتری", ["Phone"] = "شماره تماس",
            ["VehicleCount"] = "تعداد خودرو", ["Vehicles"] = "خودروها و پلاک",
            ["RepairCount"] = "تعداد تعمیرات", ["TotalBilled"] = "مجموع فاکتورها (تومان)",
            ["TotalPaid"] = "مجموع پرداختی (تومان)", ["Balance"] = "مانده حساب (تومان)"
        });
        if (customerGrid.Columns.Contains("Id")) customerGrid.Columns["Id"]!.Visible = false;
        foreach (var col in new[] { "TotalBilled", "TotalPaid", "Balance" })
            if (customerGrid.Columns.Contains(col)) customerGrid.Columns[col]!.DefaultCellStyle.Format = "N0";
    }

    TabPage ReportsTab()
    {
        var p=new TabPage("گزارش ماهانه");var root=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=4,ColumnCount=1,Padding=new Padding(6)};
        root.RowStyles.Add(new(SizeType.AutoSize));root.RowStyles.Add(new(SizeType.AutoSize));root.RowStyles.Add(new(SizeType.Percent,60));root.RowStyles.Add(new(SizeType.Percent,40));
        var f=Flow();reportRange.Items.AddRange(new object[]{"ماهانه","روزانه"});reportRange.SelectedIndex=0;Label(f,"نوع گزارش");f.Controls.Add(reportRange);Label(f,"ماه YYYY-MM");f.Controls.Add(month);Label(f,"تاریخ روز");f.Controls.Add(reportDay);f.Controls.Add(Button("نمایش گزارش",LoadRepairs));f.Controls.Add(Button("✏️ ویرایش تعمیر انتخاب‌شده",EditReportRepair));f.Controls.Add(Button("💰 ثبت دریافت از مشتری",RegisterRepairPayment));f.Controls.Add(Button("🗑️ حذف کامل تعمیر انتخاب‌شده",DeleteReportRepair));f.Controls.Add(Button("🔄 تازه‌سازی گزارش",SaveReportRepair));f.Controls.Add(Button("مانده کارکنان",LoadLedger));f.Controls.Add(Button("📋 فاکتورهای پرداخت‌نشده هر تعمیرکار",ShowMechanicDebts));
        f.Controls.Add(Button("📄 خروجی Word قطعات کم‌موجودی", ExportLowStock));
        Label(f,"از تاریخ");f.Controls.Add(weekFrom);Label(f,"تا تاریخ");f.Controls.Add(weekTo);
        f.Controls.Add(Button("📊 خروجی گزارش هفتگی", ExportWeekly));
        repairGrid.Name="reportRepairGrid";var balanceGrid=new DataGridView{Dock=DockStyle.Fill,Name="reportBalanceGrid",ReadOnly=true,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,AllowUserToAddRows=false};
        root.Controls.Add(f,0,0);root.Controls.Add(monthlyShareSummary,0,1);root.Controls.Add(repairGrid,0,2);root.Controls.Add(balanceGrid,0,3);p.Controls.Add(root);return p;
    }

    void EditReportRepair()
    {
        if (repairGrid.CurrentRow?.DataBoundItem is not RepairSummary selected)
            throw new InvalidOperationException("ابتدا یک تعمیر را در گزارش ماهانه انتخاب کن.");

        using var form = new Form { Text = $"ویرایش تعمیر شماره {selected.Id} — اطلاعات و قطعات", Width = 900, Height = 680, StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true, Font = new Font("Segoe UI", 10), FormBorderStyle = FormBorderStyle.Sizable, MinimizeBox = false };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 10, Padding = new Padding(12) };
        layout.ColumnStyles.Add(new(SizeType.AutoSize)); layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.Percent, 100)); layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize));
        var date = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Value = DateTime.TryParse(selected.RepairDate, out var parsedDate) ? parsedDate : DateTime.Today, Dock = DockStyle.Fill };
        var customerName = new TextBox { Text = selected.Customer, Dock = DockStyle.Fill };
        var vehicleModel = new TextBox { Text = selected.Vehicle, Dock = DockStyle.Fill };
        var plateNumber = new TextBox { Text = selected.Plate, Dock = DockStyle.Fill };
        var laborAmount = new NumericUpDown { Minimum = 0, Maximum = 9_000_000_000_000m, ThousandsSeparator = true, Value = Math.Min(9_000_000_000_000m, selected.LaborTotal), Dock = DockStyle.Fill };
        var discountAmount = new NumericUpDown { Minimum = 0, Maximum = 9_000_000_000_000m, ThousandsSeparator = true, Value = Math.Min(9_000_000_000_000m, Math.Max(0, selected.PartsTotal + selected.LaborTotal - selected.Total)), Dock = DockStyle.Fill };
        void AddRow(string title, Control control, int row) { layout.Controls.Add(new Label { Text = title, AutoSize = true, Anchor = AnchorStyles.Right, Padding = new Padding(0, 7, 0, 0) }, 0, row); layout.Controls.Add(control, 1, row); }
        AddRow("تاریخ تعمیر", date, 0); AddRow("نام مشتری", customerName, 1); AddRow("مدل خودرو", vehicleModel, 2); AddRow("پلاک", plateNumber, 3); AddRow("اجرت (تومان)", laborAmount, 4); AddRow("تخفیف (تومان)", discountAmount, 5);

        var table = new DataTable();
        table.Columns.Add("ProductId", typeof(int)); table.Columns.Add("Name", typeof(string)); table.Columns.Add("Quantity", typeof(int));
        table.Columns.Add("PurchasePrice", typeof(long)); table.Columns.Add("SalePrice", typeof(long)); table.Columns.Add("Subtotal", typeof(long), "Quantity * SalePrice");
        foreach (var item in repairs.GetRepairItems(selected.Id)) table.Rows.Add(item.id, item.name, item.qty, item.purchase, item.sale);
        // Define columns explicitly before binding. This avoids a NullReferenceException
        // when auto-generated columns have not yet been created by the binding engine.
        var itemsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false
        };
        itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductId", DataPropertyName = "ProductId", Visible = false });
        itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", DataPropertyName = "Name", HeaderText = "نام قطعه", ReadOnly = true });
        itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", DataPropertyName = "Quantity", HeaderText = "تعداد", ReadOnly = false });
        itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasePrice", DataPropertyName = "PurchasePrice", Visible = false });
        itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", DataPropertyName = "SalePrice", Visible = false });
        itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", DataPropertyName = "Subtotal", HeaderText = "جمع قیمت (تومان)", ReadOnly = true });
        itemsGrid.DataSource = table;
        var itemTools = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = true };
        var productPicker = new ComboBox { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = products.Search() };
        productPicker.DisplayMember = "Name";
        var addQty = new NumericUpDown { Width = 75, Minimum = 1, Maximum = 10000, Value = 1, ThousandsSeparator = true };
        itemTools.Controls.Add(new Label { Text = "قطعه جدید", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }); itemTools.Controls.Add(productPicker);
        itemTools.Controls.Add(new Label { Text = "تعداد", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }); itemTools.Controls.Add(addQty);
        var addPart = new Button { Text = "➕ افزودن قطعه", AutoSize = true };
        addPart.Click += (_, _) =>
        {
            if (productPicker.SelectedItem is not Product product) return;
            var existing = table.Rows.Cast<DataRow>().Where(r => r.RowState != DataRowState.Deleted).FirstOrDefault(r => Convert.ToInt32(r["ProductId"]) == product.Id);
            if (existing is not null) existing["Quantity"] = Convert.ToInt32(existing["Quantity"]) + (int)addQty.Value;
            else table.Rows.Add(product.Id, product.Name, (int)addQty.Value, product.PurchasePrice, product.SalePrice);
        };
        var removePart = new Button { Text = "➖ حذف قطعه انتخاب‌شده", AutoSize = true };
        removePart.Click += (_, _) => { if (itemsGrid.CurrentRow?.DataBoundItem is DataRowView row) row.Row.Delete(); };
        itemTools.Controls.Add(addPart); itemTools.Controls.Add(removePart);
        layout.Controls.Add(itemTools, 0, 6); layout.SetColumnSpan(itemTools, 2);
        layout.Controls.Add(itemsGrid, 0, 7); layout.SetColumnSpan(itemsGrid, 2);
        var totalsLabel = new Label { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.DarkSlateBlue };
        void UpdateTotals() { long partsSum = table.Rows.Cast<DataRow>().Where(r => r.RowState != DataRowState.Deleted).Sum(r => Convert.ToInt64(r["Quantity"]) * Convert.ToInt64(r["SalePrice"])); long discount = Math.Min((long)discountAmount.Value, partsSum + (long)laborAmount.Value); totalsLabel.Text = $"جمع قطعات: {partsSum:N0} تومان     |     دستمزد: {laborAmount.Value:N0} تومان     |     تخفیف: {discount:N0} تومان     |     جمع نهایی: {partsSum + (long)laborAmount.Value - discount:N0} تومان"; }
        table.ColumnChanged += (_, _) => UpdateTotals(); table.RowChanged += (_, _) => UpdateTotals(); table.RowDeleted += (_, _) => UpdateTotals(); laborAmount.ValueChanged += (_, _) => UpdateTotals(); discountAmount.ValueChanged += (_, _) => UpdateTotals();
        itemsGrid.CellEndEdit += (_, _) => UpdateTotals();
        layout.Controls.Add(totalsLabel, 0, 8); layout.SetColumnSpan(totalsLabel, 2);
        var info = new Label { Text = "با اضافه یا حذف قطعات، موجودی انبار به‌صورت خودکار اصلاح می‌شود. قیمت قطعات قدیمی حفظ می‌شود؛ قطعات جدید با قیمت فروش فعلی ثبت می‌شوند.", AutoSize = true, ForeColor = Color.DarkSlateGray };
        layout.Controls.Add(info, 0, 9); layout.SetColumnSpan(info, 2);
        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Padding = new Padding(8) };
        var save = new Button { Text = "ذخیره تغییرات", AutoSize = true };
        var cancel = new Button { Text = "انصراف", AutoSize = true, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => { try { itemsGrid.EndEdit(); form.DialogResult = DialogResult.OK; form.Close(); } catch (Exception ex) { MessageBox.Show(form, ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error); } };
        buttons.Controls.Add(save); buttons.Controls.Add(cancel); form.Controls.Add(layout); form.Controls.Add(buttons); form.AcceptButton = save; form.CancelButton = cancel;
        UpdateTotals();
        if (form.ShowDialog(this) != DialogResult.OK) return;

        var editedItems = table.Rows.Cast<DataRow>().Where(r => r.RowState != DataRowState.Deleted)
            .Select(r => (id: Convert.ToInt32(r["ProductId"]), name: Convert.ToString(r["Name"]) ?? "", qty: Convert.ToInt32(r["Quantity"]), purchase: Convert.ToInt64(r["PurchasePrice"]), sale: Convert.ToInt64(r["SalePrice"]))).ToList();
        repairs.UpdateRepairDetails(selected.Id, date.Value.ToString("yyyy-MM-dd"), customerName.Text, vehicleModel.Text, plateNumber.Text, (long)laborAmount.Value, (long)discountAmount.Value, editedItems);
        LoadRepairs(); LoadLedger(); LoadProducts(); LoadInventorySummary();
        MessageBox.Show("تغییرات تعمیر و قطعات ذخیره شد؛ جمع قطعات، موجودی انبار، بدهی و سهم کارکنان به‌روزرسانی شدند.", "ویرایش گزارش", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void DeleteReportRepair()
    {
        if (repairGrid.CurrentRow?.DataBoundItem is not RepairSummary selected)
            throw new InvalidOperationException("ابتدا یک تعمیر را در گزارش ماهانه انتخاب کن.");

        var answer = MessageBox.Show(
            this,
            $"آیا مطمئنی تعمیر شماره {selected.Id} مربوط به «{selected.Customer}» را به‌طور کامل حذف کنی؟\n\n" +
            $"جمع قطعات: {selected.PartsTotal:N0} تومان\nاجرت: {selected.LaborTotal:N0} تومان\nپرداخت‌های ثبت‌شده: {selected.Paid:N0} تومان\n\n" +
            "این کار برگشت‌پذیر نیست. پرداخت‌ها، سهم‌های کارکنان و سوابق مصرف قطعات این تعمیر حذف می‌شوند و قطعات مصرف‌شده به موجودی انبار برمی‌گردند. اطلاعات خود مشتری و خودرو حذف نمی‌شود.",
            "تأیید حذف کامل تعمیر",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes) return;
        repairs.DeleteRepair(selected.Id);
        LoadRepairs();
        LoadLedger();
        LoadProducts();
        LoadInventorySummary();
        MessageBox.Show("تعمیر و تمام سوابق وابسته به آن حذف شد؛ موجودی قطعات مصرف‌شده نیز به انبار برگشت.", "حذف انجام شد", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Kept as a separate toolbar action so users can explicitly refresh calculated report totals.
    void SaveReportRepair()
    {
        LoadRepairs();
        LoadLedger();
        MessageBox.Show("گزارش از اطلاعات ذخیره‌شده در پایگاه داده تازه‌سازی شد.", "گزارش ماهانه", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void ExportLowStock()
    {
        int threshold = products.GetLowStockThreshold();
        SaveWordCompatibleReport("گزارش_قطعات_کم_موجودی", exports.LowStockReport(threshold));
    }
    void ExportWeekly()
    {
        SaveWordCompatibleReport("گزارش_هفتگی_تعمیرگاه", exports.WeeklyReport(weekFrom.Value.Date, weekTo.Value.Date));
    }
    void SaveWordCompatibleReport(string suggestedName, string html)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "ذخیره گزارش برای ارسال به صاحب‌کار",
            Filter = "گزارش Word (*.doc)|*.doc|صفحه وب HTML (*.html)|*.html",
            FileName = suggestedName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".doc",
            AddExtension = true,
            DefaultExt = "doc"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllText(dialog.FileName, html, new System.Text.UTF8Encoding(true));
        MessageBox.Show("گزارش با موفقیت ذخیره شد. فایل .doc را می‌توانی با Microsoft Word باز کنی و برای صاحب‌کار بفرستی.", "تهیه گزارش", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void RefreshAll(){profit.Value=(decimal)products.GetProfitPercent();LoadProducts();LoadInventorySummary();LoadEmployees();RefreshVehicles();RefreshProductCombo();RefreshWorkers();LoadLedger();LoadRepairs();LoadCustomers();}
    void LoadProducts()
    {
        productGrid.DataSource = products.Search(search.Text);
        Headers(productGrid, new Dictionary<string, string> { ["Id"] = "شناسه", ["Code"] = "کد", ["Name"] = "نام قطعه", ["PurchasePrice"] = "قیمت خرید", ["SalePrice"] = "قیمت فروش", ["StockQuantity"] = "موجودی", ["Category"] = "دسته" });
        if (productGrid.Columns.Contains("Id")) productGrid.Columns["Id"]!.Visible = false;
        RefreshProductCombo();
    }
    void DeleteProduct()
    {
        if (productGrid.CurrentRow?.DataBoundItem is not Product x) { MessageBox.Show("ابتدا یک قطعه را انتخاب کن."); return; }
        if (MessageBox.Show($"قطعه «{x.Name}» غیرفعال شود؟", "تأیید", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        products.Delete(x.Id); LoadProducts(); LoadInventorySummary();
    }
    void LoadEmployees(){employeeGrid.DataSource=employees.GetAll();Headers(employeeGrid, new Dictionary<string, string> { ["Id"] = "شناسه", ["FullName"] = "نام", ["DefaultLaborPercent"] = "درصد پیش‌فرض" });if(employeeGrid.Columns.Contains("Id"))employeeGrid.Columns["Id"]!.Visible=false;RefreshWorkers();}
    void SaveEmployee(){employees.Save(employeeId,empName.Text,(double)empPercent.Value);ClearEmployee();LoadEmployees();}
    void SelectEmployee(){if(employeeGrid.CurrentRow?.DataBoundItem is not Employee x)return;employeeId=x.Id;empName.Text=x.FullName;empPercent.Value=(decimal)x.DefaultLaborPercent;}
    void ClearEmployee(){employeeId=null;empName.Clear();empPercent.Value=100;employeeGrid.ClearSelection();}
    void DeleteEmployee(){if(employeeId is null)return;if(MessageBox.Show("حساب این کارمند از فهرست فعال حذف شود؟ سوابق تعمیرات، پرداخت‌ها و حسابداری قبلی برای حفظ گزارش‌ها باقی می‌مانند؛ این حساب دیگر برای تعمیرات جدید قابل انتخاب نخواهد بود.","تأیید حذف حساب",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;employees.Delete(employeeId.Value);ClearEmployee();LoadEmployees();LoadLedger();}
    void DeleteEmployeeFromLedger(){if(ledgerEmployee.SelectedItem is not Employee selected)return;if(MessageBox.Show($"حساب «{selected.FullName}» از فهرست کارکنان فعال حذف شود؟\n\nسوابق تعمیرات و تراکنش‌های قبلی برای حفظ گزارش‌ها باقی می‌مانند؛ این کارمند دیگر برای تعمیرات جدید قابل انتخاب نخواهد بود.","تأیید حذف حساب کارمند",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;employees.Delete(selected.Id);LoadEmployees();LoadLedger();MessageBox.Show("حساب کارمند از فهرست فعال حذف شد و سوابق قبلی حفظ شدند.","حذف حساب",MessageBoxButtons.OK,MessageBoxIcon.Information);}
    void RefreshVehicles(){vehicleCombo.DataSource=null;vehicleCombo.DataSource=repairs.GetVehicles();}
    void RefreshProductCombo(){productCombo.DataSource=null;productCombo.DataSource=products.Search();productCombo.DisplayMember=nameof(Product.Name);productCombo.ValueMember=nameof(Product.Id);}
    void RefreshWorkers(){var es=employees.GetAll();workerCombo.DataSource=null;workerCombo.DataSource=es.ToList();workerCombo.DisplayMember=nameof(Employee.FullName);workerCombo.ValueMember=nameof(Employee.Id);ledgerEmployee.DataSource=null;ledgerEmployee.DataSource=es.ToList();ledgerEmployee.DisplayMember=nameof(Employee.FullName);ledgerEmployee.ValueMember=nameof(Employee.Id);}
    void PickAndAddItem()
    {
        using var picker=new PartPickerForm(products);
        if(picker.ShowDialog(this)!=DialogResult.OK || picker.SelectedProduct is not Product x) return;
        qty.Value=picker.SelectedQuantity;
        int n=picker.SelectedQuantity; int i=items.FindIndex(z=>z.id==x.Id);
        if(i>=0){var old=items[i];items[i]=(old.id,old.name,old.qty+n,old.purchase,old.sale);}
        else items.Add((x.Id,x.Name,n,x.PurchasePrice,x.SalePrice));
        RenderItems();
    }
    void RemoveItem(){if(repairItemsGrid.CurrentRow is null)return;int i=repairItemsGrid.CurrentRow.Index;if(i>=0&&i<items.Count)items.RemoveAt(i);RenderItems();}
    void RenderItems(){repairItemsGrid.DataSource=null;repairItemsGrid.DataSource=items.Select(x=>new{شناسه=x.id,نام=x.name,تعداد=x.qty,قیمت_خرید=x.purchase,قیمت_فروش=x.sale,جمع=x.sale*x.qty}).ToList();if(repairItemsGrid.Columns.Contains("شناسه"))repairItemsGrid.Columns["شناسه"]!.Visible=false;UpdateRepairTotals();}
    void UpdateRepairTotals()
    {
        decimal partsTotal=items.Sum(x=>(decimal)x.sale*x.qty);
        decimal laborTotal=labor.Value;
        decimal subtotal=partsTotal+laborTotal;
        decimal discountAmount=Math.Min(subtotal, discountAmountInput.Value);
        decimal finalTotal=subtotal-discountAmount;
        repairPartsTotalValue.Text=$"{partsTotal:N0} تومان";
        repairLaborTotalValue.Text=$"{laborTotal:N0} تومان";
        repairGrandTotalValue.Text=$"جمع قبل تخفیف: {subtotal:N0} | تخفیف: {discountAmount:N0} | قابل پرداخت: {finalTotal:N0} تومان";
    }
    void AddWorker(){if(workerCombo.SelectedItem is not Employee x)throw new InvalidOperationException("ابتدا کارمند ثبت کنید.");if(workers.Any(z=>z.id==x.Id))throw new InvalidOperationException("این کارمند قبلاً اضافه شده.");workers.Add((x.Id,x.FullName,(double)workerPercent.Value));RenderWorkers();workerPercent.Value=(decimal)x.DefaultLaborPercent;}
    void RemoveWorker(){if(workersGrid.CurrentRow is null)return;int i=workersGrid.CurrentRow.Index;if(i>=0&&i<workers.Count)workers.RemoveAt(i);RenderWorkers();}
    void RenderWorkers(){workersGrid.DataSource=null;workersGrid.DataSource=workers.Select(x=>new{شناسه=x.id,نام=x.name,درصد_سهم=x.percent}).ToList();if(workersGrid.Columns.Contains("شناسه"))workersGrid.Columns["شناسه"]!.Visible=false;}
    void SaveRepair()
    {
        if (vehicleCombo.SelectedItem is not Vehicle v)
            throw new InvalidOperationException("مشتری و خودرو را ثبت و انتخاب کنید.");

        // CreateRepair updates product stock in the same database transaction as the repair.
        long initialPayment = fullPayment.Checked ? -1 : (long)paidNow.Value;
        long subtotal = checked(items.Sum(x => x.sale * x.qty) + (long)labor.Value);
        long discountAmount = (long)Math.Min(subtotal, discountAmountInput.Value);
        repairs.CreateRepair(v.Id, description.Text, (long)labor.Value, items.ToList(), workers.ToList(), initialPayment, discountAmount);

        items.Clear(); workers.Clear(); RenderItems(); RenderWorkers();
        description.Clear(); labor.Value = 0; discountAmountInput.Value=0; paidNow.Value=0; fullPayment.Checked=true; UpdateRepairTotals();
        LoadRepairs(); LoadLedger();

        // Refresh both the inventory list and the parts selector so the new stock is visible immediately.
        LoadProducts();
        LoadInventorySummary();
        RefreshProductCombo();
        CheckLowStock(true);

        MessageBox.Show("تعمیر ثبت شد و موجودی قطعات مصرف‌شده از انبار کسر شد. اگر مبلغ کامل دریافت نشده باشد، مانده به‌عنوان بدهی مشتری ثبت شده است.", "ثبت تعمیر", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    void RegisterRepairPayment()
    {
        if(repairGrid.CurrentRow?.DataBoundItem is not RepairSummary selected)
            throw new InvalidOperationException("ابتدا در جدول گزارش تعمیرات، تعمیر موردنظر را انتخاب کن.");
        long outstanding=repairs.GetOutstanding(selected.Id);
        if(outstanding<=0) throw new InvalidOperationException("این تعمیر تسویه شده است و بدهی ندارد.");
        using var form=new Form{Text=$"ثبت دریافت وجه - تعمیر شماره {selected.Id}",Width=430,Height=240,StartPosition=FormStartPosition.CenterParent,RightToLeft=RightToLeft.Yes,RightToLeftLayout=true,Font=new Font("Segoe UI",10),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false};
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=4,Padding=new Padding(12)};
        layout.ColumnStyles.Add(new(SizeType.AutoSize));layout.ColumnStyles.Add(new(SizeType.Percent,100));
        var due=new Label{Text=$"بدهی باقی‌مانده: {outstanding:N0} تومان",AutoSize=true,Padding=new Padding(0,5,0,5)};
        var amount=new NumericUpDown{Minimum=1,Maximum=Math.Min(9_000_000_000_000m,outstanding),Value=Math.Min(9_000_000_000_000m,outstanding),ThousandsSeparator=true,Dock=DockStyle.Fill};
        var note=new TextBox{Text="دریافت وجه بابت تعمیر",Dock=DockStyle.Fill};
        var save=new Button{Text="ثبت دریافت",AutoSize=true};var cancel=new Button{Text="انصراف",DialogResult=DialogResult.Cancel,AutoSize=true};
        layout.Controls.Add(due,0,0);layout.SetColumnSpan(due,2);layout.Controls.Add(new Label{Text="مبلغ دریافتی",AutoSize=true,Padding=new Padding(0,5,0,0)},0,1);layout.Controls.Add(amount,1,1);
        layout.Controls.Add(new Label{Text="شرح",AutoSize=true,Padding=new Padding(0,5,0,0)},0,2);layout.Controls.Add(note,1,2);
        var buttons=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.RightToLeft};buttons.Controls.Add(save);buttons.Controls.Add(cancel);layout.Controls.Add(buttons,0,3);layout.SetColumnSpan(buttons,2);
        save.Click+=(_,_)=>{try{repairs.AddPayment(selected.Id,(long)amount.Value,note.Text);form.DialogResult=DialogResult.OK;form.Close();}catch(Exception ex){MessageBox.Show(form,ex.Message,"خطا",MessageBoxButtons.OK,MessageBoxIcon.Error);}};
        form.Controls.Add(layout);form.AcceptButton=save;form.CancelButton=cancel;
        if(form.ShowDialog(this)==DialogResult.OK){LoadRepairs();MessageBox.Show("دریافت وجه ثبت شد و مانده بدهی به‌روزرسانی شد.","ثبت پرداخت",MessageBoxButtons.OK,MessageBoxIcon.Information);}
    }
    void ShowMechanicDebts()
    {
        var data=repairs.GetMechanicUnpaidInvoices(month.Text);
        using var form=new Form{Text="فاکتورهای پرداخت‌نشده به تفکیک تعمیرکار مسئول",Width=1100,Height=520,StartPosition=FormStartPosition.CenterParent,RightToLeft=RightToLeft.Yes,RightToLeftLayout=true};
        var grid=Grid(); grid.DataSource=data;
        Headers(grid,new Dictionary<string,string>{{"Mechanic","تعمیرکار مسئول (اولین نفر)"},{"RepairId","شماره تعمیر"},{"RepairDate","تاریخ"},{"Customer","مشتری"},{"Vehicle","خودرو"},{"Plate","پلاک"},{"InvoiceTotal","مبلغ فاکتور"},{"Collected","مبلغ دریافت‌شده"},{"Outstanding","مانده بدهی تعمیرکار"}});
        var close=new Button{Text="بستن",Dock=DockStyle.Bottom,Height=35}; close.Click+=(_,_)=>form.Close(); form.Controls.Add(grid);form.Controls.Add(close);form.ShowDialog(this);
    }

    void EditLedgerTransactions()
    {
        using var form=new Form{Text="ویرایش تراکنش‌های حساب کارکنان",Width=1000,Height=560,StartPosition=FormStartPosition.CenterParent,RightToLeft=RightToLeft.Yes,RightToLeftLayout=true};
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Padding=new Padding(8)};root.RowStyles.Add(new(SizeType.Percent,100));root.RowStyles.Add(new(SizeType.AutoSize));
        var grid=Grid();grid.DataSource=ledger.Transactions();
        Headers(grid,new Dictionary<string,string>{{"Id","شناسه"},{"Employee","کارمند"},{"TransactionType","نوع"},{"Amount","مبلغ"},{"TransactionDate","تاریخ"},{"Description","شرح"}});
        var bar=Flow();var edit=Button("ویرایش تراکنش انتخاب‌شده",()=>Safe(()=>
        {
            if(grid.CurrentRow is null) throw new InvalidOperationException("یک تراکنش انتخاب کن.");
            var row=grid.CurrentRow; int id=Convert.ToInt32(row.Cells["Id"].Value); string type=Convert.ToString(row.Cells["TransactionType"].Value)??"Payment";
            long oldAmount=Convert.ToInt64(row.Cells["Amount"].Value); string oldDate=Convert.ToString(row.Cells["TransactionDate"].Value)??DateTime.Today.ToString("yyyy-MM-dd"); string oldDesc=Convert.ToString(row.Cells["Description"].Value)??"";
            using var editor=new Form{Text="اصلاح تراکنش",Width=480,Height=300,StartPosition=FormStartPosition.CenterParent,RightToLeft=RightToLeft.Yes,RightToLeftLayout=true};
            var f=Flow();var amount=Money();amount.Value=Math.Min(oldAmount,(long)amount.Maximum);var date=new TextBox{Width=130,Text=oldDate};var desc=new TextBox{Width=260,Text=oldDesc};
            Label(f,"مبلغ");f.Controls.Add(amount);Label(f,"تاریخ YYYY-MM-DD");f.Controls.Add(date);Label(f,"شرح");f.Controls.Add(desc);
            f.Controls.Add(Button("ذخیره تغییرات",()=>{ledger.UpdateTransaction(id,(long)amount.Value,date.Text,desc.Text,type);editor.DialogResult=DialogResult.OK;editor.Close();}));editor.Controls.Add(f);
            if(editor.ShowDialog(form)==DialogResult.OK){grid.DataSource=ledger.Transactions();LoadLedger();}
        }));
        var delete=Button("🗑️ حذف تراکنش انتخاب‌شده",()=>Safe(()=>
        {
            if(grid.CurrentRow is null) throw new InvalidOperationException("ابتدا تراکنش موردنظر را انتخاب کن.");
            int id=Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
            string employee=Convert.ToString(grid.CurrentRow.Cells["Employee"].Value)??"";
            string type=Convert.ToString(grid.CurrentRow.Cells["TransactionType"].Value)??"";
            string description=Convert.ToString(grid.CurrentRow.Cells["Description"].Value)??"";
            long amount=Convert.ToInt64(grid.CurrentRow.Cells["Amount"].Value);
            if(MessageBox.Show(form,$"این تراکنش حذف شود؟\n\nکارمند: {employee}\nنوع: {type}\nمبلغ: {amount:N0} تومان\nشرح: {description}\n\nمانده حساب بعد از حذف دوباره محاسبه می‌شود.","تأیید حذف تراکنش",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes) return;
            ledger.DeleteTransaction(id);
            grid.DataSource=ledger.Transactions();
            LoadLedger();
            MessageBox.Show(form,"تراکنش حذف شد و مانده حساب به‌روزرسانی شد.","حذف تراکنش",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }));
        bar.Controls.Add(edit);bar.Controls.Add(delete);bar.Controls.Add(Button("تازه‌سازی",()=>grid.DataSource=ledger.Transactions()));root.Controls.Add(grid,0,0);root.Controls.Add(bar,0,1);form.Controls.Add(root);form.ShowDialog(this);
    }


    void AddLostTool()
    {
        if(ledgerEmployee.SelectedItem is not Employee e) throw new InvalidOperationException("ابتدا کارمند را انتخاب کن.");
        string name=lostToolName.Text.Trim(); if(string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("نام ابزار را وارد کن.");
        using var c=GarageApp.Data.Database.Open(); using var q=c.CreateCommand();
        q.CommandText="INSERT INTO EmployeeLostTools(EmployeeId,ToolName,Quantity,EstimatedValue,LossDate,Description) VALUES($e,$n,$q,$v,$d,$x);";
        q.Parameters.AddWithValue("$e",e.Id);q.Parameters.AddWithValue("$n",name);q.Parameters.AddWithValue("$q",(int)lostToolQty.Value);q.Parameters.AddWithValue("$v",(long)lostToolValue.Value);q.Parameters.AddWithValue("$d",lostToolDate.Value.ToString("yyyy-MM-dd"));q.Parameters.AddWithValue("$x",lostToolDescription.Text.Trim());q.ExecuteNonQuery();
        lostToolName.Clear();lostToolQty.Value=1;lostToolValue.Value=0;lostToolDescription.Clear();ShowLostTools();
    }
    void ShowLostTools()
    {
        using var c=GarageApp.Data.Database.Open();using var q=c.CreateCommand();
        q.CommandText="SELECT t.Id,e.FullName AS Employee,t.ToolName,t.Quantity,t.EstimatedValue,t.LossDate,COALESCE(t.Description,'') AS Description FROM EmployeeLostTools t JOIN Employees e ON e.Id=t.EmployeeId ORDER BY t.LossDate DESC,t.Id DESC;";
        var table=new DataTable();using(var r=q.ExecuteReader())table.Load(r);
        using var form=new Form{Text="ابزارهای گم‌شده کارکنان",Width=1000,Height=520,StartPosition=FormStartPosition.CenterParent,RightToLeft=RightToLeft.Yes,RightToLeftLayout=true};
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Padding=new Padding(8)};root.RowStyles.Add(new(SizeType.Percent,100));root.RowStyles.Add(new(SizeType.AutoSize));
        var grid=Grid();grid.DataSource=table;Headers(grid,new Dictionary<string,string>{{"Id","شناسه"},{"Employee","کارمند"},{"ToolName","نام ابزار"},{"Quantity","تعداد"},{"EstimatedValue","ارزش تقریبی (تومان)"},{"LossDate","تاریخ"},{"Description","توضیحات"}});
        var bar=Flow();bar.Controls.Add(Button("🗑️ حذف مورد انتخاب‌شده",()=>{if(grid.CurrentRow is null)throw new InvalidOperationException("یک مورد را انتخاب کن.");int id=Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);if(MessageBox.Show(form,"این مورد از فهرست ابزارهای گم‌شده حذف شود؟","تأیید حذف",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;using var dc=GarageApp.Data.Database.Open();using var dq=dc.CreateCommand();dq.CommandText="DELETE FROM EmployeeLostTools WHERE Id=$id";dq.Parameters.AddWithValue("$id",id);dq.ExecuteNonQuery();form.Close();ShowLostTools();}));
        bar.Controls.Add(Button("بستن",form.Close));root.Controls.Add(grid,0,0);root.Controls.Add(bar,0,1);form.Controls.Add(root);form.ShowDialog(this);
    }

    void AddLedger(){if(ledgerEmployee.SelectedItem is not Employee e)throw new InvalidOperationException("کارمند انتخاب نشده.");string type=ledgerType.SelectedIndex switch{0=>"Payment",1=>"Advance",2=>"Debt",_=>"Credit"};ledger.Add(e.Id,type,(long)ledgerAmount.Value,ledgerDescription.Text);ledgerAmount.Value=0;ledgerDescription.Clear();LoadLedger();}
    void LoadLedger()
    {
        var data=ledger.Balances(month.Text);ledgerGrid.DataSource=data;SetLedgerHeaders(ledgerGrid);
    }
    void LoadRepairs()
    {
        var data=reportRange.SelectedIndex == 1 ? repairs.GetRepairs("", reportDay.Value.ToString("yyyy-MM-dd")) : repairs.GetRepairs(month.Text);
        repairGrid.DataSource=data;
        Headers(repairGrid, new Dictionary<string, string> { ["Id"] = "شماره", ["RepairDate"] = "تاریخ", ["Customer"] = "مشتری", ["Vehicle"] = "خودرو", ["Plate"] = "پلاک", ["Mechanics"] = "تعمیرکار(ها)", ["PartsUsed"] = "قطعات مصرف‌شده", ["PartsTotal"] = "جمع قطعات", ["LaborTotal"] = "اجرت", ["Total"] = "جمع کل", ["Paid"] = "پرداختی", ["Balance"] = "بدهی باقی‌مانده", ["Status"] = "وضعیت", ["EmployeeShareTotal"] = "سهم کارکنان از اجرت", ["OwnerShare"] = "سهم صاحب‌کار" });
        var workGrid = Controls.Find("reportBalanceGrid", true).FirstOrDefault() as DataGridView;
        if (workGrid is not null)
        {
            string selectedDay = reportRange.SelectedIndex == 1 ? reportDay.Value.ToString("yyyy-MM-dd") : "";
            string selectedMonth = reportRange.SelectedIndex == 1 ? "" : month.Text.Trim();
            workGrid.DataSource = repairs.GetEmployeeWorkSummary(selectedMonth, selectedDay);
            Headers(workGrid, new Dictionary<string, string> { ["EmployeeName"] = "نام کارمند", ["RepairCount"] = "تعداد تعمیرات انجام‌شده", ["LaborEarned"] = "مجموع سهم از اجرت (تومان)" });
        }
        long parts=data.Sum(x=>x.PartsTotal), laborTotal=data.Sum(x=>x.LaborTotal), employeeShares=data.Sum(x=>x.EmployeeShareTotal), ownerShares=data.Sum(x=>x.OwnerShare), received=data.Sum(x=>x.Paid), outstanding=data.Sum(x=>x.Balance);
        string periodLabel = reportRange.SelectedIndex == 1 ? $"روز {reportDay.Value:yyyy-MM-dd}" : $"ماه {month.Text}";
        monthlyShareSummary.Text=$"خلاصه {periodLabel}  |  قطعات (تماماً سهم صاحب‌کار): {parts:N0} تومان  |  اجرت: {laborTotal:N0} تومان  |  سهم کارکنان: {employeeShares:N0} تومان  |  سهم صاحب‌کار با احتساب قطعات: {ownerShares:N0} تومان  |  دریافتی: {received:N0} تومان  |  بدهی مشتریان: {outstanding:N0} تومان";
    }
    static void SetLedgerHeaders(DataGridView g) => Headers(g, new Dictionary<string, string> { ["Id"] = "شناسه", ["FullName"] = "نام", ["Earned"] = "دستمزد کسب‌شده", ["Paid"] = "پرداختی", ["Advance"] = "مساعده", ["Debt"] = "بدهکاری", ["Credit"] = "بستانکاری", ["CustomerInvoiceDebt"] = "مانده فاکتورهای پرداخت‌نشده مشتریان (مسئول فاکتور)", ["NetBalance"] = "مانده حساب کارکنان" });
    static void Headers(DataGridView g,Dictionary<string,string> h){foreach(var x in h)if(g.Columns.Contains(x.Key))g.Columns[x.Key]!.HeaderText=x.Value;}
}
