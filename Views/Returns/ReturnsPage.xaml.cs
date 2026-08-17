using NassefStore.Data;
using NassefStore.Data.Entities;
using NassefStore.Services;
using System.Windows;
using System.Windows.Controls;

namespace NassefStore.Views.Returns;

public partial class ReturnsPage : Page
{
    private readonly ReturnService _returnService;

    public ReturnsPage()
    {
        InitializeComponent();
        _returnService = new ReturnService(new AppDbContext());
        Loaded += async (_, _) => await LoadReturnsAsync();
    }

    private async Task LoadReturnsAsync()
    {
        try
        {
            var returns = await _returnService.GetAllAsync();
            ReturnsGrid.ItemsSource = returns;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل المرتجعات:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ReturnFromCustomer_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ReturnDialog(ReturnType.FromCustomer);
        if (dlg.ShowDialog() == true) await LoadReturnsAsync();
    }

    private async void ReturnToSupplier_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ReturnDialog(ReturnType.ToSupplier);
        if (dlg.ShowDialog() == true) await LoadReturnsAsync();
    }
}

// ══════════════════════════════════════════════════════════════
// نافذة المرتجع — عربي كامل + ScrollViewer
// ══════════════════════════════════════════════════════════════
public class ReturnDialog : Window
{
    private readonly ReturnType      _type;
    private readonly ReturnService   _returnService;
    private readonly ProductService  _productService;
    private readonly SaleService     _saleService;
    private readonly PurchaseService _purchaseService;

    private readonly ComboBox   _referenceCombo = new();
    private readonly ComboBox   _productCombo   = new();
    private readonly TextBox    _qty            = new();
    private readonly TextBox    _price          = new();
    private readonly CheckBox   _warrantyCheck  = new();
    private readonly DatePicker _warrantyExpiry = new();
    private readonly TextBox    _reason         = new();

