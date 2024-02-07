using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.DevTools.V85.Network;
using OpenQA.Selenium.Support.UI;
using System.IO;
using System.Net;
using RestSharp;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Reflection.Metadata;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Json;
using Org.BouncyCastle.Asn1.X500;
using System.Collections.Specialized;
using UndetectableBrowser.NumJob;
using Google.Apis.Util.Store;
using GemBox.Spreadsheet;
using System.Net.Http.Json;
using System.Security.Policy;

namespace UndetectableBrowser
{
    internal class Program
    {
        public static string address = "127.0.0.1";
        public static string portFromSettingsBrowser = "25325";
        public static string chromeDriverPath = "chromedriver.exe";

        static void Main(string[] args)
        {
            Dictionary<string, string> generalSettings = new Dictionary<string, string>();
            Dictionary<string, Dictionary<string, string>> profileSettings = new Dictionary<string, Dictionary<string, string>>();

            while (true)
            {
                Console.WriteLine("Меню:");
                Console.WriteLine("1. Общие настройки");
                Console.WriteLine("2. Настройки профилей");
                Console.WriteLine("3. Работа с WB");
                Console.WriteLine("4. Выход");

                //string choice = Console.ReadLine();
                string choice = "2";

                switch (choice)
                {
                    case "1":
                        HandleGeneralSettings(generalSettings);
                        break;
                    case "2":
                        HandleProfileSettings(profileSettings);
                        break;
                    case "3":
                        HandleWBSettings();
                        break;
                    case "4":
                        Console.WriteLine("До свидания!");
                        return;
                    default:
                        Console.WriteLine("Неверный ввод. Пожалуйста, выберите корректное действие.");
                        break;
                }
            }
        }
        static string GetProfilesList(string address, string port)
        {
            string url = $"http://{address}:{port}/list";
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";

            using (WebResponse response = request.GetResponse())
            using (Stream dataStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(dataStream))
            {
                string content = reader.ReadToEnd();
                JObject jsonObject = JObject.Parse(content);

                ApiResponse apiResponse = JsonConvert.DeserializeObject<ApiResponse>(content);
                JObject data = apiResponse.Data;

                return data.ToString();
            }
        }

        static string GetDebugPort(string address, string port, string profileId, string profileStatus, dynamic profileData)
        {
            if (profileStatus == "Available")
            {
                var client = new RestClient($"http://{address}:{port}/profile/start/{profileId}");
                var request = new RestRequest(address, Method.Get);
                request.Timeout = 5000;

                var response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    var startProfileResponse = JObject.Parse(response.Content);
                    return startProfileResponse["data"]["debug_port"].ToString();
                }

                return null;
            }

            return profileStatus == "Started" ? profileData["debug_port"].ToString() : null;
        }
        static IWebDriver CreateChromeDriver(ChromeDriverService service, ChromeOptions options, string address, string debugPort)
        {
            options.DebuggerAddress = $"{address}:{debugPort}";
            return new ChromeDriver(service, options);
        }

