using System.Collections.Generic;

namespace xBudget.CeiCrawler.Model
{
    public class Wallet
    {
        public IList<Institution> Accounts { get; set; }

        public Wallet()
        {
            Accounts = new List<Institution>();
        }
    }
}
