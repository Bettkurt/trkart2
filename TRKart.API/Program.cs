using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TRKart.Business.Interfaces;
using TRKart.Business.Services;
using TRKart.Core.Helpers;
using TRKart.DataAccess;
using TRKart.Repository.Interfaces;
using TRKart.Repository.Repositories;
using TRKart.API.Middleware;
using TRKart.API.Services;
using TRKart.API.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// 1. PostgreSQL connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Controller service
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 3. CORS configuration for local development
var allowedOrigins = new[] 
{
    "http://localhost:3000",
    "https://localhost:3000",  // Frontend
    "http://localhost:7037",
    "https://localhost:7037"  // Swagger/API interface
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("Set-Cookie");
    });
});

// 4. Register application services
builder.Services.AddScoped<TRKart.Core.Interfaces.IUniqueNumberChecker, TRKart.DataAccess.Services.UniqueNumberChecker>();

// 4.1. Register card expiration services
builder.Services.AddScoped<ICardExpirationService, CardExpirationService>();
builder.Services.AddHostedService<CardExpirationBackgroundService>();

// 4.2. Register card balance transfer services
builder.Services.AddScoped<ICardBalanceTransferService, CardBalanceTransferService>();
builder.Services.AddHostedService<CardBalanceTransferBackgroundService>();

// 5. Swagger + JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TRKart API", Version = "v1" });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Bearer {your JWT token}",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

// 6. JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
    };
});

// 7. Configure Token Cleanup Settings
builder.Services.Configure<TokenCleanupSettings>(
    builder.Configuration.GetSection("TokenCleanup"));

// 8. Register Background Services
builder.Services.AddHostedService<TokenCleanupService>();

// 9. DI Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddScoped<IUserCardService, UserCardService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<ITransactionRepository, TRKart.Repository.Repositories.TransactionRepository>();
builder.Services.AddScoped<IInputValidationService, TRKart.Business.Services.InputValidationService>();

var app = builder.Build();

// Use custom JWT middleware before authorization
app.UseJwtMiddleware();

// 8. Swagger only active on development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 9. Middleware order - CORS before authentication
// app.UseHttpsRedirection(); // Disabled for HTTP development
app.UseCors("AllowedOrigins");
app.UseAuthenticationMiddleware(); // Custom authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// app.MapGet("/", () => "API çalışıyor!").AllowAnonymous();
// Use the bottom one to directly connect to swagger interface
app.MapGet("/", () => Results.Redirect("/swagger/index.html", true, true)).AllowAnonymous();

// Force HTTP for development
app.Run("http://localhost:7037");