    public ReturnDialog(ReturnType type)
    {
        _type            = type;
        _returnService   = new ReturnService(new AppDbContext());
        _productService  = new ProductService(new AppDbContext());
        _saleService     = new SaleService(new AppDbContext());
        _purchaseService = new PurchaseService(new AppDbContext());

        Title  = type == ReturnType.FromCustomer ? "مرتجع من عميل" : "مرتجع للمورد";
        Width  = 460; Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;

        BuildUI();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var products = await _productService.GetAllAsync();
            _productCombo.ItemsSource       = products;
            _productCombo.DisplayMemberPath = "Name";
            _productCombo.SelectedValuePath = "Id";

            if (_type == ReturnType.FromCustomer)
            {
                var sales = await _saleService.GetSalesByDateRangeAsync(
                    DateTime.Today.AddMonths(-12), DateTime.Today);
                _referenceCombo.ItemsSource       = sales;
                _referenceCombo.DisplayMemberPath = "InvoiceNumber";
                _referenceCombo.SelectedValuePath = "Id";
            }
            else
            {
                var purchases = await _purchaseService.GetAllAsync();
                _referenceCombo.ItemsSource       = purchases;
                _referenceCombo.DisplayMemberPath = "InvoiceNumber";
                _referenceCombo.SelectedValuePath = "Id";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل البيانات:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BuildUI()
    {
        // ScrollViewer يلف كل المحتوى
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var panel = new StackPanel { Margin = new Thickness(24) };

        // العنوان
        bool isCustomer = _type == ReturnType.FromCustomer;
        panel.Children.Add(new TextBlock
        {
            Text       = isCustomer ? "↩ مرتجع من عميل" : "↪ مرتجع للمورد",
            FontSize   = 18, FontWeight = FontWeights.Bold,
            Foreground = isCustomer
                ? System.Windows.Media.Brushes.DarkBlue
                : System.Windows.Media.Brushes.DarkOrange,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // الفاتورة الأصلية
        panel.Children.Add(new TextBlock
        {
            Text       = isCustomer ? "فاتورة البيع الأصلية" : "فاتورة الشراء الأصلية",
            FontSize   = 12, Foreground = System.Windows.Media.Brushes.Gray,
            Margin     = new Thickness(0, 0, 0, 4)
        });
        _referenceCombo.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedComboBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_referenceCombo, "اختر الفاتورة (اختياري)");
        _referenceCombo.Margin = new Thickness(0, 0, 0, 12);
        panel.Children.Add(_referenceCombo);

        // المنتج
        _productCombo.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedComboBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_productCombo, "المنتج *");
        _productCombo.Margin            = new Thickness(0, 0, 0, 12);
        _productCombo.SelectionChanged += ProductCombo_SelectionChanged;
        panel.Children.Add(_productCombo);

        // الكمية والسعر
        var qtyRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        qtyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        qtyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        qtyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _qty.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_qty, "الكمية *");
        _qty.Text = "1";

        _price.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_price, "السعر (جنيه)");

        Grid.SetColumn(_qty,   0);
        Grid.SetColumn(_price, 2);
        qtyRow.Children.Add(_qty);
        qtyRow.Children.Add(_price);
        panel.Children.Add(qtyRow);

        // الضمان (للمرتجع من عميل فقط)
        if (isCustomer)
        {
            _warrantyCheck.Content = "مطالبة بالضمان";
            _warrantyCheck.Margin  = new Thickness(0, 0, 0, 8);
            _warrantyCheck.Checked   += (_, _) => _warrantyExpiry.Visibility = Visibility.Visible;
            _warrantyCheck.Unchecked += (_, _) => _warrantyExpiry.Visibility = Visibility.Collapsed;
            panel.Children.Add(_warrantyCheck);

            _warrantyExpiry.Style      = (Style)Application.Current.Resources["MaterialDesignDatePicker"];
            _warrantyExpiry.Visibility = Visibility.Collapsed;
            _warrantyExpiry.Margin     = new Thickness(0, 0, 0, 12);
            MaterialDesignThemes.Wpf.HintAssist.SetHint(_warrantyExpiry, "تاريخ انتهاء الضمان");
            panel.Children.Add(_warrantyExpiry);
        }

        // سبب الإرجاع
        _reason.Style        = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_reason, "سبب الإرجاع");
        _reason.Height        = 65;
        _reason.AcceptsReturn = true;
        _reason.TextWrapping  = TextWrapping.Wrap;
        _reason.Margin        = new Thickness(0, 0, 0, 16);
        panel.Children.Add(_reason);

        // الأزرار
        var btnRow = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var save   = new Button { Content = "تنفيذ الإرجاع", Margin = new Thickness(0, 0, 8, 0) };
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

    private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_productCombo.SelectedItem is Product p)
            _price.Text = (_type == ReturnType.FromCustomer
                ? p.SellPrice
                : p.CostPrice).ToString("N2");
    }

    private static decimal ParseDecimal(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return 0;
        t = t.Replace(",", ".").Trim();
        return decimal.TryParse(t, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : 0;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_productCombo.SelectedItem is not Product product)
        { MessageBox.Show("اختر المنتج.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        if (!int.TryParse(_qty.Text, out var qty) || qty <= 0)
        { MessageBox.Show("أدخل كمية صحيحة.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        decimal price = ParseDecimal(_price.Text);

        try
        {
            var ret = new Return
            {
                ReturnType      = _type,
                ProductId       = product.Id,
                Quantity        = qty,
                UnitPrice       = price,
                TotalPrice      = price * qty,
                IsWarrantyClaim = _warrantyCheck.IsChecked == true,
                WarrantyExpiry  = _warrantyExpiry.SelectedDate,
                Reason          = _reason.Text,
                ReturnDate      = DateTime.Now
            };

            if (_type == ReturnType.FromCustomer && _referenceCombo.SelectedItem is Sale sale)
                ret.SaleId = sale.Id;
            else if (_type == ReturnType.ToSupplier && _referenceCombo.SelectedItem is Purchase purchase)
                ret.PurchaseId = purchase.Id;

            await _returnService.ProcessReturnAsync(ret);
            MessageBox.Show("تم تنفيذ الإرجاع بنجاح.", "تم",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
