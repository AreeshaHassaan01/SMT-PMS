using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace PMS.Data
{
    public class DbInitializer
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(IConfiguration configuration, ILogger<DbInitializer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                await InitializeSqlServerAsync();
                _logger.LogInformation("SQL Server database initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SQL Server database.");
                throw;
            }
        }

        private async Task InitializeSqlServerAsync()
        {
            string masterConnString = _configuration.GetConnectionString("MasterConnection")
                ?? "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=True;";
            string defaultConnString = _configuration.GetConnectionString("DefaultConnection")
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=BagClothingPMS;Integrated Security=true;TrustServerCertificate=True;";

            // 1. Ensure BagClothingPMS database exists
            using (var masterConn = new SqlConnection(masterConnString))
            {
                await masterConn.OpenAsync();
                var checkDbSql = @"
                    IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'BagClothingPMS')
                    BEGIN
                        CREATE DATABASE [BagClothingPMS];
                    END";
                using var cmd = new SqlCommand(checkDbSql, masterConn);
                await cmd.ExecuteNonQueryAsync();
            }

            // 2. Connect to BagClothingPMS and ensure tables exist
            using (var conn = new SqlConnection(defaultConnString))
            {
                await conn.OpenAsync();

                var schemaSql = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
                    BEGIN
                        CREATE TABLE Categories (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Name NVARCHAR(100) NOT NULL,
                            Description NVARCHAR(500) NULL,
                            DisplayOrder INT DEFAULT 0
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
                    BEGIN
                        CREATE TABLE Products (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Name NVARCHAR(150) NOT NULL,
                            CategoryId INT NOT NULL FOREIGN KEY REFERENCES Categories(Id) ON DELETE CASCADE,
                            Price DECIMAL(18, 2) NOT NULL,
                            Stock INT NOT NULL DEFAULT 0,
                            Description NVARCHAR(2000) NULL,
                            ImageUrl NVARCHAR(500) NULL,
                            CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                            UpdatedAt DATETIME2 NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserAccounts')
                    BEGIN
                        CREATE TABLE UserAccounts (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Username NVARCHAR(50) NOT NULL,
                            PasswordHash NVARCHAR(256) NOT NULL,
                            FullName NVARCHAR(100) NOT NULL,
                            Email NVARCHAR(150) NOT NULL UNIQUE,
                            Role NVARCHAR(50) NOT NULL DEFAULT 'Customer',
                            CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                            LastLoginAt DATETIME2 NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CartItems')
                    BEGIN
                        CREATE TABLE CartItems (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
                            Quantity INT NOT NULL DEFAULT 1,
                            AddedAt DATETIME2 DEFAULT GETUTCDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
                    BEGIN
                        CREATE TABLE Orders (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            TotalAmount DECIMAL(18,2) NOT NULL,
                            Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                            PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'Cash on Delivery',
                            ShippingAddress NVARCHAR(500) NULL,
                            CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                            DeliveredAt DATETIME2 NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItems')
                    BEGIN
                        CREATE TABLE OrderItems (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            OrderId INT NOT NULL FOREIGN KEY REFERENCES Orders(Id) ON DELETE CASCADE,
                            ProductId INT NOT NULL,
                            ProductName NVARCHAR(150) NOT NULL,
                            ProductImageUrl NVARCHAR(500) NULL,
                            UnitPrice DECIMAL(18,2) NOT NULL,
                            Quantity INT NOT NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserProfiles')
                    BEGIN
                        CREATE TABLE UserProfiles (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL UNIQUE,
                            FullName NVARCHAR(100) NULL,
                            Phone NVARCHAR(20) NULL,
                            Address NVARCHAR(300) NULL,
                            City NVARCHAR(100) NULL,
                            Province NVARCHAR(100) NULL,
                            PostalCode NVARCHAR(20) NULL,
                            Country NVARCHAR(100) NOT NULL DEFAULT 'Pakistan',
                            CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                            UpdatedAt DATETIME2 NULL
                        );
                    END
                ";

                using (var schemaCmd = new SqlCommand(schemaSql, conn))
                {
                    await schemaCmd.ExecuteNonQueryAsync();
                }

                // 3. Seed initial Categories if none exist
                var countCategoriesSql = "SELECT COUNT(1) FROM Categories";
                using (var countCatCmd = new SqlCommand(countCategoriesSql, conn))
                {
                    int categoryCount = (int)(await countCatCmd.ExecuteScalarAsync() ?? 0);
                    if (categoryCount == 0)
                    {
                        var seedCategoriesSql = @"
                            INSERT INTO Categories (Name, Description, DisplayOrder) VALUES
                            ('Bags', 'Handbags, shoulder bags, backpacks and luggage', 1),
                            ('Clothing', 'Apparel, t-shirts, jeans, hoodies and jackets', 2),
                            ('Footwear', 'Casual shoes, boots and sneakers', 3),
                            ('Accessories', 'Scarves, belts, hats and styling accessories', 4);
                        ";
                        using var seedCatCmd = new SqlCommand(seedCategoriesSql, conn);
                        await seedCatCmd.ExecuteNonQueryAsync();
                    }
                }

                // 4. Seed Products if none exist
                var countProductsSql = "SELECT COUNT(1) FROM Products";
                using (var countProdCmd = new SqlCommand(countProductsSql, conn))
                {
                    int productCount = (int)(await countProdCmd.ExecuteScalarAsync() ?? 0);
                    if (productCount == 0)
                    {
                        var seedProductsSql = @"
                            DECLARE @BagsId INT = (SELECT TOP 1 Id FROM Categories WHERE Name = 'Bags');
                            DECLARE @ClothingId INT = (SELECT TOP 1 Id FROM Categories WHERE Name = 'Clothing');
                            DECLARE @FootwearId INT = (SELECT TOP 1 Id FROM Categories WHERE Name = 'Footwear');
                            DECLARE @AccessoriesId INT = (SELECT TOP 1 Id FROM Categories WHERE Name = 'Accessories');

                            INSERT INTO Products (Name, CategoryId, Price, Stock, Description, ImageUrl, CreatedAt) VALUES
                            ('Elegant Handbag', @BagsId, 4999.00, 25, 'Stylish and premium handbag for everyday use and formal occasions.', '/images/products/handbag.svg', GETUTCDATE()),
                            ('Classic T-Shirt', @ClothingId, 1999.00, 50, '100% breathable organic cotton classic crewneck everyday t-shirt.', '/images/products/tshirt.svg', GETUTCDATE()),
                            ('Shoulder Bag', @BagsId, 3499.00, 18, 'Chic genuine leather shoulder bag with adjustable gold-chain strap.', '/images/products/shoulder-bag.svg', GETUTCDATE()),
                            ('Winter Hoodie', @ClothingId, 5499.00, 12, 'Heavyweight fleece-lined warm pullover hoodie with kangaroo pocket.', '/images/products/hoodie.svg', GETUTCDATE()),
                            ('Denim Jeans', @ClothingId, 2999.00, 30, 'Classic slim-fit stretchable blue denim jeans with comfort waist.', '/images/products/jeans.svg', GETUTCDATE()),
                            ('Leather Backpack', @BagsId, 6499.00, 8, 'Durable multi-compartment travel and laptop leather backpack.', '/images/products/backpack.svg', GETUTCDATE()),
                            ('Casual Sneakers', @FootwearId, 4299.00, 15, 'Comfortable cushioned lightweight urban walking sneakers.', '/images/products/sneakers.svg', GETUTCDATE()),
                            ('Silk Scarf', @AccessoriesId, 1499.00, 4, 'Luxurious printed floral pattern pure silk neck scarf.', '/images/products/scarf.svg', GETUTCDATE()),
                            ('Formal Shirt', @ClothingId, 2799.00, 22, 'Crisp wrinkle-resistant tailored button-down formal business shirt.', '/images/products/shirt.svg', GETUTCDATE()),
                            ('Crossbody Mini Bag', @BagsId, 3199.00, 2, 'Compact trendy crossbody phone bag with card slots.', '/images/products/crossbody.svg', GETUTCDATE()),
                            ('Wool Overcoat', @ClothingId, 8999.00, 5, 'Tailored double-breasted long winter wool overcoat.', '/images/products/coat.svg', GETUTCDATE()),
                            ('Travel Duffle Bag', @BagsId, 7299.00, 14, 'Heavy-duty water-resistant canvas travel duffle bag.', '/images/products/duffle.svg', GETUTCDATE());
                        ";
                        using var seedProdCmd = new SqlCommand(seedProductsSql, conn);
                        await seedProdCmd.ExecuteNonQueryAsync();
                    }
                }

                // 5. Seed default Admin account into SQL Server if not present
                var checkAdminSql = "SELECT COUNT(1) FROM UserAccounts WHERE Email = @Email OR Username = @Username";
                using (var checkAdminCmd = new SqlCommand(checkAdminSql, conn))
                {
                    checkAdminCmd.Parameters.AddWithValue("@Email", "anyonecanuseit03@gmail.com");
                    checkAdminCmd.Parameters.AddWithValue("@Username", "anyonecanuseit03");
                    int adminCount = (int)(await checkAdminCmd.ExecuteScalarAsync() ?? 0);
                    if (adminCount == 0)
                    {
                        var seedAdminSql = @"
                            INSERT INTO UserAccounts (Username, PasswordHash, FullName, Email, Role, CreatedAt)
                            VALUES (@Username, @PasswordHash, @FullName, @Email, @Role, GETUTCDATE());
                        ";
                        using var seedAdminCmd = new SqlCommand(seedAdminSql, conn);
                        seedAdminCmd.Parameters.AddWithValue("@Username", "anyonecanuseit03");
                        seedAdminCmd.Parameters.AddWithValue("@PasswordHash", HashPassword("anyone123"));
                        seedAdminCmd.Parameters.AddWithValue("@FullName", "Anyone Use It");
                        seedAdminCmd.Parameters.AddWithValue("@Email", "anyonecanuseit03@gmail.com");
                        seedAdminCmd.Parameters.AddWithValue("@Role", "Admin & Stock Manager");
                        await seedAdminCmd.ExecuteNonQueryAsync();
                    }
                }

                // 6. Seed default Customer account into SQL Server if not present
                var checkCustSql = "SELECT COUNT(1) FROM UserAccounts WHERE Email = @Email OR Username = @Username";
                using (var checkCustCmd = new SqlCommand(checkCustSql, conn))
                {
                    checkCustCmd.Parameters.AddWithValue("@Email", "laptopworkonly9@gmail.com");
                    checkCustCmd.Parameters.AddWithValue("@Username", "laptopworkonly9");
                    int custCount = (int)(await checkCustCmd.ExecuteScalarAsync() ?? 0);
                    if (custCount == 0)
                    {
                        var seedCustSql = @"
                            INSERT INTO UserAccounts (Username, PasswordHash, FullName, Email, Role, CreatedAt)
                            VALUES (@Username, @PasswordHash, @FullName, @Email, @Role, GETUTCDATE());
                        ";
                        using var seedCustCmd = new SqlCommand(seedCustSql, conn);
                        seedCustCmd.Parameters.AddWithValue("@Username", "laptopworkonly9");
                        seedCustCmd.Parameters.AddWithValue("@PasswordHash", HashPassword("laptop123"));
                        seedCustCmd.Parameters.AddWithValue("@FullName", "LAPTOP");
                        seedCustCmd.Parameters.AddWithValue("@Email", "laptopworkonly9@gmail.com");
                        seedCustCmd.Parameters.AddWithValue("@Role", "Customer");
                        await seedCustCmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("PMS_SALT_" + password));
            return Convert.ToBase64String(bytes);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            return HashPassword(password) == storedHash;
        }
    }
}
