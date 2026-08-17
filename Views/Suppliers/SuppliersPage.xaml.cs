using NassefStore.Data;
using NassefStore.Data.Entities;
using NassefStore.Services;
using System.Windows;
using System.Windows.Controls;

namespace NassefStore.Views.Suppliers;

public partial class SuppliersPage : Page
{
    private readonly SupplierService _supplierService;
    private readonly PurchaseService _purchaseService;
    private List<Supplier> _allSuppliers = new();
    private Supplier? _selectedSupplier;

    public SuppliersPage()
    {
        InitializeComponent();
        _supplierService = new SupplierService(new AppDbContext());
        _purchaseService = new PurchaseService(new AppDbContext());
        DateFrom.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateTo.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await LoadSuppliersAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        try
        {
            _allSuppliers = await _supplierService.GetAllAsync();
            SuppliersList.ItemsSource = _allSuppliers;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading suppliers:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Search_Changed(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text.ToLower();
        SuppliersList.ItemsSource = _allSuppliers.Where(s =>
            s.Name.ToLower().Contains(q) || (s.Phone != null && s.Phone.Contains(q))).ToList();
    }

    private async void SuppliersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SuppliersList.SelectedItem is not Supplier supplier) return;
        _selectedSupplier = supplier;
        await LoadStatementAsync(supplier.Id);
    }

    private async Task LoadStatementAsync(int supplierId)
    {
        try
        {
            var stmt = await _supplierService.GetStatementAsync(supplierId, DateFrom.SelectedDate, DateTo.SelectedDate);

            SupplierName.Text      = stmt.Supplier.Name;
            SupplierPhone.Text     = stmt.Supplier.Phone ?? "";
            TxtTotalPurchased.Text = $"EGP {stmt.TotalPurchased:N2}";
            TxtTotalPaid.Text      = $"EGP {stmt.TotalPaid:N2}";
            TxtBalanceDue.Text     = $"EGP {stmt.TotalRemaining:N2}";
            PurchasesGrid.ItemsSource = stmt.Purchases;

            PlaceholderCard.Visibility  = Visibility.Collapsed;
            SupplierInfoCard.Visibility = Visibility.Visible;
            ActionsCard.Visibility      = Visibility.Visible;
            PurchasesCard.Visibility    = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading statement:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSupplier != null) _ = LoadStatementAsync(_selectedSupplier.Id);
    }

    private async void AddSupplier_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SupplierDialog(null);
        if (dlg.ShowDialog() == true) await LoadSuppliersAsync();
    }

    private async void NewPurchase_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSupplier == null) return;
        var dlg = new PurchaseDialog(_selectedSupplier);
        if (dlg.ShowDialog() == true) await LoadStatementAsync(_selectedSupplier.Id);
    }

    private async void AddPayment_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSupplier == null) return;
        var dlg = new SupplierPaymentDialog(_selectedSupplier);
        if (dlg.ShowDialog() == true) await LoadStatementAsync(_selectedSupplier.Id);
    }
}
