using Newtonsoft.Json.Linq;
using StackExchangeChatClient.Helpers;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebSocket4Net;

namespace StackExchangeChatClient
{
    public partial class Client
    {
        private WebSocket webSocket;

        /// <summary>
        /// Call this method to join a room
        /// If a room url is not passed, default room is joined and connection maintained here
        /// Else the client will not attempt to reconnect after joining a room
        /// Only one socket is required to be maintained because it gets all the messages from all joined rooms
        /// </summary>
        /// <param name="roomUrl"></param>
        private async Task StartSocketToListenToEvents(string roomUrl = "")
        {
            var isDefaultRoom = false;
            if (string.IsNullOrWhiteSpace(roomUrl))
            {
                isDefaultRoom = true;
                roomUrl = this.DefaultRoomUrl;
            }
            var rawLastEventId = await GetLastEventId(roomUrl);
            var lastEventId = (int)JObject.Parse(rawLastEventId)["time"];

            var rawSocketUri = await GetRawSocketUri(roomUrl);
            var socketUri = JObject.Parse(rawSocketUri)["url"].ToString() + "?l=" + lastEventId;

            var roomUri = new Uri(roomUrl);
            var root = roomUri.AbsoluteUri.Replace(roomUri.PathAndQuery, "");

            webSocket = new WebSocket(socketUri, "", null, null, "", root);
            webSocket.Opened += new EventHandler(websocket_Opened);
            webSocket.Error += new EventHandler<ErrorEventArgs>(websocket_Error);

            if (isDefaultRoom)
            {
                webSocket.Closed += new EventHandler(websocket_Closed);
                webSocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(this.MessageHandler); 
            }            

            webSocket.Open();
        }

        private void websocket_Opened(object sender, EventArgs e)
        {
            Console.WriteLine("ws opened");
            //webSocket.Send("Hello World!");
        }

        private void websocket_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("ws closed");
            webSocket = null;
            StartSocketToListenToEvents().RunSynchronously();
        }

        private void websocket_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("ws error");
        }

        private void websocket_MessageReceived(object sender, object e)
        {
            dynamic args = e;
            Console.WriteLine(args.Message);
        }

        private async Task<string> GetRawSocketUri(string roomUrl)
        {
            var uri = new Uri(roomUrl);
            var roomId = uri.PathAndQuery.Split('/')[2];
            var root = uri.AbsoluteUri.Replace(uri.PathAndQuery, "");

            var message = new HttpRequestMessage();
            message.Headers.Add("Referer", roomUrl);
            message.Headers.Add("Origin", root);
            var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("roomid", roomId),
                    new KeyValuePair<string, string>("fkey", FKey)
                });

            return await HttpClientWithRetries.Execute(root + "/ws-auth", "POST", content, message.Headers);
        }

        private async Task<string> GetLastEventId(string roomUrl)
        {
            var uri = new Uri(roomUrl);
            var roomId = uri.PathAndQuery.Split('/')[2];
            var root = uri.AbsoluteUri.Replace(uri.PathAndQuery, "");


            var url = root + "/chats/" + roomId + "/events";
            var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("mode", "Events"),
                    new KeyValuePair<string, string>("msgCount", "0"),
                    new KeyValuePair<string, string>("fkey", FKey)
                });

            return await HttpClientWithRetries.Execute(url, "POST", content);
        }
    }
}
