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

namespace StackExchangeChatClient
{
    public partial class Client : IClient
    {
        private CookieContainer CookieContainer;
        private string DefaultRoomUrl;
        private Action<object, object> MessageHandler;
        private string FKey;

        public Client()
        {
            this.CookieContainer = new CookieContainer();
        }

        public void StartClient(string username, string password, string defaultRoomUrl, Action<object, object> messageHandler)
        {
            this.DefaultRoomUrl = defaultRoomUrl;
            this.MessageHandler = messageHandler;
            SignIn(username, password);
            StartSocketToListenToEvents();
        }

        public void PostMessage(string message, int retries = 0, int roomId = 0)
        {
            var roomUrl = this.DefaultRoomUrl;

            var uri = new Uri(roomUrl);
            var baseUri = uri.AbsoluteUri.Replace(uri.PathAndQuery, "");
            if (roomId == 0)
            {
                roomId = int.Parse(uri.PathAndQuery.Split('/')[2]);    
            }
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
                var responseContent = response.Content.ReadAsStringAsync();
                dynamic postResult = responseContent.Result;
                if (postResult.id == "null")
                {
                    if (retries == 1)
                    {
                        System.Threading.Thread.Sleep(3*1000);
                        PostMessage(message, retries++);
                    }
                    else if (retries == 2)
                    {
                        System.Threading.Thread.Sleep(30*1000);
                        PostMessage(message, retries++);
                    }
                    else if (retries == 2)
                    {
                        System.Threading.Thread.Sleep(2*60*1000);
                        PostMessage(message, retries++);
                    }                    
                }
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("ERROR posting message, status: " + response.StatusCode.ToString());
                }
            }
        }


        public void PingUser(string message, string username, int roomId = 0)
        {
            message = "@" + username + " " + message;
            PostMessage(message, roomId);
        }

        public void ReplyToMessage(string message, string messageId, int roomId = 0)
        {
            message = ":" + messageId + " " + message;
            PostMessage(message, roomId);
        }
    }
}
