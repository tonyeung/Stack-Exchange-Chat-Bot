﻿using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using WebSocket4Net;

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
        public string RoomUrl { get; set; }
        public CookieContainer CookieContainer { get; set; }
        private CookieCollection responseCookies;
        private WebSocket webSocket;
        public ServiceMain()
        {
            HtmlDocument htmlDocument = new HtmlDocument();
            //todo pull this into app.config
            Username = "";
            Password = "";
            RoomUrl = "";
            

            this.CookieContainer = new CookieContainer();

            var rawLoginPage = GetLoginPage().Result;

            htmlDocument.LoadHtml(rawLoginPage);
            FKey = htmlDocument.DocumentNode.SelectSingleNode("//input[@name='fkey']").Attributes["value"].Value.Trim();

            var rawLoggedInPage = SignInToOpenIdEndPoint(FKey, Username, Password).Result;
            var signInLink = GetSignInViaOpenIdLink().Result;
            var authTokenPage = GetOpenIdAuthTokenPage(signInLink).Result;


            htmlDocument.LoadHtml(authTokenPage);
            var redirectLink = htmlDocument.DocumentNode.SelectSingleNode("//a").Attributes["href"].Value.Trim();
            var stackExchangePage = SignInToStackExchange(redirectLink).Result;

            var chatSignInPage = GetChatSignInPage().Result;

            htmlDocument.LoadHtml(chatSignInPage);
            var url = htmlDocument.DocumentNode.SelectSingleNode("//form[@id='chat-login-form']").Attributes["action"].Value.Trim();
            var token = htmlDocument.DocumentNode.SelectSingleNode("//input[@id='authToken']").Attributes["value"].Value.Trim();
            var nonce = htmlDocument.DocumentNode.SelectSingleNode("//input[@name='nonce']").Attributes["value"].Value.Trim();

            var chatRoomList = SignInToChat(url, token, nonce).Result;

            var favoriteRoomsPage = GetFavoriteRooms().Result;
            htmlDocument.LoadHtml(favoriteRoomsPage);
            FKey = htmlDocument.DocumentNode.SelectSingleNode("//input[@name='fkey']").Attributes["value"].Value.Trim();

            StartSocketToListenToEvents();
        }

        /// <summary>
        /// Need to hit stack exchange login page which contains the fkey
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetLoginPage()
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
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

        public async Task<string> SignInToOpenIdEndPoint(string FKey, string Email, string Password)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri("https://openid.stackexchange.com/");
                var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("fkey", FKey),
                    new KeyValuePair<string, string>("email", Email),
                    new KeyValuePair<string, string>("password", Password)
                });

                HttpResponseMessage response = await client.PostAsync("account/login/submit", content);
                if (response.IsSuccessStatusCode)
                {
                    responseCookies = CookieContainer.GetCookies(new Uri("https://openid.stackexchange.com"));
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR logging in, status: " + response.StatusCode.ToString());
                }
            }
        }

        public async Task<string> GetSignInViaOpenIdLink()
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri("https://stackexchange.com/");
                var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("from", "https://stackexchange.com/users/login#log-in")
                });

                HttpResponseMessage response = await client.PostAsync("users/signin", content);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR getting open id sign in link, status: " + response.StatusCode.ToString());
                }
            }
        }

        public async Task<string> GetOpenIdAuthTokenPage(string signInLink)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(signInLink);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR getting open id auth token, status: " + response.StatusCode.ToString());
                }
            }
        }

        public async Task<string> SignInToStackExchange(string signInLink)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(signInLink);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR signing in to Stack Exchange, status: " + response.StatusCode.ToString());
                }
            }
        }

        /// <summary>
        /// Gets the chat Sign In page which contains the url, auth token, and nonce required for login
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetChatSignInPage()
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri("http://stackexchange.com/users/chat-login");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR getting the chat sign in page, status: " + response.StatusCode.ToString());
                }
            }
        }

        public async Task<string> SignInToChat(string signInUrl, string authToken, string nonce)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(signInUrl);
                client.DefaultRequestHeaders.Add("Referer", "http://stackexchange.com/users/chat-login");
                var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("authToken", authToken),
                    new KeyValuePair<string, string>("nonce", nonce)
                });

                HttpResponseMessage response = await client.PostAsync("", content);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR signing into chat, status: " + response.StatusCode.ToString());
                }
            }
        }

        /// <summary>
        /// Need to hit stack exchange chat favorites page to refresh the fkey
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetFavoriteRooms()
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri("http://chat.stackexchange.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("chats/join/favorite");
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

        public async Task<string> GetRawSocketUri(string RoomUrl)
        {
            var uri = new Uri(RoomUrl);
            var roomId = uri.PathAndQuery.Split('/')[2];

            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(uri.AbsoluteUri.Replace(uri.PathAndQuery, "") + "/ws-auth");
                client.DefaultRequestHeaders.Add("Referer", RoomUrl);
                client.DefaultRequestHeaders.Add("Origin", uri.AbsoluteUri.Replace(uri.PathAndQuery, ""));
                var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("roomid", roomId),
                    new KeyValuePair<string, string>("fkey", FKey.Replace("-", ""))
                });

                HttpResponseMessage response = await client.PostAsync("", content);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR signing into chat, status: " + response.StatusCode.ToString());
                }
            }
        }

        public async Task<string> GetLastEventId(string RoomUrl)
        {
            var uri = new Uri(RoomUrl);
            var roomId = uri.PathAndQuery.Split('/')[2];

            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(uri.AbsoluteUri.Replace(uri.PathAndQuery, "") + "/chats/" + roomId + "/events");
                var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("mode", "Events"),
                    new KeyValuePair<string, string>("msgCount", "0"),
                    new KeyValuePair<string, string>("fkey", FKey.Replace("-", ""))
                });

                HttpResponseMessage response = await client.PostAsync("", content);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR signing into chat, status: " + response.StatusCode.ToString());
                }
            }
        }

        public void StartSocketToListenToEvents()
        {

            var rawLastEventId = GetLastEventId(RoomUrl).Result;
            var lastEventId = (int)JObject.Parse(rawLastEventId)["time"];

            var rawSocketUri = GetRawSocketUri(RoomUrl).Result;
            var socketUri = JObject.Parse(rawSocketUri)["url"].ToString() + "?l=" + lastEventId;

            var roomUri = new Uri(RoomUrl);
            var root = roomUri.AbsoluteUri.Replace(roomUri.PathAndQuery, "");

            webSocket = new WebSocket(socketUri, "", null, null, "", root);
            webSocket.Opened += new EventHandler(websocket_Opened);
            webSocket.Error += new EventHandler<ErrorEventArgs>(websocket_Error);
            webSocket.Closed += new EventHandler(websocket_Closed);
            webSocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);

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
            StartSocketToListenToEvents();
        }

        private void websocket_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("ws error");
        }

        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        public async void Start() {
            var message = Console.ReadLine();

            var uri = new Uri(RoomUrl);
            var roomId = uri.PathAndQuery.Split('/')[2];

            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(uri.AbsoluteUri.Replace(uri.PathAndQuery, "") + "/chats/" + roomId + "/messages/new");
                var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("text", message),
                    new KeyValuePair<string, string>("fkey", FKey.Replace("-", ""))
                });

                HttpResponseMessage response = await client.PostAsync("", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR signing into chat, status: " + response.StatusCode.ToString());
                }
            }
        }
        public void Stop() { 
        
        }
    }
}
