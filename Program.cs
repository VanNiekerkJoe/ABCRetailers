using ABCRetailers.Data;
using ABCRetailers.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// === CONFIGURATION SETUP ===
// Add configuration sources
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// === SERVICES ===
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();

builder.Services.AddHttpClient<ApiClientService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ApiClientService>();

// === DATABASE CONFIGURATION ===
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    // Try to get from environment variables as fallback
    connectionString = Environment.GetEnvironmentVariable("SQLCONNSTR_DefaultConnection")
                    ?? Environment.GetEnvironmentVariable("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

Console.WriteLine($"Database Connection: {(!string.IsNullOrEmpty(connectionString) ? "Found" : "Not Found")}");

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(connectionString));

// Alternative: Add with error handling
// builder.Services.AddDbContext<AuthDbContext>((serviceProvider, options) =>
// {
//     var config = serviceProvider.GetRequiredService<IConfiguration>();
//     var connString = config.GetConnectionString("DefaultConnection");
//     if (string.IsNullOrEmpty(connString))
//     {
//         throw new InvalidOperationException("Database connection string is not configured.");
//     }
//     options.UseSqlServer(connString);
// });

builder.Services.AddAntiforgery();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

// === DATABASE INITIALIZATION ===
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    // Test database connection
    await dbContext.Database.CanConnectAsync();
    Console.WriteLine("Database connection successful!");
}
catch (Exception ex)
{
    Console.WriteLine($"Database connection failed: {ex.Message}");
    // Don't throw here - let the app start and show user-friendly error
}

// === MIDDLEWARE ===
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAntiforgery();
app.UseAuthorization();

// === ROUTES ===
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Console.WriteLine("Application starting...");
app.Run();