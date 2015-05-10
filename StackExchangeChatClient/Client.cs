using Newtonsoft.Json;
using StackExchangeChatClient.Helpers;
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
        private string DefaultRoomUrl;
        private Action<object, object> MessageHandler;
        private string FKey;


        public async Task StartClientAsync(string username, string password, string defaultRoomUrl, Action<object, object> messageHandler)
        {
            this.DefaultRoomUrl = defaultRoomUrl;
            this.MessageHandler = messageHandler;
            await SignInAsync(username, password);
            await StartSocketToListenToEvents();
        }

        public async Task PostMessageAsync(string message, int roomId = 0)
        {
            var roomUrl = this.DefaultRoomUrl;

            var uri = new Uri(roomUrl);
            var baseUri = uri.AbsoluteUri.Replace(uri.PathAndQuery, "");
            if (roomId == 0)
            {
                roomId = int.Parse(uri.PathAndQuery.Split('/')[2]);
            }
            var baseAddres = baseUri + "/chats/" + roomId + "/messages/new";

            var content = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("text", message),
                new KeyValuePair<string, string>("fkey", FKey)
            });

            await HttpClientWithRetries.Execute(baseAddres, "POST", content);
        }

        public async Task PingUserAsync(string message, string username, int roomId = 0)
        {
            message = "@" + username + " " + message;
            await PostMessageAsync(message, roomId);
        }

        public async Task ReplyToMessage(string message, int messageId, int roomId = 0)
        {
            message = ":" + messageId + " " + message;
            await PostMessageAsync(message, roomId);
        }
    }
}
