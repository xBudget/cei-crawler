using System.Collections.Generic;

namespace xBudget.CeiCrawler.Model
{
    public class Institution
    {
        public string Name { get; set; }
        public string Account { get; set; }
        public IList<Stock> Stocks { get; set; }
        public IList<Treasure> Treasures { get; set; }

        public Institution()
        {
            Stocks = new List<Stock>();
            Treasures = new List<Treasure>();
        }
    }
}
