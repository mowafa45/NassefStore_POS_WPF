using NassefStore.Data;
using NassefStore.Data.Entities;
using NassefStore.Services;
using System.Windows;
using System.Windows.Controls;

namespace NassefStore.Views.Sales;

public partial class SalesHistoryPage : Page
{
    private readonly SaleService _saleService;
    private List<Sale> _allSales = new();
    private DateTime _from = DateTime.Today;
    private DateTime _to   = DateTime.Today;

    public SalesHistoryPage()
    {
        InitializeComponent();
        _saleService = new SaleService(new AppDbContext());
        DateFrom.SelectedDate = DateTime.Today;
        DateTo.SelectedDate   = DateTime.Today;
        Loaded += async (_, _) => await LoadSalesAsync(DateTime.Today, DateTime.Today);
    }

    private async Task LoadSalesAsync(DateTime from, DateTime to)
    {
        try
        {
            _from     = from;
            _to       = to;
            _allSales = await _saleService.GetSalesByDateRangeAsync(from, to);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل المبيعات:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allSales.AsEnumerable();

        var q = SearchBox.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(q))
            filtered = filtered.Where(s =>
                s.InvoiceNumber.ToLower().Contains(q) ||
                (s.Customer?.Name.ToLower().Contains(q) ?? false));

        if (CreditOnly.IsChecked == true)
            filtered = filtered.Where(s => s.IsCredit);

        var list = filtered.ToList();
        SalesGrid.ItemsSource = list;
        TxtCount.Text = $"{list.Count}";
        TxtTotal.Text = $"{list.Sum(s => s.NetAmount):N2} جنيه";
    }

    private void Search_Changed(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void CreditOnly_Changed(object sender, RoutedEventArgs e)  => ApplyFilter();

    private void Search_Click(object sender, RoutedEventArgs e)
    {
        if (DateFrom.SelectedDate.HasValue && DateTo.SelectedDate.HasValue)
            _ = LoadSalesAsync(DateFrom.SelectedDate.Value, DateTo.SelectedDate.Value);
    }

    private void Today_Click(object sender, RoutedEventArgs e)
    {
        DateFrom.SelectedDate = DateTo.SelectedDate = DateTime.Today;
        _ = LoadSalesAsync(DateTime.Today, DateTime.Today);
    }

    private void SalesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    // ── طباعة ────────────────────────────────────────────────
    private async void PrintInvoice_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not Sale sale) return;
        try
        {
            var full = await _saleService.GetByIdAsync(sale.Id);
            if (full != null) new InvoiceWindow(full).ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── سداد الآجل ───────────────────────────────────────────
    private async void PayCredit_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not Sale sale) return;

        if (!sale.IsCredit || sale.RemainingAmount <= 0)
        {
            MessageBox.Show("هذه الفاتورة مسددة بالكامل.", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var full = await _saleService.GetByIdAsync(sale.Id);
        if (full == null) return;

        var dlg = new PayCreditDialog(full);
        if (dlg.ShowDialog() == true)
            await LoadSalesAsync(_from, _to);
    }

    // ── تعديل الفاتورة ───────────────────────────────────────
    private async void EditSale_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not Sale sale) return;

        var full = await _saleService.GetByIdAsync(sale.Id);
        if (full == null) return;

