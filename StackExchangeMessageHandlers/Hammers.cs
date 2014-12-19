using StackExchangeChatInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchangeMessageHandlers
{
    public partial class MessageHandlers : IHandleMessages
    {
        public Action<ChatMessage, IClient> HandleMessage
        {
            get
            {
                return (ChatMessage message, IClient client) =>
                {
                    client.PostMessage("hammer", message.room_id);
                };
            }
        }        
    }
}
