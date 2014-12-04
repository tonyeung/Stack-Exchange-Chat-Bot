using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace StackExchangeChatService
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<ServiceMain>(s =>
                {
                    s.ConstructUsing(name => new ServiceMain());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Stack Exchange Chat Bot");
                x.SetDisplayName("StackExchangeChatBotService");
                x.SetServiceName("StackExchangeChatBotService");
            });
        }
    }

    public class ServiceMain
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FKey { get; set; }
        public ServiceMain()
        {
            //todo pull this into app.config
            Username = "";
            Password = "";
            var rawLoginPage = GetLoginPage().Result;
            FKey = GetValueFromHtmlNode("input", "value", "fkey", rawLoginPage);
        }

        public async Task<string> GetLoginPage()
        {
            using (var client = new HttpClient())
            {
                // New code:
                client.BaseAddress = new Uri("https://openid.stackexchange.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("account/login");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR getting RawFKey, status: " + response.StatusCode.ToString());
                }
            }
        }

        public static string GetValueFromHtmlNode(string element, string attribute, string name, string html)
        {
            try
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                var nodeFilter = "//" + element + "[@name='" + name + "']";
                return htmlDocument.DocumentNode.SelectSingleNode(nodeFilter).Attributes[attribute].Value;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Start() { 
        
        }
        public void Stop() { 
        
        }
    }
}
