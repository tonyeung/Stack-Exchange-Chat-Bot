using System;
namespace StackExchangeChatInterfaces
{
    public interface IMessageHandlers
    {
        Action<object, IClient> MessageHandler { get; }
    }
}
