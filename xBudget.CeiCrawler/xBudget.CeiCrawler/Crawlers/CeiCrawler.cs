using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using xBudget.CeiCrawler.Exceptions;

namespace xBudget.CeiCrawler.Crawlers
{
    public class CeiCrawler
    {
#warning httpclient must be a static property.
        private readonly HttpClient _httpClient;
        private string _user;
        private string _password;
        private bool _loginDone;

        private const string URL_LOGIN = "https://cei.b3.com.br/CEI_Responsivo/login.aspx?MSG=SESENC";

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
            _loginDone = false;

            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }            
        }

        public async Task Login()
        {
            if (_loginDone)
            {
                return;
            }

            var loginGetRequestResult = await _httpClient.GetAsync(URL_LOGIN);
            loginGetRequestResult.EnsureSuccessStatusCode();

            ConfigureHeaders(loginGetRequestResult.Headers);

            var webDocumentGetLogin = new HtmlDocument();
            webDocumentGetLogin.LoadHtml(await loginGetRequestResult.Content.ReadAsStringAsync());

            var loginForm = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$txtLogin", _user),
                new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$txtSenha", _password),
                new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$smLoad", "ctl00$ContentPlaceHolder1$UpdatePanel1|ctl00$ContentPlaceHolder1$btnLogar"),
                new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$btnLogar", "Entrar")
            };

            loginForm.AddRange(GetHiddenFields(webDocumentGetLogin.DocumentNode));

            var resultPostLogin = await _httpClient.PostAsync(URL_LOGIN, new FormUrlEncodedContent(loginForm));
            
            if (!CheckCookie(resultPostLogin.Headers))
            {
                throw new LoginFailedExecption();
            }

            _loginDone = true;
        }

        /// <summary>
        /// Checks if there is user cookie.
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        private bool CheckCookie(HttpResponseHeaders headers)
        {
            var cookie = headers.SingleOrDefault(x => x.Key == "Set-Cookie");
            if (cookie.Key == null)
            {
                return false;
            }

            return cookie.Value.Count() == 6;
        }

        /// <summary>
        /// Clears the request headers and adds the headers from the response of previous request.
        /// </summary>
        /// <param name="headers"></param>
        private void ConfigureHeaders(HttpResponseHeaders headers)
        {
            foreach (var header in headers)
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                _httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36");
            }
        }

        /// <summary>
        /// Gets some hidden fields necessary to make the request.
        /// The CEI website validate some of those fields to try avoid crawlers.
        /// </summary>
        /// <param name="htmlNode"></param>
        /// <returns></returns>
        private List<KeyValuePair<string, string>> GetHiddenFields(HtmlNode htmlNode)
        {
            var result = new List<KeyValuePair<string, string>>();
            var inputs = htmlNode.SelectNodes("//input[(@type='hidden')]");

            foreach (var input in inputs)
            {
                var key = input.GetAttributeValue("name", "");
                var value = input.GetAttributeValue("value", "");

                result.Add(new KeyValuePair<string, string>(key, value));
            }

            if (string.IsNullOrEmpty(result.SingleOrDefault(x => x.Key == "__EVENTTARGET").Key))
            {
                result.Add(new KeyValuePair<string, string>("__EVENTTARGET", ""));
            }

            if (string.IsNullOrEmpty(result.SingleOrDefault(x => x.Key == "__EVENTARGUMENT").Key))
            {
                result.Add(new KeyValuePair<string, string>("__EVENTARGUMENT", ""));
            }

            if (string.IsNullOrEmpty(result.SingleOrDefault(x => x.Key == "__ASYNCPOST").Key))
            {
                result.Add(new KeyValuePair<string, string>("__ASYNCPOST", "true"));
            }

            return result;
        }
    }
}
