using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using HomeServicesPortal.Data;
using HomeServicesPortal.Infrastructure;
using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Mappings;
using HomeServicesPortal.Middleware;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Repositories;
using HomeServicesPortal.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    DevSqlTunnelBootstrap.EnsureStarted(
        builder.Configuration,
        builder.Environment,
        LoggerFactory.Create(b => b.AddConsole()).CreateLogger("DevSqlTunnel"));
}

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var isDevelopment = builder.Environment.IsDevelopment();

void ConfigureSqlServer(DbContextOptionsBuilder options) =>
    options.UseSqlServer(connectionString, sql =>
    {
        if (isDevelopment)
        {
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(3),
                errorNumbersToAdd: null);
        }
    });

builder.Services.AddDbContext<SahulatAppDbContext>(ConfigureSqlServer);
builder.Services.AddDbContext<AppDbContext>(ConfigureSqlServer);

builder.Services.AddAutoMapper(typeof(AuthMappingProfile));

builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IServiceCategoryService, ServiceCategoryService>();
builder.Services.AddScoped<IServiceProviderService, ServiceProviderService>();
builder.Services.AddScoped<IProviderLocationService, ProviderLocationService>();
builder.Services.AddScoped<IProviderAvailabilityService, ProviderAvailabilityService>();
builder.Services.AddScoped<IProviderDetailService, ProviderDetailService>();
builder.Services.AddScoped<IClientAddressService, ClientAddressService>();
builder.Services.AddScoped<ICustomerServiceRequestService, CustomerServiceRequestService>();
builder.Services.AddScoped<IServiceBookingApiService, ServiceBookingApiService>();
builder.Services.AddScoped<IProviderDocumentService, ProviderDocumentService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
builder.Services.AddScoped<IProviderQuoteService, ProviderQuoteService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBookingTrackingService, BookingTrackingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICommissionRuleService, CommissionRuleService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("Jwt:Key is not configured in appsettings.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SahulatGharTak";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SahulatGharTakClients";

// Cookie auth for admin portal (UsersLogin + Staff). No AspNet Identity tables / migrations.
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/adminportal";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/adminportal";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(
                    ApiResponse<object>.Fail("Authentication required. Provide a valid Bearer token."));
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(
                    ApiResponse<object>.Fail("Access denied."));
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(
                        ApiResponse<object>.Fail("Authentication required. Provide a valid Bearer token."));
                }
            },
            OnForbidden = async context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(
                        ApiResponse<object>.Fail("Access denied."));
                }
            }
        };
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
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
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

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sahulat Ghar Tak API v1");
    options.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Run();
