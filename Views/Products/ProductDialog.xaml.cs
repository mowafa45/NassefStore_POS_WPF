using NassefStore.Data;
using NassefStore.Data.Entities;
using NassefStore.Services;
using System.Windows;
using System.Windows.Controls;

namespace NassefStore.Views.Products;

public partial class ProductDialog : Window
{
    private readonly Product? _existing;
    private readonly ProductService _service;
    private readonly AppDbContext _db;

    private static readonly Dictionary<string, string> UnitMap = new()
    {
        { "قطعة", "Piece" }, { "متر",    "Meter" },
        { "كيلو", "Kg"    }, { "كرتونة", "Box"   },
        { "رول",  "Roll"  }, { "لتر",    "Liter" },
        { "طقم",  "Set"   },
    };

    public ProductDialog(Product? existing)
    {
        InitializeComponent();
        _db       = new AppDbContext();
        _service  = new ProductService(_db);
        _existing = existing;
        Loaded   += async (_, _) => await LoadAsync();
    }

    // Simple decimal parser — handles both "." and ","
    private static decimal ParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Replace(",", ".").Trim();
        return decimal.TryParse(text,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var r) ? r : 0;
    }

    private async Task LoadAsync()
    {
        try
        {
            CmbCategory.ItemsSource = _db.Categories.ToList();

            if (_existing != null)
            {
                DialogTitle.Text          = "تعديل منتج";
                TxtName.Text              = _existing.Name;
                CmbCategory.SelectedValue = _existing.CategoryId;
                TxtCostPrice.Text         = _existing.CostPrice.ToString("N2");
                TxtSellPrice.Text         = _existing.SellPrice.ToString("N2");
                TxtStock.Text             = _existing.StockQuantity.ToString();
                TxtMinStock.Text          = _existing.MinStockLevel.ToString();
                TxtWarranty.Text          = _existing.WarrantyMonths.ToString();
                TxtBarcode.Text           = _existing.Barcode ?? "";
                TxtDescription.Text       = _existing.Description ?? "";

                var arabicUnit = UnitMap.FirstOrDefault(x => x.Value == _existing.Unit).Key ?? "قطعة";
                foreach (ComboBoxItem item in CmbUnit.Items)
                    if (item.Content?.ToString() == arabicUnit)
                    { CmbUnit.SelectedItem = item; break; }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل البيانات:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text) || CmbCategory.SelectedValue == null)
        {
            MessageBox.Show("من فضلك أدخل اسم المنتج والفئة.", "تحقق من البيانات",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal cost = ParseDecimal(TxtCostPrice.Text);
        decimal sell = ParseDecimal(TxtSellPrice.Text);

        if (!int.TryParse(TxtStock.Text, out var stock))
        {
            MessageBox.Show("من فضلك أدخل كمية صحيحة.", "تحقق من البيانات",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int.TryParse(TxtMinStock.Text, out var minStock);
        int.TryParse(TxtWarranty.Text, out var warranty);

        var arabicLabel = (CmbUnit.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "قطعة";
        var unitStored  = UnitMap.TryGetValue(arabicLabel, out var u) ? u : "Piece";

        try
        {
            if (_existing != null)
            {
                _existing.Name           = TxtName.Text.Trim();
                _existing.CategoryId     = (int)CmbCategory.SelectedValue;
                _existing.CostPrice      = cost;
                _existing.SellPrice      = sell;
                _existing.StockQuantity  = stock;
                _existing.MinStockLevel  = minStock > 0 ? minStock : 5;
                _existing.WarrantyMonths = warranty;
                _existing.Unit           = unitStored;
                _existing.Barcode        = TxtBarcode.Text.Trim();
                _existing.Description    = TxtDescription.Text.Trim();
                await _service.UpdateAsync(_existing);
            }
            else
            {
                await _service.AddAsync(new Product
                {
                    Name           = TxtName.Text.Trim(),
                    CategoryId     = (int)CmbCategory.SelectedValue,
                    CostPrice      = cost,
                    SellPrice      = sell,
                    StockQuantity  = stock,
                    MinStockLevel  = minStock > 0 ? minStock : 5,
                    WarrantyMonths = warranty,
                    Unit           = unitStored,
                    Barcode        = TxtBarcode.Text.Trim(),
                    Description    = TxtDescription.Text.Trim()
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
