using CurrencyData.Entities;
using Microsoft.EntityFrameworkCore;

namespace CurrencyData.Data
{
    public class CurrencyContext : DbContext
    {
        public DbSet<CurrencyRate> CurrencyRates { get; set; }

        public CurrencyContext(DbContextOptions<CurrencyContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CurrencyRate>()
                .HasKey(c => c.Id);
            base.OnModelCreating(modelBuilder);
        }
    }
}
