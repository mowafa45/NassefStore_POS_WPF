using NassefStore.Data;
using NassefStore.Services;
using System.Windows;
using System.Windows.Controls;

namespace NassefStore.Views.Reports;

public partial class ReportsPage : Page
{
    private readonly ReportService _reportService;

    public ReportsPage()
    {
        InitializeComponent();
        _reportService = new ReportService(new AppDbContext());
        DateFrom.SelectedDate = DateTime.Today;
        DateTo.SelectedDate   = DateTime.Today;
    }

    private async Task LoadReportAsync(DateTime from, DateTime to)
    {
        try
        {
            var report = await _reportService.GetSalesReportAsync(from, to);

            TxtRevenue.Text      = $"EGP {report.TotalRevenue:N2}";
            TxtCash.Text         = $"EGP {report.TotalCash:N2}";
            TxtVodafone.Text     = $"EGP {report.TotalVodafone:N2}";
            TxtInstaPay.Text     = $"EGP {report.TotalInstaPay:N2}";
            TxtCredit.Text       = $"EGP {report.TotalCredit:N2}";
            TxtInvoiceCount.Text = $"{report.InvoiceCount} invoices";
            SalesGrid.ItemsSource      = report.Sales;
            TopProductsList.ItemsSource = report.TopProducts;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating report:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (!DateFrom.SelectedDate.HasValue || !DateTo.SelectedDate.HasValue)
        { MessageBox.Show("Please select a date range.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        _ = LoadReportAsync(DateFrom.SelectedDate.Value, DateTo.SelectedDate.Value);
    }

    private void Today_Click(object sender, RoutedEventArgs e)
    {
        DateFrom.SelectedDate = DateTo.SelectedDate = DateTime.Today;
        _ = LoadReportAsync(DateTime.Today, DateTime.Today);
    }

    private void ThisMonth_Click(object sender, RoutedEventArgs e)
    {
        var from = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateFrom.SelectedDate = from;
        DateTo.SelectedDate   = DateTime.Today;
        _ = LoadReportAsync(from, DateTime.Today);
    }
}
