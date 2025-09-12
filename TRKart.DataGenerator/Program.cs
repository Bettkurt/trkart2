using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TRKart.DataAccess;
using TRKart.DataAccess.Services;
using TRKart.Core.Interfaces;
using TRKart.DataGenerator.Database;
using TRKart.DataGenerator.Services;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Setup DI
var serviceProvider = new ServiceCollection()
    .AddSingleton<IConfiguration>(configuration)
    .AddScoped<DatabaseContextFactory>()
    .AddScoped(provider =>
    {
        var factory = provider.GetRequiredService<DatabaseContextFactory>();
        return factory.CreateContext();
    })
    .AddScoped<IUniqueNumberChecker, UniqueNumberChecker>()
    .AddScoped<DataGeneratorService>()
    .BuildServiceProvider();

// Run the application
await RunAsync(serviceProvider);

async Task RunAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var generator = scope.ServiceProvider.GetRequiredService<DataGeneratorService>();
    
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("=============================================");
    Console.WriteLine("        TRKart Test Data Generator");
    Console.WriteLine("=============================================");
    Console.ResetColor();
    
    Console.WriteLine("\nThis tool will generate test data for the TRKart system.");
    Console.WriteLine("Please make sure you have backed up your database before proceeding.\n");
    
   // Console.Write("Enter the number of customers to generate (press Enter for default): ");
   // if (int.TryParse(Console.ReadLine(), out int customerCount) && customerCount > 0)
   // {
        var config = configuration.GetSection("DataGeneration").Get<DataGenerationSettings>() ?? new DataGenerationSettings();
        config.CustomerCount = 100;
        configuration.GetSection("DataGeneration").Bind(config);
   // }
    
    Console.WriteLine("\nStarting data generation with the following settings:");
    Console.WriteLine($"- Customers: {configuration.GetValue<int>("DataGeneration:CustomerCount")}");
    Console.WriteLine($"- Cards per customer: {configuration.GetValue<int>("DataGeneration:CardsPerCustomer")}");
    Console.WriteLine($"- Transactions per card: {configuration.GetValue<int>("DataGeneration:TransactionsPerCard")}");
    Console.WriteLine($"- Max sessions per user: {configuration.GetValue<int>("DataGeneration:MaxSessionsPerUser")}");
    Console.WriteLine($"- Date range: {configuration.GetValue<DateTimeOffset>("DataGeneration:StartDate"):yyyy-MM-dd} to {configuration.GetValue<DateTimeOffset>("DataGeneration:EndDate"):yyyy-MM-dd}");
    
    Console.Write("\nDo you want to continue? (y/n): ");
    var key = Console.ReadKey();
    if (key.KeyChar != 'y' && key.KeyChar != 'Y')
    {
        Console.WriteLine("\nOperation cancelled by user.");
        return;
    }
    
    Console.WriteLine("\n\nStarting data generation...");
    
    try
    {
        await generator.GenerateDataAsync();
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nData generation completed successfully!");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\nAn error occurred during data generation: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
    finally
    {
        Console.ResetColor();
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
