using System;

namespace xBudget.CeiCrawler.Model
{
    public class Dividend
    {
        public string Stock { get; set; }
        public string StockType { get; set; }
        public string Code { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public decimal Quantity { get; set; }
        public int Factor { get; set; }
        public decimal GrossValue { get; set; }
        public decimal NetValue { get; set; }
    }
}
