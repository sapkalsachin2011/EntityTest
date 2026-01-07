---

## Recent Updates

### Logging
- Configured log4net for file-based logging.
- Log files are now written to `EntityTestApi/Log/Supplierlog.txt` (project directory) instead of the build output folder.
- Logging configuration is managed in `EntityTestApi/log4net.config`.
- Middleware logs all requests and responses.

### CI/CD Pipeline
- Added a `Jenkinsfile` for automated build, test, and publish pipeline using Jenkins.
- Pipeline stages: checkout, restore, build, test (xUnit, NUnit, MSTest), publish, and artifact archiving.
- Supports integration with GitHub for automatic pipeline triggers on commit.
- Artifacts are archived by Jenkins after each successful build.

### Git & GitHub Integration
- Project is now a Git repository and changes are pushed to GitHub.
- Jenkins can be configured to poll GitHub or use webhooks for automatic pipeline runs on new commits.
- Branch specifier set to `main` for single-branch builds.

---

**Legend:**
- The controller injects ExternalApiService (via DI).
- ExternalApiService can use either IHttpClientFactory (recommended, managed) or direct HttpClient (static, not recommended for production).
- Both approaches ultimately call the external API, but only the factory approach is managed and pooled by ASP.NET Core.
## External API Integration & Comparison

This project demonstrates two approaches for making HTTP requests to external APIs:

- **IHttpClientFactory (recommended):**
  - Managed by ASP.NET Core for connection pooling and DI.
  - Used in `ExternalApiService.GetDataFromExternalApiAsync()`.
- **Direct HttpClient (not recommended for production):**
  - Manual instantiation, can cause socket exhaustion if misused.
  - Used in `ExternalApiService.GetDataWithDirectHttpClientAsync()`.

### Test & Compare Both Methods

Use the provided endpoint to compare both approaches:

```
GET http://localhost:5274/api/ExternalApiTest/externalapi/compare?url=https://jsonplaceholder.typicode.com/todos/1
```

**Sample Postman/REST Client file:**

See `PostmanTEST/ExternalApiComparison.http` for a ready-to-use test request.

**Response Example:**
```json
{
  "IHttpClientFactory": "{...external API response...}",
  "DirectHttpClient": "{...external API response...}"
}
```

Both results should be the same, but IHttpClientFactory is preferred for real-world applications.

# EntityTest

# EntityTest API — .NET 10 Web API with Entity Framework Core & SQL Server

A modern ASP.NET Core Web API project demonstrating advanced .NET concepts:
- Entity Framework Core (with SQL Server)
- Database migrations & seeding
- Categories & ProductDetails (relations)
- RowVersion concurrency (optimistic locking)
- DTOs for API responses
- Global exception handling (middleware & IExceptionHandler)
- In-memory caching
- Content negotiation (JSON/XML)
- Docker Compose for local dev
- Multiple test projects (xUnit, NUnit, MSTest)
- Postman/HTTP integration tests


## Repository & Unit of Work Patterns

This project demonstrates both the Repository and Unit of Work patterns for data access abstraction and transactional consistency.

### Repository Pattern
- Generic `IRepository<T>` and `Repository<T>` provide CRUD operations for any entity.
- Entity-specific repositories (e.g., `ISupplierRepository`, `IProductRepository`) add custom queries.
- Example: `SuppliersController` uses `ISupplierRepository` for CRUD on Supplier.

### Unit of Work Pattern
- `IUnitOfWork` exposes multiple repositories (e.g., `Suppliers`, `Products`) and a single `CompleteAsync()` method to commit all changes atomically.
- Example: `SupplierUowController` demonstrates CRUD and an atomic endpoint that creates both a Supplier and a Product in one transaction.

#### Atomic Transaction Example
POST `/api/supplieruow/atomic` with:
```json
{
  "supplierName": "Atomic Supplier",
  "supplierDescription": "Created with product in one transaction.",
  "supplierEmail": "atomic@supplier.com",
  "productName": "Atomic Product",
  "productDescription": "Created atomically with supplier.",
  "productPrice": 123.45
}
```
Both records are saved only if both succeed.

### Test Files
- `PostmanTEST/Suppliers.http` — CRUD for Supplier (Repository pattern)
- `PostmanTEST/SuppliersUow.http` — CRUD for Supplier (Unit of Work)
- `PostmanTEST/SuppliersUowAtomic.http` — Atomic Supplier+Product creation

## Project Structure

