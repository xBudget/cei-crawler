using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace xBudget.CeiCrawler.Crawlers
{
    public class CeiCrawler
    {
        private static HttpClient _httpClient;
        private string _user;
        private string _password;

        private const string URL_LOGIN = "";

        public CeiCrawler(string user, string password)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            _user = user;
            _password = password;

            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }            
        }

        private async Task Login()
        {
            var loginGetRequestResult = await _httpClient.GetAsync(URL_LOGIN);
        }

        public async Task GetWallet(DateTime dateTime)
        {
            await Login();
        }
    }
}
