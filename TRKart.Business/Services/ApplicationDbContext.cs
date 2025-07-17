using Microsoft.EntityFrameworkCore;
using TRKart.Entities;

namespace TRKart.Business.Services
{
    public class ApplicationDbContext
    {
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<SessionToken> SessionTokens { get; set; } = null!;
        internal void SaveChanges()
        {
            throw new NotImplementedException();
        }

        internal Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }
    }
}