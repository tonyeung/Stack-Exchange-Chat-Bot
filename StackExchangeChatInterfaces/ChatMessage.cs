using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchangeChatInterfaces
{
    public class ChatMessage
    {
        int event_type { get; set; }
        int time_stamp { get; set; }
        string content { get; set; }
        int id { get; set; }
        int user_id { get; set; }
        string user_name { get; set; }
        int room_id { get; set; }
        string room_name { get; set; }
        int message_id { get; set; }
    }
}
