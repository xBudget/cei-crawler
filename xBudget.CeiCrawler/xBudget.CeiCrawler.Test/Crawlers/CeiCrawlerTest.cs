using System;
using System.Threading.Tasks;
using xBudget.CeiCrawler.Exceptions;
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

        [Fact]
        public async Task CeiCrawler_InvalidLogin()
        {
            var crawler = new xBudget.CeiCrawler.Crawlers.CeiCrawler(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            
            await Assert.ThrowsAsync<LoginFailedExecption>(async () =>
            {
                await crawler.Login();
            });
        }
        
        [Fact]
        public async Task CeiCrawler_ValidLogin()
        {
            var username = Environment.GetEnvironmentVariable("CEI_USERNAME", EnvironmentVariableTarget.Machine);
            var password = Environment.GetEnvironmentVariable("CEI_PASSWORD", EnvironmentVariableTarget.Machine);

            var crawler = new xBudget.CeiCrawler.Crawlers.CeiCrawler(username, password);
            await crawler.Login();
            await crawler.Login();
        }
    }
}
