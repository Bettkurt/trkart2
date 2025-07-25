using Microsoft.EntityFrameworkCore;
using TRKart.Entities;
using TRKart.Entities.Models;

namespace TRKart.DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        // Constructor - EF Core bu yapıyı kullanarak context'i oluşturur
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Veritabanı tabloları (DbSet'ler)
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<SessionToken> SessionTokens { get; set; } = null!;
        public DbSet<Transaction> Transaction { get; set; } = null!;
        public DbSet<UserCard> UserCard { get; set; } = null!;


    }
}
