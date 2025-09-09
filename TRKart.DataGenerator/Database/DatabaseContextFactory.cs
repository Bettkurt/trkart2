using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TRKart.DataAccess;

namespace TRKart.DataGenerator.Database
{
    public class DatabaseContextFactory
    {
        private readonly IConfiguration _configuration;

        public DatabaseContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ApplicationDbContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));
            
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
