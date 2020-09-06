using System;

namespace xBudget.CeiCrawler.Model
{
    public class Operation
    {
        public DateTime Date { get; set; }
        public string OperationType { get; set; }
        public string Market { get; set; }
        public DateTime? Expiration { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalValue { get; set; }
        public int QuotationFactor { get; set; }
    }
}
