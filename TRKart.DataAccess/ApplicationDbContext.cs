// ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using TRKart.Entities;

namespace TRKart.DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<SessionToken> SessionTokens { get; set; } = null!;
    }
}
