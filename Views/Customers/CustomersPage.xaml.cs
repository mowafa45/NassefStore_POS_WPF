using NassefStore.Data;
using NassefStore.Data.Entities;
using NassefStore.Services;
using System.Windows;
using System.Windows.Controls;

namespace NassefStore.Views.Customers;

public partial class CustomersPage : Page
{
    private readonly CustomerService _customerService;
    private readonly SaleService     _saleService;
    private List<Customer> _allCustomers = new();
    private Customer? _selectedCustomer;

    public CustomersPage()
    {
        InitializeComponent();
        _customerService = new CustomerService(new AppDbContext());
        _saleService     = new SaleService(new AppDbContext());
        Loaded += async (_, _) => await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            _allCustomers = await _customerService.GetAllAsync();
            CustomersList.ItemsSource = null;
            CustomersList.ItemsSource = _allCustomers;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل العملاء:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Search_Changed(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text.ToLower();
        CustomersList.ItemsSource = _allCustomers.Where(c =>
            c.Name.ToLower().Contains(q) ||
            (c.Phone != null && c.Phone.Contains(q))).ToList();
    }

    private async void CustomersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CustomersList.SelectedItem is not Customer customer) return;
        _selectedCustomer = customer;
        await LoadStatementAsync(customer);
    }

    private async Task LoadStatementAsync(Customer customer)
    {
        try
        {
            var stmt = await _customerService.GetStatementAsync(customer.Id);
            CustomerName.Text = stmt.Customer.Name;
            CustomerPhone.Text = stmt.Customer.Phone ?? "";
            TxtTotal.Text   = $"{stmt.TotalPurchased:N2} ج";
            TxtPaid.Text    = $"{stmt.TotalPaid:N2} ج";
            TxtBalance.Text = $"{stmt.TotalRemaining:N2} ج";
            SalesGrid.ItemsSource = stmt.Sales;

            PlaceholderCard.Visibility = Visibility.Collapsed;
            InfoCard.Visibility        = Visibility.Visible;
            ActionsCard.Visibility     = Visibility.Visible;
            SalesCard.Visibility       = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل البيانات:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── إضافة عميل ───────────────────────────────────────────
    private async void AddCustomer_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new CustomerDialog(null);
        if (dlg.ShowDialog() == true) await LoadCustomersAsync();
    }

    // ── تعديل عميل ───────────────────────────────────────────
    private async void EditCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not Customer customer) return;

        // Reload fresh from DB to avoid stale data
        var db = new AppDbContext();
        var fresh = await db.Customers.FindAsync(customer.Id);
        if (fresh == null) return;

        var dlg = new CustomerDialog(fresh);
        if (dlg.ShowDialog() == true)
        {
            await LoadCustomersAsync();
            // Re-select if was viewing this customer
            if (_selectedCustomer?.Id == customer.Id)
                await LoadStatementAsync(fresh);
        }
    }

    // ── حذف عميل ─────────────────────────────────────────────
    private async void DeleteCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not Customer customer) return;

        var confirm = MessageBox.Show(
            $"هل تريد حذف العميل \"{customer.Name}\"؟\nلن يتم حذف الفواتير المرتبطة به.",
            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            await _customerService.DeleteAsync(customer.Id);
            if (_selectedCustomer?.Id == customer.Id)
            {
                _selectedCustomer = null;
                PlaceholderCard.Visibility = Visibility.Visible;
                InfoCard.Visibility        = Visibility.Collapsed;
                ActionsCard.Visibility     = Visibility.Collapsed;
                SalesCard.Visibility       = Visibility.Collapsed;
            }
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الحذف:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── دفعة آجل ─────────────────────────────────────────────
    private async void AddPayment_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCustomer == null) return;
        var dlg = new CustomerPaymentDialog(_selectedCustomer);
        if (dlg.ShowDialog() == true)
            await LoadStatementAsync(_selectedCustomer);
    }
}

// ── نافذة إضافة / تعديل عميل ────────────────────────────────
public class CustomerDialog : Window
{
    private readonly Customer? _existing;
    private readonly CustomerService _service;
    private readonly TextBox _name  = new();
    private readonly TextBox _phone = new();
    private readonly TextBox _addr  = new();
    private readonly TextBox _notes = new();

    public CustomerDialog(Customer? existing)
    {
        _existing = existing;
        _service  = new CustomerService(new AppDbContext());
        Title     = existing == null ? "إضافة عميل" : "تعديل عميل";
        Width = 400; Height = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;
        ResizeMode = ResizeMode.NoResize;
        BuildUI();
    }

