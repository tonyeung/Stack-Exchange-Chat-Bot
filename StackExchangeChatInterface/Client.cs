using StackExchangeChatInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WebSocket4Net;

namespace StackExchangeChatInterface
{
    public partial class Client : IClient
    {
        private CookieContainer CookieContainer;
        private string DefaultRoomUrl;
        private Action<object, object> MessageHandler;
        private string FKey;

        public Client(string username, string password, string defaultRoomUrl, Action<object, object> messageHandler)
        {
            this.DefaultRoomUrl = defaultRoomUrl;
            this.CookieContainer = new CookieContainer();
            this.MessageHandler = messageHandler;

            SignIn(username, password);
            StartSocketToListenToEvents();
        }

        public void PostMessage(string message, string roomUrl = "")
        {
            if (string.IsNullOrWhiteSpace(roomUrl))
            {
                roomUrl = this.DefaultRoomUrl;
            }

            var uri = new Uri(roomUrl);
            var baseUri = uri.AbsoluteUri.Replace(uri.PathAndQuery, "");
            var roomId = uri.PathAndQuery.Split('/')[2];
            var baseAddres = new Uri(baseUri + "/chats/" + roomId + "/messages/new");

            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = baseAddres;
                var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("text", message),
                    new KeyValuePair<string, string>("fkey", FKey)
                });

                HttpResponseMessage response = client.PostAsync("", content).Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("ERROR posting message, status: " + response.StatusCode.ToString());
                }
            }

        }
    }
}
