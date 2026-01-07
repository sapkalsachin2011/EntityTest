using Microsoft.EntityFrameworkCore;
using EntityTestApi.Data;
using Microsoft.IdentityModel.Tokens;
using EntityTestApi.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add log4net as a logging provider
builder.Logging.ClearProviders();
var log4netConfigPath = Path.Combine(AppContext.BaseDirectory, "log4net.config");
builder.Logging.AddLog4Net(log4netConfigPath);

// Enable log4net internal debugging for troubleshooting
log4net.Util.LogLog.InternalDebugging = true;

// Configure DbContext (uses ConnectionStrings:DefaultConnection from appsettings.json)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Fail fast with a helpful message if the connection string is not configured.
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.\n" +
        "Set it in appsettings.json (ConnectionStrings:DefaultConnection),\n" +
        "or set the environment variable 'ConnectionStrings__DefaultConnection'.\n" +
        "Example: export ConnectionStrings__DefaultConnection=\"Server=localhost,1433;Database=EntityTestDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;\"");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddScoped<EntityTestApi.Data.ISupplierRepository, EntityTestApi.Data.SupplierRepository>();
builder.Services.AddScoped<EntityTestApi.Data.IProductRepository, EntityTestApi.Data.ProductRepository>();
builder.Services.AddScoped<EntityTestApi.Data.IUnitOfWork, EntityTestApi.Data.UnitOfWork>();
//builder.Services.AddScoped<EntityTestApi.Data.IProductRepository, EntityTestApi.Data.ProductRepository>();
//builder.Services.AddScoped(typeof(EntityTestApi.Data.IRepository<>), typeof(EntityTestApi.Data.Repository<>));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register ExternalApiService with IHttpClientFactory
builder.Services.AddHttpClient<EntityTestApi.Services.ExternalApiService>();

// Register CQRS command and query handlers
builder.Services.AddScoped<EntityTestApi.CQRS.Commands.ICommandHandler<EntityTestApi.CQRS.Commands.CreateProductCommand>, EntityTestApi.CQRS.Commands.CreateProductCommandHandler>();
builder.Services.AddScoped<EntityTestApi.CQRS.Queries.IQueryHandler<EntityTestApi.CQRS.Queries.GetProductsQuery, IEnumerable<string>>, EntityTestApi.CQRS.Queries.GetProductsQueryHandler>();

// Add support for content negotiation (JSON and XML)
builder.Services.AddControllers(options =>
{
    options.RespectBrowserAcceptHeader = true; // Enable content negotiation based on Accept header
    options.ReturnHttpNotAcceptable = true; // Return 406 if requested format not supported
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
})
.AddXmlSerializerFormatters() // Add XML support
.AddXmlDataContractSerializerFormatters(); // Add XML DataContract support


builder.Services.AddOutputCache();
builder.Services.AddMemoryCache(); // Add In-Memory Caching
builder.Services.AddScoped<EntityTestApi.Middleware.CustomLoggingMiddleware>();
builder.Services.AddScoped<GlobalExceptionHandlerMiddleware>(); // Register exception handler

// Register ProductCache as singleton
builder.Services.AddSingleton<EntityTestApi.Services.IProductCache, EntityTestApi.Services.ProductCache>();

// Add Redis distributed cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:Configuration"];
});

var app = builder.Build();

// Ensure Log directory exists for log4net file appender
var logDir = Path.Combine(AppContext.BaseDirectory, "Log");
if (!Directory.Exists(logDir))
{
    Directory.CreateDirectory(logDir);
}

// Ensure Supplierlog.txt file exists (create if missing)
var logFile = Path.Combine(logDir, "Supplierlog.txt");
if (!File.Exists(logFile))
{
    File.Create(logFile).Dispose(); // Create and close the file
}

// Print log directory and file path to console
Console.WriteLine($"Log directory: {logDir}");
Console.WriteLine($"Log file: {logFile}");

// Configure the HTTP request pipeline.
// Global exception handler - MUST be first middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>(); // Classic IMiddleware approach
// app.UseExceptionHandler<CustomExceptionHandler>(); // Modern IExceptionHandler approach (ASP.NET Core 8+)

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseOutputCache();
app.UseHttpsRedirection();  
app.UseAuthorization();

// Middleware 1 - Always executes first
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    Console.WriteLine("→ Middleware 1: BEFORE calling next");
    logger.LogInformation("→ Middleware 1: BEFORE calling next");
    await next(); // Calls next middleware
    Console.WriteLine("← Middleware 1: AFTER");
    logger.LogInformation("← Middleware 1: AFTER");
});

