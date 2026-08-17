# 🔧 Nassef Store — Management System

A complete desktop POS and management system built with **WPF + C# (.NET 8) + SQLite**.

---

## 📋 Features

| Module | Description |
|---|---|
| **Dashboard** | Daily summary — sales, cash, Vodafone, InstaPay, credit, low stock alert |
| **New Sale (POS)** | Product search, cart, discount, payment method, print invoice |
| **Sales History** | Browse all sales by date range, filter by credit-only, reprint invoices |
| **Products** | Add/edit/delete products, stock levels, low-stock highlighting, categories |
| **Purchases** | Record purchases from suppliers, track what's paid vs. remaining |
| **Suppliers** | Full account statement per supplier, add payments, purchase history |
| **Customers** | Customer account, credit balance, payment history |
| **Returns** | Return from customer (warranty support) or return to supplier |
| **Reports** | Sales report by date range — revenue, cash, Vodafone, InstaPay, credit, top products |

---

## 🛠️ Requirements

- **Windows 10/11** (64-bit)
- **.NET 8 SDK** → https://dotnet.microsoft.com/download/dotnet/8
- **Visual Studio 2022** (Community is free) with:
  - "Desktop development with C++" workload
  - ".NET desktop development" workload

---

## 🚀 Setup & Run

### Step 1 — Clone / Copy the project
Place the `NassefStore` folder anywhere on your PC, e.g. `C:\Projects\NassefStore`

### Step 2 — Restore NuGet packages
Open a terminal in the project folder and run:
```
dotnet restore
```

### Step 3 — Apply database migrations
```
dotnet ef database update
```
> If `dotnet ef` is not found, install it:
> `dotnet tool install --global dotnet-ef`

**Alternative (no EF tools):** The app auto-creates the database on first launch using `MigrateAsync()`.

### Step 4 — Run
```
dotnet run
```
Or open `NassefStore.csproj` in Visual Studio and press **F5**.

---

## 📁 Project Structure

```
NassefStore/
├── Data/
│   ├── Entities/       ← All database models
│   ├── AppDbContext.cs ← EF Core DbContext + SQLite config
│   └── schema.sql      ← Raw SQL schema (reference/backup)
├── Services/
│   └── Services.cs     ← Business logic (Sale, Purchase, Product, Report...)
├── Views/
│   ├── Dashboard/      ← DashboardPage
│   ├── Sales/          ← NewSalePage, SalesHistoryPage, PurchasesPage, InvoiceWindow
│   ├── Products/       ← ProductsPage, ProductDialog
│   ├── Suppliers/      ← SuppliersPage, SupplierDialogs
│   ├── Customers/      ← CustomersPage
│   ├── Returns/        ← ReturnsPage, ReturnDialog
│   └── Reports/        ← ReportsPage
├── Helpers/
│   └── Converters.cs   ← XAML value converters
├── App.xaml            ← Material Design theme + global styles
└── MainWindow.xaml     ← Shell with sidebar navigation
```

---

## 🗃️ Database

- **Engine:** SQLite (file-based, no server needed)
- **Location:** `%LOCALAPPDATA%\NassefStore\nassef.db`
  - Windows: `C:\Users\[YourName]\AppData\Local\NassefStore\nassef.db`
- **Backup:** Just copy the `.db` file

---

## 💳 Payment Methods Supported

| Code | Method |
|---|---|
| Cash | نقدي |
| VodafoneCash | فودافون كاش |
| InstaPay | انستا باي |
| Credit | آجل |

---

## 📦 NuGet Packages Used

```xml
Microsoft.EntityFrameworkCore         8.0.0
Microsoft.EntityFrameworkCore.Sqlite  8.0.0
Microsoft.EntityFrameworkCore.Tools   8.0.0
MaterialDesignThemes                  4.9.0
CommunityToolkit.Mvvm                 8.2.2
LiveChartsCore.SkiaSharpView.WPF      2.0.0-rc2
```

---

## 🔧 First Time Configuration

1. Open **Products** → Add your initial inventory with stock quantities
2. Open **Suppliers** → Add your suppliers
3. You're ready to make sales!

---

## 🖨️ Invoice Printing

Invoices print directly to any Windows printer.  
Format: Store name, invoice number, date, items table, totals, payment method, remaining balance (if credit).

---

## 📊 Reports

Go to **Reports** → select date range → click **Generate Report**:
- Total revenue breakdown by payment method
- Per-invoice listing with customer and remaining
- Top 10 products by revenue

---

## 🔔 Low Stock Alerts

- Each product has a **Min Stock Level** (default: 5)
- When stock drops at or below that level, a red alert appears in the sidebar
- Click the alert to jump directly to the low-stock products list

---

## 💡 Tips

- **Walk-in customer:** Leave customer field empty when making a sale
- **Credit sale:** Choose "Credit (آجل)" as payment method and enter amount paid (can be 0)
- **Warranty return:** In Returns → "Return from Customer" → check "Warranty Claim" → set expiry date
- **Supplier restock:** Go to Suppliers → select supplier → click "New Purchase"
