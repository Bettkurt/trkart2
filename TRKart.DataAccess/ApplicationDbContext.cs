using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TRKart.Entities;

namespace TRKart.DataAccess
{
    
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<SessionToken> SessionTokens { get; set; } = null!;
    }
}
