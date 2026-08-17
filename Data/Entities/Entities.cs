// ============================================================
// ENTITIES - All Database Models for Nassef Store
// ============================================================

namespace NassefStore.Data.Entities;

// ── Product Category ─────────────────────────────────────────
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";          // e.g. Hardware, Plumbing, Electrical
    public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

// ── Product ───────────────────────────────────────────────────
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public string Unit { get; set; } = "Piece";     // Piece / Meter / Kg / Box
    public decimal CostPrice { get; set; }           // شراء
    public decimal SellPrice { get; set; }           // بيع
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; } = 5;      // تنبيه النواقص
    public int WarrantyMonths { get; set; } = 0;     // شهور الضمان
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
}

// ── Supplier (مورد) ───────────────────────────────────────────
public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}

// ── Customer (عميل) ───────────────────────────────────────────
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}

// ── Sale (فاتورة بيع) ─────────────────────────────────────────
public class Sale
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    // Payment Method
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    // آجل = Credit
    public bool IsCredit { get; set; } = false;
    public DateTime? DueDate { get; set; }

    public string? Notes { get; set; }
    public bool IsCancelled { get; set; } = false;

    // Walk-in customer or registered
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<Return> Returns { get; set; } = new List<Return>();
    public ICollection<CreditPayment> CreditPayments { get; set; } = new List<CreditPayment>();
}

// ── Sale Item (بند في الفاتورة) ───────────────────────────────
public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; } = 0;
    public decimal TotalPrice { get; set; }
}

// ── Purchase (فاتورة شراء من مورد) ───────────────────────────
public class Purchase
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public DateTime PurchaseDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public bool IsCredit { get; set; } = false;
    public DateTime? DueDate { get; set; }

    public string? Notes { get; set; }

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
    public ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
}

// ── Purchase Item ─────────────────────────────────────────────
public class PurchaseItem
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

// ── Return (مرتجع) ────────────────────────────────────────────
public class Return
{
    public int Id { get; set; }
    public DateTime ReturnDate { get; set; } = DateTime.Now;
    public ReturnType ReturnType { get; set; }       // FromCustomer / ToSupplier

    // If return from customer
    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    // If return to supplier
    public int? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    public bool IsWarrantyClaim { get; set; } = false;
    public DateTime? WarrantyExpiry { get; set; }

    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

// ── Credit Payment (دفعة على الآجل - للعميل) ─────────────────
public class CreditPayment
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Notes { get; set; }
}

// ── Supplier Payment (دفعة للمورد) ───────────────────────────
public class SupplierPayment
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;

    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Notes { get; set; }
}

// ── Stock Alert (النواقص) ─────────────────────────────────────
public class StockAlert
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int CurrentStock { get; set; }
    public int MinStock { get; set; }
    public DateTime AlertDate { get; set; } = DateTime.Now;
    public bool IsResolved { get; set; } = false;
}

// ── Enums ─────────────────────────────────────────────────────
public enum PaymentMethod
{
    Cash = 1,
    VodafoneCash = 2,
    InstaPay = 3,
    Credit = 4           // آجل
}

public enum ReturnType
{
    FromCustomer = 1,    // عميل رجع للمحل
    ToSupplier = 2       // محل رجع للمورد
}
