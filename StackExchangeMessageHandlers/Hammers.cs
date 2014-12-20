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
                    if (message.room_id == 14368)
                        //client.PostMessage("hammer", roomId:message.room_id);
                        //client.ReplyToMessage("hammer", message.message_id, message.room_id);
                        client.PingUser("hammer", message.user_name, message.room_id);
                };
            }
        }        
    }
}
