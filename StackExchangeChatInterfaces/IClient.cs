using System;
using System.Threading.Tasks;
namespace StackExchangeChatInterfaces
{
    public interface IClient
    {
        Task PingUserAsync(string message, string username, int roomId = 0);
        Task PostMessageAsync(string message, int roomId = 0);
        Task ReplyToMessage(string message, int messageId, int roomId = 0);
        Task StartClientAsync(string username, string password, string defaultRoomUrl, Action<object, object> messageHandler);
    }
}
