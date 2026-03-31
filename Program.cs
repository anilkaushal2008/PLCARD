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
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// --- 3. Core Web Services ---
builder.Services.AddRazorPages();

// --- 4. Custom Sync Services (CRITICAL) ---
// Add HttpClient so the worker can talk to your 10 remote APIs
builder.Services.AddHttpClient();

// Add SyncService so your "Register" pages can add items to the queue
builder.Services.AddScoped<SyncService>();

// Add the Background Worker to process the queue automatically
builder.Services.AddHostedService<GlobalSyncWorker>();

// --- 5. Build the Application ---
// The "Kitchen is closed" after this line!
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
app.UseStaticFiles(); // Note: Changed MapStaticAssets to standard UseStaticFiles for compatibility

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();