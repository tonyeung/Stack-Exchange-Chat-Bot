using System;
namespace StackExchangeChatInterfaces
{
    public interface IHandleMessages
    {
        Action<object, IClient> HandleMessage { get; }
    }
}
