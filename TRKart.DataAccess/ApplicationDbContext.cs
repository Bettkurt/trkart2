using Microsoft.EntityFrameworkCore;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;

namespace TRKart.DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        // Constructor - EF Core creates context using this constructor
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tables from database
        public DbSet<Customers> Customers { get; set; } = null!;
        public DbSet<PasswordHistory> PasswordHistory { get; set; } = null!;
        public DbSet<SessionToken> SessionToken { get; set; } = null!;
        public DbSet<TokenBlacklist> TokenBlacklist { get; set; } = null!;
        public DbSet<UserCard> UserCard { get; set; } = null!;
        public DbSet<UserCard> UserCards => Set<UserCard>();
        public DbSet<CardBlacklist> CardBlacklist { get; set; } = null!;
        public DbSet<CardLimits> CardLimits { get; set; } = null!;
        public DbSet<CardUpdates> CardUpdates { get; set; } = null!;
        public DbSet<Transaction> Transaction { get; set; } = null!;
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<Wallet> Wallets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the one-to-one relationship between Customers and Wallet
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.Customer)
                .WithOne(c => c.Wallet)
                .HasForeignKey<Wallet>(w => w.CustomerID)
                .IsRequired();

            // Configure enum properties to be stored as integers
            modelBuilder.Entity<Wallet>()
                .Property(w => w.Status)
                .HasConversion<int>();

            modelBuilder.Entity<UserCard>()
                .Property(u => u.CardStatus)
                .HasConversion<int>();

            // Configure the relationship between Wallet and Transaction
            // Configure TransactionStatus as an enum
            modelBuilder.Entity<Transaction>()
                .Property(t => t.TransactionStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(t => t.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}