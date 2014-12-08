﻿using StackExchangeChatInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchangeMessageHandlers
{
    public partial class MessageHandlers : IMessageHandlers
    {
        public Action<object, IClient> MessageHandler
        {
            get
            {
                return delegate(object o, IClient client)
                {
                    client.PostMessage("hammer");
                };
            }
        }        
    }
}