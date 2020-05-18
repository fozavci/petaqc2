using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace PetaqImplant
{
    public class UDPClientSocket : LinkedImplantSocket
    {
        public UdpClient UDPService { get; set; }
        public UdpClient UDPClient { get; set; }
        public StreamString UDPClientStream { get; set; }
        public int Port { get; set; }


        public override void Connect(string sid, string sa, string option = null)
        {
            Console.Error.WriteLine("UDP service is linking.");

            ServerAddress = sa;
            sessionId = sid;

            // Starting the UDP connection as a thread
            clientThread = new Thread(() =>
            {
                try
                {
                    // Setting up the variables for UDP connection
                    if (option != null) { Port = int.Parse(option); }
                    else
                    {
                        Console.WriteLine("UDP port is required to connect.");
                    }

                    // Connect to the UDP socket.  
                    UDPClient = new UdpClient();
                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ServerAddress), Port);
                    UDPClient.Connect(ep);

                    Status = true;

                    // Register linked implants as registration started
                    Console.WriteLine("Session {0} is added to the linked sessions.", sessionId);
                    Program.RegisterLinkedImplants();

                    // Request registration first
                    if (! Registration)
                    {
                        AskRegistration();
                    }

                    // Ask route updates
                    AskRouteUpdates();

                    // Start reading data from the socket
                    while (true)
                    {
                        // Reading the data from the socket
                        byte[] data = UDPClient.Receive(ref ep);
                        string output = Encoding.UTF8.GetString(data);

                        // Process the data and send to the linked service
                        LinkedServiceSend(output);
                    }

                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("UDP Client Exception on Connect/Receive: " + e.Message);
                }
                finally
                {
                    // Dispose the UDP client connection
                    Disconnect();
                }
            });
            clientThread.Start();
        }

        public override void Disconnect()
        {
            if (Status)
            {
                UDPClient.Dispose();
                Status = false; // setting the status to false
                Program.RegisterLinkedImplants();

            }
            else
            {
                Console.WriteLine("The UDP link is not connected.");
            }
        }


        public override bool Send(string data = "")
        {
            if (data == "") return false;

            try
            {
                if (Status)
                {
                    // sending the data to the linked implant 
                    byte[] databytes = Encoding.UTF8.GetBytes(data);
                    UDPClient.Send(databytes, databytes.Length);
                }
                else
                {
                    Console.WriteLine("The UDP link is not connected.");
                }
                return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("UDP Client Exception on Send: " + e.Message);
                Status = false; // setting the status to false
                return false;
            }

        }


    }
}
