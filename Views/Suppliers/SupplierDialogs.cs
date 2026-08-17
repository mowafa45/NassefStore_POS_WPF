using NassefStore.Data;
using NassefStore.Data.Entities;
using NassefStore.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace NassefStore.Views.Suppliers;

// ══════════════════════════════════════════════════════════════
// نافذة إضافة / تعديل مورد
// ══════════════════════════════════════════════════════════════
public class SupplierDialog : Window
{
    private readonly Supplier? _existing;
    private readonly SupplierService _service;
    private readonly TextBox _name    = new();
    private readonly TextBox _phone   = new();
    private readonly TextBox _address = new();
    private readonly TextBox _notes   = new();

    public SupplierDialog(Supplier? existing)
    {
        _existing = existing;
        _service  = new SupplierService(new AppDbContext());
        Title     = existing == null ? "إضافة مورد" : "تعديل مورد";
        Width = 420; Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;
        ResizeMode = ResizeMode.NoResize;
        BuildUI();
    }

    private void BuildUI()
    {
        // ScrollViewer يحتوي كل المحتوى
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var panel = new StackPanel { Margin = new Thickness(24) };

        // عنوان النافذة
        panel.Children.Add(new TextBlock
        {
            Text       = _existing == null ? "إضافة مورد جديد" : $"تعديل: {_existing.Name}",
            FontSize   = 18,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.DarkBlue,
            Margin     = new Thickness(0, 0, 0, 16)
        });

        void AddField(string hint, TextBox box, bool multiline = false)
        {
            MaterialDesignThemes.Wpf.HintAssist.SetHint(box, hint);
            box.Style  = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
            box.Margin = new Thickness(0, 0, 0, 12);
            if (multiline)
            {
                box.Height        = 70;
                box.AcceptsReturn = true;
                box.TextWrapping  = TextWrapping.Wrap;
            }
            panel.Children.Add(box);
        }

        AddField("اسم المورد *",  _name);
        AddField("رقم الهاتف",    _phone);
        AddField("العنوان",       _address);
        AddField("ملاحظات",       _notes, multiline: true);

        if (_existing != null)
        {
            _name.Text    = _existing.Name;
            _phone.Text   = _existing.Phone   ?? "";
            _address.Text = _existing.Address ?? "";
            _notes.Text   = _existing.Notes   ?? "";
        }

        // أزرار الحفظ والإلغاء
        var btnRow = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin              = new Thickness(0, 8, 0, 0)
        };
        var save   = new Button { Content = "حفظ",   Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "إلغاء", IsCancel = true };
        save.Style   = (Style)Application.Current.Resources["MaterialDesignRaisedButton"];
        cancel.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedButton"];
        save.Click  += Save_Click;
        btnRow.Children.Add(save);
        btnRow.Children.Add(cancel);
        panel.Children.Add(btnRow);

