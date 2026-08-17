using NassefStore.Data;
using NassefStore.Services;
using NassefStore.Views.Dashboard;
using NassefStore.Views.Sales;
using NassefStore.Views.Products;
using NassefStore.Views.Suppliers;
using NassefStore.Views.Customers;
using NassefStore.Views.Reports;
using NassefStore.Views.Returns;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NassefStore.Views;

public partial class MainWindow : Window
{
    private readonly ProductService _productService;
    private Button? _activeButton;

    public MainWindow()
    {
        InitializeComponent();
        _productService = new ProductService(new AppDbContext());
        CurrentDateText.Text = DateTime.Now.ToString("dddd، dd MMMM yyyy",
            new System.Globalization.CultureInfo("ar-EG"));
        Loaded += async (_, _) =>
        {
            NavigateTo(new DashboardPage(), BtnDashboard, "لوحة التحكم");
            await CheckLowStockAsync();
        };
    }

    private async Task CheckLowStockAsync()
    {
        try
        {
            var lowStock = await _productService.GetLowStockAsync();
            if (lowStock.Count > 0)
            {
                LowStockAlert.Visibility = Visibility.Visible;
                LowStockCount.Text = lowStock.Count.ToString();
            }
        }
        catch { }
    }

    private void NavigateTo(Page page, Button btn, string title)
    {
        MainFrame.Navigate(page);
        PageTitleBar.Text = title;

        if (_activeButton != null)
            _activeButton.Background = Brushes.Transparent;

        btn.Background = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
        _activeButton = btn;
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e)    => NavigateTo(new DashboardPage(),             BtnDashboard,    "لوحة التحكم");
    private void NavSales_Click(object sender, RoutedEventArgs e)        => NavigateTo(new NewSalePage(),               BtnSales,        "فاتورة بيع جديدة");
    private void NavSalesHistory_Click(object sender, RoutedEventArgs e) => NavigateTo(new SalesHistoryPage(),          BtnSalesHistory, "سجل المبيعات");
    private void NavProducts_Click(object sender, RoutedEventArgs e)     => NavigateTo(new ProductsPage(),              BtnProducts,     "المنتجات والمخزون");
    private void NavPurchases_Click(object sender, RoutedEventArgs e)    => NavigateTo(new PurchasesPage(),             BtnPurchases,    "المشتريات");
    private void NavSuppliers_Click(object sender, RoutedEventArgs e)    => NavigateTo(new SuppliersPage(),             BtnSuppliers,    "الموردون");
    private void NavCustomers_Click(object sender, RoutedEventArgs e)    => NavigateTo(new CustomersPage(),             BtnCustomers,    "العملاء");
    private void NavReturns_Click(object sender, RoutedEventArgs e)      => NavigateTo(new ReturnsPage(),               BtnReturns,      "المرتجعات");
    private void NavReports_Click(object sender, RoutedEventArgs e)      => NavigateTo(new ReportsPage(),               BtnReports,      "التقارير");

    private void LowStockAlert_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => NavigateTo(new ProductsPage(showLowStockOnly: true), BtnProducts, "تنبيه النواقص");
}
