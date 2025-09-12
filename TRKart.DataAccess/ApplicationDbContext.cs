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
        public DbSet<CardBlacklist> CardBlacklist { get; set; } = null!;
        public DbSet<CardLimits> CardLimits { get; set; } = null!;
        public DbSet<CardUpdates> CardUpdates { get; set; } = null!;
        public DbSet<Transaction> Transaction { get; set; } = null!;

        // Tables related to Security
        public DbSet<AuditEvents> AuditEvents { get; set; } = null!;
        public DbSet<SecurityEvents> SecurityEvents { get; set; } = null!;
        public DbSet<RateLimiting> RateLimiting { get; set; } = null!;
    }
}