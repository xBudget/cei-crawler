using System;
using Xunit;

namespace xBudget.CeiCrawler.Test
{
    public class CeiCrawlerTest
    {
        [Fact]
        public void CeiCrawler_EmptyUser()
        {
            Assert.Throws<ArgumentNullException>(() => new xBudget.CeiCrawler.Crawlers.CeiCrawler(string.Empty, Guid.NewGuid().ToString()));
        }

        [Fact]
        public void CeiCrawler_EmptyPassword()
        {
            Assert.Throws<ArgumentNullException>(() => new xBudget.CeiCrawler.Crawlers.CeiCrawler(Guid.NewGuid().ToString(), string.Empty));
        }
    }
}
