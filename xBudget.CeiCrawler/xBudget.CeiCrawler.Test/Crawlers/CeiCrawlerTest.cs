using System;
using System.Threading.Tasks;
using xBudget.CeiCrawler.Exceptions;
using Xunit;

namespace xBudget.CeiCrawler.Test
{
    public class CeiCrawlerTest
    {
        private readonly string _username;
        private readonly string _password;

        public CeiCrawlerTest()
        {
            _username = Environment.GetEnvironmentVariable("CEI_USERNAME", EnvironmentVariableTarget.Machine) ?? Environment.GetEnvironmentVariable("CEI_USERNAME");
            _password = Environment.GetEnvironmentVariable("CEI_PASSWORD", EnvironmentVariableTarget.Machine) ?? Environment.GetEnvironmentVariable("CEI_PASSWORD");
        }

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
            var crawler = new xBudget.CeiCrawler.Crawlers.CeiCrawler(_username, _password);
            await crawler.Login();
            await crawler.Login();
        }

        [Fact]
        public async Task CeiCrawler_GetWallet()
        {
            var crawler = new xBudget.CeiCrawler.Crawlers.CeiCrawler(_username, _password);
            await crawler.GetWallet();
        }

        [Fact]
        public async Task CeiCrawler_GetWallet_InvalidDate()
        {
            var crawler = new xBudget.CeiCrawler.Crawlers.CeiCrawler(_username, _password);
            await Assert.ThrowsAsync<InvalidDateRangeException>(async () =>  await crawler.GetWallet(DateTime.MinValue));
        }

        [Fact]
        public async Task CeiCrawler_GetWallet_DaysAgo()
        {
            var date = DateTime.Now.AddDays(-3);

            var crawler = new xBudget.CeiCrawler.Crawlers.CeiCrawler(_username, _password);
            var result = await crawler.GetWallet(date);

            Assert.Equal(date.Date, result.Date);
        }

        [Fact]
        public async Task CeiCrawler_GetOperations()
        {
            var crawler = new xBudget.CeiCrawler.Crawlers.CeiCrawler(_username, _password);
            var result = await crawler.GetOperations();
        }
    }
}
