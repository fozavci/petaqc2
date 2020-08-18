using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;


namespace PetaqService
{
    public class Program
    {
        public static bool serviceIsRunning = true;
        public static string logDate = DateTime.Now.ToString("yyyy-M-dd---HH-mm-ss");

        public static PetaqConsole petaconsole;

	    // services to listen
	    //public static string serviceURLs = "https://0.0.0.0:443;http://0.0.0.0:80";

        // container for the configuration
        public static Dictionary<string, string> configuration = new Dictionary<string, string>();

        // container for implant sockets
        public static ConcurrentDictionary<string, ImplantServiceSocket> implantSockets = new ConcurrentDictionary<string, ImplantServiceSocket>();

        // routing table for linked implants
        public static ConcurrentDictionary<string, string> implantRoutes = new ConcurrentDictionary<string, string>();
        // Usage:              
        // implantRoutes.TryAdd("X1", "NextHop in Linked Implants");

        // scenarios
        public static ConcurrentDictionary<string, dynamic> scenarios = new ConcurrentDictionary<string, dynamic>();


        public static void Main(string[] args)
        {
            // Used for encrypted communications - AES Key and IV
            // Make sure PetaqImplant also uses the same keys (Configuration.cs)
            // In future this will change with TLS communications
            configuration.Add("SESSION_KEY", "Petaq-TestSessionKey");
            configuration.Add("SESSION_IV", "Petaq-TestSessionIV");

            if (args.Length != 0)
            {                
                configuration["SESSION_KEY"] = args[0];
            }

            Console.WriteLine(logDate);

            // Starting Console
            petaconsole = new PetaqConsole();
            Thread console = new Thread(new ThreadStart(petaconsole.Start));
            console.Start();

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
              //.UseUrls(serviceURLs)
              .UseStartup<Startup>();
    }
}
