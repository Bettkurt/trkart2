using Microsoft.EntityFrameworkCore;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;

namespace TRKart.DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        // Constructor - EF Core bu yapıyı kullanarak context'i oluşturur
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tables from database
        public DbSet<Customers> Customers { get; set; } = null!;
        public DbSet<SessionToken> SessionToken { get; set; } = null!;
        public DbSet<UserCard> UserCard { get; set; } = null!;
        public DbSet<Transaction> Transaction { get; set; } = null!;
    }
}