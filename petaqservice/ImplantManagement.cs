using System;
using System.Linq;


namespace PetaqService
{
    public class ImplantManagement
    {
        // Generate random strings as websocket identifiers
        private static Random random = new Random();

        // Random string generator for the implant socket IDs
        public static string RandomStringGenerator(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Create an implant object and return it
        public static ImplantServiceSocket CreateImplant(string socketId, dynamic socket)
        {
            // generating a random identifier for the socket
            if (socketId == null) { socketId = RandomStringGenerator(20); }
            
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
