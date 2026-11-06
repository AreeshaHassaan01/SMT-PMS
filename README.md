# 🛍️ Bag & Clothing Store — Product Management System (PMS)

A modern, high-performance **Product Management System (PMS)** built with **ASP.NET Core 8**, **Razor Pages**, **C#**, **SQL Server**, and **ADO.NET** using a clean **Repository Pattern**. Designed directly from the UI mockups for a Bag & Clothing Store, featuring a dual-database setup (**SQL Server** for catalog & inventory data + **SQLite** for user accounts & authentication).

---

## 📸 UI & Features Overview

The system faithfully implements the UI and workflows from the design specification:

- **Dashboard**:
  - **4 Stat Metric Cards**:
    - **Total Products** (12)
    - **Total Categories** (4)
    - **In Stock** (9)
    - **Low Stock** (3)
  - **Products Table**:
    - `#`, `Image` thumbnail, `Name`, `Category` badge (e.g. *Bags*, *Clothing*), `Price (PKR)`, `Stock` status with color-coding (*green* for in stock, *orange* for low stock $\le 5$, *red* for out of stock), and `Actions` (*Edit* blue pencil, *Delete* red trash can, *View* details).
    - **+ Add Product** button.
    - **Live Search & Category Filtering**.
- **+ Add New Product**:
  - `Product Name *`
  - `Category *` (Dropdown populated from SQL Server)
  - `Price (PKR) *`
  - `Stock Quantity *`
  - `Description`
  - `Product Image` (File upload with live client-side preview)
  - Full client-side & server-side validation to prevent invalid entries
- **✏️ Edit Product**:
  - Side-by-side layout: Product Image preview container with **Change Image** button on the left, input fields on the right.
  - Updates SQL Server via clean parameterized ADO.NET query.
- **👁️ Product Details**:
  - Prominent product image display, formatted currency (`Rs. X,XXX`), category pill, stock level badge, and detailed description.
  - One-click `← Back to List` navigation.
- **🗑️ Delete Product**:
  - Soft-red alert card with `! Are you sure? You are about to delete this product:`.
  - Preview box with image thumbnail, product name, and price.
  - Warning note: `This action cannot be undone.`
  - `Cancel` & solid red `Delete` action.
- **🏷️ Categories Management**:
  - View all categories with dynamic product counts.
  - Add, edit, and safely delete categories (prevents deletion if products are currently assigned).
- **👤 Accounts & Authentication (SQLite Database)**:
  - Account info stored locally in `accounts.db` via ADO.NET (`Microsoft.Data.Sqlite`).
  - Stores `Username`, `PasswordHash` (salted SHA-256), `FullName`, `Email`, `Role`, `CreatedAt`, and `LastLoginAt`.
  - **Sleek Dark Login Page (`/Account/Login`)**:
    - "Welcome back" card with Email Address, Password (with eye toggle & Forgot? prompt), and `SECRET-CODE:` box.
    - Switch link: *"Don't have an account? Create one"*.
  - **Dedicated Registration Page (`/Account/Register`)**:
    - "Create Account" card with Full Name, Email Address, Password, Confirm Password, and `SECRET-CODE:` box.
    - Switch link: *"Already have an account? Sign In"*.
  - **🚪 Logout Protection**:
    - Clicking Logout immediately signs out and returns to the Login page (`/Account/Login`).
    - Unauthenticated access automatically redirects to the Login page.

---

## 🏗️ Architecture & Clean Repository Pattern

The project is structured with clean separation of concerns and pure ADO.NET:

```
PMS/
├── Data/
│   ├── ISqlConnectionFactory.cs      # Factory creating SqlConnection for SQL Server
│   ├── SqlConnectionFactory.cs
│   ├── ISqliteConnectionFactory.cs   # Factory creating SqliteConnection for SQLite
│   ├── SqliteConnectionFactory.cs
│   └── DbInitializer.cs              # Auto-creates databases, tables & seeds initial data
├── Models/
│   ├── Product.cs                    # Product model with validation annotations
│   ├── Category.cs                   # Category model
│   ├── UserAccount.cs                # User account model
│   └── DashboardStats.cs             # Aggregated stats model
├── Repositories/
│   ├── IProductRepository.cs         # Product repository contract
│   ├── ProductRepository.cs          # Pure ADO.NET SqlCommand & SqlDataReader implementation
│   ├── ICategoryRepository.cs        # Category repository contract
│   ├── CategoryRepository.cs         # Category ADO.NET implementation
│   ├── IAccountRepository.cs         # Account repository contract
│   └── AccountRepository.cs          # SQLite ADO.NET account data access
├── Pages/
│   ├── Index.cshtml                  # Dashboard matching mockup
│   ├── Products/
│   │   ├── Index.cshtml              # Products inventory table with search/filter
│   │   ├── Create.cshtml             # Add New Product form
│   │   ├── Edit.cshtml               # Edit Product side-by-side view
│   │   ├── Details.cshtml            # Product Details view
│   │   └── Delete.cshtml             # Delete Product confirmation
│   ├── Categories/
│   │   └── Index.cshtml              # Category manager with modal editing
│   ├── Account/
│   │   ├── Index.cshtml              # SQLite accounts viewer & user creator
│   │   ├── Login.cshtml              # Sign-in form with cookie authentication
│   │   └── Logout.cshtml             # Sign-out handler
│   └── About.cshtml                  # Architecture, tech stack & feature notes
├── wwwroot/
│   ├── css/site.css                  # Custom theme, sidebar, cards, pills & badges
│   ├── images/products/              # Clean vector illustrations for seed products
│   └── uploads/                      # User-uploaded product images
├── appsettings.json                  # Connection strings for SQL Server & SQLite
└── Program.cs                        # Service registration & pipeline configuration
```

---

## ⚡ Zero-Configuration Setup & Running

Both SQL Server and SQLite databases are initialized and seeded **automatically** on application startup.

### 1. Run the application
```powershell
dotnet run --launch-profile http
```

### 2. Access the Application
Open your browser and navigate to:
```
http://localhost:5074
```

## 🛡️ Database Implementation Details

### SQL Server (ADO.NET)
- Uses `Microsoft.Data.SqlClient`.
- Uses parameterized `SqlCommand` queries (`@Name`, `@CategoryId`, `@Price`, etc.) to prevent SQL Injection.
- Uses `OUTPUT INSERTED.Id` on inserts to immediately retrieve generated identity IDs.
- Database automatically created if absent: `BagClothingPMS` on `(localdb)\MSSQLLocalDB`.

### SQLite (ADO.NET)
- Uses `Microsoft.Data.Sqlite`.
- Stores accounts in `accounts.db`.
- Salted SHA-256 password hashing.

