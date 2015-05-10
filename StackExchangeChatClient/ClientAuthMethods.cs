using HtmlAgilityPack;
using StackExchangeChatClient.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace StackExchangeChatClient
{
    public partial class Client
    {
        private async Task SignInAsync(string username, string password)
        {
            try
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                var rawLoginPage = await GetLoginPageAsync();

                htmlDocument.LoadHtml(rawLoginPage);
                FKey = htmlDocument.DocumentNode.SelectSingleNode("//input[@name='fkey']").Attributes["value"].Value.Trim();

                var rawLoggedInPage = await SignInToOpenIdEndPointAsync(username, password);
                var signInLink = await GetSignInViaOpenIdLinkAsync();
                var authTokenPage = await GetOpenIdAuthTokenPageAsync(signInLink);

                htmlDocument.LoadHtml(authTokenPage);
                var redirectLink = htmlDocument.DocumentNode.SelectSingleNode("//a").Attributes["href"].Value.Trim();
                var stackExchangePage = await SignInToStackExchangeAsync(redirectLink);

                var chatSignInPage = await GetChatSignInPageAsync();

                htmlDocument.LoadHtml(chatSignInPage);
                var url = htmlDocument.DocumentNode.SelectSingleNode("//form[@id='chat-login-form']").Attributes["action"].Value.Trim();
                var token = htmlDocument.DocumentNode.SelectSingleNode("//input[@id='authToken']").Attributes["value"].Value.Trim();
                var nonce = htmlDocument.DocumentNode.SelectSingleNode("//input[@name='nonce']").Attributes["value"].Value.Trim();

                var chatRoomList = await SignInToChatAsync(url, token, nonce);

                var favoriteRoomsPage = await GetFavoriteRoomsAsync();
                htmlDocument.LoadHtml(favoriteRoomsPage);
                FKey = htmlDocument.DocumentNode.SelectSingleNode("//input[@name='fkey']").Attributes["value"].Value.Trim();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Need to hit stack exchange login page which contains the fkey
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetLoginPageAsync()
        {
            var url = "https://openid.stackexchange.com/account/login";
            var message = new HttpRequestMessage();
            message.Headers.Accept.Clear();
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return await HttpClientWithRetries.Execute(url, "GET", headers: message.Headers);
        }

        private async Task<string> SignInToOpenIdEndPointAsync(string email, string password)
        {
            var content = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("fkey", FKey),
                new KeyValuePair<string, string>("email", email),
                new KeyValuePair<string, string>("password", password)
            });

            return await HttpClientWithRetries.Execute("https://openid.stackexchange.com/account/login/submit", "POST", content);
        }

        private async Task<string> GetSignInViaOpenIdLinkAsync()
        {
            var content = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("from", "https://stackexchange.com/users/login#log-in")
            });

            return await HttpClientWithRetries.Execute("https://stackexchange.com/users/signin", "POST", content);
        }

        private async Task<string> GetOpenIdAuthTokenPageAsync(string signInLink)
        {
            var message = new HttpRequestMessage();
            message.Headers.Accept.Clear();
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return await HttpClientWithRetries.Execute(signInLink, "GET", headers: message.Headers);
        }

        private async Task<string> SignInToStackExchangeAsync(string redirectLink)
        {
            var message = new HttpRequestMessage();
            message.Headers.Accept.Clear();
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return await HttpClientWithRetries.Execute(redirectLink, "GET", headers: message.Headers);
        }

        /// <summary>
        /// Gets the chat Sign In page which contains the url, auth token, and nonce required for login
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetChatSignInPageAsync()
        {
            var url = "http://stackexchange.com/users/chat-login";
            var message = new HttpRequestMessage();
            message.Headers.Accept.Clear();
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return await HttpClientWithRetries.Execute(url, "GET", headers: message.Headers);
        }

        private async Task<string> SignInToChatAsync(string signInUrl, string authToken, string nonce)
        {
            var message = new HttpRequestMessage();
            message.Headers.Add("Referer", "http://stackexchange.com/users/chat-login");
            var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("authToken", authToken),
                    new KeyValuePair<string, string>("nonce", nonce)
                });

            return await HttpClientWithRetries.Execute(signInUrl, "POST", content, message.Headers);
        }

        /// <summary>
        /// Need to hit stack exchange chat favorites page to refresh the fkey
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetFavoriteRoomsAsync()
        {
            var url = "http://chat.stackexchange.com/chats/join/favorite";
            var message = new HttpRequestMessage();
            message.Headers.Accept.Clear();
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return await HttpClientWithRetries.Execute(url, "GET", headers: message.Headers);
        }
    }
}
