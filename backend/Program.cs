using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.Models;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;
using VirtualTryOn.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173", "http://127.0.0.1:5173"];

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .SelectMany(item => item.Value!.Errors.Select(error => error.ErrorMessage))
                .ToArray();

            return new BadRequestObjectResult(ApiError.Create("Validation failed.", errors));
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
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration
        .GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is not configured.");



    options.UseSqlServer(
        connectionString,
        sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);

            sqlServerOptions.CommandTimeout(60);
        });
});
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection(GoogleAuthOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<AppUrlOptions>(builder.Configuration.GetSection(AppUrlOptions.SectionName));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddSingleton<GoogleTokenVerifier>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<FavoriteService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ProductReviewService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AdminProductService>();
builder.Services.AddScoped<PromotionService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<AdminCustomerService>();
builder.Services.AddScoped<AdminCategoryService>();
builder.Services.AddScoped<AdminAuditService>();
builder.Services.AddSignalR();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var jwtSecret = Encoding.UTF8.GetBytes(jwtOptions.Secret);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(jwtSecret),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    await CatalogSeeder.SeedAsync(dbContext);
  //  await ProductSeeder.SeedAsync(dbContext);
}
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception) when (IsDatabaseUnavailable(exception))
    {
        if (context.Response.HasStarted)
        {
            throw;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(ApiError.Create(
            "Database is temporarily unavailable.",
            "Please wait a moment and try again."));
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var origin = context.Context.Request.Headers.Origin.ToString();

        if (allowedOrigins.Contains(
                origin,
                StringComparer.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.AccessControlAllowOrigin = origin;
            context.Context.Response.Headers.Vary = "Origin";
        }
    }
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");

app.Run();

static bool IsDatabaseUnavailable(Exception exception)
{
    if (exception is TimeoutException)
    {
        return true;
    }

    if (exception is SqlException sqlException)
    {
        foreach (SqlError error in sqlException.Errors)
        {
            if (IsTransientSqlError(error.Number))
            {
                return true;
            }
        }
    }

    return exception.InnerException is not null && IsDatabaseUnavailable(exception.InnerException);
}

static bool IsTransientSqlError(int errorNumber)
{
    return errorNumber is
        -2 or
        53 or
        64 or
        233 or
        4060 or
        10053 or
        10054 or
        10060 or
        10928 or
        10929 or
        40197 or
        40501 or
        40613;
}