        scroll.Content = panel;
        Content        = scroll;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            MessageBox.Show("اسم المورد مطلوب.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            if (_existing != null)
            {
                _existing.Name    = _name.Text.Trim();
                _existing.Phone   = _phone.Text.Trim();
                _existing.Address = _address.Text.Trim();
                _existing.Notes   = _notes.Text.Trim();
                await _service.UpdateAsync(_existing);
            }
            else
            {
                await _service.AddAsync(new Supplier
                {
                    Name    = _name.Text.Trim(),
                    Phone   = _phone.Text.Trim(),
                    Address = _address.Text.Trim(),
                    Notes   = _notes.Text.Trim()
                });
            }
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الحفظ:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

// ══════════════════════════════════════════════════════════════
// نافذة فاتورة شراء جديدة
// ══════════════════════════════════════════════════════════════
public class PurchaseDialog : Window
{
    private readonly Supplier        _supplier;
    private readonly PurchaseService _purchaseService;
    private readonly ProductService  _productService;
    private readonly ObservableCollection<PurchaseCartItem> _items = new();

    private readonly ComboBox   _productCombo = new();
    private readonly TextBox    _qty          = new();
    private readonly TextBox    _cost         = new();
    private readonly TextBox    _paid         = new();
    private readonly TextBox    _notes        = new();
    private readonly ComboBox   _payMethod    = new();
    private readonly DatePicker _dueDate      = new();
    private readonly TextBlock  _totalText    = new();
    private          DataGrid?  _grid;

    public PurchaseDialog(Supplier supplier)
    {
        _supplier        = supplier;
        _purchaseService = new PurchaseService(new AppDbContext());
        _productService  = new ProductService(new AppDbContext());
        Title  = $"فاتورة شراء — {supplier.Name}";
        Width  = 680; Height = 620;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;
        BuildUI();
        Loaded += async (_, _) => await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var products = await _productService.GetAllAsync();
            _productCombo.ItemsSource       = products;
            _productCombo.DisplayMemberPath = "Name";
            _productCombo.SelectedValuePath = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل المنتجات:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static decimal ParseDecimal(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return 0;
        t = t.Replace(",", ".").Trim();
        return decimal.TryParse(t, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : 0;
    }

    private void BuildUI()
    {
        // الـ layout الرئيسي: ScrollViewer → DockPanel
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var dock = new DockPanel { Margin = new Thickness(20), LastChildFill = true };

        // ── أزرار الحفظ (أسفل) ──────────────────────────────
        var btnRow = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin              = new Thickness(0, 16, 0, 0)
        };
        var save   = new Button { Content = "حفظ الفاتورة", Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "إلغاء", IsCancel = true };
        save.Style   = (Style)Application.Current.Resources["MaterialDesignRaisedButton"];
        cancel.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedButton"];
        save.Click  += Save_Click;
        btnRow.Children.Add(save);
        btnRow.Children.Add(cancel);
        DockPanel.SetDock(btnRow, Dock.Bottom);
        dock.Children.Add(btnRow);

        // ── المحتوى الرئيسي ──────────────────────────────────
        var mainGrid = new Grid();
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

        // الجانب الأيسر: المنتجات
        var leftPanel = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };

        leftPanel.Children.Add(new TextBlock
        {
            Text = "إضافة منتجات", FontSize = 14, FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        });

        // اختيار المنتج
        _productCombo.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedComboBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_productCombo, "اختر منتجاً");
        _productCombo.Margin = new Thickness(0, 0, 0, 8);
        leftPanel.Children.Add(_productCombo);

        // الكمية والسعر
        var qtyRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        qtyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        qtyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        qtyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        qtyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        qtyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        _qty.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_qty, "الكمية");
        _qty.Text = "1";
        _cost.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_cost, "سعر الشراء");

        var addBtn = new Button { Content = "+ إضافة", Height = 38 };
        addBtn.Style  = (Style)Application.Current.Resources["MaterialDesignRaisedButton"];
        addBtn.Click += AddItem_Click;

        Grid.SetColumn(_qty,    0);
        Grid.SetColumn(_cost,   2);
        Grid.SetColumn(addBtn,  4);
        qtyRow.Children.Add(_qty);
        qtyRow.Children.Add(_cost);
        qtyRow.Children.Add(addBtn);
        leftPanel.Children.Add(qtyRow);

