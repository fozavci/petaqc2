using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
Run Scenarios:
    scenario IMPLANTID scenariofilepath
    scenarios
    scenarios SCENARIOID
    scenarios SCENARIOID output
    scenarios SCENARIOID export reportfilepath
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
                    case "scenarios":
                        if (Program.scenarios.Count == 0)
                        {
                            Console.WriteLine("No scenario processed yet.");
                            break;
                        }

                        if (cmd.Length > 1)
                        {
                            // raise a warning for misuse
                            if (!Program.scenarios.ContainsKey(cmd[1]))
                            {
                                Console.WriteLine("Usage: Invalid scenario selected.");
                                break;
                            }

                            //  the scenario ID
                            string scenarioID = cmd[1];
                                
                            if (cmd.Length > 2)
                            {
                                switch (cmd[2])
                                {
                                    case "output":
                                        Console.WriteLine("\nScenario report for {0}\n", scenarioID);
                                        foreach (var sc in Program.scenarios[scenarioID].report.scenario)
                                        {

                                            Console.WriteLine("* Mitre ID: {0}\tName: {1}", sc.mitreid, sc.name);
                                            Console.WriteLine("  Start: {0}\tStop: {1}", sc.starttime, sc.stoptime);
                                            Console.WriteLine("  Result:\t{0}\n", sc.result);
                                            string output = sc.output;
                                            Console.WriteLine("  Output:\n   {0}\n", output.Replace("\n", "\n   ")); 
                                        }
                                        break;
                                    case "export":
                                        if (cmd.Length < 4)
                                        {
                                            Console.WriteLine("Usage: scenarios scenarioID export FILEPATH");
                                            break;
                                        }

                                        // Creating the files folder if doesn't exist
                                        string filename = cmd[3];
                                        string filenameonly = Regex.Split(filename, "/").Last();
                                        string filefullpath = Path.Combine(filename);
                                        try
                                        {
                                            Console.WriteLine("\nScenario {0} is exporting to {1}.\n", scenarioID, filefullpath);

                                            // opening file as a stream to write the data
                                            var fileStream = new FileStream(filefullpath, FileMode.OpenOrCreate);
                                            StreamWriter outputStream = new StreamWriter(fileStream);

                                            // writing the content to the local file
                                            foreach (var sc in Program.scenarios[scenarioID].report.scenario)
                                            {

                                                outputStream.WriteLine("* Mitre ID: {0}\tName: {1}", sc.mitreid, sc.name);
                                                outputStream.WriteLine("  Start: {0}\tStop: {1}", sc.starttime, sc.stoptime);
                                                outputStream.WriteLine("  Result:\t{0}\n", sc.result);
                                                string output = sc.output;
                                                outputStream.WriteLine("  Output:\n   {0}\n", output.Replace("\n", "\n   "));

                                            }

                                            // close the file and output streams
                                            outputStream.Close();
                                            fileStream.Close();
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine("Files folder or file couldn't be created: {0}.", e);
                                        }
                                        break;
                                    default:
                                        Console.WriteLine("Unknown scenarios action, try output or export.");
                                        break;
                                }
                            }
                            else
                            {
                                Console.WriteLine("\nScenario report for {0}\n", scenarioID);
                                foreach (var sc in Program.scenarios[scenarioID].report.scenario)
                                {

                                    Console.WriteLine("* Mitre ID: {0}\tName: {1}", sc.mitreid, sc.name);
                                    Console.WriteLine("  Start: {0}\tStop: {1}", sc.starttime, sc.stoptime);
                                    Console.WriteLine("  Result:\t{0}\n", sc.result);
                                }
                            }
                                
                        }

                        else
                        {

                            Console.WriteLine("Scenario ID\t\tImplant ID\t\tThreat Actor\t\tStatus\t\tDate");
                            foreach (var i in Program.scenarios)
                            {
                                string scenarioID = i.Key;
                                dynamic scenario = i.Value;
                                Console.WriteLine(scenarioID + "\t" + scenario.implantID + "\t"
                                    + scenario.threat_actor + "\t\t" + scenario.status + "\t\t" + scenario.start);
                            }
                        }
                            
                        break;
                    case "scenario":
                        // check whether any implants
                        if (Program.implantSockets.Count == 0)
                        {
                            Console.WriteLine("No implants registered yet.");
                        }
                        else
                        {
                            // no scenario file or ImplantID is given
                            if (cmd.Length < 3)
                            {
                                Console.WriteLine("No scenario file or implantID given. Use scenario ImplantID scenariofile.");
                                break;
                            }

                            // check whether the implant is in the list
                            if (cmd.Length < 2 || cmd[1] == "" || (!Program.implantSockets.ContainsKey(cmd[1])))
                            {
                                Console.WriteLine("No valid implant selected.");
                                break;
                            }

                            // check whether the implant is connected
                            if (! Program.implantSockets[cmd[1]].implantStatus)
                            {
                                Console.WriteLine("Implant is not connected.");
                                break;
                            }

                            string implantID= cmd[1];
                            string scenarioFile= cmd[2];

                            // check whether the implant is connected
                            if (!File.Exists(scenarioFile))
                            {
                                Console.WriteLine("The scenario file is inaccesible.");
                                break;
                            }

                            // generating a random identifier for the scenario
                            string scenarioID = Common.RandomStringGenerator(20);

                            Console.WriteLine("Selected implant is {0}.", implantID);
                            Console.WriteLine("Scenario file is {0}", scenarioFile);


                            try
                            {
                                Console.WriteLine("Constructing the scenario...");
                                // Getting the scenario file content
                                string scenario = File.ReadAllText(scenarioFile);

                                // Fix the space issue before parsing
                                scenario = scenario.Replace("\\ ", " ");
                                dynamic scenarioObj = JsonConvert.DeserializeObject(scenario);

                                // add the scenario to the scenarios list
                                Program.scenarios.TryAdd(scenarioID, scenarioObj);

                                Program.scenarios[scenarioID].scenarioID = scenarioID;
                                Program.scenarios[scenarioID].status = "inprogress";
                                Program.scenarios[scenarioID].implantID = implantID;
                                Program.scenarios[scenarioID].scenario = new JArray();

                                Console.WriteLine("Threat Actor: {0}", scenarioObj.threat_actor);
                                Console.WriteLine("Scenario ID: {0}", scenarioObj.scenarioID);


                                // loading the TTPs
                                foreach (string ttpId in scenarioObj.ttps)
                                {
                                    Console.WriteLine("TTP ID {0} is processing...", ttpId);
                                    try
                                    {
                                        // combining the TTP content path                                    
                                        string ttpcontentpath = Path.Combine("Scenarios", "TTPs", ttpId + ".json");

                                        // Getting the scenario file content
                                        string ttpcontent = File.ReadAllText(ttpcontentpath);

                                        // parse the TTP content
                                        dynamic ttpcontentObj = JsonConvert.DeserializeObject(ttpcontent);

                                        // take the backup of the original instruction in case of file content replacement
                                        ttpcontentObj.original_instructions = ttpcontentObj.instructions;

                                        for (int i = 0; i < ttpcontentObj.instructions.Count; i++)
                                        {
                                            string instruction = ttpcontentObj.instructions[i].ToString();
                                            if (instruction.Contains(" file "))
                                            {
                                                // The TTP requires a file to include
                                                string ttpfiletoencode = instruction.ToString().Split(" file ")[1].Split(" ")[0];
                                                Console.WriteLine("TTP file to include: {0}", ttpfiletoencode);

                                                // Getting the file content encoded
                                                byte[] ttpfilecontent = File.ReadAllBytes(ttpfiletoencode);
                                                string ttpfilecontentencoded = Convert.ToBase64String(ttpfilecontent);

                                                // Replacing the filename with the content
                                                ttpcontentObj.instructions[i] = ((string)instruction).Replace(ttpfiletoencode, ttpfilecontentencoded);
                                            }
                                        }

                                        // Adding the ttpcontentObj to the scenario
                                        Program.scenarios[scenarioID].scenario.Add(ttpcontentObj);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("TTP couldn't be added:\n{0}", ex);
                                    }                                    

                                }
                                
                                // scenario to push to the implant
                                string scenario_topush = JsonConvert.SerializeObject(Program.scenarios[scenarioID]);

                                // encode and encrypt the scenario content
                                string scenariocontentencoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(scenario_topush));
                                string scenariocontentencrypted = Common.Encrypt(scenariocontentencoded);

                                //Telling the implant the instruction is delivering in 2 parts
                                // Stating it's MULTIPARTTRANSMISSION, then giving the file content size and parameters to implant
                                string instsend = "MULTIPARTTRANSMISSION " + scenariocontentencrypted.Length + " " + cmdline.Replace(scenarioFile, "FILECONTENTPLACEHOLDER");
                                Program.implantSockets[implantID].Send(instsend).Wait();
                                // Pushing the file content encoded to implant
                                // Don't get confused, the encrypted content size is sent because its the actual data size
                                // Because Socket.Send will encrypt it before sending
                                Program.implantSockets[implantID].Send(scenariocontentencoded).Wait();
                                Console.WriteLine("Scenario is running on {0}.", implantID);

                                // update the scenario status and timestamp
                                Program.scenarios[scenarioID].status = "running";
                                scenarioObj.start = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                            }
                            catch (Exception ex)
                            {
                                // update the scenario status and timestamp
                                Console.WriteLine("Scenario construction error: {0}", ex);
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
                                    case "upload":
                                        if (ccmd.Length < 3)
                                        {
                                            Console.WriteLine("Please use 'upload LOCALFILE REMOTEFILE'");
                                            break;
                                        }
                                        // Regex the quote in the file names
                                        var filenames = Regex.Matches(ccmdline, @"[\""].+?[\""]|[^ ]+")
                                        .Cast<Match>()
                                        .Select(x => x.Value.Trim('"'))
                                        .ToList();
                                        string localfilename = filenames[1];
                                        string remotefilename = filenames[2];
                                        try
                                        {
                                            // Getting the file content encrypted
                                            byte[] filecontent = File.ReadAllBytes(localfilename);
                                            string filecontentencoded = Convert.ToBase64String(filecontent);
                                            string filecontentencrypted = Common.Encrypt(filecontentencoded);
                                            // Telling the implant the instruction is delivering in 2 parts
                                            // Stating it's MULTIPARTTRANSMISSION, then giving the file content size and remote file name to implant
                                            string filesend = "MULTIPARTTRANSMISSION " + filecontentencrypted.Length + " " + ccmdline.Replace(localfilename, "FILECONTENTPLACEHOLDER");
                                            Program.implantSockets[cmd[1]].Send(filesend).Wait();
                                            // Pushing the file content encoded to implant
                                            // Don't get confused, the encrypted content size is sent because its the actual data size
                                            // Because Socket.Send will encrypt it before sending
                                            Program.implantSockets[cmd[1]].Send(filecontentencoded).Wait();
                                            Console.WriteLine("File upload successfully finished.");
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("Upload error: {0}", ex.Message);
                                            Console.WriteLine("Please use 'upload LOCALFILE REMOTEFILE'");
                                        }
                                        break;
                                    case "exit":
                                        // sending the command to the implant socket
                                        Program.implantSockets[cmd[1]].Send(ccmdline).Wait();
                                        stillconnected = false;
                                        Program.implantSockets[cmd[1]].SetConsoleOutput(stillconnected);
                                        Console.Write("# ");
                                        break;                                    
                                    case "":
                                        break;
                                    default:
                                        if (ccmd.Length > 1 && ccmd[1] == "file")
                                        {
                                            
                                            // Regex the quote in the file names
                                            var commandList = Regex.Matches(ccmdline, @"[\""].+?[\""]|[^ ]+")
                                            .Cast<Match>()
                                            .Select(x => x.Value.Trim('"'))
                                            .ToList();
                                            // File name is the third
                                            string filename = commandList[2];

                                            Console.WriteLine(filename);
                                            try
                                            {
                                                Console.WriteLine("File is sending to the implant.");
                                                // Getting the file content encrypted
                                                byte[] filecontent = File.ReadAllBytes(filename);
                                                string filecontentencoded = Convert.ToBase64String(filecontent);
                                                string filecontentencrypted = Common.Encrypt(filecontentencoded);
                                                // Telling the implant the instruction is delivering in 2 parts
                                                // Stating it's MULTIPARTTRANSMISSION, then giving the file content size and parameters to implant
                                                string instsend = "MULTIPARTTRANSMISSION " + filecontentencrypted.Length + " " + ccmdline.Replace(filename,"FILECONTENTPLACEHOLDER");
                                                Program.implantSockets[cmd[1]].Send(instsend).Wait();
                                                // Pushing the file content encoded to implant
                                                // Don't get confused, the encrypted content size is sent because its the actual data size
                                                // Because Socket.Send will encrypt it before sending
                                                Program.implantSockets[cmd[1]].Send(filecontentencoded).Wait();
                                                Console.WriteLine("File upload successfully finished.");
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine("Upload error: {0}", ex.Message);
                                            }
                                        }
                                        else
                                        {
                                            Program.implantSockets[cmd[1]].Send(ccmdline).Wait();
                                        }
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
