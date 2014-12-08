using StackExchangeChatInterface;
using StackExchangeChatInterfaces;
using StructureMap;
using StructureMap.Graph;
using System;
using System.Collections.Generic;
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
                    scan.AddAllTypesOf<IMessageHandlers>(); //need to copy the dll instead of directly referencing it, same goes for the client dll
                });
                x.For<IClient>().Use<Client>(); //need to check if there's a way to scan for this and still get an instance via Container like line 59
            });

            HostFactory.Run(x =>
            {
                x.Service<ServiceMain>(s =>
                {
                    s.ConstructUsing(name => new ServiceMain(container));
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

        public ServiceMain(IContainer structureMapContainer)
        {
            var username = "";
            var password = "";
            var roomUrl = "";

            var messageHandlers = structureMapContainer.GetAllInstances<IMessageHandlers>();

            chatInterface = structureMapContainer
                .With("username").EqualTo(username)
                .With("password").EqualTo(password)
                .With("defaultRoomUrl").EqualTo(roomUrl)
                .With<Action<object, object>>(delegate(object sender, object messageWrapper)
                    {
                        //{"r14368":{"e":[{"event_type":1,"time_stamp":1418019645,"content":"{\u0026quot;r‌​14368\u0026quot;:{\u0026quot;t\u0026quot;:35251580,\u0026quot;d\u0026quot;:1}}","‌​id":35251581,"user_id":106166,"user_name":"HoiHoi-san","room_id":14368,"room_name‌​":"HoiHoi-san\u0027s Testbed","message_id":18952030}],"t":35251581,"d":1}}
                        string message = ((dynamic)messageWrapper).Message;
                        Console.WriteLine("");
                        Console.WriteLine(message);
                        foreach (var item in messageHandlers)
                        {
                            item.MessageHandler.Invoke(message, chatInterface);
                        }                        
                    }).GetInstance<IClient>();
        }

        public void Start() {
            var message = Console.ReadLine();
            chatInterface.PostMessage(message, "http://chat.stackexchange.com/rooms/14368/hoihoi-sans-testbed");
        }

        public void Stop() { 
        
        }
    }
}
