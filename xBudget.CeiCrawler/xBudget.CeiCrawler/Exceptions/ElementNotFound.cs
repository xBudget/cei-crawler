using System;

namespace xBudget.CeiCrawler.Exceptions
{
    public class ElementNotFound : Exception
    {
        public ElementNotFound(string message): base(message)
        {

        }
    }
}
