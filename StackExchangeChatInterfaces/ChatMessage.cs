using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchangeChatInterfaces
{
    public class ChatMessage
    {
        public int event_type { get; set; }
        public int time_stamp { get; set; }
        public string content { get; set; }
        public int id { get; set; }
        public int user_id { get; set; }
        public string user_name { get; set; }
        public int room_id { get; set; }
        public string room_name { get; set; }
        public int message_id { get; set; }
    }
}
