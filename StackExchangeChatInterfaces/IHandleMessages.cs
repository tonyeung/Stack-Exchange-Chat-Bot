using System;
namespace StackExchangeChatInterfaces
{
    public interface IHandleMessages
    {
        Action<ChatMessage, IClient> HandleMessage { get; }
    }
}
