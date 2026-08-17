using Microsoft.EntityFrameworkCore;
using NassefStore.Data;
using NassefStore.Data.Entities;

namespace NassefStore.Services;

// ══════════════════════════════════════════════════════════════
// PRODUCT SERVICE
// ══════════════════════════════════════════════════════════════
public class ProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;

    public async Task<List<Product>> GetAllAsync() =>
        await _db.Products.Include(p => p.Category)
                          .Where(p => p.IsActive)
                          .OrderBy(p => p.Name)
                          .ToListAsync();

    public async Task<List<Product>> SearchAsync(string query) =>
        await _db.Products.Include(p => p.Category)
                          .Where(p => p.IsActive && (
                              p.Name.Contains(query) ||
                              (p.Barcode != null && p.Barcode.Contains(query))))
                          .ToListAsync();

    public async Task<List<Product>> GetLowStockAsync() =>
        await _db.Products.Include(p => p.Category)
                          .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
                          .ToListAsync();

    public async Task<Product?> GetByIdAsync(int id) =>
        await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

    public async Task AddAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null) { p.IsActive = false; await _db.SaveChangesAsync(); }
    }

    public async Task UpdateStockAsync(int productId, int quantityChange)
    {
        var p = await _db.Products.FindAsync(productId);
        if (p != null) { p.StockQuantity += quantityChange; await _db.SaveChangesAsync(); }
    }
}

// ══════════════════════════════════════════════════════════════
// SALE SERVICE
// ══════════════════════════════════════════════════════════════
public class SaleService
{
    private readonly AppDbContext _db;
    public SaleService(AppDbContext db) => _db = db;

    public async Task<Sale> CreateSaleAsync(Sale sale)
    {
        // Generate invoice number: INV-YYYYMMDD-001
        var today = DateTime.Today;
        var countToday = await _db.Sales.CountAsync(s => s.SaleDate.Date == today);
        sale.InvoiceNumber = $"INV-{today:yyyyMMdd}-{(countToday + 1):D3}";
        sale.NetAmount = sale.TotalAmount - sale.DiscountAmount;
        sale.RemainingAmount = sale.NetAmount - sale.PaidAmount;
        sale.IsCredit = sale.RemainingAmount > 0;

        _db.Sales.Add(sale);

        // Deduct stock for each item
        foreach (var item in sale.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity -= item.Quantity;
                if (product.StockQuantity <= product.MinStockLevel)
                {
                    _db.StockAlerts.Add(new StockAlert
                    {
                        ProductId = product.Id,
                        CurrentStock = product.StockQuantity,
                        MinStock = product.MinStockLevel
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        return sale;
    }

    public async Task<List<Sale>> GetSalesByDateAsync(DateTime date) =>
        await _db.Sales.Include(s => s.Customer)
                       .Include(s => s.Items).ThenInclude(i => i.Product)
                       .Where(s => s.SaleDate.Date == date.Date && !s.IsCancelled)
                       .OrderByDescending(s => s.SaleDate)
                       .ToListAsync();

    public async Task<List<Sale>> GetSalesByDateRangeAsync(DateTime from, DateTime to) =>
        await _db.Sales.Include(s => s.Customer)
                       .Include(s => s.Items).ThenInclude(i => i.Product)
                       .Where(s => s.SaleDate.Date >= from.Date && s.SaleDate.Date <= to.Date && !s.IsCancelled)
                       .OrderByDescending(s => s.SaleDate)
                       .ToListAsync();

    public async Task<List<Sale>> GetCreditSalesAsync() =>
        await _db.Sales.Include(s => s.Customer)
                       .Where(s => s.IsCredit && s.RemainingAmount > 0 && !s.IsCancelled)
                       .OrderByDescending(s => s.SaleDate)
                       .ToListAsync();

    public async Task<Sale?> GetByIdAsync(int id) =>
        await _db.Sales.Include(s => s.Customer)
                       .Include(s => s.Items).ThenInclude(i => i.Product)
                       .Include(s => s.CreditPayments)
                       .FirstOrDefaultAsync(s => s.Id == id);


    // إلغاء فاتورة (Soft Delete)
    public async Task CancelSaleAsync(int saleId)
    {
        var sale = await _db.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == saleId);
        if (sale == null) return;

        sale.IsCancelled = true;

        // إرجاع المخزون
        foreach (var item in sale.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product != null)
                product.StockQuantity += item.Quantity;
        }

        await _db.SaveChangesAsync();
    }

    // تعديل بيانات الفاتورة
    public async Task UpdateSaleAsync(Sale sale)
    {
        _db.Sales.Update(sale);
        await _db.SaveChangesAsync();
    }

    public async Task AddCreditPaymentAsync(CreditPayment payment)
    {
        _db.CreditPayments.Add(payment);
        var sale = await _db.Sales.FindAsync(payment.SaleId);
        if (sale != null)
        {
            sale.PaidAmount += payment.Amount;
            sale.RemainingAmount -= payment.Amount;
            if (sale.RemainingAmount <= 0) { sale.RemainingAmount = 0; sale.IsCredit = false; }
        }
        await _db.SaveChangesAsync();
    }

    // Daily summary
    public async Task<DailySummary> GetDailySummaryAsync(DateTime date)
    {
        var sales = await _db.Sales
            .Where(s => s.SaleDate.Date == date.Date && !s.IsCancelled)
            .ToListAsync();

        return new DailySummary
        {
            Date = date,
            TotalSales = sales.Sum(s => s.NetAmount),
            CashSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.PaidAmount),
            VodafoneCash = sales.Where(s => s.PaymentMethod == PaymentMethod.VodafoneCash).Sum(s => s.PaidAmount),
            InstaPay = sales.Where(s => s.PaymentMethod == PaymentMethod.InstaPay).Sum(s => s.PaidAmount),
            CreditSales = sales.Where(s => s.IsCredit).Sum(s => s.RemainingAmount),
            InvoiceCount = sales.Count
        };
    }
}

// ══════════════════════════════════════════════════════════════
// PURCHASE SERVICE
// ══════════════════════════════════════════════════════════════
public class PurchaseService
{
    private readonly AppDbContext _db;
    public PurchaseService(AppDbContext db) => _db = db;