        // جدول البنود
        _grid = new DataGrid
        {
            AutoGenerateColumns = false,
            CanUserAddRows      = false,
            MinHeight           = 150,
            Style               = (Style)Application.Current.Resources["MaterialDesignDataGrid"],
            ItemsSource         = _items
        };
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header  = "المنتج",
            Binding = new System.Windows.Data.Binding("ProductName"),
            Width   = new DataGridLength(1, DataGridLengthUnitType.Star)
        });
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header  = "كمية",
            Binding = new System.Windows.Data.Binding("Quantity"),
            Width   = new DataGridLength(55)
        });
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header  = "السعر",
            Binding = new System.Windows.Data.Binding("UnitCost") { StringFormat = "N2" },
            Width   = new DataGridLength(90)
        });
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header  = "الإجمالي",
            Binding = new System.Windows.Data.Binding("LineTotal") { StringFormat = "{0:N2} ج" },
            Width   = new DataGridLength(100)
        });
        _items.CollectionChanged += (_, _) => UpdateTotal();
        leftPanel.Children.Add(_grid);

        Grid.SetColumn(leftPanel, 0);
        mainGrid.Children.Add(leftPanel);

        // الجانب الأيمن: الدفع
        var rightPanel = new StackPanel();

        rightPanel.Children.Add(new TextBlock
        {
            Text = "بيانات الدفع", FontSize = 14, FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        });

        _totalText.FontSize   = 17;
        _totalText.FontWeight = FontWeights.Bold;
        _totalText.Text       = "الإجمالي: 0.00 ج";
        _totalText.Margin     = new Thickness(0, 0, 0, 10);
        rightPanel.Children.Add(_totalText);

        MaterialDesignThemes.Wpf.HintAssist.SetHint(_paid, "المبلغ المدفوع (جنيه)");
        _paid.Style  = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        _paid.Margin = new Thickness(0, 0, 0, 10);
        rightPanel.Children.Add(_paid);

        _payMethod.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedComboBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_payMethod, "طريقة الدفع");
        _payMethod.Items.Add(new ComboBoxItem { Content = "كاش",          IsSelected = true });
        _payMethod.Items.Add(new ComboBoxItem { Content = "فودافون كاش" });
        _payMethod.Items.Add(new ComboBoxItem { Content = "انستا باي"   });
        _payMethod.Items.Add(new ComboBoxItem { Content = "آجل"         });
        _payMethod.Margin = new Thickness(0, 0, 0, 10);
        rightPanel.Children.Add(_payMethod);

        _dueDate.Style  = (Style)Application.Current.Resources["MaterialDesignDatePicker"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_dueDate, "تاريخ الاستحقاق");
        _dueDate.Margin = new Thickness(0, 0, 0, 10);
        rightPanel.Children.Add(_dueDate);

        MaterialDesignThemes.Wpf.HintAssist.SetHint(_notes, "ملاحظات");
        _notes.Style        = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        _notes.Height       = 70;
        _notes.AcceptsReturn = true;
        _notes.TextWrapping = TextWrapping.Wrap;
        rightPanel.Children.Add(_notes);

        Grid.SetColumn(rightPanel, 1);
        mainGrid.Children.Add(rightPanel);

        dock.Children.Add(mainGrid);
        scroll.Content = dock;
        Content        = scroll;
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if (_productCombo.SelectedItem is not Product product) return;
        if (!int.TryParse(_qty.Text, out var qty) || qty <= 0) { MessageBox.Show("أدخل كمية صحيحة."); return; }
        decimal cost = ParseDecimal(_cost.Text);

        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing != null) existing.Quantity += qty;
        else _items.Add(new PurchaseCartItem
        {
            ProductId   = product.Id,
            ProductName = product.Name,
            Quantity    = qty,
            UnitCost    = cost
        });
        if (_grid != null) _grid.Items.Refresh();
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        _totalText.Text = $"الإجمالي: {_items.Sum(i => i.LineTotal):N2} ج";
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
        {
            MessageBox.Show("من فضلك أضف منتجاً واحداً على الأقل.", "تحقق",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        decimal paid = ParseDecimal(_paid.Text);
        var method   = _payMethod.SelectedIndex switch
        {
            1 => PaymentMethod.VodafoneCash,
            2 => PaymentMethod.InstaPay,
            3 => PaymentMethod.Credit,
            _ => PaymentMethod.Cash
        };
        try
        {
            var purchase = new Purchase
            {
                SupplierId    = _supplier.Id,
                TotalAmount   = _items.Sum(i => i.LineTotal),
                PaidAmount    = paid,
                PaymentMethod = method,
                DueDate       = _dueDate.SelectedDate,
                Notes         = _notes.Text,
                Items         = _items.Select(i => new PurchaseItem
                {
                    ProductId = i.ProductId,
                    Quantity  = i.Quantity,
                    UnitCost  = i.UnitCost,
                    TotalCost = i.LineTotal
                }).ToList()
            };
            await _purchaseService.CreatePurchaseAsync(purchase);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الحفظ:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public class PurchaseCartItem
{
    public int     ProductId   { get; set; }
    public string  ProductName { get; set; } = "";
    public int     Quantity    { get; set; }
    public decimal UnitCost    { get; set; }
    public decimal LineTotal   => Quantity * UnitCost;
}

// ══════════════════════════════════════════════════════════════
// نافذة إضافة دفعة للمورد
// ══════════════════════════════════════════════════════════════
public class SupplierPaymentDialog : Window
{
    private readonly Supplier        _supplier;
    private readonly PurchaseService _purchaseService;
    private readonly ComboBox        _purchaseCombo = new();
    private readonly TextBox         _amount        = new();
    private readonly TextBox         _notes         = new();
    private readonly ComboBox        _method        = new();

    public SupplierPaymentDialog(Supplier supplier)
    {
        _supplier        = supplier;
        _purchaseService = new PurchaseService(new AppDbContext());
        Title  = $"إضافة دفعة — {supplier.Name}";
        Width  = 400; Height = 380;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;
        ResizeMode = ResizeMode.NoResize;
        BuildUI();
        Loaded += async (_, _) => await LoadPurchasesAsync();
    }

    private async Task LoadPurchasesAsync()
    {
        try
        {
            var purchases = await _purchaseService.GetBySupplierAsync(_supplier.Id);
            var unpaid    = purchases.Where(p => p.RemainingAmount > 0).ToList();
            _purchaseCombo.ItemsSource       = unpaid;
            _purchaseCombo.DisplayMemberPath = "InvoiceNumber";
            _purchaseCombo.SelectedValuePath = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل الفواتير:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BuildUI()
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var panel = new StackPanel { Margin = new Thickness(24) };

        panel.Children.Add(new TextBlock
        {
            Text       = $"إضافة دفعة للمورد: {_supplier.Name}",
            FontSize   = 16, FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.DarkBlue,
            Margin     = new Thickness(0, 0, 0, 16)
        });

        _purchaseCombo.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedComboBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_purchaseCombo, "اختر الفاتورة *");
        _purchaseCombo.Margin = new Thickness(0, 0, 0, 12);

        MaterialDesignThemes.Wpf.HintAssist.SetHint(_amount, "المبلغ (جنيه) *");
        _amount.Style  = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        _amount.Margin = new Thickness(0, 0, 0, 12);

        _method.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedComboBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_method, "طريقة الدفع");
        _method.Items.Add(new ComboBoxItem { Content = "كاش",          IsSelected = true });
        _method.Items.Add(new ComboBoxItem { Content = "فودافون كاش" });
        _method.Items.Add(new ComboBoxItem { Content = "انستا باي"   });
        _method.Margin = new Thickness(0, 0, 0, 12);

        MaterialDesignThemes.Wpf.HintAssist.SetHint(_notes, "ملاحظات");
        _notes.Style  = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        _notes.Margin = new Thickness(0, 0, 0, 16);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        var save   = new Button { Content = "حفظ الدفعة", Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "إلغاء", IsCancel = true };
        save.Style   = (Style)Application.Current.Resources["MaterialDesignRaisedButton"];
        cancel.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedButton"];
        save.Click  += Save_Click;
        btnRow.Children.Add(save);
        btnRow.Children.Add(cancel);

        panel.Children.Add(_purchaseCombo);
        panel.Children.Add(_amount);
        panel.Children.Add(_method);
        panel.Children.Add(_notes);
        panel.Children.Add(btnRow);

        scroll.Content = panel;
        Content        = scroll;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_purchaseCombo.SelectedItem is not Purchase purchase)
        { MessageBox.Show("اختر الفاتورة.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        var text = _amount.Text.Replace(",", ".").Trim();
        if (!decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        { MessageBox.Show("أدخل مبلغاً صحيحاً.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        try
        {
            var method = _method.SelectedIndex switch
            {
                1 => PaymentMethod.VodafoneCash,
                2 => PaymentMethod.InstaPay,
                _ => PaymentMethod.Cash
            };
            await _purchaseService.AddPaymentAsync(new SupplierPayment
            {
                PurchaseId    = purchase.Id,
                Amount        = amount,
                PaymentMethod = method,
                Notes         = _notes.Text
            });
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الحفظ:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
