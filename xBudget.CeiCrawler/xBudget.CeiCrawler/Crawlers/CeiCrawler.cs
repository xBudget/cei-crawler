using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using xBudget.CeiCrawler.Exceptions;
using xBudget.CeiCrawler.Model;

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
        private const string URL_WALLET = "https://cei.b3.com.br/CEI_Responsivo/ConsultarCarteiraAtivos.aspx";

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
                _httpClient = new HttpClient() { Timeout = TimeSpan.FromMinutes(2) };
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

        public async Task<Wallet> GetWallet(DateTime? date = null)
        {
            await Login();

            var walletPageGetResult = await _httpClient.GetAsync(URL_WALLET);
            
            var documentWalletGetPage = new HtmlDocument();
            documentWalletGetPage.LoadHtml(await walletPageGetResult.Content.ReadAsStringAsync());

            var currentDateString = documentWalletGetPage.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_txtData\"]").GetAttributeValue("value", "");
            var currentDate = ParseSiteDate(currentDateString);

            if (date.HasValue && date < currentDate.AddMonths(-2))
            {
                throw new InvalidDateRangeException($"Invalid date. Current CEI date is { currentDateString }. The date must be only two months older than CEI current date.");
            }

            var formWalletPage = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$ddlAgentes", "0"),
                new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$ddlContas", "0"),
                new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$txtData", date.HasValue ? date.Value.ToString("dd/MM/yyyy") : currentDateString),
                new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$btnConsultar", "Consultar"),
                new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$ToolkitScriptManager1", "ctl00$ContentPlaceHolder1$updFiltro|ctl00$ContentPlaceHolder1$btnConsultar"),
                new KeyValuePair<string, string>("ctl00_ContentPlaceHolder1_ToolkitScriptManager1_HiddenField", ""),
                new KeyValuePair<string, string>("__LASTFOCUS", "")
            };
            formWalletPage.AddRange(GetHiddenFields(documentWalletGetPage.DocumentNode));

            var walletPagePostResult = await _httpClient.PostAsync(URL_WALLET, new FormUrlEncodedContent(formWalletPage));
            walletPagePostResult.EnsureSuccessStatusCode();

            var documentWalletPostPage = new HtmlDocument();
            documentWalletPostPage.LoadHtml(await walletPagePostResult.Content.ReadAsStringAsync());

            var walletData = new Wallet();

            var tables = documentWalletPostPage.DocumentNode.SelectNodes("//table").Where(x => !string.IsNullOrEmpty(x.Id));

            if (tables.Count() == 0)
            {
                throw new ElementNotFound("There aren't account tables with stock/treasure data.");
            }

            foreach (var table in tables)
            {
                var tbody = table.ChildNodes.SingleOrDefault(x => x.Name == "tbody");
                if (tbody == null)
                {
                    continue;
                }

                var result = Regex.Match(table.Id, "(_ctl\\d{2}_)").Captures.First();

                var tableBroker = $"ctl00_ContentPlaceHolder1_rptAgenteContaMercado{ result.Value }lblAgenteContas";
                var brokerNameElement = documentWalletPostPage.DocumentNode.SelectNodes("//span").Where(x => x.Id == tableBroker).SingleOrDefault();
                if (brokerNameElement == null)
                {
                    throw new ElementNotFound("Institution name not found.");
                }

                var dateHtmlComponent = documentWalletPostPage.DocumentNode.SelectNodes("//input").Where(x => x.Id == "ctl00_ContentPlaceHolder1_txtData").Single();
                var dateStringValue = dateHtmlComponent.GetAttributeValue<string>("value", DateTime.MinValue.ToString("dd/MM/yyyy"));

                walletData.Date = DateTime.ParseExact(dateStringValue, "dd/MM/yyyy", null);
                if (walletData.Date == DateTime.MinValue)
                {
                    throw new ElementNotFound("Date not found.");
                }

                var brokerName = brokerNameElement.InnerText.Split('-');
                var institutionData = new Institution
                {
                    Name = brokerName[0],
                    Account = brokerName[1]
                };

                walletData.Accounts.Add(institutionData);

                foreach (var rows in tbody.ChildNodes)
                {
                    var columns = rows.ChildNodes.Where(x => x.Name == "td").ToList();

                    if (!columns.Any())
                    {
                        continue;
                    }

                    var stock = new Stock
                    {
                        CompanyName = columns[0].InnerText.Trim(),
                        Type = columns[1].InnerText.Trim(),
                        Code = columns[2].InnerText.Trim(),
                        Isin = columns[3].InnerText.Trim(),
                        Price = decimal.Parse(columns[4].InnerText.Trim().Replace(".", "").Replace(",", "."), NumberStyles.Currency),
                        Quantity = int.Parse(columns[5].InnerText.Trim()),
                        QuotationFactor = int.Parse(columns[6].InnerText.Trim()),
                        TotalValue = decimal.Parse(columns[7].InnerText.Trim().Replace(".", "").Replace(",", "."), NumberStyles.Currency)
                    };

                    institutionData.Stocks.Add(stock);
                }
            }

            return walletData;
        }

        private DateTime ParseSiteDate(string date)
        {
            return DateTime.ParseExact(date, "dd/MM/yyyy", null);
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