    public async Task<Purchase> CreatePurchaseAsync(Purchase purchase)
    {
        var today = DateTime.Today;
        var count = await _db.Purchases.CountAsync(p => p.PurchaseDate.Date == today);
        purchase.InvoiceNumber = $"PUR-{today:yyyyMMdd}-{(count + 1):D3}";
        purchase.RemainingAmount = purchase.TotalAmount - purchase.PaidAmount;
        purchase.IsCredit = purchase.RemainingAmount > 0;

        _db.Purchases.Add(purchase);

        // Add stock for each item
        foreach (var item in purchase.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity += item.Quantity;
                product.CostPrice = item.UnitCost; // Update cost price
            }
        }

        await _db.SaveChangesAsync();
        return purchase;
    }

    public async Task<List<Purchase>> GetBySupplierAsync(int supplierId) =>
        await _db.Purchases.Include(p => p.Items).ThenInclude(i => i.Product)
                           .Include(p => p.Payments)
                           .Where(p => p.SupplierId == supplierId)
                           .OrderByDescending(p => p.PurchaseDate)
                           .ToListAsync();

    public async Task AddPaymentAsync(SupplierPayment payment)
    {
        _db.SupplierPayments.Add(payment);
        var purchase = await _db.Purchases.FindAsync(payment.PurchaseId);
        if (purchase != null)
        {
            purchase.PaidAmount += payment.Amount;
            purchase.RemainingAmount -= payment.Amount;
            if (purchase.RemainingAmount <= 0) { purchase.RemainingAmount = 0; purchase.IsCredit = false; }
        }
        await _db.SaveChangesAsync();
    }

    public async Task<List<Purchase>> GetAllAsync() =>
        await _db.Purchases.Include(p => p.Supplier)
                           .OrderByDescending(p => p.PurchaseDate)
                           .ToListAsync();
}

// ══════════════════════════════════════════════════════════════
// SUPPLIER SERVICE
// ══════════════════════════════════════════════════════════════
public class SupplierService
{
    private readonly AppDbContext _db;
    public SupplierService(AppDbContext db) => _db = db;

    public async Task<List<Supplier>> GetAllAsync() =>
        await _db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();

    public async Task<SupplierStatement> GetStatementAsync(int supplierId, DateTime? from = null, DateTime? to = null)
    {
        var supplier = await _db.Suppliers.FindAsync(supplierId);
        var query = _db.Purchases.Include(p => p.Items).ThenInclude(i => i.Product)
                                  .Include(p => p.Payments)
                                  .Where(p => p.SupplierId == supplierId);

        if (from.HasValue) query = query.Where(p => p.PurchaseDate >= from.Value);
        if (to.HasValue) query = query.Where(p => p.PurchaseDate <= to.Value);

        var purchases = await query.OrderByDescending(p => p.PurchaseDate).ToListAsync();

        return new SupplierStatement
        {
            Supplier = supplier!,
            Purchases = purchases,
            TotalPurchased = purchases.Sum(p => p.TotalAmount),
            TotalPaid = purchases.Sum(p => p.PaidAmount),
            TotalRemaining = purchases.Sum(p => p.RemainingAmount)
        };
    }

    public async Task AddAsync(Supplier supplier) { _db.Suppliers.Add(supplier); await _db.SaveChangesAsync(); }
    public async Task UpdateAsync(Supplier supplier) { _db.Suppliers.Update(supplier); await _db.SaveChangesAsync(); }
}

// ══════════════════════════════════════════════════════════════
// CUSTOMER SERVICE
// ══════════════════════════════════════════════════════════════
public class CustomerService
{
    private readonly AppDbContext _db;
    public CustomerService(AppDbContext db) => _db = db;