        var dlg = new EditSaleDialog(full);
        if (dlg.ShowDialog() == true)
            await LoadSalesAsync(_from, _to);
    }

    // ── حذف الفاتورة ─────────────────────────────────────────
    private async void DeleteSale_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not Sale sale) return;

        var confirm = MessageBox.Show(
            $"هل تريد حذف الفاتورة {sale.InvoiceNumber}؟\nسيتم إلغاؤها ولن تُحذف من السجل.",
            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            await _saleService.CancelSaleAsync(sale.Id);
            await LoadSalesAsync(_from, _to);
            MessageBox.Show("تم إلغاء الفاتورة بنجاح.", "تم",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

// ══════════════════════════════════════════════════════════════
// نافذة سداد الآجل
// ══════════════════════════════════════════════════════════════
public class PayCreditDialog : Window
{
    private readonly Sale        _sale;
    private readonly SaleService _saleService;
    private readonly TextBox     _amount = new();
    private readonly ComboBox    _method = new();
    private readonly TextBox     _notes  = new();

    public PayCreditDialog(Sale sale)
    {
        _sale        = sale;
        _saleService = new SaleService(new AppDbContext());
        Title  = $"سداد آجل — {sale.InvoiceNumber}";
        Width  = 400; Height = 380;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;
        ResizeMode = ResizeMode.NoResize;
        BuildUI();
    }

    private void BuildUI()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var panel  = new StackPanel { Margin = new Thickness(24) };

        // معلومات الفاتورة
        panel.Children.Add(new TextBlock
        {
            Text       = "سداد مبلغ آجل",
            FontSize   = 18, FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.DarkGreen,
            Margin     = new Thickness(0, 0, 0, 12)
        });

        // بطاقة معلومات
        var infoCard = new Border
        {
            Background   = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF3E0")),
            CornerRadius = new CornerRadius(8),
            Padding      = new Thickness(12),
            Margin       = new Thickness(0, 0, 0, 16)
        };
        var infoPanel = new StackPanel();
        infoPanel.Children.Add(new TextBlock
        {
            Text = $"رقم الفاتورة: {_sale.InvoiceNumber}",
            FontWeight = FontWeights.SemiBold
        });
        infoPanel.Children.Add(new TextBlock
        {
            Text = $"إجمالي الفاتورة: {_sale.NetAmount:N2} جنيه",
            Foreground = System.Windows.Media.Brushes.Gray, FontSize = 12
        });
        infoPanel.Children.Add(new TextBlock
        {
            Text       = $"المتبقي: {_sale.RemainingAmount:N2} جنيه",
            Foreground = System.Windows.Media.Brushes.OrangeRed,
            FontWeight = FontWeights.Bold, FontSize = 15
        });
        infoCard.Child = infoPanel;
        panel.Children.Add(infoCard);

        // حقل المبلغ
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_amount, $"المبلغ المدفوع (الحد الأقصى {_sale.RemainingAmount:N2})");
        _amount.Style  = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        _amount.Margin = new Thickness(0, 0, 0, 12);
        // اقتراح المبلغ الكامل
        _amount.Text   = _sale.RemainingAmount.ToString("N2");
        panel.Children.Add(_amount);

        // طريقة الدفع
        _method.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedComboBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_method, "طريقة الدفع");
        _method.Items.Add(new ComboBoxItem { Content = "كاش",          IsSelected = true });
        _method.Items.Add(new ComboBoxItem { Content = "فودافون كاش" });
        _method.Items.Add(new ComboBoxItem { Content = "انستا باي"   });
        _method.Margin = new Thickness(0, 0, 0, 12);
        panel.Children.Add(_method);

        // ملاحظات
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_notes, "ملاحظات (اختياري)");
        _notes.Style  = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        _notes.Margin = new Thickness(0, 0, 0, 16);
        panel.Children.Add(_notes);

        // أزرار
        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        var save   = new Button { Content = "تأكيد السداد", Margin = new Thickness(0, 0, 8, 0) };
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

    private static decimal ParseDecimal(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return 0;
        t = t.Replace(",", ".").Trim();
        return decimal.TryParse(t, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : 0;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        decimal amount = ParseDecimal(_amount.Text);

        if (amount <= 0)
        {
            MessageBox.Show("أدخل مبلغاً صحيحاً.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (amount > _sale.RemainingAmount)
        {
            MessageBox.Show($"المبلغ أكبر من المتبقي ({_sale.RemainingAmount:N2} جنيه).",
                "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                SaleId        = _sale.Id,
                Amount        = amount,
                PaymentMethod = method,
                Notes         = _notes.Text
            });

            MessageBox.Show(
                amount >= _sale.RemainingAmount
                    ? "✓ تم السداد الكامل للفاتورة."
                    : $"✓ تم تسجيل الدفعة.\nالمتبقي: {_sale.RemainingAmount - amount:N2} جنيه",
                "تم", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

// ══════════════════════════════════════════════════════════════
// نافذة تعديل الفاتورة
// ══════════════════════════════════════════════════════════════
public class EditSaleDialog : Window
{
    private readonly Sale        _sale;
    private readonly SaleService _saleService;
    private readonly TextBox     _discount = new();
    private readonly TextBox     _paid     = new();
    private readonly TextBox     _notes    = new();
    private readonly ComboBox    _method   = new();
    private readonly DatePicker  _dueDate  = new();
    private readonly TextBlock   _remaining = new();

    public EditSaleDialog(Sale sale)
    {
        _sale        = sale;
        _saleService = new SaleService(new AppDbContext());
        Title  = $"تعديل فاتورة — {sale.InvoiceNumber}";
        Width  = 460; Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;
        BuildUI();
    }

    private void BuildUI()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var panel  = new StackPanel { Margin = new Thickness(24) };

        panel.Children.Add(new TextBlock
        {
            Text       = $"تعديل الفاتورة: {_sale.InvoiceNumber}",
            FontSize   = 18, FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.DarkBlue,
            Margin     = new Thickness(0, 0, 0, 12)
        });

        // معلومات ثابتة
        var infoCard = new Border
        {
            Background   = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E3F2FD")),
            CornerRadius = new CornerRadius(8),
            Padding      = new Thickness(12),
            Margin       = new Thickness(0, 0, 0, 16)
        };
        var infoPanel = new StackPanel();
        infoPanel.Children.Add(new TextBlock
        {
            Text = $"التاريخ: {_sale.SaleDate:dd/MM/yyyy HH:mm}",
            FontSize = 12, Foreground = System.Windows.Media.Brushes.Gray
        });
        infoPanel.Children.Add(new TextBlock
        {
            Text       = $"إجمالي البنود: {_sale.TotalAmount:N2} جنيه",
            FontWeight = FontWeights.Bold
        });
        infoCard.Child = infoPanel;
        panel.Children.Add(infoCard);

        // الخصم
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_discount, "الخصم (جنيه)");
        _discount.Style  = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        _discount.Text   = _sale.DiscountAmount.ToString("N2");
        _discount.Margin = new Thickness(0, 0, 0, 12);
        _discount.TextChanged += (_, _) => UpdateRemaining();
        panel.Children.Add(new TextBlock { Text = "الخصم", FontSize = 12, Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0,0,0,4) });
        panel.Children.Add(_discount);

        // المبلغ المدفوع
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_paid, "المبلغ المدفوع (جنيه)");
        _paid.Style  = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        _paid.Text   = _sale.PaidAmount.ToString("N2");
        _paid.Margin = new Thickness(0, 0, 0, 12);
        _paid.TextChanged += (_, _) => UpdateRemaining();
        panel.Children.Add(new TextBlock { Text = "المبلغ المدفوع", FontSize = 12, Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0,0,0,4) });
        panel.Children.Add(_paid);

        // المتبقي (قراءة فقط)
        _remaining.FontSize   = 15;
        _remaining.FontWeight = FontWeights.Bold;
        _remaining.Foreground = System.Windows.Media.Brushes.OrangeRed;
        _remaining.Margin     = new Thickness(0, 0, 0, 12);
        UpdateRemaining();
        panel.Children.Add(_remaining);

        // طريقة الدفع
        _method.Style = (Style)Application.Current.Resources["MaterialDesignOutlinedComboBox"];
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_method, "طريقة الدفع");
        _method.Items.Add(new ComboBoxItem { Content = "كاش"          });
        _method.Items.Add(new ComboBoxItem { Content = "فودافون كاش" });
        _method.Items.Add(new ComboBoxItem { Content = "انستا باي"   });
        _method.Items.Add(new ComboBoxItem { Content = "آجل"         });
        _method.SelectedIndex = _sale.PaymentMethod switch
        {
            PaymentMethod.VodafoneCash => 1,
            PaymentMethod.InstaPay     => 2,
            PaymentMethod.Credit       => 3,
            _                          => 0
        };
        _method.Margin = new Thickness(0, 0, 0, 12);
        panel.Children.Add(new TextBlock { Text = "طريقة الدفع", FontSize = 12, Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0,0,0,4) });
        panel.Children.Add(_method);

        // تاريخ الاستحقاق
        _dueDate.Style        = (Style)Application.Current.Resources["MaterialDesignDatePicker"];
        _dueDate.SelectedDate = _sale.DueDate;
        _dueDate.Margin       = new Thickness(0, 0, 0, 12);
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_dueDate, "تاريخ الاستحقاق (للآجل)");
        panel.Children.Add(new TextBlock { Text = "تاريخ الاستحقاق", FontSize = 12, Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0,0,0,4) });
        panel.Children.Add(_dueDate);

        // ملاحظات
        MaterialDesignThemes.Wpf.HintAssist.SetHint(_notes, "ملاحظات");
        _notes.Style        = (Style)Application.Current.Resources["MaterialDesignOutlinedTextBox"];
        _notes.Text         = _sale.Notes ?? "";
        _notes.Height       = 60;
        _notes.AcceptsReturn = true;
        _notes.TextWrapping = TextWrapping.Wrap;
        _notes.Margin       = new Thickness(0, 0, 0, 16);
        panel.Children.Add(new TextBlock { Text = "ملاحظات", FontSize = 12, Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0,0,0,4) });
        panel.Children.Add(_notes);

        // أزرار
        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        var save   = new Button { Content = "حفظ التعديلات", Margin = new Thickness(0, 0, 8, 0) };
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

    private static decimal ParseDecimal(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return 0;
        t = t.Replace(",", ".").Trim();
        return decimal.TryParse(t, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : 0;
    }

    private void UpdateRemaining()
    {
        decimal disc      = ParseDecimal(_discount.Text);
        decimal paid      = ParseDecimal(_paid.Text);
        decimal net       = _sale.TotalAmount - disc;
        decimal remaining = Math.Max(0, net - paid);
        _remaining.Text   = $"المتبقي: {remaining:N2} جنيه";
        _remaining.Foreground = remaining > 0
            ? System.Windows.Media.Brushes.OrangeRed
            : System.Windows.Media.Brushes.Green;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            decimal discount = ParseDecimal(_discount.Text);
            decimal paid     = ParseDecimal(_paid.Text);
            decimal net      = _sale.TotalAmount - discount;
            decimal remaining = Math.Max(0, net - paid);

            var method = _method.SelectedIndex switch
            {
                1 => PaymentMethod.VodafoneCash,
                2 => PaymentMethod.InstaPay,
                3 => PaymentMethod.Credit,
                _ => PaymentMethod.Cash
            };

            _sale.DiscountAmount  = discount;
            _sale.NetAmount       = net;
            _sale.PaidAmount      = paid;
            _sale.RemainingAmount = remaining;
            _sale.IsCredit        = remaining > 0;
            _sale.PaymentMethod   = method;
            _sale.DueDate         = _dueDate.SelectedDate;
            _sale.Notes           = _notes.Text;

            await _saleService.UpdateSaleAsync(_sale);

            MessageBox.Show("تم حفظ التعديلات بنجاح.", "تم",
                MessageBoxButton.OK, MessageBoxImage.Information);
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
