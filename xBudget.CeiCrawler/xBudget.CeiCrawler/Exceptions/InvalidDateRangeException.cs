using System;

namespace xBudget.CeiCrawler.Exceptions
{
    public class InvalidDateRangeException : Exception
    {
        public InvalidDateRangeException(string message) : base(message)
        {

        }
    }
}
