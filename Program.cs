using ABCRetailers.Data;
using ABCRetailers.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// === SERVICES ===
builder.Services.AddControllersWithViews();  // MVC
builder.Services.AddControllers();           // API

builder.Services.AddHttpClient<ApiClientService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ApiClientService>();

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// === MIDDLEWARE ===
app.UseDeveloperExceptionPage();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAntiforgery();
app.UseAuthorization();

// === ROUTES ===
// THESE TWO LINES ARE REQUIRED:
app.MapControllers();                    // API ROUTES
app.MapControllerRoute(                  // MVC ROUTES
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();