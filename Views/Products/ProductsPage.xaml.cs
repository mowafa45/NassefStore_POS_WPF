using Microsoft.EntityFrameworkCore;
using NassefStore.Data;
using NassefStore.Data.Entities;
using NassefStore.Services;
using System.Windows;
using System.Windows.Controls;

namespace NassefStore.Views.Products;

public class ProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Category Category { get; set; } = null!;
    public string Unit { get; set; } = "";
    public decimal CostPrice { get; set; }
    public decimal SellPrice { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
    public int WarrantyMonths { get; set; }
    public bool IsLowStock => StockQuantity <= MinStockLevel;
}

public partial class ProductsPage : Page
{
    private readonly ProductService _productService;
    private readonly AppDbContext _db;
    private List<ProductViewModel> _allProducts = new();

    public ProductsPage(bool showLowStockOnly = false)
    {
        InitializeComponent();
        _db = new AppDbContext();
        _productService = new ProductService(_db);
        if (showLowStockOnly) LowStockFilter.IsChecked = true;
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var categories = _db.Categories.ToList();
            CategoryFilter.Items.Clear();
            CategoryFilter.Items.Add(new Category { Id = 0, Name = "All Categories" });
            foreach (var c in categories) CategoryFilter.Items.Add(c);
            if (CategoryFilter.SelectedIndex < 0) CategoryFilter.SelectedIndex = 0;

            var products = await _productService.GetAllAsync();
            _allProducts = products.Select(p => new ProductViewModel
            {
                Id = p.Id, Name = p.Name, Category = p.Category, Unit = p.Unit,
                CostPrice = p.CostPrice, SellPrice = p.SellPrice,
                StockQuantity = p.StockQuantity, MinStockLevel = p.MinStockLevel,
                WarrantyMonths = p.WarrantyMonths
            }).ToList();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading products:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allProducts.AsEnumerable();
        var search = SearchBox.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
            filtered = filtered.Where(p => p.Name.ToLower().Contains(search));

        if (CategoryFilter.SelectedItem is Category cat && cat.Id > 0)
            filtered = filtered.Where(p => p.Category?.Id == cat.Id);

        if (LowStockFilter.IsChecked == true)
            filtered = filtered.Where(p => p.IsLowStock);

        ProductsGrid.ItemsSource = filtered.ToList();
    }

    private void Search_Changed(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void Category_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void LowStock_Changed(object sender, RoutedEventArgs e) => ApplyFilter();
    private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private async void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProductDialog(null);
        if (dialog.ShowDialog() == true) await LoadDataAsync();
    }

    private async void EditProduct_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is ProductViewModel vm)
        {
            var product = await _db.Products.FindAsync(vm.Id);
            var dialog = new ProductDialog(product);
            if (dialog.ShowDialog() == true) await LoadDataAsync();
        }
    }

    private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is ProductViewModel vm)
        {
            var confirm = MessageBox.Show($"Delete '{vm.Name}'?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
            {
                await _productService.DeleteAsync(vm.Id);
                await LoadDataAsync();
            }
        }
    }
}