    private void BuildUI()
    {
        var panel = new StackPanel { Margin = new Thickness(24) };

        // عنوان
        panel.Children.Add(new TextBlock
        {
            Text       = _existing == null ? "إضافة عميل جديد" : $"تعديل: {_existing.Name}",
            FontSize   = 18, FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.DarkBlue,
            Margin     = new Thickness(0, 0, 0, 16)
        });

        void AddField(string hint, TextBox box)
        {
            MaterialDesignThemes.Wpf.HintAssist.SetHint(box, hint);
            box.Style  = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
            box.Margin = new Thickness(0, 0, 0, 10);
            panel.Children.Add(box);
        }

        AddField("اسم العميل *", _name);
        AddField("رقم الهاتف",   _phone);
        AddField("العنوان",      _addr);
        AddField("ملاحظات",      _notes);

        if (_existing != null)
        {
            _name.Text  = _existing.Name;
            _phone.Text = _existing.Phone  ?? "";
            _addr.Text  = _existing.Address ?? "";
            _notes.Text = _existing.Notes  ?? "";
        }

        var btnRow = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var save   = new Button { Content = "حفظ", Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "إلغاء", IsCancel = true };
        save.Style   = (Style)Application.Current.Resources["MaterialDesignRaisedButton"];
        cancel.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedButton"];
        save.Click  += Save_Click;
        btnRow.Children.Add(save);
        btnRow.Children.Add(cancel);
        panel.Children.Add(btnRow);
        Content = panel;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            MessageBox.Show("اسم العميل مطلوب.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            if (_existing != null)
            {
                _existing.Name    = _name.Text.Trim();
                _existing.Phone   = _phone.Text.Trim();
                _existing.Address = _addr.Text.Trim();
                _existing.Notes   = _notes.Text.Trim();
                await _service.UpdateAsync(_existing);
            }
            else
            {
                await _service.AddAsync(new Customer
                {
                    Name    = _name.Text.Trim(),
                    Phone   = _phone.Text.Trim(),
                    Address = _addr.Text.Trim(),
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

// ── نافذة تسجيل دفعة آجل ────────────────────────────────────
public class CustomerPaymentDialog : Window
{
    private readonly Customer    _customer;
    private readonly SaleService _saleService;
    private readonly ComboBox    _saleCombo = new();
    private readonly TextBox     _amount    = new();
    private readonly ComboBox    _method    = new();

    public CustomerPaymentDialog(Customer customer)
    {
        _customer    = customer;
        _saleService = new SaleService(new AppDbContext());
        Title  = $"دفعة آجل — {customer.Name}";
        Width  = 380; Height = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;
        ResizeMode = ResizeMode.NoResize;
        BuildUI();
        Loaded += async (_, _) => await LoadSalesAsync();
    }

    private async Task LoadSalesAsync()
    {
        try
        {
            var sales = await _saleService.GetCreditSalesAsync();
            _saleCombo.ItemsSource        = sales.Where(s => s.CustomerId == _customer.Id).ToList();
            _saleCombo.DisplayMemberPath  = "InvoiceNumber";
            _saleCombo.SelectedValuePath  = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل الفواتير:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BuildUI()
    {
        var panel = new StackPanel { Margin = new Thickness(24) };

        _saleCombo.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedComboBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_saleCombo, "اختر الفاتورة *");
        _saleCombo.Margin = new Thickness(0, 0, 0, 10);

        MaterialDesignThemes.Wpf.HintAssist.SetHint(_amount, "المبلغ (جنيه) *");
        _amount.Style  = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        _amount.Margin = new Thickness(0, 0, 0, 10);

        _method.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedComboBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_method, "طريقة الدفع");
        _method.Items.Add(new ComboBoxItem { Content = "كاش",         IsSelected = true });
        _method.Items.Add(new ComboBoxItem { Content = "فودافون كاش" });
        _method.Items.Add(new ComboBoxItem { Content = "انستا باي"   });
        _method.Margin = new Thickness(0, 0, 0, 10);

        var btnRow = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var save   = new Button { Content = "حفظ الدفعة", Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "إلغاء", IsCancel = true };
        save.Style   = (Style)Application.Current.Resources["MaterialDesignRaisedButton"];
        cancel.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedButton"];
        save.Click  += Save_Click;
        btnRow.Children.Add(save);
        btnRow.Children.Add(cancel);

        panel.Children.Add(_saleCombo);
        panel.Children.Add(_amount);
        panel.Children.Add(_method);
        panel.Children.Add(btnRow);
        Content = panel;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_saleCombo.SelectedItem is not Sale sale)
        {
            MessageBox.Show("اختر الفاتورة.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var text = _amount.Text.Replace(",", ".").Trim();
        if (!decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            MessageBox.Show("أدخل مبلغاً صحيحاً.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var method = _method.SelectedIndex switch
            {
                1 => PaymentMethod.VodafoneCash,
                2 => PaymentMethod.InstaPay,
                _ => PaymentMethod.Cash
            };
            await _saleService.AddCreditPaymentAsync(new CreditPayment
            {
                SaleId        = sale.Id,
                Amount        = amount,
                PaymentMethod = method
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
