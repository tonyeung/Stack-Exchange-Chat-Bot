using System;
namespace StackExchangeChatInterfaces
{
    public interface IClient
    {
        void StartClient(string username, string password, string defaultRoomUrl, Action<object, object> rawSocketMessage);
        void PostMessage(string message, int? roomUrl = null);
    }
}
