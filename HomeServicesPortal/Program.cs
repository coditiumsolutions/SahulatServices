using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HomeServicesPortal.Data;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Repositories;
using HomeServicesPortal.Services;
using HomeServicesPortal.Services.Auth;

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
builder.Services.AddScoped<IProviderDocumentService, ProviderDocumentService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
builder.Services.AddScoped<IProviderQuoteService, ProviderQuoteService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBookingTrackingService, BookingTrackingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("Jwt:Key is not configured in appsettings.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SahulatGharTak";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SahulatGharTakClients";

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/adminportal";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/adminportal";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Sahulat Ghar Tak API",
        Version = "v1",
        Description = "Public REST APIs for Sahulat Ghar Tak mobile and web clients."
    });
    options.DocInclusionPredicate((_, apiDesc) =>
        apiDesc.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true);

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Enter JWT token from POST /api/auth/login"
    });
    options.AddSecurityRequirement(_ => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer"),
            []
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("An unexpected error occurred."));
                return;
            }

            context.Response.Redirect("/Home/Error");
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sahulat Ghar Tak API v1");
    options.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Seed Identity roles and a default Super Admin account (development / first run).
// Note: In production, run a dedicated migration/seed step.
try
{
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
}
catch (Exception ex) when (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogWarning(ex, "Database seed skipped — is the SQL tunnel running? (scripts/dev-sql-tunnel.ps1)");
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();
app.MapRazorPages();

app.Run();
