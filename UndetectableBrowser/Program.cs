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
using static Org.BouncyCastle.Asn1.Cmp.Challenge;

namespace UndetectableBrowser
{
    internal class Program
    {
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

                string choice = Console.ReadLine();

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

                return data.ToString(); // Преобразование объекта data в строку
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

        static void StopProfile(string address, string port, string profileId)
        {
            var client = new RestClient($"http://{address}:{port}/profile/stop/{profileId}");
            var request = new RestRequest(address, Method.Get);
            client.Execute(request);
        }
        // Меню метод
        static void HandleGeneralSettings(Dictionary<string, string> generalSettings)
        {
            while (true)
            {
                Console.WriteLine("Общие настройки:");
                Console.WriteLine("1. Указать путь к файлу с прокси");
                Console.WriteLine("2. Указать путь к гугл таблице");
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
                        // Ваш код для обработки указания пути к гугл таблице
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

                string choice = Console.ReadLine();

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
                                driver.Navigate().GoToUrl("https://whoer.net");
                                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                                js.ExecuteScript("window.open();");

                                var windowHandles = driver.WindowHandles;
                                driver.SwitchTo().Window(driver.WindowHandles[1]);
                                driver.Navigate().GoToUrl("https://browserleaks.com/js");
                                Thread.Sleep(5000);
                            }
                        }
                    }
                }
                else if (status == "Locked")
                {
                    continue;
                }
            }
        }

        static void HandleWBSettings()
        {
            // Запуск модуля регистрации профилей на wb
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

        static void HandleSendToCloud(Dictionary<string, Dictionary<string, string>> profileSettings)
        {
            Console.WriteLine("Выбрано отправить профиль в облако");
            // Ваш код для обработки отправки профиля в облако
        }

        static void HandleMakeLocal(Dictionary<string, Dictionary<string, string>> profileSettings)
        {
            Console.WriteLine("Выбрано сделать профиль локальным");
            // Ваш код для обработки сделать профиль локальным
        }

        static void HandleProfileInfo(Dictionary<string, Dictionary<string, string>> profileSettings)
        {
            Console.WriteLine("Выбран вывод информации о профиле");
            // Ваш код для вывода информации о профиле
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

            private int counter = 1;

            public string GenerateAccountName()
            {
                string accountName = $"WB{rnd.Next(1,9999)}";
                counter++;
                return accountName;
            }
        }
    }
}
