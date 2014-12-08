using System;
namespace StackExchangeChatInterfaces
{
    public interface IClient
    {
        void PostMessage(string message, string roomUrl = "");
    }
}
