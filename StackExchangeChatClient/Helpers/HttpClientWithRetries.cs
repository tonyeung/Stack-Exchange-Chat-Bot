using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace StackExchangeChatClient.Helpers
{
    public static class HttpClientWithRetries
    {
        private static Lazy<CookieContainer> CookieContainer = new Lazy<CookieContainer>();
        
        public static async Task<string> Execute(
            string url,
            string method,
            HttpContent content = null,
            HttpRequestHeaders headers = null)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = CookieContainer.Value })
            using (var client = new HttpClient(new RetryDelegatingHandler(handler)))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                HttpResponseMessage response = new HttpResponseMessage();

                if (method == "GET")
                {
                    response = await GetAsync(client, url);
                }

                if (method == "POST")
                {
                    response = await PostAsync(client, url, content);
                }

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR url: " + url + ", status: " + response.StatusCode.ToString());
                }
            }
        }

        static Task<HttpResponseMessage> GetAsync(HttpClient client, string url)
        {
            return client.GetAsync(url);
        }

        static Task<HttpResponseMessage> PostAsync(HttpClient client, string url, HttpContent content)
        {
            return client.PostAsync(url, content);
        }
    }
}
