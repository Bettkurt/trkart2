using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TRKart.DataAccess;
using TRKart.DataAccess.Services;
using TRKart.Entities.Enums;
using TRKart.Entities.Models;
using BCrypt.Net;
using TRKart.Core.Helpers;
using TRKart.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TRKart.DataGenerator.Services
{
    public class DataGeneratorService
    {
        private readonly ApplicationDbContext _context;
        private readonly Faker _faker;
        private readonly DataGenerationSettings _settings;
        private readonly IUniqueNumberChecker _uniqueNumberChecker;
        private readonly Random _random;

        private async Task UpdateCardStatusesAsync()
        {
            Console.WriteLine("Updating card statuses...");
            var allCards = await _context.UserCard.ToListAsync();
            int updatedCount = 0;

            foreach (var card in allCards)
            {
                // Skip cards that are already Deactivated or Lost
                if (card.CardStatus == CardStatus.Deactivated || card.CardStatus == CardStatus.Lost)
                    continue;

                // 20% chance to update status (Deactivated or Lost)
                if (_random.Next(100) < 20)
                {
                    card.CardStatus = _random.Next(2) == 0 ? CardStatus.Deactivated : CardStatus.Lost;
                    updatedCount++;
                }
                
                // Save in batches of 100
                if (updatedCount % 100 == 0)
                {
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                    Console.WriteLine($"Updated {updatedCount} card statuses...");
                }
            }
            
            // Save any remaining updates
            if (updatedCount % 100 != 0)
            {
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
                Console.WriteLine($"Updated {updatedCount} card statuses in total");
            }
        }

        public DataGeneratorService(ApplicationDbContext context, IConfiguration configuration, IUniqueNumberChecker uniqueNumberChecker)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _faker = new Faker("tr");
            _random = new Random();
            _settings = configuration?.GetSection("DataGeneration").Get<DataGenerationSettings>() 
                ?? throw new ArgumentNullException(nameof(configuration));
            _uniqueNumberChecker = uniqueNumberChecker ?? throw new ArgumentNullException(nameof(uniqueNumberChecker));
        }
        
        private async Task ClearExistingDataAsync()
        {
            Console.WriteLine("Clearing existing data...");
            
            // Clear data in reverse order to respect foreign key constraints
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"SessionToken\"");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Transaction\"");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"UserCard\"");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Customers\"");
            
            Console.WriteLine("Existing data cleared successfully");
        }

        public async Task GenerateDataAsync()
        {
            Console.WriteLine("Starting data generation...");

            //a Commented out to test a DB full of data
            // Clear existing data
            // await ClearExistingDataAsync();

            try
            {
                // 1. Generate Customers
                Console.WriteLine("\n=== Step 1: Generating Customers ===");
                await GenerateCustomersAsync(_settings.CustomerCount);

                // 2. Generate Sessions
                Console.WriteLine("\n=== Step 2: Generating Sessions ===");
                await GenerateSessionsAsync();

                // 3. Generate UserCards (all Inactive)
                Console.WriteLine("\n=== Step 3: Generating UserCards ===");
                await GenerateUserCardsAsync(updateStatuses: false);

                // 4. Initial Transactions (Load transactions first, then some random transactions and transfers)
                Console.WriteLine("\n=== Step 4: Generating Initial Transactions ===");
                await GenerateInitialTransactionsAsync();

                // 5. Update some card statuses (randomly to 0:Deactivated or 2:Lost)
                Console.WriteLine("\n=== Step 5: Updating Card Statuses ===");
                await UpdateCardStatusesAsync();

                // 6. Generate more transactions after status updates
                Console.WriteLine("\n=== Step 6: Generating Additional Transactions ===");
                await GenerateAdditionalTransactionsAsync();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n=== Data generation completed successfully! ===");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError during data generation: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                Console.ResetColor();
                throw;
            }
        }

        private async Task GenerateCustomersAsync(int count)
        {
            Console.WriteLine($"Generating {count} customers...");
            var customers = new List<Customers>();
            
            for (int i = 0; i < count; i++)
            {
                var customerNumber = await CustomerNumberHelper.GenerateCustomerNumberAsync(_uniqueNumberChecker);
                var customer = new Customers
                {
                    CustomerNumber = customerNumber,
                    FullName = _faker.Name.FullName(),
                    Email = _faker.Internet.Email(),
                    VerifiedUser = _random.Next(100) < 95, // 95% verified
                    EmailLastUpdatedAt = DateTimeOffset.UtcNow,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234")
                };
                customers.Add(customer);

                if (customers.Count % 100 == 0)
                {
                    await _context.Customers.AddRangeAsync(customers);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Added {i + 1} customers...");
                    customers.Clear();
                }
            }

            if (customers.Any())
            {
                await _context.Customers.AddRangeAsync(customers);
                await _context.SaveChangesAsync();
            }

            Console.WriteLine($"Generated {count} customers");
        }

        private async Task GenerateUserCardsAsync(bool updateStatuses = true)
        {
            Console.WriteLine("Generating user cards...");
            // Only get customers that don't have any cards yet
            var customersWithoutCards = await _context.Customers
                .Where(c => !_context.UserCard.Any(uc => uc.CustomerID == c.CustomerID))
                .ToListAsync();
            
            var cards = new List<UserCard>();
            int totalCards = 0;

            foreach (var customer in customersWithoutCards)
            {
                int cardCount = _random.Next(1, _settings.CardsPerCustomer + 1);

                for (int i = 0; i < cardCount; i++)
                {
                    var cardNumber = await CardNumberHelper.GenerateCardNumberAsync(_uniqueNumberChecker);
                    
                    // Generate varied expiration dates: 20% expired, 80% normal
                    DateTime expirationDate;
                    var expirationType = _random.Next(100);
                    if (expirationType < 20) // 20% expired cards
                    {
                        expirationDate = _faker.Date.Between(DateTime.UtcNow.AddYears(-2), DateTime.UtcNow.AddDays(-1));
                    }
                    else // 80% normal expiration (1-5 years)
                    {
                        expirationDate = _faker.Date.Between(DateTime.UtcNow.AddYears(1), DateTime.UtcNow.AddYears(5));
                    }
                    
                    var card = new UserCard
                    {
                        CustomerID = customer.CustomerID,
                        CardNumber = cardNumber,
                        Balance = _random.Next(100, 10000),
                        // All cards start as Inactive (3) - status updates will be handled separately
                        CardStatus = GetInitialCardStatus(updateStatuses, _random),
                        CardType = (CardType)_random.Next(3), // 0: Standard, 1: Gold, 2: Platinum
                        CardExpirationDate = expirationDate, // Set explicit expiration date
                    };
                    cards.Add(card);
                    totalCards++;

                    if (cards.Count % 100 == 0)
                    {
                        await _context.UserCard.AddRangeAsync(cards);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"Added {totalCards} cards...");
                        cards.Clear();
                    }
                }
            }

            if (cards.Any())
            {
                await _context.UserCard.AddRangeAsync(cards);
                await _context.SaveChangesAsync();
            }

            Console.WriteLine($"Generated {totalCards} user cards");
        }

        private async Task GenerateInitialTransactionsAsync()
        {
            Console.WriteLine("Generating initial transactions (Loads only)...");
            var cards = await _context.UserCard.ToListAsync();
            var transactions = new List<Transaction>();
            int totalTransactions = 0;

            // First, generate a Load transaction for each card
            foreach (var card in cards)
            {
                var loadTransaction = new Transaction
                {
                    CardID = card.CardID,
                    Amount = _random.Next(1000, 10000), // Initial load amount
                    TransactionType = TransactionType.Load,
                    Description = "Initial card load"
                };
                transactions.Add(loadTransaction);
                totalTransactions++;

                // Save in batches of 1000
                if (transactions.Count >= 1000)
                {
                    await _context.Transaction.AddRangeAsync(transactions);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Added {totalTransactions} initial transactions...");
                    transactions.Clear();
                    _context.ChangeTracker.Clear();
                }
            }

            // Save any remaining transactions
            if (transactions.Any())
            {
                await _context.Transaction.AddRangeAsync(transactions);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
            }
            
            // Generate some denied transactions for testing
            await GenerateDeniedTransactionsAsync();

            // Now generate some random transactions and transfers
            totalTransactions = await GenerateRandomTransactionsAndTransfersAsync(totalTransactions);
        }

        private async Task GenerateAdditionalTransactionsAsync()
        {
            Console.WriteLine("Generating additional transactions...");
            await GenerateRandomTransactionsAndTransfersAsync(0);
        }

        private async Task<int> GenerateRandomTransactionsAndTransfersAsync(int initialCount)
        {
            var cards = await _context.UserCard.ToListAsync();
            var transactions = new List<Transaction>();
            int totalTransactions = initialCount;
            
            // Generate initial Load transaction for each card
            totalTransactions = await GenerateInitialLoadTransactionsAsync(cards, transactions, totalTransactions);
            
            // Generate regular transactions (Pay, Load, Refund)
            var transactionTypes = new[] { TransactionType.Pay, TransactionType.Load, TransactionType.Refund };
            totalTransactions = await GenerateRegularTransactionsAsync(cards, transactions, totalTransactions, transactionTypes);
            
            // Generate transfers (pairs of TransferOut and TransferIn)
            totalTransactions = await GenerateTransfersAsync(cards, transactions, totalTransactions);
            
            // Save any remaining transactions
            if (transactions.Count > 0)
            {
                await SaveTransactionsBatchAsync(transactions, totalTransactions);
            }
            
            return totalTransactions;
        }
        
        private async Task<int> GenerateInitialLoadTransactionsAsync(List<UserCard> cards, List<Transaction> transactions, int initialCount)
        {
            int totalTransactions = initialCount;
            foreach (var cardId in cards.Select(c => c.CardID))
            {
                transactions.Add(CreateLoadTransaction(cardId, true));
                totalTransactions++;

                if (transactions.Count >= 1000)
                {
                    await SaveTransactionsBatchAsync(transactions, totalTransactions);
                }
            }
            return totalTransactions;
        }
        
        private async Task<int> GenerateRegularTransactionsAsync(List<UserCard> cards, List<Transaction> transactions, 
            int totalTransactions, TransactionType[] transactionTypes)
        {
            for (int i = 0; i < _settings.TransactionsPerCard * cards.Count; i++)
            {
                var card = cards[_random.Next(cards.Count)];
                var transactionType = transactionTypes[_random.Next(transactionTypes.Length)];
                
                var transaction = new Transaction
                {
                    CardID = card.CardID,
                    Amount = _random.Next(10, 1000),
                    TransactionType = transactionType,
                    TransactionDate = new DateTimeOffset(_faker.Date.Between(_settings.StartDate.DateTime, _settings.EndDate.DateTime), TimeSpan.Zero),
                    Description = $"{transactionType} transaction"
                };
                
                transactions.Add(transaction);
                totalTransactions++;

                if (transactions.Count >= 1000)
                {
                    await SaveTransactionsBatchAsync(transactions, totalTransactions);
                }
            }

            return totalTransactions;
        }

        private static CardStatus GetInitialCardStatus(bool updateStatuses, Random random)
        {
            if (!updateStatuses)
            {
                return CardStatus.Inactive;
            }
            
            return random.Next(2) == 0 ? CardStatus.Deactivated : CardStatus.Lost;
        }
        
        private Transaction CreateLoadTransaction(int cardId, bool isInitialLoad = false)
        {
            var amount = isInitialLoad 
                ? _random.Next(100, 1000) 
                : _random.Next(10, 500);

            return new Transaction
            {
                CardID = cardId,
                Amount = amount,
                TransactionType = TransactionType.Load,
                Description = isInitialLoad ? "Initial card load" : "Card load",
                TransactionDate = new DateTimeOffset(_faker.Date.Between(_settings.StartDate.DateTime, _settings.EndDate.DateTime), TimeSpan.Zero)
            };
        }
        
        private async Task SaveTransactionsBatchAsync(List<Transaction> transactions, int totalTransactions)
        {
            await _context.Transaction.AddRangeAsync(transactions);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Added {totalTransactions} transactions...");
            transactions.Clear();
            _context.ChangeTracker.Clear();
        }

        private async Task<int> GenerateTransfersAsync(List<UserCard> cards, List<Transaction> transactions, int totalTransactions)
        {
            // Generate TransferOut/TransferIn pairs between cards of the same customer
            var customers = await _context.Customers
                .Include(c => c.UserCards)
                .Where(c => c.UserCards.Count > 1)
                .ToListAsync();

            foreach (var customer in customers)
            {
                var customerCards = customer.UserCards.ToList();
                int transferCount = _random.Next(1, customerCards.Count); // Number of transfers for this customer

                for (int i = 0; i < transferCount; i++)
                {
                    // Get two different random cards for the transfer
                    var sourceCard = customerCards[_random.Next(customerCards.Count)];
                    var targetCard = customerCards.First(c => c.CardID != sourceCard.CardID);
                    
                    var maxCreatedAt = new[] { sourceCard.CreatedAt, targetCard.CreatedAt }.Max();
                    var amount = _random.Next(10, 1000);

                    // Create TransferOut transaction
                    var transferOut = new Transaction
                    {
                        CardID = sourceCard.CardID,
                        Amount = amount,
                        TransactionType = TransactionType.TransferOut,
                        TransactionDate = new DateTimeOffset(_faker.Date.Between(maxCreatedAt.DateTime, _settings.EndDate.DateTime), TimeSpan.Zero),
                        Description = $"Transfer to Card {targetCard.CardNumber}"
                    };

                    // Create TransferIn transaction
                    var transferIn = new Transaction
                    {
                        CardID = targetCard.CardID,
                        Amount = amount, // Positive amount
                        TransactionType = TransactionType.TransferIn,
                        TransactionDate = new DateTimeOffset(_faker.Date.Between(maxCreatedAt.DateTime, _settings.EndDate.DateTime), TimeSpan.Zero),
                        Description = $"Transfer from Card {sourceCard.CardNumber}"
                    };

                    // Save TransferOut transaction first to get its ID
                    await _context.Transaction.AddAsync(transferOut);
                    await _context.SaveChangesAsync();
                    
                    // Now set the TransferTransactionID for TransferIn
                    transferIn.TransferTransactionID = transferOut.TransactionID;
                    
                    // Save TransferIn transaction
                    await _context.Transaction.AddAsync(transferIn);
                    await _context.SaveChangesAsync();
                    
                    // Update the TransferOut's TransferTransactionID to point to TransferIn
                    transferOut.TransferTransactionID = transferIn.TransactionID;
                    await _context.SaveChangesAsync();
                    
                    totalTransactions += 2;
                    Console.WriteLine($"Generated transfer pair: {totalTransactions} transactions total...");
                    
                    // Clear change tracker to prevent memory issues
                    _context.ChangeTracker.Clear();
                }
            }

            return totalTransactions;
        }

        private async Task GenerateSessionsAsync()
        {
            Console.WriteLine("Generating sessions...");
            var customers = await _context.Customers.ToListAsync();
            var sessions = new List<SessionToken>();
            int totalSessions = 0;
            var currentDate = DateTimeOffset.UtcNow;

            foreach (var customer in customers)
            {
                int sessionCount = _random.Next(1, _settings.MaxSessionsPerUser + 1);
                
                for (int i = 0; i < sessionCount; i++)
                {
                    var sessionDate = new DateTimeOffset(_faker.Date.Between(currentDate.AddMonths(-4).DateTime, currentDate.DateTime), TimeSpan.Zero);
                    var expiryDate = sessionDate.AddDays(7);
                    var session = new SessionToken
                    {
                        CustomerID = customer.CustomerID,
                        AccessToken = Guid.NewGuid().ToString(),
                        RefreshToken = Guid.NewGuid().ToString(),
                        AccessTokenExpiration = expiryDate.AddDays(_random.Next(1, 8)),
                        RefreshTokenExpiration = expiryDate,
                       // IsRevoked = !isActive,
                       // DeviceInfo = _random.Next(100) < 80 ? _faker.System.Device() : null,
                        IPAddress = _random.Next(100) < 80 ? _faker.Internet.Ip() : null
                    };
                    sessions.Add(session);
                    totalSessions++;

                    if (sessions.Count % 1000 == 0)
                    {
                        await _context.SessionToken.AddRangeAsync(sessions);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"Added {totalSessions} sessions...");
                        sessions.Clear();
                    }
                }
            }

            if (sessions.Any())
            {
                await _context.SessionToken.AddRangeAsync(sessions);
                await _context.SaveChangesAsync();
            }

            Console.WriteLine($"Generated {totalSessions} sessions");
        }
        
        private async Task GenerateDeniedTransactionsAsync()
        {
            Console.WriteLine("Generating denied transactions for testing...");
            var cards = await _context.UserCard.ToListAsync();
            var deniedTransactions = new List<Transaction>();
            
            // Generate 5-10 denied transactions
            int deniedCount = _random.Next(5, 11);
            
            for (int i = 0; i < deniedCount; i++)
            {
                var card = cards[_random.Next(cards.Count)];
                
                // Create transactions that would be denied:
                // 1. Transactions on expired cards
                // 2. Transactions exceeding card balance
                // 3. Transactions on inactive/blacklisted cards
                
                var transactionType = _random.Next(3);
                Transaction deniedTransaction;
                
                if (transactionType == 0) // Expired card transaction
                {
                    deniedTransaction = new Transaction
                    {
                        CardID = card.CardID,
                        Amount = _random.Next(10, 500),
                        TransactionType = TransactionType.Pay,
                        TransactionDate = new DateTimeOffset(_faker.Date.Between(_settings.StartDate.DateTime, _settings.EndDate.DateTime), TimeSpan.Zero),
                        Description = "Test transaction - expired card (should be denied by trigger)"
                    };
                }
                else if (transactionType == 1) // Insufficient balance
                {
                    deniedTransaction = new Transaction
                    {
                        CardID = card.CardID,
                        Amount = card.Balance + _random.Next(100, 1000), // Amount exceeds balance
                        TransactionType = TransactionType.Pay,
                        TransactionDate = new DateTimeOffset(_faker.Date.Between(_settings.StartDate.DateTime, _settings.EndDate.DateTime), TimeSpan.Zero),
                        Description = "Test transaction - insufficient balance (should be denied by trigger)"
                    };
                }
                else // Inactive card
                {
                    deniedTransaction = new Transaction
                    {
                        CardID = card.CardID,
                        Amount = _random.Next(10, 500),
                        TransactionType = TransactionType.Pay,
                        TransactionDate = new DateTimeOffset(_faker.Date.Between(_settings.StartDate.DateTime, _settings.EndDate.DateTime), TimeSpan.Zero),
                        Description = "Test transaction - inactive card (should be denied by trigger)"
                    };
                }
                
                deniedTransactions.Add(deniedTransaction);
            }
            
            if (deniedTransactions.Any())
            {
                await _context.Transaction.AddRangeAsync(deniedTransactions);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Generated {deniedTransactions.Count} denied transactions");
            }
        }
    }

    public class DataGenerationSettings
    {
        public int CustomerCount { get; set; } = 100;
        public int CardsPerCustomer { get; set; } = 3;
        public int TransactionsPerCard { get; set; } = 10;
        public int MaxSessionsPerUser { get; set; } = 5;
        private DateTimeOffset _startDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public DateTimeOffset StartDate 
        { 
            get => _startDate;
            set => _startDate = value;
        }
        
        private DateTimeOffset _endDate = DateTimeOffset.UtcNow;
        public DateTimeOffset EndDate 
        { 
            get => _endDate;
            set => _endDate = value;
        }
    }
}
