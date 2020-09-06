using System;
using System.Collections.Generic;

namespace xBudget.CeiCrawler.Model
{
    public class OperationHistory
    {
        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }
        public IList<Operation> Operations { get; set; }

        public OperationHistory()
        {
            Operations = new List<Operation>();
        }
    }
}
