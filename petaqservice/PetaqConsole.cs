using System;
using System.Threading;
using System.Threading.Tasks;


namespace PetaqService
{
    public class PetaqConsole
    {
        public static string implantname = null;
        public static bool stillconnected = false;
        public static Thread readThread;
        public void ResetMenu()
        {
            
            if (implantname == null)
            {
                Console.Write("# ");
            }
            else
            {
                Console.Write("{0} # ", implantname);                
            }
        }

        public void Start()
        {
            Thread.Sleep(2000);
            Console.WriteLine("Petaq - Purple Team Simulation Kit");
            while (Program.serviceIsRunning)
            {
                Console.Write("# ");
                string cmdline = Console.ReadLine();
                string[] cmd = cmdline.Split(" ");
                switch (cmd[0])
                {
                    case "":
                        Console.Write("");
                        break;
                    case "help":
                        Console.WriteLine(@"Help:
    help
List the Implants:
    list
Use the Implant:
    use SessionID
Remove the Implant:
    remove SessionID
Show Routes for Linked Implants:
    route
Exit:
    exit
    terminate");
                        break;
                    case "list":
                        if (Program.implantSockets.Count == 0)
                        {
                            Console.WriteLine("No implants registered yet.");
                        }
                        else
                        {
                            Console.WriteLine("Session ID\t\tUser Name\t\tHostname\tIP Address\tStatus\t\tLink URI");
                            foreach (var i in Program.implantSockets)
                            {
                                var implant = i.Value;
                                string username = implant.userName;
                                string hostname = implant.hostName;
                                string status = "disconnected";
                                if (implant.implantStatus) { status = "connected"; }
                                if (username.Length < 15) { username += "\t"; }
                                if (hostname.Length < 8) { hostname += "\t"; }
                                Console.WriteLine(i.Key + "\t" + username + "\t"
                                    + hostname + "\t" + implant.implantIP + "\t" + status + "\t" + implant.link_uri);
                            }
                        }
                        break;
                    case "route":
                        if (Program.implantRoutes.Count == 0)
                        {
                            Console.WriteLine("Routing table is empty.");
                        }
                        else
                        {                            
                            foreach (var r in Program.implantRoutes)
                            {
                                Console.WriteLine("{0} is linked through {1}.", r.Key, r.Value);
                            }
                        }
                        
                        break;
                    case "remove":
                        if (cmd.Length < 2 || cmd[1] == "")
                        {
                            Console.WriteLine("No implant selected.");
                            break;
                        }
                        string iname = cmd[1];
                        if (Program.implantSockets.ContainsKey(iname))
                        {
                            if (! Program.implantSockets[iname].implantStatus)
                            {
                                Program.implantRoutes.TryRemove(iname, out string inamevalue);
                                Program.implantSockets.TryRemove(iname, out ImplantServiceSocket inamesvalue);
                                Console.WriteLine("{0} is removed.", iname);
                            }
                            else
                            {
                                Console.WriteLine("{0} is still connected. To remove it from the list, unlink or exit first.", iname);
                            }
                        }
                        else
                        {
                            Console.WriteLine("{0} is not in the implant list.", iname);
                        }
                        break;
                    case "use":
                        if (cmd.Length < 2 || cmd[1] == "" || (! Program.implantSockets.ContainsKey(cmd[1]))) {
                            Console.WriteLine("No valid implant selected.");
                            break;
                        }
                        if (Program.implantSockets[cmd[1]].implantStatus)
                        {
                            //bool stillconnected = true;
                            implantname = cmd[1];
                            stillconnected = true;

                            Program.implantSockets[cmd[1]].SetConsoleOutput(stillconnected);
                            Console.WriteLine("Use 'back' instruction for the main menu.");
                            while (stillconnected)
                            {
                                Console.Write("{0} # ", implantname);
                                string ccmdline = Console.ReadLine();
                                string[] ccmd = ccmdline.Split(" ");
                                switch (ccmd[0])
                                {
                                    case "back":
                                        Console.WriteLine("Going back to main menu.");
                                        stillconnected = false;
                                        Program.implantSockets[cmd[1]].SetConsoleOutput(stillconnected);
                                        implantname = null;
                                        break;
                                    case "exit":
                                        // sending the command to the implant socket
                                        Program.implantSockets[cmd[1]].Send(ccmdline).Wait();
                                        stillconnected = false;
                                        Program.implantSockets[cmd[1]].SetConsoleOutput(stillconnected);
                                        Console.Write("# ");
                                        break;
                                    default:
                                        Program.implantSockets[cmd[1]].Send(ccmdline).Wait();
                                        break;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("{0} is not connected.", cmd[1]);
                        }
                        
                        break;
                    case "exit":
                        Console.WriteLine("Also use CTRL+C for stopping the implant services.");
                        return;
                    default:
                        Console.WriteLine("Unknown instruction: {0}", cmdline);
                        break;
                }
            }

        }
    }
}
