-- NassefStore Database Schema
-- SQLite compatible
-- Run this if EF Migrations are not used

CREATE TABLE IF NOT EXISTS Categories (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT
);

CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    Barcode TEXT,
    Unit TEXT NOT NULL DEFAULT 'Piece',
    CostPrice REAL NOT NULL DEFAULT 0,
    SellPrice REAL NOT NULL DEFAULT 0,
    StockQuantity INTEGER NOT NULL DEFAULT 0,
    MinStockLevel INTEGER NOT NULL DEFAULT 5,
    WarrantyMonths INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    CategoryId INTEGER NOT NULL,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);

CREATE TABLE IF NOT EXISTS Suppliers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Phone TEXT,
    Address TEXT,
    Notes TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Customers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Phone TEXT,
    Address TEXT,
    Notes TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Sales (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    InvoiceNumber TEXT NOT NULL,
    SaleDate TEXT NOT NULL,
    TotalAmount REAL NOT NULL DEFAULT 0,
    DiscountAmount REAL NOT NULL DEFAULT 0,
    NetAmount REAL NOT NULL DEFAULT 0,
    PaidAmount REAL NOT NULL DEFAULT 0,
    RemainingAmount REAL NOT NULL DEFAULT 0,
    PaymentMethod INTEGER NOT NULL DEFAULT 1,
    IsCredit INTEGER NOT NULL DEFAULT 0,
    DueDate TEXT,
    Notes TEXT,
    IsCancelled INTEGER NOT NULL DEFAULT 0,
    CustomerId INTEGER,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

CREATE TABLE IF NOT EXISTS SaleItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitPrice REAL NOT NULL,
    Discount REAL NOT NULL DEFAULT 0,
    TotalPrice REAL NOT NULL,
    FOREIGN KEY (SaleId) REFERENCES Sales(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE IF NOT EXISTS Purchases (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    InvoiceNumber TEXT NOT NULL,
    PurchaseDate TEXT NOT NULL,
    TotalAmount REAL NOT NULL DEFAULT 0,
    PaidAmount REAL NOT NULL DEFAULT 0,
    RemainingAmount REAL NOT NULL DEFAULT 0,
    PaymentMethod INTEGER NOT NULL DEFAULT 1,
    IsCredit INTEGER NOT NULL DEFAULT 0,
    DueDate TEXT,
    Notes TEXT,
    SupplierId INTEGER NOT NULL,
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id)
);

CREATE TABLE IF NOT EXISTS PurchaseItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PurchaseId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitCost REAL NOT NULL,
    TotalCost REAL NOT NULL,
    FOREIGN KEY (PurchaseId) REFERENCES Purchases(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE IF NOT EXISTS Returns (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReturnDate TEXT NOT NULL,
    ReturnType INTEGER NOT NULL,
    SaleId INTEGER,
    PurchaseId INTEGER,
    ProductId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitPrice REAL NOT NULL,
    TotalPrice REAL NOT NULL,
    IsWarrantyClaim INTEGER NOT NULL DEFAULT 0,
    WarrantyExpiry TEXT,
    Reason TEXT,
    Notes TEXT,
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE IF NOT EXISTS CreditPayments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleId INTEGER NOT NULL,
    PaymentDate TEXT NOT NULL,
    Amount REAL NOT NULL,
    PaymentMethod INTEGER NOT NULL DEFAULT 1,
    Notes TEXT,
    FOREIGN KEY (SaleId) REFERENCES Sales(Id)
);

CREATE TABLE IF NOT EXISTS SupplierPayments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PurchaseId INTEGER NOT NULL,
    PaymentDate TEXT NOT NULL,
    Amount REAL NOT NULL,
    PaymentMethod INTEGER NOT NULL DEFAULT 1,
    Notes TEXT,
    FOREIGN KEY (PurchaseId) REFERENCES Purchases(Id)
);

CREATE TABLE IF NOT EXISTS StockAlerts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    CurrentStock INTEGER NOT NULL,
    MinStock INTEGER NOT NULL,
    AlertDate TEXT NOT NULL,
    IsResolved INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

-- Seed default categories
INSERT OR IGNORE INTO Categories (Id, Name, Description) VALUES
(1, 'Hardware',          'Hinges, Screws, Hammers, Screwdrivers'),
(2, 'Plumbing',          'Pipes, Fittings, Valves'),
(3, 'Electrical',        'Wires, Cables, Sockets, Switches'),
(4, 'Lighting',          'Bulbs, LED, Fixtures'),
(5, 'Building Materials','Cement tools, Paint brushes, etc.'),
(6, 'Other',             'Miscellaneous items');
