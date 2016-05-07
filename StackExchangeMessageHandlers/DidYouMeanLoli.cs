using StackExchangeChatInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace StackExchangeMessageHandlers
{
    class DidYouMeanLoli : IMessageHandlers
    {
        public Action<object, IClient> MessageHandler
        {
            get
            {
                return delegate(object o, IClient client)
                {
                    dynamic chatMessage = o;

                    var messageid = chatMessage.MessageID;
                    var userid = chatMessage.UserId;

                    var userIsAgainstLoliCorrection = false;

                    try
                    {
                        var list = JsonConvert.DeserializeObject<List<int>>(File.ReadAllText("DidYouMeanLoli opted out user ids.txt"));

                        if (list.Contains(userid))
                            userIsAgainstLoliCorrection = true;
                    }
                    catch (Exception ex)
                    {
                        // Log("[X] Could not load list of users who are against \"Did you mean loli?\" correction: " + ex.ToString());
                    }

                    if (!userIsAgainstLoliCorrection)
                        client.PostMessage(":" + messageid + " Did you mean *loli?*");
                };
            }
        }
    }
}