        static async void StopProfile(string address, string port, string profileId)
        {
            string url = $"http://localhost:25325/profile/stop/{profileId}";
            using (var client = new HttpClient())
            {
                try
                {
                    // Отправляем GET-запрос
                    HttpResponseMessage response = await client.GetAsync(url);

                    // Проверяем статус ответа
                    if (response.IsSuccessStatusCode)
                    {
                        // Если запрос успешен, выводим сообщение
                        Console.WriteLine("GET запрос выполнен успешно.");
                    }
                    else
                    {
                        // Если возникла ошибка, выводим статус код
                        Console.WriteLine($"Ошибка при выполнении GET запроса: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    // Если произошла исключительная ситуация, выводим сообщение об ошибке
                    Console.WriteLine($"Произошла ошибка: {ex.Message}");
                }
            }
        }
        // Меню метод
        static void HandleGeneralSettings(Dictionary<string, string> generalSettings)
        {
            while (true)
            {
                Console.WriteLine("Общие настройки:");
                Console.WriteLine("1. Указать путь к файлу с прокси");
                Console.WriteLine("2. Указать путь к таблице");
                Console.WriteLine("3. Вернуться в предыдущее меню");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("Выбрано: Установить прокси");

                        Console.WriteLine("Введите путь к файлу proxy.txt");
                        string filePath = Console.ReadLine();

                        string randomProxy = GetRandomLineFromFile(filePath);

                        if (randomProxy != null)
                        {
                            Console.WriteLine($"Случайная строка из файла: {randomProxy}");
                        }

                        Variables.proxySet = randomProxy;

                        break;
                    case "2":
                        Console.WriteLine("Выбрано: Указать путь к гугл таблице");

                        Console.WriteLine("Введите путь к таблице");
                        Variables.googleTblPath = Console.ReadLine();
                        Console.WriteLine($"Путь к таблице - {Variables.googleTblPath}");

                        break;
                    case "3":
                        return;
                    default:
                        Console.WriteLine("Неверный ввод. Пожалуйста, выберите корректное действие.");
                        break;
                }
            }
        }
        // Меню метод
        static void HandleProfileSettings(Dictionary<string, Dictionary<string, string>> profileSettings)
        {
            while (true)
            {
                Console.WriteLine("Настройки профилей:");
                Console.WriteLine("1. Создание профилей");
                Console.WriteLine("2. Запуск и проверка профилей");
                Console.WriteLine("3. Отправить профиль в облако");
                Console.WriteLine("4. Сделать локальным");
                Console.WriteLine("5. Вывести информацию о профиле");
                Console.WriteLine("6. Вернуться в предыдущее меню");

                //string choice = Console.ReadLine();
                string choice = "2";

                switch (choice)
                {
                    case "1":
                        HandleCreateProfiles(profileSettings);
                        break;
                    case "2":
                        HandleRunProfiles(profileSettings);
                        break;
                    case "3":
                        HandleSendToCloud(profileSettings);
                        break;
                    case "4":
                        HandleMakeLocal(profileSettings);
                        break;
                    case "5":
                        HandleProfileInfo(profileSettings);
                        break;
                    case "6":
                        return;
                    default:
                        Console.WriteLine("Неверный ввод. Пожалуйста, выберите корректное действие.");
                        break;
                }
            }
        }

        static void HandleRunProfiles(Dictionary<string, Dictionary<string, string>> profileSettings)
        {
            Console.WriteLine("Выбрано запуск и проверка профилей");

            Console.WriteLine("Укажите - сколько одновременно профилей запустить (числом)");
            //int counter = Convert.ToInt32(Console.ReadLine());
            int counter = 1;
            int cnt = 0;

            string address = "127.0.0.1";
            string portFromSettingsBrowser = "25325";
            string chromeDriverPath = "chromedriver.exe";

            ChromeOptions chromeOptions = new ChromeOptions();
            ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();

            var listResponse = GetProfilesList(address, portFromSettingsBrowser).ToString();

            Dictionary<string, Dictionary<string, object>> data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(listResponse);

            foreach (var profileEntry in data)
            {
                string profileId = profileEntry.Key;
                string status = (string)profileEntry.Value["status"];

                Console.WriteLine($"ProfileId: {profileId}, Status: {status}");

                // Действия с профилем в зависимости от статуса
                if (status == "Available")
                {
                    // http://{address}:{port_from_settings_browser}/profile/start/{profile_id}
                    string url = $"http://{address}:{portFromSettingsBrowser}/profile/start/{profileId}";
                    WebRequest request = WebRequest.Create(url);
                    request.Method = "GET";

                    using (WebResponse response = request.GetResponse())
                    using (Stream dataStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(dataStream))
                    {
                        string content = reader.ReadToEnd();

                        ApiResponse response2 = JsonConvert.DeserializeObject<ApiResponse>(content);

                        if (response2 != null && response2.Data != null)
                        {
                            foreach (var dataEntry in response2.Data)
                            {
                                string debugPort = dataEntry.Value?.ToString();
                                string status2 = response2.Status;

                                Console.WriteLine($"Debug Port: {debugPort}, Status: {status2}");
                            }
                        }
                    }
                }

                // Получаем debug_port для работы с selenium
                if (status == "Started")
                {
                    Dictionary<string, ProfileData> profiles2 = JsonConvert.DeserializeObject<Dictionary<string, ProfileData>>(listResponse);

                    foreach (var profileEntry2 in profiles2)
                    {
                        string profileId2 = profileEntry.Key;
                        ProfileData profileData2 = profileEntry2.Value;

                        string cloudId2 = profileData2.CloudId;
                        string debugPort2 = profileData2.DebugPort;

                        Console.WriteLine($"ProfileId: {profileId2}, CloudId: {cloudId2}, DebugPort: {debugPort2}");

                        // Запуск хром c профилем
                        if (!String.IsNullOrWhiteSpace(debugPort2))
                        {
                            using (var driver = CreateChromeDriver(chromeDriverService, chromeOptions, address, debugPort2))
                            {
                                //driver.Navigate().GoToUrl("https://whoer.net");
                            }
                        }
                    }
                }
                if (status == "Locked")
                {
                    Console.WriteLine("Профиль заблокирован, не берем в работу");
                    continue;
                }
                if (cnt == counter)
                {
                    Console.WriteLine($"Загрузили указанное - {counter} число профилей");
                    break;
                }
                cnt++;
            }
            // Запуск работы с WB - Регистрация на открыхы провилях
            HandleWBSettings();
        }

        [Obsolete]
        static async void HandleWBSettings()
        {
            Random rnd = new Random();
            var listResponse = GetProfilesList(address, portFromSettingsBrowser).ToString();

            Dictionary<string, ProfileData> profiles = JsonConvert.DeserializeObject<Dictionary<string, ProfileData>>(listResponse);

            ChromeOptions chromeOptions = new ChromeOptions();
            ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();

            foreach (var profileEntry in profiles)
            {
                string profileId = profileEntry.Key;
                ProfileData profileData = profileEntry.Value;

                string cloudId = profileData.CloudId;
                string debugPort = profileData.DebugPort;
                string profName = profileData.Name;

                Console.WriteLine($"ProfileId: {profileId}, CloudId: {cloudId}, DebugPort: {debugPort}, ProfileName: {profName}");
                Variables.order_id = profileId;
                Variables.profNameInUndetect = profName;
                Variables.profRegStatus = "Не залогинен";

                // Запуск хром c профилем
                if (!String.IsNullOrWhiteSpace(debugPort))
                {
                    using (var driver = CreateChromeDriver(chromeDriverService, chromeOptions, address, debugPort))
                    {
                        driver.Navigate().GoToUrl("https://wildberries.ru");
                        Thread.Sleep(16000);

                        // Проверка - залогинен ли профиль
                        List <IWebElement> baskets = driver.FindElements(By.XPath("//div[contains(@class, 'navbar-pc__item')]//a[contains(@data-wba-header-name, 'Favorites')]|//span[contains(@class, 'navbar-pc__icon--favorites')]")).ToList();
                        if (baskets.Count > 0)
                        {
                            IWebElement bs = baskets.FirstOrDefault();
                            bs.Click();
                            Thread.Sleep(2000);

                            //List<IWebElement> basket_empty = driver.FindElements(By.XPath("//h1[contains(@class,'section-header basket-empty__title')]")).ToList();
                            //if (basket_empty.Count > 0)
                            //{
                                //IWebElement bse = basket_empty.FirstOrDefault();
                                //if (bse.Text.Contains("В корзине пока пусто"))
                                //{
                                    Console.WriteLine("Профиль залогинен");
                                    Variables.profRegStatus = "Залогинен";

                                    // Присваиваем профилю папку - InLogin
                                    string url = $"http://localhost:25325/profile/update/{profileId}";

                                    var parameters = new
                                    {
                                        //proxy = "socks5://127.0.0.1:5555:login:pass",
                                        //notes = "Text",
                                        name = $"WB{rnd.Next(1, 9999)}",
                                        folder = "InLogin",
                                        tags = new[] { "wbLogined", "logined" }
                                        //geolocation = "12.44, 13.524",
                                        //cookies = new[] { new { } },
                                        //type = "cloud",
                                        //group = "group_example",
                                        //accounts = new[]
                                        //{
                                        //    new { website = "facebook.com", username = "test@gmail.com", password = "123456" },
                                        //    new { website = "mail.com", username = "test@gmail.com", password = "123456" }
                                        //}
                                    };

                                    // Сериализуем параметры в JSON
                                    string json = JsonConvert.SerializeObject(parameters);

                                    // Создаем HttpClient
                                    using (var client = new HttpClient())
                                    {
                                        // Отправляем POST запрос
                                        var response = await client.PostAsJsonAsync(url, json);

                                        var content = await response.Content.ReadAsStringAsync();

                                        // Проверяем статус ответа
                                        if (response.IsSuccessStatusCode)
                                        {
                                            Console.WriteLine("POST запрос выполнен успешно.");
                                            StopProfile(address, portFromSettingsBrowser, profileId);
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Ошибка при выполнении POST запроса: {response.StatusCode}");
                                        }
                                    }
                                //}
                            //}
                        }
                        else
                        {
                            try
                            {
                                Thread.Sleep(4000);
                                // Клик на иконку профиля
                                List<IWebElement> profile = driver.FindElements(By.XPath("//a[contains(@aria-label, 'Личный кабинет')]|//span[contains(@class, 'navbar-pc__icon--profile')]|//span[contains(@class, 'navbar-mobile__icon--profile')]")).ToList();
                                if (profile.Count > 0)
                                {
                                    IWebElement pr = profile.FirstOrDefault();
                                    pr.Click();
                                    Thread.Sleep(2000);
                                }

                                List<IWebElement> createOrEnter = driver.FindElements(By.XPath("//a[contains(@data-wba-header-name, 'Login')]")).ToList();
                                if (createOrEnter.Count > 0)
                                {
                                    IWebElement coe = createOrEnter.FirstOrDefault();
                                    coe.Click();
                                    Thread.Sleep(2000);
                                }

                                // Запрос номера
                                NumJob.OnlineSimJob job = new NumJob.OnlineSimJob();
                                job.GetNum();

                                // Ввод номера в поле для номера на wb
                                List<IWebElement> inputPhone = driver.FindElements(By.XPath("//input[contains(@class, 'input-item')]")).ToList();
                                if (inputPhone.Count > 0)
                                {
                                    IWebElement coe = inputPhone.FirstOrDefault();
                                    coe.Click();
                                    Thread.Sleep(2000);

                                    Variables.order_phone = Variables.order_phone.Substring(1);

                                    // Побуквенный ввод символов
                                    for (int i = 0; i < Variables.order_phone.Length; i++)
                                    {
                                        coe.SendKeys(Variables.order_phone[i].ToString());
                                        Thread.Sleep(400);
                                    }
                                    Thread.Sleep(2000);

                                    // Клик на "получить код"
                                    List<IWebElement> orderCode = driver.FindElements(By.XPath("//button[contains(@id, 'requestCode')]")).ToList();
                                    if (orderCode.Count > 0)
                                    {
                                        IWebElement orC = orderCode.FirstOrDefault();
                                        orC.Click();
                                        Thread.Sleep(3000);
                                    }

                                    // Запрос к модулю гадания капчи
                                    SolveCaptchaModule(driver);

                                    Thread.Sleep(35000);

                                    // Получение смс с номера телефона
                                    string sms = OnlineSimJob.GetAnswer(Variables.order_id);

                                    if (String.IsNullOrWhiteSpace(sms))
                                    {
                                        Console.WriteLine("Не удалось получить смс, ждем прихода смс");
                                        Thread.Sleep(35000);
                                        Console.WriteLine("Повторный запрос смс");

                                        List<IWebElement> codeInput = driver.FindElements(By.XPath("//button[contains(@id, 'requestCode')]")).ToList();
                                        if (codeInput.Count > 0)
                                        {
                                            IWebElement ci = codeInput.FirstOrDefault();
                                            ci.Click();
                                            Thread.Sleep(3000);
                                            // Запрос к модулю гадания капчи
                                            SolveCaptchaModule(driver);
                                        }
                                    }
                                    else
                                    {
                                        int i = 0;
                                        // Ввод смс
                                        List<IWebElement> codeInput = driver.FindElements(By.XPath("//input[contains(@class, 'input-item j-b-charinput')]")).ToList();
                                        if (codeInput.Count > 0)
                                        {
                                            foreach (var elm in codeInput)
                                            {
                                                elm.Click();
                                                Thread.Sleep(100);
                                                elm.SendKeys(sms[i].ToString());
                                                i++;
                                            }
                                        }

                                        // Ввод имени профиля
                                        //a[contains(@class, 'navbar-pc__link j-wba-header-item')]
                                        List<IWebElement> profileEnter = driver.FindElements(By.XPath("//a[contains(@class, 'navbar-pc__link j-wba-header-item')]")).ToList();
                                        if (profileEnter.Count > 0)
                                        {
                                            IWebElement elm2 = profileEnter[2];
                                            elm2.Click();
                                            Thread.Sleep(2000);
                                        }
                                    }
                                }
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine("Ошибка - " + ex.Message);
                            }
                        }
                    }
                }
            }

            // Запись отчета о регистрации в таблицу

            SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");

            ExcelFile workbook = ExcelFile.Load(Variables.googleTblPath);
            ExcelWorksheet worksheet = workbook.Worksheets[0];
            int rows = worksheet.Rows.Count;
            worksheet.Cells[$"A{rows + 1}"].Value = Variables.order_id;
            worksheet.Cells[$"B{rows + 1}"].Value = Variables.profNameInUndetect;
            worksheet.Cells[$"C{rows + 1}"].Value = Variables.profRegStatus;
            worksheet.Cells[$"D{rows + 1}"].Value = Variables.order_phone;
            worksheet.Cells[$"E{rows + 1}"].Value = Variables.profRegPass;
            worksheet.Cells[$"F{rows + 1}"].Value = Variables.profRegWork;
            workbook.Save(Variables.googleTblPath);


            // Запись отчета в гугл таблицу
            //string credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "client_secret_429036755400-sd57p3e6aidd27es71ulmafig7627oru.apps.googleusercontent.com.json");
            //string credPath = Path.Combine(Directory.GetCurrentDirectory(), "token.json");

            //// Имя таблицы и диапазон ячеек
            //string apiKey = "AIzaSyD2Ks0lAK_9zC2iVL4fvDxa-mEuDlniN5g";
            //string spreadsheetId = "1L_XhA7tTllEQvuAruzubnbZD_9botgLsN_M8CUQp8YA";
            //string range = "Лист1!A1:B2";

            // https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{range}:append
            // string url = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{range}:append?key={apiKey}";

            //GoogleCredential credential;
            //// Создание авторизации
            ////var credential = GoogleCredential.FromFile(credentialsPath)
            ////    .CreateScoped(SheetsService.Scope.Spreadsheets);

            //using(var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            //{
            //    credential = GoogleCredential.FromStream(stream)
            //        .CreateScoped(new[] { SheetsService.Scope.Spreadsheets });
            //}

            //// Создание службы гугл
            //var service = new SheetsService(new BaseClientService.Initializer()
            //{
            //    HttpClientInitializer = credential,
            //    ApplicationName = "WBProfiles"
            //});

            //// Создание объекта ValueRange для записи данных
            //var valueRange = new ValueRange()
            //{
            //    Values = new List<IList<object>> { new List<object> { "Test1", "Test2" } }
            //};

            //// Выполнение запроса на запись данных
            //var appendRequest = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
            //appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            //var appendResponse = appendRequest.Execute();

            // Вариант работает для запросов где проходит api

            //var data = new
            //{
            //    values = new[] { new[] { "Test1", "Test2" } }
            //};

            //var client = new HttpClient();
            //var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

            //var response = await client.PostAsync(url, content);
            //var responseContent = await response.Content.ReadAsStringAsync();
            //Console.WriteLine(responseContent);
        }

        private static void SolveCaptchaModule(IWebDriver driver)
        {
            string ApiKey = "e2ccde01603536a8dd9b357d9532d1f3";
            const int CAPTCHA_TRIES = 6;
            const int RESPONSE_TRIES_COUNT = 3;
            const int RESPONSE_TIME = 18000;
            const int REGISTRATION_TRIES = 3;

            Random rnd = new Random();

            ConsoleChangeColor("Старт гадания капчи", "green");

            // Проверка на исчерпанные попытки
            List<IWebElement> errCounts = driver.FindElements(By.XPath("//p[contains(@class, 'form-block__message--error')]")).ToList();
            if (errCounts.Count > 0)
            {
                IWebElement lone = errCounts.FirstOrDefault();
                if (lone.Text.Contains("Вы исчерпали"))
                {
                    ConsoleChangeColor("Out of tries...", "darkgrey");
                    driver.Quit();
                    ConsoleChangeColor("reg1 start", "green");
                }
            }

            string responseString = "";
            string responseToken = "";
            Dictionary<string, object> jsonDictResponse = new Dictionary<string, object>();
            int j;
            for (j = 0; j < CAPTCHA_TRIES; j++)
            {
                var captchaElems = driver.FindElements(By.XPath(@"//div[@class='form-block__captcha-wrap']/img[@class='form-block__captcha-img']"));
                if (captchaElems.Count == 0)
                    break;
                var screenShot = ((ITakesScreenshot)captchaElems[0]).GetScreenshot();
                string base64string = screenShot.AsBase64EncodedString;
                using (var client = new WebClient())
                {
                    var values = new NameValueCollection();
                    values["key"] = ApiKey;
                    values["method"] = "base64";
                    values["body"] = base64string;
                    //values["lang"] = "ru";
                    values["json"] = "1";

                    var response = client.UploadValues("http://rucaptcha.com/in.php", values);
                    responseString = Encoding.Default.GetString(response);
                }
                if (responseString != "")
                {
                    var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
                    if ((jsonDict?["status"]?.ToString() ?? "0") == "1")
                    {
                        for (int k = 0; k < RESPONSE_TRIES_COUNT; k++)
                        {
                            using (var wb = new WebClient())
                            {
                                ConsoleChangeColor("Sending request to rucaptcha..", "darkgrey");

                                // Проверка на Failed to fetch
                                List<IWebElement> ff = driver.FindElements(By.XPath("//button[contains(@class, 'popup-alert__close')]")).ToList();
                                if (ff.Count > 0)
                                {
                                    ConsoleChangeColor("Oops! Got failed to fetch... closing..", "darkcyan");
                                    IWebElement lone = ff.FirstOrDefault();
                                    lone.Click();
                                    Thread.Sleep(2000);
                                }

                                Thread.Sleep(rnd.Next(2000, 3500));

                                ConsoleChangeColor("Отправляем запрос к сервису rucaptcha", "green");
                                responseString = wb.DownloadString(@"http://rucaptcha.com/res.php?key=" + ApiKey + "&action=get&json=1&id=" + jsonDict["request"].ToString());
                                jsonDictResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
                                if (jsonDictResponse != null && jsonDictResponse["status"].ToString() == "1")
                                {
                                    break;
                                }
                                Thread.Sleep(RESPONSE_TIME);

                                // Проверка на исчерпанные попытки
                                List<IWebElement> errCounts4 = driver.FindElements(By.XPath("//p[contains(@class, 'form-block__message--error')]")).ToList();
                                if (errCounts4.Count > 0)
                                {
                                    IWebElement lone = errCounts.FirstOrDefault();
                                    if (lone.Text.Contains("Вы исчерпали"))
                                    {
                                        ConsoleChangeColor("Out of tries...", "darkcyan");
                                        driver.Quit();
                                        ConsoleChangeColor("reg2 start...", "green");
                                    }
                                }
                            }
                        }
                        if (j != RESPONSE_TRIES_COUNT)
                        {
                            string solution = jsonDictResponse["request"].ToString();
                            IWebElement inputElem = driver.FindElement(By.XPath(@"//div[@class='form-block__captcha-wrap']/input[@name='smsCaptchaCode']"));
                            inputElem.SendKeys(solution);
                            IWebElement submitElem = driver.FindElement(By.XPath(@"//div[@class='login__captcha form-block form-block--captcha']/button[@class='login__btn btn-main-lg']"));
                            submitElem.Click();
                            Thread.Sleep(rnd.Next(2500, 4500));
                            var captchaElems1 = driver.FindElements(By.XPath(@"//div[@class='form-block__captcha-wrap']/img[@class='form-block__captcha-img']"));
                            if (captchaElems1.Count == 0)
                                break;
                        }
                    }
                }
                IWebElement reCaptchaBtnElem = driver.FindElement(By.XPath(@"//button[contains(@class, 'form-block__captcha-reload')]"));
                reCaptchaBtnElem.Click();
                Thread.Sleep(rnd.Next(2500, 4500));
            }
            ConsoleChangeColor("Finished with captcha", "green");
        }

        static async void HandleCreateProfiles(Dictionary<string, Dictionary<string, string>> profileSettings)
        {
            string proxySet = String.Empty;

            Console.WriteLine("Выбрано создание профилей");
            Console.WriteLine("1.2 - Случайные параметры");

            string apiUrl = "http://localhost:25325/profile/create";

            AccountNameGenerator generator = new AccountNameGenerator();
            string accountName = generator.GenerateAccountName();
            Console.WriteLine(accountName);

            // Ваши параметры запроса
            var requestData = new
            {
                name = accountName,
                os = "Windows",
                browser = "Chrome",
                proxy = Variables.proxySet
            };

            try
            {
                var response = await SendPostRequest(apiUrl, requestData);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Запрос успешно выполнен. Ответ от сервера:");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseBody);
                }
                else
                {
                    Console.WriteLine($"Ошибка при выполнении запроса. Код ответа: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }
        static async Task<HttpResponseMessage> SendPostRequest(string apiUrl, object data)
        {
            using (HttpClient client = new HttpClient())
            {
                // Преобразование данных в JSON
                string jsonData = JsonConvert.SerializeObject(data);

                // Определение содержимого запроса
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Отправка POST-запроса
                return await client.PostAsync(apiUrl, content);
            }
        }

        static async void HandleSendToCloud(Dictionary<string, Dictionary<string, string>> profileSettings)
        {
            Console.WriteLine("Выбрано отправить профиль в облако");

            Console.WriteLine("Введите имя профиля:");
            string profileName = Console.ReadLine();

            // Получение списка профилей
            var listResponse = GetProfilesList(address, portFromSettingsBrowser).ToString();

            // Получение id профиля по его имени
            string profileId = GetProfileIdByName(listResponse, profileName);

            if (profileId != null)
            {
                // URL для отправки POST-запроса
                string url = "http://localhost:25325/profile/tocloud";

                // Данные для отправки
                string jsonData2 = $"{{\"profiles\": [\"{profileId}\"]}}";

                // Отправка POST-запроса
                await SendPostRequest(url, jsonData2);

                Console.WriteLine($"Профиль - '{profileName}' отправлен в облако.");
            }
            else
            {
                Console.WriteLine($"Профиль с именем '{profileName}' не найден.");
            }
        }
        // Метод гугл таблицы
        static IList<IList<object>> ReadData(SheetsService service, string spreadsheetId, string range)
        {
            SpreadsheetsResource.ValuesResource.GetRequest request =
                service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            return response.Values;
        }
        // Метод гугл таблицы
        static void WriteData(SheetsService service, string spreadsheetId, string range, IList<IList<object>> values)
        {
            ValueRange body = new ValueRange
            {
                Values = values
            };

            SpreadsheetsResource.ValuesResource.UpdateRequest request =
                service.Spreadsheets.Values.Update(body, spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            UpdateValuesResponse response = request.Execute();
        }

        static async void HandleMakeLocal(Dictionary<string, Dictionary<string, string>> profileSettings)
        {
            Console.WriteLine("Выбрано сделать профиль локальным");

            Console.WriteLine("Введите имя профиля:");
            string profileName = Console.ReadLine();

            // Получение списка профилей
            var listResponse = GetProfilesList(address, portFromSettingsBrowser).ToString();

            // Получение id профиля по его имени
            string profileId = GetProfileIdByName(listResponse, profileName);

            if (profileId != null)
            {
                // URL для отправки POST-запроса
                string url = "http://localhost:25325/profile/tolocal";

                // Данные для отправки
                string jsonData2 = $"{{\"profiles\": [\"{profileId}\"]}}";

                // Отправка POST-запроса
                await SendPostRequest(url, jsonData2);

                Console.WriteLine($"Профиль - '{profileName}' отправлен в облако.");
            }
            else
            {
                Console.WriteLine($"Профиль с именем '{profileName}' не найден.");
            }
        }

        static void HandleProfileInfo(Dictionary<string, Dictionary<string, string>> profileSettings)
        {
            Console.WriteLine("Выбран вывод информации о профиле");

            var listResponse = GetProfilesList(address, portFromSettingsBrowser).ToString();

            string profilesJson = listResponse;
            DisplayProfileNames(profilesJson);

            Console.Write("Введите имя профиля для просмотра подробной информации: ");
            string selectedProfileName = Console.ReadLine();

            DisplayProfileInfoByName(profilesJson, selectedProfileName, address, portFromSettingsBrowser);
        }
        static string GetRandomLineFromFile(string filePath)
        {
            try
            {
                // Считываем все строки из файла
                string[] lines = File.ReadAllLines(filePath);

                // Проверяем, что файл не пуст
                if (lines.Length == 0)
                {
                    Console.WriteLine("Файл пуст.");
                    return null;
                }

                // Генерируем случайный индекс
                Random random = new Random();
                int randomIndex = random.Next(0, lines.Length);

                // Возвращаем случайную строку
                return lines[randomIndex];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
                return null;
            }
        }
        static async Task SendPostRequest(string url, string jsonData)
        {
            using (HttpClient client = new HttpClient())
            {
                // Установка контента запроса
                StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Отправка POST-запроса
                HttpResponseMessage response = await client.PostAsync(url, content);

                // Проверка успешности запроса
                if (response.IsSuccessStatusCode)
                {
                    // Обработка успешного ответа
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ответ сервера: {responseContent}");
                }
                else
                {
                    // Обработка ошибочного ответа
                    Console.WriteLine($"Ошибка: {response.StatusCode}");
                }
            }
        }
        static void DisplayProfileNames(string profilesJson)
        {
            JObject profiles = JObject.Parse(profilesJson);

            Console.WriteLine("Список профилей:");
            foreach (var profile in profiles)
            {
                string profileId = profile.Key;
                string profileName = profile.Value["name"].ToString();

                Console.WriteLine($"{profileId}: {profileName}");
            }
        }
        static string GetProfileIdByName(string profilesData, string profileName)
        {
            try
            {
                JObject profilesObject = JObject.Parse(profilesData);

                foreach (var profile in profilesObject)
                {
                    string currentProfileId = profile.Key;
                    string currentProfileName = profile.Value["name"].ToString();

                    if (currentProfileName == profileName)
                    {
                        return currentProfileId;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке данных о профилях: {ex.Message}");
            }

            return null;
        }
        static void DisplayProfileInfoByName(string profilesJson, string selectedProfileName, string address, string port)
        {
            JObject profiles = JObject.Parse(profilesJson);

            var matchingProfile = profiles
                .Properties()
                .Where(p => p.Value["name"].ToString() == selectedProfileName)
                .FirstOrDefault();

            if (matchingProfile != null)
            {
                string selectedProfileId = matchingProfile.Name;

                // Выполняем HTTP-запрос
                string getInfoUrl = $"http://{address}:{port}/profile/getinfo/{selectedProfileId}";

                try
                {
                    WebRequest request = WebRequest.Create(getInfoUrl);
                    request.Method = "GET";

                    using (WebResponse response = request.GetResponse())
                    using (Stream dataStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(dataStream))
                    {
                        string content = reader.ReadToEnd();
                        // Разбиваем строку ответа на подстроки
                        JObject responseData = JObject.Parse(content);
                        JObject data = responseData["data"] as JObject;

                        // Выводим каждый параметр с новой строки
                        Console.WriteLine($"Информация о профиле {selectedProfileName} (ID: {selectedProfileId}):");
                        foreach (var property in data.Properties())
                        {
                            Console.WriteLine($"{property.Name}: {property.Value}");
                        }
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine($"Не удалось получить информацию о профиле {selectedProfileName}. Ошибка: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Профиль с именем {selectedProfileName} не найден.");
            }
        }

        public static void ConsoleChangeColor(string message, string color)
        {
            if (color == "red")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else if (color == "white")
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else if (color == "yellow")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else if (color == "black")
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else if (color == "green")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else if (color == "magenta")
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else if (color == "blue")
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else if (color == "cyan")
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else if (color == "darkcyan")
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else if (color == "darkblue")
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else if (color == "darkgreen")
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else if (color == "darkgrey")
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public class ApiResponse
        {
            [JsonProperty("data")]
            public JObject Data { get; set; }

            [JsonProperty("debug_port")]
            public JObject debug_port { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }
        }

        public class ProfileData
        {
            [JsonProperty("cloud_id")]
            public string CloudId { get; set; }

            [JsonProperty("debug_port")]
            public string DebugPort { get; set; }

            [JsonProperty("folder")]
            public string Folder { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("tags")]
            public List<string> Tags { get; set; }

            [JsonProperty("websocket_link")]
            public string WebsocketLink { get; set; }
        }
        class AccountNameGenerator
        {
            Random rnd = new Random();

            public string GenerateAccountName()
            {
                string accountName = $"WB{rnd.Next(1,9999)}";
                return accountName;
            }
        }
    }
}
