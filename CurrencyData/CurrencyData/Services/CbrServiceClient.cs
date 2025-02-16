using CurrencyData.Entities;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

namespace CurrencyData.Services
{
    public class CbrServiceClient
    {
        private readonly HttpClient _httpClient;

        public CbrServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<CurrencyRate>> GetCurrencyRatesAsync(DateTime date)
        {
            string dateString = date.ToString("dd/MM/yyyy");
            string url = $"https://www.cbr.ru/scripts/XML_daily.asp?date_req={dateString}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            byte[] contentBytes = await response.Content.ReadAsByteArrayAsync();
            string xmlContent = Encoding.GetEncoding("windows-1251").GetString(contentBytes);

            var rates = ParseXml(xmlContent, date);
            return rates;
        }

        private List<CurrencyRate> ParseXml(string xmlContent, DateTime rateDate)
        {
            var list = new List<CurrencyRate>();
            var doc = XDocument.Parse(xmlContent);

            foreach (var element in doc.Descendants("Valute"))
            {
                try
                {
                    var numCode = element.Element("NumCode")?.Value;
                    var charCode = element.Element("CharCode")?.Value;
                    var nominalStr = element.Element("Nominal")?.Value;
                    var name = element.Element("Name")?.Value;
                    var valueStr = element.Element("Value")?.Value;

                    int nominal = int.Parse(nominalStr);
                    decimal value = decimal.Parse(valueStr, new CultureInfo("ru-RU"));

                    list.Add(new CurrencyRate
                    {
                        RateDate = DateTime.SpecifyKind(rateDate.Date, DateTimeKind.Utc),
                        NumCode = numCode,
                        CharCode = charCode,
                        Nominal = nominal,
                        Name = name,
                        Value = value
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при разборе элемента: {ex.Message}");
                }
            }

            return list;
        }
    }
}
