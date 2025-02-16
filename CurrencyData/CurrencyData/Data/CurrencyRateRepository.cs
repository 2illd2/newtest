using CurrencyData.Entities;
using Microsoft.EntityFrameworkCore;

namespace CurrencyData.Data
{

    public class CurrencyRateRepository
    {
        private readonly CurrencyContext _context;

        public CurrencyRateRepository(CurrencyContext context)
        {
            _context = context;
        }

        public async Task EnsureDatabaseCreatedAsync()
        {
            await _context.Database.EnsureCreatedAsync();
        }

        public async Task<bool> IsDataForDateExistsAsync(DateTime date)
        {
            return await _context.CurrencyRates.AnyAsync(r => r.RateDate.Date == date.Date);
        }

        public async Task<DateTime?> GetLatestRateDateAsync()
        {
            return await _context.CurrencyRates.MaxAsync(r => (DateTime?)r.RateDate);
        }

        public async Task InsertRatesAsync(IEnumerable<CurrencyRate> rates)
        {
            await _context.CurrencyRates.AddRangeAsync(rates);
            await _context.SaveChangesAsync();
        }
    }
}
