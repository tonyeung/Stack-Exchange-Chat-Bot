using System;
namespace StackExchangeChatInterfaces
{
    public interface IClient
    {
        void StartClient(string username, string password, string defaultRoomUrl, Action<object, object> rawSocketMessage);
        void PostMessage(string message, int retries = 0, int roomId = 0);
        void PingUser(string message, string username, int roomId = 0);
        void ReplyToMessage(string message, string messageId, int roomId = 0);
    }
}
