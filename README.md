
# EntityTest
covers most of the .net concepts
=======
# EntityTest API — .NET 10 Web API with Entity Framework Core & SQL Server

A complete ASP.NET Core Web API project demonstrating Entity Framework Core integration with SQL Server, including database migrations, CRUD operations, and seed data.

## Project Structure

```
EntityTestApi/
├── Controllers/
│   └── ProductsController.cs       # REST API endpoints (CRUD)
├── Data/
│   ├── ApplicationDbContext.cs     # EF Core DbContext
│   ├── ApplicationDbContextFactory.cs # Design-time factory for migrations
│   └── Migrations/                 # EF Core migration files
├── Models/
│   └── Product.cs                  # Product entity model
├── Properties/
│   └── launchSettings.json         # Launch configuration
├── appsettings.json                # App configuration & connection string
├── Program.cs                      # App startup & DI configuration
└── EntityTestApi.csproj            # Project file
```

## Prerequisites

- **.NET 10 SDK** — [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **SQL Server instance** — Running on `localhost:1433` (or adjust connection string)
  - Default credentials: `User Id=SA; Password=Test@2011` (configured in `appsettings.json`)

## Setup & Installation

### 1. Clone/Navigate to Project
```bash
cd /Users/sachinsapkal/Projects/entitycore/Entitytest
```

### 2. Update Connection String (if needed)
Edit `EntityTestApi/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=EntityTestDb;User Id=SA;Password=Test@2011;TrustServerCertificate=True;"
  }
}
```

Replace:
- `localhost,1433` with your SQL Server instance (e.g., `myserver.database.windows.net`)
- `EntityTestDb` with your desired database name
- `SA` and `Test@2011` with your SQL Server credentials

### 3. Install Dependencies
```bash
cd EntityTestApi
dotnet restore
```

### 4. Create Database & Run Migrations
```bash
dotnet ef database update
```

This will:
- Create the `EntityTestDb` database on your SQL Server instance
- Create the `Products` table
- Seed 5 sample products (Laptop, Monitor, Keyboard, Mouse, Headphones)

### 5. Build the Project
```bash
dotnet build
```

## Running the Application

Start the API server:
```bash
dotnet run --project EntityTestApi
```

The app will start on **`http://localhost:5274`**

You should see console output:
```
✓ Sample products seeded successfully!
```

## API Endpoints & CRUD Operations

### Base URL
```
http://localhost:5274/api/products
```

### Root Endpoint
```bash
curl http://localhost:5274/
```
Response:
```json
{
  "message": "Welcome to EntityTest API!",
  "docs": "Visit http://localhost:5274/openapi/v1.json for API specification"
}
```

### 1. GET All Products
```bash
curl http://localhost:5274/api/products
```

**Response:**
```json
[
  {
    "id": 1,
    "name": "Laptop",
    "description": "High-performance laptop for developers",
    "price": 1299.99
  },
  {
    "id": 2,
    "name": "Monitor",
    "description": "4K Ultra HD Monitor 27 inch",
    "price": 399.99
  },
  ...
]
```

### 2. GET Product by ID
```bash
curl http://localhost:5274/api/products/1
```

**Response:**
```json
{
  "id": 1,
  "name": "Laptop",
  "description": "High-performance laptop for developers",
  "price": 1299.99
}
```

**Error Response (404):**
```json
{
  "message": "Product with ID 999 not found"
}
```

### 3. CREATE Product (POST)
```bash
curl -X POST http://localhost:5274/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "USB-C Cable",
    "description": "High-speed USB-C cable",
    "price": 19.99
  }'
```

**Response (201 Created):**
```json
{
  "id": 6,
  "name": "USB-C Cable",
  "description": "High-speed USB-C cable",
  "price": 19.99
}
```

### 4. UPDATE Product (PUT)
```bash
curl -X PUT http://localhost:5274/api/products/1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Gaming Laptop",
    "description": "High-performance gaming laptop",
    "price": 1599.99
  }'
```

**Response (200 OK):**
```json
{
  "id": 1,
  "name": "Gaming Laptop",
  "description": "High-performance gaming laptop",
  "price": 1599.99
}
```

### 5. DELETE Product
```bash
curl -X DELETE http://localhost:5274/api/products/1
```

**Response (204 No Content)** — No body returned, just success status.

## Database Details

**Database Name:** `EntityTestDb`

**Table:** `Products`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| Id | int | NO | Primary Key, Identity |
| Name | nvarchar(200) | NO | Required, max 200 chars |
| Description | nvarchar(max) | YES | Optional |
| Price | decimal(18,2) | NO | Currency format |

## Managing Migrations

### Create a New Migration
```bash
cd EntityTestApi
dotnet ef migrations add <MigrationName>
```

Example:
```bash
dotnet ef migrations add AddProductCategoryColumn
```

### Apply Migrations to Database
```bash
dotnet ef database update
```

### Rollback to Previous Migration
```bash
dotnet ef database update <PreviousMigrationName>
```

### List All Migrations
```bash
dotnet ef migrations list
```

## Project Architecture

### Models (`Models/Product.cs`)
- **Product** entity with properties: Id, Name, Description, Price
- Data annotations for validation (Required, MaxLength)
- Decimal precision configured via `OnModelCreating()`

### DbContext (`Data/ApplicationDbContext.cs`)
- Inherits from `DbContext`
- Exposes `DbSet<Product> Products` for database operations
- Configures entity mappings and column types

### Controllers (`Controllers/ProductsController.cs`)
- RESTful API endpoints for CRUD operations
- Dependency injection of `ApplicationDbContext`
- Async/await for database operations
- Error handling with proper HTTP status codes
- Request/Response DTOs (CreateProductRequest, UpdateProductRequest)

### Dependency Injection (`Program.cs`)
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddControllers();
```

## Configuration Files

### `appsettings.json`
Application configuration including:
- Logging levels
- Connection strings
- Allowed hosts

### `launchSettings.json`
Development profile with:
- Port configuration (5274)
- ASPNETCORE_ENVIRONMENT=Development
- Connection string environment variable override

### `EntityTestApi.csproj`
Project file with NuGet package dependencies:
- `Microsoft.EntityFrameworkCore.SqlServer` — EF Core SQL Server provider
- `Microsoft.EntityFrameworkCore.Design` — Design-time EF Core tools

## Troubleshooting

### "Connection string not found"
**Solution:** Ensure `appsettings.json` has the `ConnectionStrings:DefaultConnection` key with a valid connection string.

### "Address already in use" (Port 5274)
**Solution:** 
- Kill the existing process: `lsof -i :5274 | grep -v COMMAND | awk '{print $2}' | xargs kill -9`
- Or change the port in `Properties/launchSettings.json`

### "Cannot connect to SQL Server"
**Solution:**
- Verify SQL Server is running: Check Docker container or local service
- Confirm credentials in connection string (server, user, password)
- Test connectivity: Try connecting with VS Code SQL Server extension

### Migrations not running
**Solution:**
- Ensure `ApplicationDbContextFactory` is in the project for design-time factory
- Run from the `EntityTestApi` folder: `cd EntityTestApi && dotnet ef ...`

## Next Steps & Improvements

### 1. **Pagination**
Add pagination to the `GetProducts()` endpoint:
```csharp
public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
{
    var products = await _context.Products
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    return Ok(products);
}
```

### 2. **Validation & Data Annotations**
Add more validation to the `Product` model:
```csharp
[Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
public decimal Price { get; set; }
```

### 3. **DTOs (Data Transfer Objects)**
Create separate DTOs for requests/responses to decouple API from entity model:
```csharp
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

### 4. **Error Handling Middleware**
Add global exception handling:
```csharp
app.UseExceptionHandler("/error");
```

### 5. **Unit & Integration Tests**
Create an xUnit test project:
```bash
dotnet new xunit -n EntityTestApi.Tests
dotnet sln add EntityTestApi.Tests/EntityTestApi.Tests.csproj
```

Test CRUD operations with `WebApplicationFactory` and in-memory database.

### 6. **API Documentation (Swagger)**
Install Swagger packages:
```bash
dotnet add package Swashbuckle.AspNetCore
```

Configure in `Program.cs`:
```csharp
builder.Services.AddSwaggerGen();
app.UseSwagger();
app.UseSwaggerUI();
```

Access at `http://localhost:5274/swagger/index.html`

### 7. **Authentication & Authorization**
Add JWT tokens or other auth schemes for production APIs.

### 8. **Logging**
Use Serilog or other logging frameworks for better observability:
```bash
dotnet add package Serilog.AspNetCore
```

### 9. **Async/Await Best Practices**
Ensure all database operations use `async` methods (`ToListAsync()`, `FindAsync()`, `SaveChangesAsync()`).

### 10. **Database Indexing**
Add indexes for frequently queried columns:
```csharp
modelBuilder.Entity<Product>()
    .HasIndex(p => p.Name)
    .IsUnique();
```

## Resources

- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Web API Documentation](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [SQL Server Documentation](https://learn.microsoft.com/en-us/sql/sql-server/)
- [Migrations in EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

## License

This project is open source and available under the MIT License.

---

**Created:** November 30, 2025  
**Version:** 1.0.0  
**Target Framework:** .NET 10
>>>>>>> b74b4de (Initial Commit)
