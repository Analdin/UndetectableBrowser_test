using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UndetectableBrowser.NumJob
{
    public class OnlineSimJob
    {
        public void GetNum()
        {
            string fullUrl = $"https://give-sms.com/api/v1/?method=getnumber&service=uu&userkey=c0UcJZ6FPpSreydOvzv3&country=ru";
            string method = "GET";

            WebRequest request = WebRequest.Create(fullUrl);
            request.Method = method;

            // Получение ответа
            using (WebResponse response = request.GetResponse())
            using (Stream dataStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(dataStream))
            {
                // Обработка ответа
                Console.WriteLine($"{(int)((HttpWebResponse)response).StatusCode} {((HttpWebResponse)response).StatusDescription}");
                string jsonString = reader.ReadToEnd();
                Console.WriteLine(jsonString);

                if(reader.ReadToEnd().Contains("TRY_AGAIN_LATER"))
                {
                    Console.WriteLine("Не удалось получить номер, ошибка сервиса");
                }

                try
                {
                    // Парсинг JSON
                    var jsonObject = JsonConvert.DeserializeObject<ApiResponse>(jsonString);

                    // Получение значений и вывод
                    Variables.order_id = jsonObject.data.order_id;
                    Variables.order_phone = jsonObject.data.phone;

                    Console.WriteLine($"order_id: {Variables.order_id}");
                    Console.WriteLine($"phone: {Variables.order_phone}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке JSON: {ex.Message}");
                }
            }
        }

        public static string GetAnswer(string id)
        {
            string fullUrl = $"https://give-sms.com/api/v1/?method=getcode&order_id={id}&userkey=c0UcJZ6FPpSreydOvzv3";
            string method = "GET";
            string mimeType = "application/x-www-form-urlencoded";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullUrl);
            request.Method = method;
            request.ContentType = mimeType;

            string answer = String.Empty;

            try
            {
                using (WebResponse response = request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    Console.WriteLine($"{((HttpWebResponse)response).StatusCode} {((HttpWebResponse)response).StatusDescription}");
                    string responseBody = reader.ReadToEnd();
                    Console.WriteLine("Ответ сервиса - " + responseBody);
                    answer = responseBody;
                }
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    Console.WriteLine($"Error: {errorResponse.StatusCode} {errorResponse.StatusDescription}");
                }
                else
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            var jsonObject = JsonConvert.DeserializeObject<dynamic>(answer);
            string fullSms = jsonObject.data.fullSms;

            return fullSms;
        }
        public class ApiResponse
        {
            public int status { get; set; }
            public Data data { get; set; }
        }

        public class Data
        {
            public string order_id { get; set; }
            public string phone { get; set; }
            public DateTime start_time { get; set; }
        }
    }
}
