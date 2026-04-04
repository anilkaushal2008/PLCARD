using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;
using PLCARD.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Database Connections ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<PLCARDContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- 2. Identity (Login/Security) ---
// MODIFIED: Added .AddRoles<IdentityRole>() and set RequireConfirmedAccount to false
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false; // Optional: Makes testing easier
})
    .AddRoles<IdentityRole>() // CRITICAL: This enables the RoleManager
    .AddEntityFrameworkStores<ApplicationDbContext>();

// --- 3. Core Web Services ---
builder.Services.AddRazorPages();

// --- 4. Custom Sync Services ---
builder.Services.AddHttpClient();
builder.Services.AddScoped<SyncService>();
builder.Services.AddHostedService<GlobalSyncWorker>();

// --- 5. Build the Application ---
var app = builder.Build();

// --- 6. Configure the HTTP Pipeline (Middleware) ---
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Middleware order is important
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// --- 7. Seed Roles and Admin User ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // This will now work because we added .AddRoles above
        await DbInitializer.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database seeding.");
    }
}

app.Run();