    public async Task<List<Customer>> GetAllAsync() =>
        await _db.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

    public async Task<CustomerStatement> GetStatementAsync(int customerId)
    {
        var customer = await _db.Customers.FindAsync(customerId);
        var sales = await _db.Sales.Include(s => s.Items).ThenInclude(i => i.Product)
                                    .Include(s => s.CreditPayments)
                                    .Where(s => s.CustomerId == customerId && !s.IsCancelled)
                                    .OrderByDescending(s => s.SaleDate)
                                    .ToListAsync();
        return new CustomerStatement
        {
            Customer = customer!,
            Sales = sales,
            TotalPurchased = sales.Sum(s => s.NetAmount),
            TotalPaid = sales.Sum(s => s.PaidAmount),
            TotalRemaining = sales.Sum(s => s.RemainingAmount)
        };
    }

    public async Task AddAsync(Customer customer) { _db.Customers.Add(customer); await _db.SaveChangesAsync(); }
    public async Task UpdateAsync(Customer customer) { _db.Customers.Update(customer); await _db.SaveChangesAsync(); }
    public async Task DeleteAsync(int id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c != null) { c.IsActive = false; await _db.SaveChangesAsync(); }
    }
}

// ══════════════════════════════════════════════════════════════
// RETURN SERVICE
// ══════════════════════════════════════════════════════════════
public class ReturnService
{
    private readonly AppDbContext _db;
    public ReturnService(AppDbContext db) => _db = db;

    public async Task ProcessReturnAsync(Return ret)
    {
        _db.Returns.Add(ret);

        var product = await _db.Products.FindAsync(ret.ProductId);
        if (product != null)
        {
            // Customer return → stock goes back in
            if (ret.ReturnType == ReturnType.FromCustomer)
                product.StockQuantity += ret.Quantity;
            // Return to supplier → stock goes out
            else
                product.StockQuantity -= ret.Quantity;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<Return>> GetAllAsync() =>
        await _db.Returns.Include(r => r.Product)
                         .Include(r => r.Sale).ThenInclude(s => s!.Customer)
                         .OrderByDescending(r => r.ReturnDate)
                         .ToListAsync();
}

// ══════════════════════════════════════════════════════════════
// REPORT SERVICE
// ══════════════════════════════════════════════════════════════
public class ReportService
{
    private readonly AppDbContext _db;
    public ReportService(AppDbContext db) => _db = db;

    public async Task<SalesReport> GetSalesReportAsync(DateTime from, DateTime to)
    {
        var sales = await _db.Sales
            .Include(s => s.Customer)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Where(s => s.SaleDate.Date >= from.Date && s.SaleDate.Date <= to.Date && !s.IsCancelled)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        var items = sales.SelectMany(s => s.Items).ToList();

        return new SalesReport
        {
            From = from, To = to,
            Sales = sales,
            TotalRevenue = sales.Sum(s => s.NetAmount),
            TotalCash = sales.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.PaidAmount),
            TotalVodafone = sales.Where(s => s.PaymentMethod == PaymentMethod.VodafoneCash).Sum(s => s.PaidAmount),
            TotalInstaPay = sales.Where(s => s.PaymentMethod == PaymentMethod.InstaPay).Sum(s => s.PaidAmount),
            TotalCredit = sales.Sum(s => s.RemainingAmount),
            InvoiceCount = sales.Count,
            TopProducts = items.GroupBy(i => i.Product.Name)
                               .Select(g => new ProductSummary { Name = g.Key, Quantity = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.TotalPrice) })
                               .OrderByDescending(p => p.Revenue)
                               .Take(10)
                               .ToList()
        };
    }

    public async Task<List<Product>> GetLowStockReportAsync() =>
        await _db.Products.Include(p => p.Category)
                          .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
                          .OrderBy(p => p.StockQuantity)
                          .ToListAsync();
}

// ══════════════════════════════════════════════════════════════
// REPORT DTOs
// ══════════════════════════════════════════════════════════════
public class DailySummary
{
    public DateTime Date { get; set; }
    public decimal TotalSales { get; set; }
    public decimal CashSales { get; set; }
    public decimal VodafoneCash { get; set; }
    public decimal InstaPay { get; set; }
    public decimal CreditSales { get; set; }
    public int InvoiceCount { get; set; }
}

public class SalesReport
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<Sale> Sales { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalVodafone { get; set; }
    public decimal TotalInstaPay { get; set; }
    public decimal TotalCredit { get; set; }
    public int InvoiceCount { get; set; }
    public List<ProductSummary> TopProducts { get; set; } = new();
}

public class ProductSummary
{
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class SupplierStatement
{
    public Supplier Supplier { get; set; } = null!;
    public List<Purchase> Purchases { get; set; } = new();
    public decimal TotalPurchased { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
}

public class CustomerStatement
{
    public Customer Customer { get; set; } = null!;
    public List<Sale> Sales { get; set; } = new();
    public decimal TotalPurchased { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
}
