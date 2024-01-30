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

namespace UndetectableBrowser
{
    internal class Program
    {
        static void Main(string[] args)
        {
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
                if(status == "Available")
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
                        if(!String.IsNullOrWhiteSpace(debugPort2))
                        {
                            using(var driver = CreateChromeDriver(chromeDriverService, chromeOptions, address, debugPort2))
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



            //foreach (KeyValuePair<string, dynamic> entry in (IEnumerable<KeyValuePair<string, dynamic>>)listResponse)
            //{
            //    string profileId = entry.Key;
            //    dynamic profileData = entry.Value;

            //    if (profileData["folder"] == "test" && profileData["status"] != "Locked")
            //    {
            //        var debugPort = GetDebugPort(address, portFromSettingsBrowser, profileId, profileData["status"], profileData);

            //        if (!string.IsNullOrEmpty(debugPort))
            //        {
            //            using (var driver = CreateChromeDriver(chromeDriverService, chromeOptions, address, debugPort))
            //            {
            //                driver.Navigate().GoToUrl("https://whoer.net/");
            //                ((IJavaScriptExecutor)driver).ExecuteScript("window.open();");
            //                driver.SwitchTo().Window(driver.WindowHandles[^1]);
            //                driver.Navigate().GoToUrl("https://browserleaks.com/js");

            //                // Добавьте любые другие действия здесь

            //                Thread.Sleep(5000);
            //            }

            //            StopProfile(address, portFromSettingsBrowser, profileId);
            //        }
            //    }
            //}
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
    }
}
