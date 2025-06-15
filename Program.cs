using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using web_lab_4.Models;
using web_lab_4.Repositories;
using web_lab_4.Data;
using web_lab_4.Services;
using web_lab_4.Core.Interface;
using web_lab_4.Core.Manager;
using Microsoft.AspNetCore.Http.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddDefaultTokenProviders()
.AddDefaultUI()
.AddEntityFrameworkStores<ApplicationDbContext>();

// SỬA LẠI CÁCH ĐỌC CONFIGURATION - DÙNG ĐÚNG KEY TỪ APPSETTINGS.JSON
builder.Services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
        var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        
        if (string.IsNullOrEmpty(googleClientId))
        {
            throw new InvalidOperationException("Google ClientId is not configured in appsettings.json");
        }
        
        if (string.IsNullOrEmpty(googleClientSecret))
        {
            throw new InvalidOperationException("Google ClientSecret is not configured in appsettings.json");
        }
        
        googleOptions.ClientId = googleClientId;
        googleOptions.ClientSecret = googleClientSecret;
        
        // Use the default callback path that matches ASP.NET Core Identity
        googleOptions.CallbackPath = "/signin-google";
        
        // Add required scopes
        googleOptions.Scope.Clear();
        googleOptions.Scope.Add("openid");
        googleOptions.Scope.Add("profile");
        googleOptions.Scope.Add("email");
        
        googleOptions.SaveTokens = true;
        
        // Enhanced error handling
        googleOptions.Events.OnRemoteFailure = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError($"Google authentication failed: {context.Failure?.Message}");
            logger.LogError($"Request URL: {context.Request.GetDisplayUrl()}");
            
            // Redirect to login with error message
            context.Response.Redirect("/Identity/Account/Login?error=external_login_failed");
            context.HandleResponse();
            return Task.CompletedTask;
        };
        
        googleOptions.Events.OnAccessDenied = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning($"Google authentication access denied: {context.AccessDeniedPath}");
            
            context.Response.Redirect("/Identity/Account/Login?error=access_denied");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    })
    .AddFacebook(facebookOptions =>
    {
        var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
        var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
        
        if (string.IsNullOrEmpty(facebookAppId))
        {
            throw new InvalidOperationException("Facebook AppId is not configured in appsettings.json");
        }
        
        if (string.IsNullOrEmpty(facebookAppSecret))
        {
            throw new InvalidOperationException("Facebook AppSecret is not configured in appsettings.json");
        }
        
        facebookOptions.AppId = facebookAppId;
        facebookOptions.AppSecret = facebookAppSecret;
        facebookOptions.CallbackPath = "/signin-facebook";
        
        facebookOptions.Events.OnRemoteFailure = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError($"Facebook authentication failed: {context.Failure?.Message}");
            
            context.Response.Redirect("/Identity/Account/Login?error=external");
            context.HandleResponse();
            return Task.CompletedTask;
        };
        
        facebookOptions.Scope.Add("email");
        facebookOptions.Scope.Add("public_profile");
        facebookOptions.Fields.Add("picture");
        facebookOptions.SaveTokens = true;
    });

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Keep existing Repository registrations
builder.Services.AddScoped<IDashboardRepository, EFDashboardRepository>(); 
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
builder.Services.AddScoped<ISessionManager, SessionManager>();
builder.Services.AddMemoryCache();

// Register Review services
builder.Services.AddScoped<IReviewRepository, EFReviewRepository>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DataSeeder.SeedRolesAndUsers(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline
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
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
