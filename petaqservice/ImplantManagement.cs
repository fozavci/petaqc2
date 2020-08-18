using System;


namespace PetaqService
{
    public class ImplantManagement
    {


        // Create an implant object and return it
        public static ImplantServiceSocket CreateImplant(string socketId, dynamic socket)
        {
            // generating a random identifier for the socket
            if (socketId == null) { socketId = Common.RandomStringGenerator(20); }
            
            // creating the client object for the socket
            ImplantServiceSocket implant = new ImplantServiceSocket(socketId, socket);

            // setting the log file for the client socket session
            implant.SetLogFile(new LogFile(socketId));

            // adding the implant socket session to the sockets
            Program.implantSockets.TryAdd(socketId, implant);

            return implant;

        }
    }
}
