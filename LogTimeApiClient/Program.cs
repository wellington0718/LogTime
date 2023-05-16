using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LogTimeApiClient
{
    internal static class Program
    {
        private static readonly HttpClient client;
        static Program()
        {
            client = new HttpClient { BaseAddress = new Uri("http://localhost:55723/api/Session/") };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private static void Main(string[] args)
        {
            const string accessTokenEndPoint = "GetAccessToken";
            const string saveActiveSession = "SaveActiveSession";
            var parameters = new { EmployeeId = "2929", Password = "godismylord0718" };

            var responseMessage = GetAccessToken(accessTokenEndPoint, parameters);
            var httResponse = JsonSerializer.Deserialize<HttResponse>(responseMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (httResponse.Status != 401)
            {
              var response = SaveActiveSession(saveActiveSession, httResponse.AccessToken, httResponse.EmployeeId);
            }
            else
            {
                Console.WriteLine($"{httResponse.Status}:{httResponse.Title}");
            }

            Console.ReadLine();
        }

        private static string GetAccessToken<T>(string endPoint, T parameters)
        {
            try
            {
                var responseMessage = client.PostAsJsonAsync(endPoint, parameters).Result;
                var accessToken = responseMessage.Content.ReadAsStringAsync().Result;
                return accessToken;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetBaseException().Message);
                return string.Empty;
            }
        }

        private static int SaveActiveSession(string endPoint, string accessToken, string userId)
        {
            try
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = client.PostAsJsonAsync(endPoint, new {userId}).Result;
                int.TryParse(response.Content.ReadAsStringAsync().Result, out int sessionId);
                return sessionId;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetBaseException().Message);
                return 0;
            }
        }
    }
}
