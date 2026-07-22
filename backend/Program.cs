using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.Models;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>()
    ?? [
        "http://localhost:5173",
        "http://127.0.0.1:5173"
    ];

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory =
            context =>
            {
                var errors = context.ModelState
                    .Where(item =>
                        item.Value?.Errors.Count > 0)
                    .SelectMany(item =>
                        item.Value!.Errors.Select(
                            error =>
                                error.ErrorMessage))
                    .ToArray();

                return new BadRequestObjectResult(
                    ApiError.Create(
                        "Validation failed.",
                        errors));
            };
    });

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        var connectionString =
            builder.Configuration
                .GetConnectionString(
                    "DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured.");

        options.UseSqlServer(connectionString);
    });

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(
        JwtOptions.SectionName));

builder.Services.Configure<GoogleAuthOptions>(
    builder.Configuration.GetSection(
        GoogleAuthOptions.SectionName));

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(
        EmailOptions.SectionName));

builder.Services.Configure<AppUrlOptions>(
    builder.Configuration.GetSection(
        AppUrlOptions.SectionName));

builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddSingleton<
    GoogleTokenVerifier>();

builder.Services.AddScoped<
    IEmailSender,
    SmtpEmailSender>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<FavoriteService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ProductReviewService>();
builder.Services.AddScoped<NotificationService>();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? new JwtOptions();

var jwtSecret = Encoding.UTF8.GetBytes(
    jwtOptions.Secret);

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        jwtSecret),
                ClockSkew =
                    TimeSpan.FromMinutes(1)
            };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope =
       app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    await CatalogSeeder.SeedAsync(dbContext);
    await ProductSeeder.SeedAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();