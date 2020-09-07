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
        private const string URL_OPERATIONS = "https://cei.b3.com.br/cei_responsivo/negociacao-de-ativos.aspx";
        private const string URL_DIVIDENDS = "https://cei.b3.com.br/cei_responsivo/ConsultarProventos.aspx";

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

        public async Task<OperationHistory> GetOperations(DateTime? startDate = null, DateTime? endDate = null)
        {
            await Login();

            var result = new OperationHistory();

            var operationsPageGetResult = await _httpClient.GetAsync(URL_OPERATIONS);
            var documentOperationsPageGet = new HtmlDocument();
            documentOperationsPageGet.LoadHtml(await operationsPageGetResult.Content.ReadAsStringAsync());

            var startDateFromPage = documentOperationsPageGet.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_txtDataDeBolsa\"]").GetAttributeValue("value", "");
            if (string.IsNullOrEmpty(startDateFromPage))
            {
                throw new ApplicationException("Initial date not found.");
            }

            if (startDate.HasValue && startDate.Value < DateTime.ParseExact(startDateFromPage, "dd/MM/yyyy", null))
            {
                throw new InvalidDateRangeException($"Start date needs be grater than { startDateFromPage }");
            }

            var endDateFrompage = documentOperationsPageGet.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_txtDataAteBolsa\"]").GetAttributeValue("value", "");
            if (string.IsNullOrEmpty(endDateFrompage))
            {
                throw new ApplicationException("End date not found.");
            }

            if (endDate.HasValue && endDate.Value > DateTime.ParseExact(endDateFrompage, "dd/MM/yyyy", null))
            {
                throw new InvalidDateRangeException($"Start date needs be grater than { endDateFrompage }");
            }

            var brokers = documentOperationsPageGet.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_ddlAgentes\"]");
            var brokersCodes = brokers.ChildNodes.Where(x => x.Name == "option" && x.GetAttributeValue("value", -1) != -1).Select(x => x.GetAttributeValue("value", -1));

            foreach (var broker in brokersCodes)
            {
                var operationsPageForm = new List<KeyValuePair<string, string>>();
                operationsPageForm.AddRange(GetHiddenFields(documentOperationsPageGet.DocumentNode));
                operationsPageForm.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$hdnPDF_EXCEL", ""));
                operationsPageForm.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$ddlAgentes", broker.ToString()));
                operationsPageForm.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$ddlContas", "0"));
                operationsPageForm.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$txtDataDeBolsa", startDate.HasValue ? startDate.Value.ToString("dd/MM/yyyy") : startDateFromPage));
                operationsPageForm.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$txtDataAteBolsa", endDate.HasValue ? endDate.Value.ToString("dd/MM/yyyy") : endDateFrompage));
                operationsPageForm.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$ToolkitScriptManager1", "ctl00$ContentPlaceHolder1$updFiltro|ctl00$ContentPlaceHolder1$btnConsultar"));
                operationsPageForm.Add(new KeyValuePair<string, string>("ctl00_ContentPlaceHolder1_ToolkitScriptManager1_HiddenField", ""));
                operationsPageForm.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$btnConsultar", "Consultar"));

                var operationsPagePostResult = await _httpClient.PostAsync(URL_OPERATIONS, new FormUrlEncodedContent(operationsPageForm));
                var documentOperationsPagePost = new HtmlDocument();
                documentOperationsPagePost.LoadHtml(await operationsPagePostResult.Content.ReadAsStringAsync());

                var tables = documentOperationsPagePost.DocumentNode.SelectNodes("//table");
                var tableOperations = tables.First();
                var tbody = tableOperations.ChildNodes.SingleOrDefault(x => x.Name == "tbody");

                if (tbody == null)
                {
                    continue;
                }

                foreach (var rows in tbody.ChildNodes)
                {
                    var columns = rows.ChildNodes.Where(x => x.Name == "td").ToList();

                    if (!columns.Any())
                    {
                        continue;
                    }

                    var operation = new Operation
                    {
                        Date = DateTime.ParseExact(columns[0].InnerText.Trim(), "dd/MM/yyyy", null),
                        OperationType = columns[1].InnerText.Trim(),
                        Market = columns[2].InnerText.Trim(),
                        Expiration = string.IsNullOrEmpty(columns[3].InnerText.Trim()) ? null : (DateTime?) DateTime.ParseExact(columns[3].InnerText.Trim(), "dd/MM/yyyy", null),
                        Code = columns[4].InnerText.Trim(),
                        Name = columns[5].InnerText.Trim(),
                        Quantity = int.Parse(columns[6].InnerText.Trim()),
                        Price = decimal.Parse(columns[7].InnerText.Trim().Replace(".", "").Replace(",", "."), NumberStyles.Currency),
                        TotalValue = decimal.Parse(columns[8].InnerText.Trim().Replace(".", "").Replace(",", "."), NumberStyles.Currency),
                        QuotationFactor = int.Parse(columns[9].InnerText.Trim())
                    };

                    result.Operations.Add(operation);
                }
            }

            return result;
        }

        public async Task<List<Dividend>> GetDividends()
        {
            await Login();

            var result = new List<Dividend>();

            var dividendsPageGetResult = await _httpClient.GetAsync(URL_DIVIDENDS);
            var documentDividendsGetPage = new HtmlDocument();
            documentDividendsGetPage.LoadHtml(await dividendsPageGetResult.Content.ReadAsStringAsync());

            var endDate = documentDividendsGetPage.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_txtData\"]").GetAttributeValue("value", "");
            if (string.IsNullOrEmpty(endDate))
            {
                throw new ApplicationException("End date not found.");
            }

            var formDividendsPage = new List<KeyValuePair<string, string>>();
            formDividendsPage.AddRange(GetHiddenFields(documentDividendsGetPage.DocumentNode));
            formDividendsPage.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$ddlAgentes", "0"));
            formDividendsPage.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$ddlContas", "0"));
            formDividendsPage.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$txtData", endDate));
            formDividendsPage.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$btnConsultar", "Consultar"));
            formDividendsPage.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$ToolkitScriptManager1", "ctl00$ContentPlaceHolder1$updFiltro|ctl00$ContentPlaceHolder1$btnConsultar"));
            formDividendsPage.Add(new KeyValuePair<string, string>("ctl00_ContentPlaceHolder1_ToolkitScriptManager1_HiddenField", ""));

            var dividendsPagePostResult = await _httpClient.PostAsync(URL_DIVIDENDS, new FormUrlEncodedContent(formDividendsPage));
            var documentDividendsPostPage = new HtmlDocument();
            documentDividendsPostPage.LoadHtml(await dividendsPagePostResult.Content.ReadAsStringAsync());

            var tables = documentDividendsPostPage.DocumentNode.SelectNodes("//table");
            var accountNumber = 0;
            foreach (var table in tables)
            {
                var tbody = table.ChildNodes.SingleOrDefault(x => x.Name == "tbody");
                if (tbody == null)
                {
                    continue;
                }

                foreach (var rows in tbody.ChildNodes)
                {
                    var columns = rows.ChildNodes.Where(x => x.Name == "td").ToList();

                    if (!columns.Any())
                    {
                        continue;
                    }

                    DateTime date;
                    var dateText = columns[3].InnerText.Trim();
                    if (!DateTime.TryParseExact(dateText, "dd/MM/yyyy", null, DateTimeStyles.None, out date))
                    {
                        date = DateTime.MinValue;
                    }

                    try
                    {
                        result.Add(new Dividend
                        {
                            Stock = columns[0].InnerText.Trim(),
                            StockType = columns[1].InnerText.Trim(),
                            Code = columns[2].InnerText.Trim(),
                            Date = date,
                            Type = columns[4].InnerText.Trim(),
                            Quantity = decimal.Parse(columns[5].InnerText.Trim().Replace(".", "").Replace(",", "."), NumberStyles.Currency),
                            Factor = int.Parse(columns[6].InnerText.Trim()),
                            GrossValue = decimal.Parse(columns[7].InnerText.Trim().Replace(".", "").Replace(",", "."), NumberStyles.Currency),
                            NetValue = decimal.Parse(columns[8].InnerText.Trim().Replace(".", "").Replace(",", "."), NumberStyles.Currency)
                        });
                    }
                    catch(Exception ex)
                    {

                    }                    
                }
            }

            return result;
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

                var tableBroker = $"ctl00_ContentPlaceHolder1_rptAgenteContaMercado{ Regex.Match(table.Id, "(_ctl\\d{2}_)").Captures.First() }lblAgenteContas";
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
