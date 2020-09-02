namespace xBudget.CeiCrawler.Model
{
    public class Stock
    {
        public string CompanyName { get; set; }
        public string Type { get; set; }
        public string Code { get; set; }
        public string Isin { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int QuotationFactor { get; set; }
        public decimal TotalValue { get; set; }
    }
}
