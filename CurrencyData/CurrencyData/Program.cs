using CurrencyData.Entities;
using CurrencyData.Data;
using CurrencyData.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net.Http;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var configuration = new ConfigurationBuilder()
                                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                .Build();

        string connectionString = configuration.GetConnectionString("DefaultConnection");
        string provider = configuration["DatabaseProvider"];

        var optionsBuilder = new DbContextOptionsBuilder<CurrencyContext>();

        if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseNpgsql(connectionString);
        }
        else if (provider.Equals("SQLServer", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
        else
        {
            throw new Exception("Неверно указан провайдер базы данных в конфигурации.");
        }

        using var context = new CurrencyContext(optionsBuilder.Options);
        var repository = new CurrencyRateRepository(context);
        await repository.EnsureDatabaseCreatedAsync();

        var httpClient = new HttpClient();
        var cbrClient = new CbrServiceClient(httpClient);
        DateTime todayLocal = DateTime.Today;
        DateTime? latestRateDate = await repository.GetLatestRateDateAsync();

        if (latestRateDate == null)
        {
            DateTime startDate = todayLocal.AddDays(-30);
            Console.WriteLine("Первичная загрузка данных за последние 30 дней:");
            for (DateTime date = startDate; date <= todayLocal; date = date.AddDays(1))
            {
                try
                {
                    var rates = await cbrClient.GetCurrencyRatesAsync(date);
                    await repository.InsertRatesAsync(rates);
                    Console.WriteLine($"Вставлены данные за {date:dd/MM/yyyy}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при загрузке или вставке данных за {date:dd/MM/yyyy}: {ex.Message}");
                }
            }
        }

        TimeSpan checkTime = new TimeSpan(15, 23, 0); // пример для времени 15:23

        while (true)
        {
            DateTime now = DateTime.Now;
            DateTime nextCheck = now.Date.Add(checkTime);
            if (now > nextCheck)
            {
                nextCheck = nextCheck.AddDays(1);
            }
            TimeSpan delay = nextCheck - now;
            Console.WriteLine($"Следующая проверка запланирована на {nextCheck}");
            await Task.Delay(delay);

            todayLocal = DateTime.Today;
            latestRateDate = await repository.GetLatestRateDateAsync();

            if (latestRateDate == null)
            {
                Console.WriteLine("База данных пуста. Выполняется первичная загрузка.");
                DateTime startDate = todayLocal.AddDays(-30);
                for (DateTime date = startDate; date <= todayLocal; date = date.AddDays(1))
                {
                    try
                    {
                        var rates = await cbrClient.GetCurrencyRatesAsync(date);
                        await repository.InsertRatesAsync(rates);
                        Console.WriteLine($"Вставлены данные за {date:dd/MM/yyyy}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при загрузке или вставке данных за {date:dd/MM/yyyy}: {ex.Message}");
                    }
                }
            }
            else if (latestRateDate.Value.Date < todayLocal)
            {
                Console.WriteLine("Добавление данных для новых дней:");
                for (DateTime date = latestRateDate.Value.AddDays(1); date <= todayLocal; date = date.AddDays(1))
                {
                    try
                    {
                        var rates = await cbrClient.GetCurrencyRatesAsync(date);
                        if (rates != null && rates.Any())
                        {
                            await repository.InsertRatesAsync(rates);
                            Console.WriteLine($"Вставлены данные за {date:dd/MM/yyyy}");
                        }
                        else
                        {
                            Console.WriteLine($"Новые данные за {date:dd/MM/yyyy} пока отсутствуют.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при загрузке или вставке данных за {date:dd/MM/yyyy}: {ex.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("Данные за сегодняшний день уже актуальны. Новых данных нет.");
            }
        }
    }
}