// Middleware 2 - Executes second
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    Console.WriteLine("  → Middleware 2: BEFORE calling next");
    logger.LogInformation("  → Middleware 2: BEFORE calling next");
    await next(); // Calls next middleware
    Console.WriteLine("  ← Middleware 2: AFTER");
    logger.LogInformation("  ← Middleware 2: AFTER");
});

// Middleware 3 - Executes third
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    Console.WriteLine("    → Middleware 3: BEFORE calling next");
    logger.LogInformation("    → Middleware 3: BEFORE calling next");
    await next(); // Calls next middleware
    Console.WriteLine("    ← Middleware 3: AFTER");
    logger.LogInformation("    ← Middleware 3: AFTER");
});

app.UseMiddleware<EntityTestApi.Middleware.CustomLoggingMiddleware>();

app.Map("/Sachin",CustomCode);

// OPTION A: Uncomment this to test TERMINAL middleware behavior
// This catches ALL requests and STOPS the pipeline
// app.Run(async (context) =>
// {
//     Console.WriteLine("      ⚠ TERMINAL MIDDLEWARE: Pipeline ends here!");
//     await context.Response.WriteAsync("Terminal middleware executed. No controllers will run.");
// });

 app.MapControllers();


// // Add middleware BEFORE app.Run() - this will execute
// app.Use(async (context, next) =>
// {
//     Console.WriteLine("✓ Middleware 1: BEFORE terminal middleware");
//     await next(); // This calls the next middleware
//     Console.WriteLine("✓ Middleware 1: AFTER terminal middleware (will NOT print if app.Run used)");
// });


// app.Run(async (context) =>
// {
//     Console.WriteLine("✓ Terminal middleware: This catches ALL requests!");
//     await context.Response.WriteAsync("Terminal middleware executed. No controllers will run.");
// });



// Seed sample product data on startup in Development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Only seed if the Products table is empty
        if (!dbContext.Products.Any())
        {
            var sampleProducts = new EntityTestApi.Models.Product[]
            {
                new EntityTestApi.Models.Product
                {
                    Name = "Laptop",
                    Description = "High-performance laptop for developers",
                    Price = 1299.99m
                },
                new EntityTestApi.Models.Product
                {
                    Name = "Monitor",
                    Description = "4K Ultra HD Monitor 27 inch",
                    Price = 399.99m
                },
                new EntityTestApi.Models.Product
                {
                    Name = "Keyboard",
                    Description = "Mechanical gaming keyboard",
                    Price = 149.99m
                },
                new EntityTestApi.Models.Product
                {
                    Name = "Mouse",
                    Description = "Wireless ergonomic mouse",
                    Price = 49.99m
                },
                new EntityTestApi.Models.Product
                {
                    Name = "Headphones",
                    Description = "Noise-cancelling Bluetooth headphones",
                    Price = 199.99m
                }
            };

            dbContext.Products.AddRange(sampleProducts);
            dbContext.SaveChanges();
            Console.WriteLine("✓ Sample products seeded successfully!");
        }
    }
}

// Simple root endpoint
app.MapGet("/", () => new { message = "Welcome to EntityTest API!", docs = "Visit http://localhost:5274/openapi/v1.json for API specification" });

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

//app.Run();
// app.Run(async (context) =>
// {
//    // await Task.Delay(1000);
//     Console.WriteLine("✓ Application started successfully!");
// });



// Add CustomCode method here
void CustomCode(IApplicationBuilder app)
{
    // Middleware inside the /Sachin branch
    app.Use(async (context, next) =>
    {
        Console.WriteLine("      → Inside /Sachin branch - Middleware 1");
        await context.Response.WriteAsync("Hello from /Sachin middleware 1\n");
        await next();
        Console.WriteLine("      ← Exiting /Sachin branch - Middleware 1");
    });

    app.Use(async (context, next) =>
    {
        Console.WriteLine("        → Inside /Sachin branch - Middleware 2");
        await context.Response.WriteAsync("Hello from /Sachin middleware 2\n");
        await next();
        Console.WriteLine("        ← Exiting /Sachin branch - Middleware 2");
    });

    // Terminal middleware for this branch
    app.Run(async (context) =>
    {
        Console.WriteLine("          → Terminal middleware in /Sachin branch");
        await context.Response.WriteAsync("Final message from /Sachin branch!");
    });
}

Console.WriteLine("✓ Application configured successfully!");
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}