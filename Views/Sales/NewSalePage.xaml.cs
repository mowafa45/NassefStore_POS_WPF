using NassefStore.Data;
using NassefStore.Data.Entities;
using NassefStore.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NassefStore.Views.Sales;

public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal Discount { get; set; } = 0;
    public decimal LineTotal => (UnitPrice * Quantity) - Discount;
    public int MaxStock { get; set; }
}

public partial class NewSalePage : Page
{
    private readonly ProductService  _productService;
    private readonly SaleService     _saleService;
    private readonly CustomerService _customerService;
    private readonly ObservableCollection<CartItem> _cart = new();
    private List<Product> _searchResults = new();

    public NewSalePage()
    {
        InitializeComponent();
        _productService  = new ProductService(new AppDbContext());
        _saleService     = new SaleService(new AppDbContext());
        _customerService = new CustomerService(new AppDbContext());
        CartGrid.ItemsSource = _cart;
        _cart.CollectionChanged += (_, _) => UpdateTotals();
        Loaded += async (_, _) => await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var customers = await _customerService.GetAllAsync();
            CustomerCombo.ItemsSource = customers;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل العملاء:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static decimal ParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Replace(",", ".").Trim();
        return decimal.TryParse(text,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var r) ? r : 0;
    }

    // ══ البحث ══════════════════════════════════════════════════
    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            var query = SearchBox.Text.Trim();
            if (query.Length < 1) { SearchPopup.IsOpen = false; return; }
            _searchResults = await _productService.SearchAsync(query);
            SearchResults.ItemsSource = _searchResults.Count > 0 ? _searchResults : null;
            SearchPopup.IsOpen = _searchResults.Count > 0;
        }
        catch { SearchPopup.IsOpen = false; }
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && SearchPopup.IsOpen) SearchResults.Focus();
        if (e.Key == Key.Enter) AddSelectedProduct();
    }

    private void SearchResult_DoubleClick(object sender, MouseButtonEventArgs e)
        => AddSelectedProduct();

    private void AddProduct_Click(object sender, RoutedEventArgs e)
        => AddSelectedProduct();

    private void AddSelectedProduct()
    {
        Product? product = null;
        if (SearchResults.SelectedItem is Product sel) product = sel;
        else if (_searchResults.Count >= 1)            product = _searchResults[0];
        if (product == null) return;

        var existing = _cart.FirstOrDefault(c => c.ProductId == product.Id);
        if (existing != null)
        {
            existing.Quantity++;
            CartGrid.Items.Refresh();
        }
        else
        {
            _cart.Add(new CartItem
            {
                ProductId   = product.Id,
                ProductName = product.Name,
                UnitPrice   = product.SellPrice,
                MaxStock    = product.StockQuantity
            });
        }
        SearchBox.Clear();
        SearchPopup.IsOpen = false;
        UpdateTotals();
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is CartItem item)
        {
            _cart.Remove(item);
            UpdateTotals();
        }
    }

    // ══ FIX: منع crash الـ Enter في الـ DataGrid ══════════════
    // الحل: نمنع Enter من الوصول للـ DataGrid خالص
    // ونستخدم TextBox مخصص للكمية بدل تعديل الـ DataGrid مباشرة
    private void CartGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            e.Handled = true; // امنع الـ DataGrid من معالجة Enter

            try
            {
                // انهِ التعديل الحالي بدون exception
                var cell = CartGrid.CurrentCell;
                if (CartGrid.IsReadOnly) return;

                // انتقل للخلية التالية بدل Enter
                CartGrid.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
            catch { /* تجاهل أي خطأ */ }

            // حدّث الإجماليات بعد تأخير بسيط
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    try { CartGrid.Items.Refresh(); UpdateTotals(); }
                    catch { }
                }));
        }
    }

    private void CartGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;

        // تحديث الإجماليات بعد انتهاء التعديل
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
            new Action(() =>
            {
                try { CartGrid.Items.Refresh(); UpdateTotals(); }
                catch { }
            }));
    }

    // ══ الإجماليات ════════════════════════════════════════════
    private void UpdateTotals()
    {
        try
        {
            decimal subtotal = _cart.Sum(c => c.LineTotal);
            decimal discount = ParseDecimal(TxtDiscount.Text);
            decimal paid     = ParseDecimal(TxtPaid.Text);
            decimal net      = subtotal - discount;

            TxtSubtotal.Text  = $"{subtotal:N2} جنيه";
            TxtNetTotal.Text  = $"{net:N2} جنيه";
            TxtRemaining.Text = $"{Math.Max(0, net - paid):N2} جنيه";
        }
        catch { }
    }

    private void Discount_Changed(object sender, TextChangedEventArgs e) => UpdateTotals();
    private void Paid_Changed(object sender, TextChangedEventArgs e)     => UpdateTotals();

    private void PaymentMethod_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (DueDatePanel == null) return;
        bool isCredit = PaymentMethodCombo.SelectedIndex == 3;
        DueDatePanel.Visibility = isCredit ? Visibility.Visible : Visibility.Collapsed;
        if (!isCredit)
        {
            decimal net = _cart.Sum(c => c.LineTotal) - ParseDecimal(TxtDiscount.Text);
            TxtPaid.Text = net.ToString("N2");
        }
        else TxtPaid.Text = "0";
        UpdateTotals();
    }

    // ══ إتمام البيع ═══════════════════════════════════════════
    private async void CompleteSale_Click(object sender, RoutedEventArgs e)
    {
        if (_cart.Count == 0)
        {
            MessageBox.Show("من فضلك أضف منتجاً واحداً على الأقل.", "السلة فارغة",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // أنهِ أي تعديل مفتوح بأمان
        try
        {
            CartGrid.CancelEdit(DataGridEditingUnit.Cell);
            CartGrid.CancelEdit(DataGridEditingUnit.Row);
        }
        catch { }

        decimal discount = ParseDecimal(TxtDiscount.Text);
        decimal paid     = ParseDecimal(TxtPaid.Text);
        decimal subtotal = _cart.Sum(c => c.LineTotal);

        try
        {
            var paymentMethod = PaymentMethodCombo.SelectedIndex switch
            {
                1 => PaymentMethod.VodafoneCash,
                2 => PaymentMethod.InstaPay,
                3 => PaymentMethod.Credit,
                _ => PaymentMethod.Cash
            };

            int? customerId = null;
            if (CustomerCombo.SelectedItem is Customer c2) customerId = c2.Id;

            var sale = new Sale
            {
                SaleDate       = DateTime.Now,
                TotalAmount    = subtotal,
                DiscountAmount = discount,
                PaidAmount     = paid,
                PaymentMethod  = paymentMethod,
                DueDate        = DueDatePicker.SelectedDate,
                Notes          = TxtNotes.Text,
                CustomerId     = customerId,
                Items          = _cart.Select(c => new SaleItem
                {
                    ProductId  = c.ProductId,
                    Quantity   = c.Quantity,
                    UnitPrice  = c.UnitPrice,
                    Discount   = c.Discount,
                    TotalPrice = c.LineTotal
                }).ToList()
            };

            var saved = await _saleService.CreateSaleAsync(sale);

            var result = MessageBox.Show(
                $"تم البيع بنجاح!\nرقم الفاتورة: {saved.InvoiceNumber}\nالإجمالي: {saved.NetAmount:N2} جنيه\n\nهل تريد طباعة الفاتورة؟",
                "تم البيع", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                var fullSale = await _saleService.GetByIdAsync(saved.Id);
                if (fullSale != null) new InvoiceWindow(fullSale).ShowDialog();
            }

            Clear_Click(sender, e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ الفاتورة:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        try { CartGrid.CancelEdit(); } catch { }
        _cart.Clear();
        SearchBox.Clear();
        TxtDiscount.Text = "";
        TxtPaid.Text     = "";
        TxtNotes.Text    = "";
        CustomerCombo.SelectedIndex      = -1;
        PaymentMethodCombo.SelectedIndex = 0;
        UpdateTotals();
    }
}
