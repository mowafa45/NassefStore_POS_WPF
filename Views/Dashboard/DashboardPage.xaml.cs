using NassefStore.Data;
using NassefStore.Services;
using System.Windows;
using System.Windows.Controls;

namespace NassefStore.Views.Dashboard;

public partial class DashboardPage : Page
{
    private readonly SaleService _saleService;
    private readonly ProductService _productService;

    public DashboardPage()
    {
        InitializeComponent();
        _saleService = new SaleService(new AppDbContext());
        _productService = new ProductService(new AppDbContext());
        DashDate.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await LoadDataAsync(DateTime.Today);
    }

    private async Task LoadDataAsync(DateTime date)
    {
        try
        {
            var summary = await _saleService.GetDailySummaryAsync(date);
            var sales    = await _saleService.GetSalesByDateAsync(date);
            var lowStock = await _productService.GetLowStockAsync();

            TxtTotalSales.Text   = $"EGP {summary.TotalSales:N2}";
            TxtInvoiceCount.Text = $"{summary.InvoiceCount} invoices";
            TxtCash.Text         = $"EGP {summary.CashSales:N2}";
            TxtVodafone.Text     = $"Vodafone: EGP {summary.VodafoneCash:N2}";
            TxtInstaPay.Text     = $"InstaPay: EGP {summary.InstaPay:N2}";
            TxtCredit.Text       = $"EGP {summary.CreditSales:N2}";
            TxtLowStock.Text     = lowStock.Count.ToString();
            RecentSalesGrid.ItemsSource = sales;

            if (lowStock.Count == 0)
            {
                LowStockList.Visibility   = Visibility.Collapsed;
                NoLowStockText.Visibility = Visibility.Visible;
            }
            else
            {
                LowStockList.ItemsSource  = lowStock;
                LowStockList.Visibility   = Visibility.Visible;
                NoLowStockText.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dashboard error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (DashDate.SelectedDate.HasValue)
            _ = LoadDataAsync(DashDate.SelectedDate.Value);
    }
}
