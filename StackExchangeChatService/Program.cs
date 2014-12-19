using Newtonsoft.Json;
using StackExchangeChatInterfaces;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.Graph;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace StackExchangeChatService
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new Container(x =>
            {
                x.Scan(scan =>
                {
                    scan.AssembliesFromApplicationBaseDirectory();
                    scan.ExcludeNamespace("StructureMap");
                    scan.WithDefaultConventions();
                    scan.AddAllTypesOf<IHandleMessages>(); //need to copy MessageHandlers and ChatClient dlls into the service bin folder instead of referencing
                });
            });

            var client = container.GetInstance<IClient>();
            var handlers = container.GetAllInstances<IHandleMessages>();

            HostFactory.Run(x =>
            {
                x.Service<ServiceMain>(s =>
                {
                    s.ConstructUsing(name => new ServiceMain(client, handlers));
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Stack Exchange Chat Bot");
                x.SetDisplayName("StackExchangeChatBotService");
                x.SetServiceName("StackExchangeChatBotService");
            });
        }
    }

    public class ServiceMain
    {
        private IClient chatInterface;
        private IEnumerable<IHandleMessages> handlers;

        public ServiceMain(IClient client, IEnumerable<IHandleMessages> handlers)
        {
            chatInterface = client;
            this.handlers = handlers;
        }

        public void Start()
        {
            int userid = int.Parse(ConfigurationManager.AppSettings["userid"]);
            var username = ConfigurationManager.AppSettings["username"].ToString();
            var password = ConfigurationManager.AppSettings["password"].ToString();
            var roomUrl = ConfigurationManager.AppSettings["roomUrl"].ToString();

            chatInterface.StartClient(username, password, roomUrl, (object sender, object rawSocketMessage) =>
            {
                string message = ((dynamic)rawSocketMessage).Message;
                var roomMessage = getChatMessage(message);
                if (roomMessage != null && roomMessage.user_id != userid)
                {
                    Console.WriteLine("");
                    Console.WriteLine(roomMessage);
                    foreach (var item in handlers)
                    {
                        item.HandleMessage.Invoke(roomMessage, chatInterface);
                    }
                }
            });
            var post = Console.ReadLine();
            chatInterface.PostMessage(post);
        }

        public void Stop() { 
        
        }

        private ChatMessage getChatMessage(string message)
        {
            //message = @"{""r14368"":{""e"":[{""event_type"":1,""time_stamp"":1418973788,""content"":""hammer"",""id"":35648111,""user_id"":106166,""user_name"":""HoiHoi-san"",""room_id"":14368,""room_name"":""HoiHoi-san\u0027s Testbed"",""message_id"":19159074}],""t"":35648112,""d"":2},""r6697"":{""t"":35648112,""d"":2}}";
            dynamic rawMessage = JsonConvert.DeserializeObject(message);
            ChatMessage result = null;
            try
            {
                result = rawMessage.r6697.e[0].ToObject<ChatMessage>();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) { }
            catch
            {
                result = rawMessage.r14368.e[0].ToObject<ChatMessage>();
            }
            return result;
        }
    }
}
