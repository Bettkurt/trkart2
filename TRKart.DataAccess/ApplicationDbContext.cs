using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TRKart.Entities;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<SessionToken> SessionTokens { get; set; } = null!;
    public DbSet<UserCard> UserCards { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserCard>()
            .HasKey(u => u.CardID); // Primary Key
    }
}

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=trkartdb;Username=postgres;Password=1234");
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
