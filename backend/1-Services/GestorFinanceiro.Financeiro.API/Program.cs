using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using GestorFinanceiro.Financeiro.API.Filters;
using GestorFinanceiro.Financeiro.API.Middleware;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Auth;
using GestorFinanceiro.Financeiro.Infra.Context;
using GestorFinanceiro.Financeiro.Infra.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});
builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Section 'JwtSettings' was not found.");

if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    throw new InvalidOperationException("JwtSettings:SecretKey must be configured.");
}

if (Encoding.UTF8.GetByteCount(jwtSettings.SecretKey) < 32)
{
    throw new InvalidOperationException("JwtSettings:SecretKey must be at least 256 bits (32 bytes) long for HMAC-SHA256.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationActionFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

if (allowedOrigins.Length == 0 && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("CorsSettings:AllowedOrigins must be configured in non-Development environments.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }
        else
        {
            policy.AllowAnyOrigin();
        }

        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GestorFinanceiro API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT no formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddDbContextCheck<FinanceiroDbContext>()
    .AddNpgSql(connectionString, name: "postgresql");

var app = builder.Build();

await SeedAdminUserAsync(app);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsPolicy");
app.UseAuthentication();

app.Use(async (context, next) =>
{
    var requestLogger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequestScope");
    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? context.User.FindFirstValue("sub")
        ?? "anonymous";

    using (requestLogger.BeginScope(new Dictionary<string, object>
    {
        ["service.name"] = "financeiro",
        ["userId"] = userId,
        ["request_path"] = context.Request.Path.ToString(),
        ["http_method"] = context.Request.Method
    }))
    {
        await next();
    }
});

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

static async Task SeedAdminUserAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("AdminSeed");

    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    var existingUsers = await userRepository.GetAllAsync(CancellationToken.None);
    if (existingUsers.Any())
    {
        return;
    }

    var adminName = app.Configuration["AdminSeed:Name"] ?? "Administrador";
    var adminEmail = app.Configuration["AdminSeed:Email"] ?? "admin@gestorfinanceiro.local";
    var adminPassword = app.Configuration["AdminSeed:Password"] ?? "Admin123!";

    var adminUser = User.Create(
        adminName,
        adminEmail,
        passwordHasher.Hash(adminPassword),
        UserRole.Admin,
        "system");

    await userRepository.AddAsync(adminUser, CancellationToken.None);
    await unitOfWork.SaveChangesAsync(CancellationToken.None);

    logger.LogInformation("Default admin user seeded with email {AdminEmail}", adminEmail);
}
