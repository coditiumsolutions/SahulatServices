using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HomeServicesPortal.Data;
using HomeServicesPortal.Repositories;
using HomeServicesPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<SahulatAppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IServiceCategoryService, ServiceCategoryService>();
builder.Services.AddScoped<IServiceProviderService, ServiceProviderService>();
builder.Services.AddScoped<IProviderLocationService, ProviderLocationService>();
builder.Services.AddScoped<IProviderAvailabilityService, ProviderAvailabilityService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Seed Identity roles and a default Super Admin account (development / first run).
// Note: In production, run a dedicated migration/seed step.
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string[] roles = ["Super Admin", "Admin", "Dispatcher", "Customer Support"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Seed default admin users (created on first run only).
    var defaultUsers = new[]
    {
        new { UserName = "admin", Email = "admin@homeservices.local", Password = "Admin@123", Role = "Super Admin" },
        new { UserName = "superadmin@homeservices.local", Email = "superadmin@homeservices.local", Password = "Pakistan@786", Role = "Super Admin" }
    };

    foreach (var seed in defaultUsers)
    {
        var existing = await userManager.FindByNameAsync(seed.UserName)
                       ?? await userManager.FindByEmailAsync(seed.Email);

        if (existing == null)
        {
            var user = new IdentityUser
            {
                UserName = seed.UserName,
                Email = seed.Email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, seed.Password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, seed.Role);
            }
        }
        else if (!await userManager.IsInRoleAsync(existing, seed.Role))
        {
            await userManager.AddToRoleAsync(existing, seed.Role);
        }
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