```
EntityTestApi/
├── Controllers/
│   ├── ProductsController.cs       # REST API endpoints (CRUD, error, concurrency)
│   ├── SuppliersController.cs      # CRUD for Supplier (Repository pattern)
│   ├── SupplierUowController.cs    # CRUD & atomic for Supplier/Product (Unit of Work)
│   ├── ErrorController.cs          # Global error endpoint
│   └── ExternalApiTestController.cs # Compare IHttpClientFactory vs direct HttpClient
├── Data/
│   ├── ApplicationDbContext.cs     # EF Core DbContext (Products, Suppliers, Categories, ProductDetails)
│   ├── IRepository.cs              # Generic repository interface
│   ├── Repository.cs               # Generic repository implementation
│   ├── ISupplierRepository.cs      # Supplier-specific repository
│   ├── SupplierRepository.cs       # Supplier repository implementation
│   ├── IProductRepository.cs       # Product-specific repository
│   ├── ProductRepository.cs        # Product repository implementation
│   ├── IUnitOfWork.cs              # Unit of Work interface
│   ├── UnitOfWork.cs               # Unit of Work implementation
│   ├── ApplicationDbContextFactory.cs # Design-time factory for migrations
│   ├── SeedEmployees.sql           # SQL seed script (departments, employees)
│   └── Migrations/                 # EF Core migration files
├── Exceptions/
│   └── CustomExceptions.cs         # NotFound, Validation, Conflict, BusinessRule
├── Middleware/
│   ├── CustomExceptionHandler.cs   # IExceptionHandler (ASP.NET Core 8+)
│   ├── GlobalExceptionHandlerMiddleware.cs # Classic middleware
│   └── CustomLoggingMiddleware.cs  # Request/response logging
├── Models/
│   ├── Product.cs                  # Product entity (with RowVersion)
│   ├── Supplier.cs                 # Supplier entity
│   ├── Category.cs                 # Category entity (1:N)
│   ├── ProductDetail.cs            # ProductDetail entity (1:1)
│   └── DTOs/
│       └── ProductDto.cs           # API DTO (no circular refs)

├── CQRS/
│   ├── Commands/
│   │   └── CreateProductCommandHandler.cs # CQRS command & handler example
│   └── Queries/
│       └── GetProductsQueryHandler.cs    # CQRS query & handler example
├── Properties/
│   └── launchSettings.json         # Launch configuration
├── appsettings.json                # App configuration & connection string
├── appsettings.Development.json    # Dev config
├── Program.cs                      # App startup, DI, seeding, middleware
├── Dockerfile                      # API Docker build
├── docker-compose.yml              # Compose for API + SQL Server
└── EntityTestApi.csproj            # Project file
## CQRS Pattern (Command Query Responsibility Segregation)

This project demonstrates CQRS by separating read and write logic into dedicated handlers:

- **Commands**: Write operations (e.g., create product) handled by command handlers
- **Queries**: Read operations (e.g., get products) handled by query handlers

CQRS handlers are registered in DI and can be tested via dedicated endpoints (see below).

### CQRS Endpoints

#### 1. Create Product (CQRS)
```bash
curl -X POST http://localhost:5274/api/products/cqrs \
  -H "Content-Type: application/json" \
  -d '{
    "name": "CQRS Product",
    "price": 99.99
  }'
```
**Response:**
```json
{
  "message": "Product creation command handled (CQRS pattern)."
}
```

#### 2. Get Products (CQRS)
```bash
curl http://localhost:5274/api/products/cqrs
```
**Response:**
```json
["Product1", "Product2"]
```


Test Projects:
- EntityTestApi.Tests/ (xUnit)
- EntityTestApi.NUnit.Tests/ (NUnit)
- EntityTestApi.MSTest.Tests/ (MSTest)

Integration/Manual API Tests:
- PostmanTEST/*.http (HTTP/REST scripts for VS Code/REST Client/Postman)
```

## Prerequisites

- **.NET 10 SDK** — [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker** (for local SQL Server & API)
- **SQL Server** — via Docker or local install (default: `localhost:1433`)
  - Default credentials: `User Id=SA; Password=Test@2011` (see `appsettings.json`)

## Setup & Installation

### 1. Clone/Navigate to Project
```bash
git clone <repo-url>
cd entitycore/Entitytest
```

### 2. (Optional) Update Connection String
Edit `EntityTestApi/appsettings.json` if not using Docker Compose:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=EntityTestDb;User Id=sa;Password=Test@2011;TrustServerCertificate=True;"
  }
}
```

### 3. Run with Docker Compose (Recommended)
```bash
docker-compose up --build
```
This will start both SQL Server and the API. The API will be available at `http://localhost:5274`.

### 4. Or Run Locally (Manual SQL Server)
```bash
cd EntityTestApi
dotnet restore
dotnet ef database update   # Creates DB, tables, seeds data
dotnet run
```
API runs at `http://localhost:5274`.


### 5. Build the Project
```bash
dotnet build
```


## Running the Application

Start the API server (if not using Docker Compose):
```bash
dotnet run --project EntityTestApi
```

The app will start on **`http://localhost:5274`**

You should see console output:
```
✓ Sample products seeded successfully!
```


## API Endpoints & Features

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


### 1. GET All Products (with Category)
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
    "price": 1299.99,
    "categoryId": 1,
    "categoryName": "Default"
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
  "price": 1299.99,
  "categoryId": 1,
  "categoryName": "Default"
}
```
**Error Response (404):**
```json
{
  "statusCode": 404,
  "message": "Product with ID 999 not found",
  "details": "...stack trace...",
  "timestamp": "..."
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
  "price": 19.99,
  "categoryId": 1,
  "categoryName": "Default"
}
```


### 4. UPDATE Product (PUT, with RowVersion for concurrency)
```bash
curl -X PUT http://localhost:5274/api/products/1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Gaming Laptop",
    "description": "High-performance gaming laptop",
    "price": 1599.99,
    "rowVersion": "AAAAAAAAVfc="
  }'
