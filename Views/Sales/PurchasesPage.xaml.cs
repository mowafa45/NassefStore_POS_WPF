using NassefStore.Data;
using NassefStore.Data.Entities;
using NassefStore.Services;
using System.Windows;
using System.Windows.Controls;

namespace NassefStore.Views.Sales;

public partial class PurchasesPage : Page
{
    private readonly PurchaseService _purchaseService;
    private List<Purchase> _allPurchases = new();

    public PurchasesPage()
    {
        InitializeComponent();
        _purchaseService = new PurchaseService(new AppDbContext());
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateFrom.SelectedDate = start;
        DateTo.SelectedDate   = DateTime.Today;
        Loaded += async (_, _) => await LoadAsync(start, DateTime.Today);
    }

    private async Task LoadAsync(DateTime from, DateTime to)
    {
        try
        {
            var all = await _purchaseService.GetAllAsync();
            _allPurchases = all
                .Where(p => p.PurchaseDate.Date >= from.Date && p.PurchaseDate.Date <= to.Date)
                .ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading purchases:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allPurchases.AsEnumerable();
        if (UnpaidOnly.IsChecked == true)
            filtered = filtered.Where(p => p.RemainingAmount > 0);

        var list = filtered.ToList();
        PurchasesGrid.ItemsSource = list;
        TxtCount.Text = list.Count.ToString();
        TxtTotal.Text = $"EGP {list.Sum(p => p.TotalAmount):N2}";
    }

    private void Search_Click(object sender, RoutedEventArgs e)
    {
        if (DateFrom.SelectedDate.HasValue && DateTo.SelectedDate.HasValue)
            _ = LoadAsync(DateFrom.SelectedDate.Value, DateTo.SelectedDate.Value);
    }

    private void ThisMonth_Click(object sender, RoutedEventArgs e)
    {
        var from = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateFrom.SelectedDate = from;
        DateTo.SelectedDate   = DateTime.Today;
        _ = LoadAsync(from, DateTime.Today);
    }

    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();
}
