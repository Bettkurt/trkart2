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
using Serilog;
using Serilog.Events;
using TRKart.API.Services;
using TRKart.API.BackgroundServices;
using Hangfire;
using TRKart.API.BackgroundServices.Hangfire;
using TRKart.API;
using TRKart.API.Filters;
//using TRKart.API.Mapping;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

try
{
    // Make the main method async
    await RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

async Task RunAsync()
{
    Log.Information("Starting TRKart API...");

    // Add Serilog to the application
    builder.Host.UseSerilog();

    // 1. PostgreSQL connection
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // 2. Controller service
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

    // 3. CORS configuration
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
        ?? new[] 
        {
            "http://localhost:3000",   // Frontend (HTTP)
            "https://localhost:3000",  // Frontend (HTTPS)
            "http://localhost:7037",   // API (HTTP)
            "https://localhost:7037"   // API (HTTPS)
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

    // 4.2. Register card balance transfer services
    builder.Services.AddScoped<ICardBalanceTransferService, CardBalanceTransferService>();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
       
        };
    });

    // 7. Configure Token Cleanup Settings
    builder.Services.Configure<TokenCleanupSettings>(
        builder.Configuration.GetSection("TokenCleanup"));

    // 8. Register Background Services
    builder.Services.AddHostedService<TokenCleanupBackgroundService>();

    // 10. DI Services
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddSingleton<JwtHelper>();
    builder.Services.AddScoped<IUserCardService, UserCardService>();
    builder.Services.AddScoped<ITransactionService, TransactionService>();
    builder.Services.AddScoped<ITransferService, TransferService>();
    builder.Services.AddScoped<ITopUpService, TopUpService>();
    builder.Services.AddScoped<ITransactionRepository, TRKart.Repository.Repositories.TransactionRepository>();
    builder.Services.AddScoped<IInputValidationService, TRKart.Business.Services.InputValidationService>();
    builder.Services.AddScoped<IWalletService>(sp => 
        new WalletService(
            sp.GetRequiredService<ApplicationDbContext>(),
            sp.GetRequiredService<IUserCardService>(),
            sp.GetRequiredService<ILogger<WalletService>>()));
    builder.Services.AddScoped<ICardBalanceTransferService, CardBalanceTransferService>();
    
    // 10. Add Hangfire services with dedicated connection string
    var hangfireConnection = builder.Configuration.GetConnectionString("HangfireConnection") ?? 
        throw new InvalidOperationException("HangfireConnection string is not configured");
        
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(hangfireConnection));

    builder.Services.AddHangfireServer(options =>
    {
        options.ServerName = $"{Environment.MachineName}";
        options.WorkerCount = 1;
    });
    
    // Register Hangfire services
    builder.Services.AddScoped<HangfireCardBalanceTransferService>();
    builder.Services.AddScoped<HangfireCardExpirationService>();

    var app = builder.Build();

    // Database initialization is handled by migrations at startup
    // No need for manual migration in code
    
    // Configure Hangfire dashboard without authentication
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new DashboardNoAuthorizationFilter() },
        DashboardTitle = "TRKart Jobs (Development)",
        StatsPollingInterval = 60000, // 1 minute
        DisplayStorageConnectionString = false,
        IgnoreAntiforgeryToken = true
    });
    
    // Initialize Hangfire background processing
    GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3 });
    
    // Schedule recurring jobs if not already scheduled
    using (var scope = app.Services.CreateScope())
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        
        // Schedule card expiration check to run monthly on the 1st at midnight UTC
        recurringJobManager.AddOrUpdate<HangfireCardExpirationService>(
            "card-expiration-check",
            x => x.CheckAndUpdateExpiredCardsAsync(),
            Cron.Monthly,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
            
        // Schedule balance transfer to run daily at midnight UTC
        recurringJobManager.AddOrUpdate<HangfireCardBalanceTransferService>(
            "card-balance-transfer",
            x => x.TransferBalancesAsync(),
            Cron.Daily,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
    
    // Use CORS before authentication
    app.UseCors("AllowedOrigins");
    
    // Use custom JWT middleware before authorization
    app.UseAuthenticationMiddleware();
    
    // Add authentication and authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // 8. Swagger only active on development environment
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage(); 
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // 9. Middleware order - CORS before authentication
    // app.UseHttpsRedirection(); // Disabled for HTTP development
    // Note: CORS, Authentication, and Authorization are already configured above

    app.MapControllers();
    // app.MapGet("/", () => "API çalışıyor!").AllowAnonymous();
    // Use the bottom one to directly connect to swagger interface
    app.MapGet("/", () => Results.Redirect("/swagger/index.html", true, true)).AllowAnonymous();

    // Get the URL from configuration or use default
    var url = builder.Configuration["ApplicationUrl"] ?? "http://localhost:7037";
    Console.WriteLine($"Starting server on {url}");
    await app.RunAsync(url);
   
}