```
**Response (200 OK):**
```json
{
  "id": 1,
  "name": "Gaming Laptop",
  "description": "High-performance gaming laptop",
  "price": 1599.99,
  "categoryId": 1,
  "categoryName": "Default"
}
```
**Concurrency Error (409):**
```json
{
  "message": "The record you attempted to edit was modified by another user after you got the original value. The edit operation was canceled."
}
```


### 5. DELETE Product
```bash
curl -X DELETE http://localhost:5274/api/products/1
```
**Response (204 No Content)** — No body returned, just success status.


## Database Details

**Database Name:** `EntityTestDb`

**Tables:**



### Products
| Column      | Type            | Nullable | Notes                        |
|-------------|-----------------|----------|------------------------------|
| Id          | int             | NO       | Primary Key, Identity        |
| Name        | nvarchar(200)   | NO       | Required, max 200 chars      |
| Description | nvarchar(max)   | YES      | Optional                     |
| Price       | decimal(18,2)   | NO       | Currency format              |
| CategoryId  | int             | NO       | FK to Categories, default 1  |
| RowVersion  | rowversion      | NO       | For concurrency checks       |

### Suppliers
| Column        | Type            | Nullable | Notes                        |
|---------------|-----------------|----------|------------------------------|
| Id            | int             | NO       | Primary Key, Identity        |
| Name          | nvarchar(200)   | NO       | Required, max 200 chars      |
| Description   | nvarchar(1000)  | YES      | Optional                     |
| ContactEmail  | nvarchar(100)   | YES      | Optional                     |

### Categories
| Column | Type          | Nullable | Notes                 |
|--------|---------------|----------|-----------------------|
| Id     | int           | NO       | Primary Key           |
| Name   | nvarchar(100) | NO       | Required, unique      |

### ProductDetails
| Column    | Type          | Nullable | Notes                |
|-----------|---------------|----------|----------------------|
| Id        | int           | NO       | Primary Key          |
| Details   | nvarchar(max) | NO       |                      |
| ProductId | int           | NO       | 1:1 with Product     |

### Suppliers
| Column        | Type            | Nullable | Notes                        |
|---------------|-----------------|----------|------------------------------|
| Id            | int             | NO       | Primary Key, Identity        |
| Name          | nvarchar(200)   | NO       | Required, max 200 chars      |
| Description   | nvarchar(1000)  | YES      | Optional                     |
| ContactEmail  | nvarchar(100)   | YES      | Optional                     |

### Categories
| Column | Type          | Nullable | Notes                 |
|--------|---------------|----------|-----------------------|
| Id     | int           | NO       | Primary Key           |
| Name   | nvarchar(100) | NO       | Required, unique      |

### ProductDetails
| Column    | Type          | Nullable | Notes                |
|-----------|---------------|----------|----------------------|
| Id        | int           | NO       | Primary Key          |
| Details   | nvarchar(max) | NO       |                      |
| ProductId | int           | NO       | 1:1 with Product     |


## Managing Migrations & Seeding

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


## Project Architecture & Features


### Models
- **Product**: Id, Name, Description, Price, CategoryId, RowVersion, ProductDetail
- **Category**: Id, Name, Products (1:N)
- **ProductDetail**: Id, Details, ProductId (1:1)
- **DTOs**: ProductDto (no circular refs)

### DbContext
- `ApplicationDbContext` exposes DbSets for all entities
- Configures relationships, seeding, decimal precision, rowversion

### Controllers
- `ProductsController`: CRUD, error simulation, concurrency, content negotiation (JSON/XML)
- `ErrorController`: Global error endpoint

### Middleware
- `GlobalExceptionHandlerMiddleware`: Catches all unhandled exceptions, returns JSON error
- `CustomExceptionHandler`: IExceptionHandler (ASP.NET Core 8+)
- `CustomLoggingMiddleware`: Logs all requests/responses

### Caching
- In-memory caching for GET all products

### Content Negotiation
- Supports JSON and XML via Accept header or ?format=xml

### Testing
- xUnit, NUnit, MSTest projects for ProductsController
- Postman/HTTP files for manual/integration API tests

### Docker
- Dockerfile for API
- docker-compose.yml for API + SQL Server

### Seeding
- Products seeded on startup (dev)
- SQL script for Employees/Departments

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


## Troubleshooting & Tips

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
Test projects included:
- `EntityTestApi.Tests` (xUnit)
- `EntityTestApi.NUnit.Tests` (NUnit)
- `EntityTestApi.MSTest.Tests` (MSTest)

Test CRUD operations, error handling, and caching using in-memory database and mocks.

HTTP/REST integration tests: see `PostmanTEST/*.http` for concurrency, error, and CRUD scenarios.

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
**Last Updated:** January 5, 2026
**Version:** 1.0.0  
**Target Framework:** .NET 10
