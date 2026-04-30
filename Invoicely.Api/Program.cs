using System.Text;
using FluentValidation;
using Invoicely.Core.Entities;
using Invoicely.Core.Enums;
using Invoicely.Core.Interfaces;
using Invoicely.Infrastructure.Data;
using Invoicely.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("FinanceOrAdmin", p => p.RequireRole("Admin", "FinanceManager"));
    options.AddPolicy("AllRoles", p => p.RequireRole("Admin", "FinanceManager", "Employee", "Viewer"));
    options.AddPolicy("CanCreateInvoice", p => p.RequireRole("Admin", "FinanceManager", "Employee"));
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();

var allowedOrigins = (builder.Configuration["AllowedOrigins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    SeedDemoUsers(db);
}

app.Run();

static void SeedDemoUsers(AppDbContext db)
{
    if (db.Users.Any()) return;

    var users = new[]
    {
        new User { Name = "Admin User",      Email = "admin@invoicely.com",    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),    Role = UserRole.Admin },
        new User { Name = "Finance Manager", Email = "finance@invoicely.com",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("finance123"),  Role = UserRole.FinanceManager },
        new User { Name = "Employee",        Email = "employee@invoicely.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("employee123"), Role = UserRole.Employee },
        new User { Name = "Viewer",          Email = "viewer@invoicely.com",   PasswordHash = BCrypt.Net.BCrypt.HashPassword("viewer123"),   Role = UserRole.Viewer },
    };

    db.Users.AddRange(users);
    db.SaveChanges();
}
