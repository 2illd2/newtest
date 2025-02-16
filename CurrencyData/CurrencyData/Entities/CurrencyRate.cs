namespace CurrencyData.Entities
{
    public class CurrencyRate
    {
        public int Id { get; set; }
        public string NumCode { get; set; }
        public string CharCode { get; set; }
        public int Nominal { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public DateTime RateDate { get; set; }
    }
}
