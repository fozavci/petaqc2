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

namespace PetaqImplant
{
    public class ImplantServiceSocket
    {
        public bool Status { get; set; }
        public bool LinkStatus { get; set; }
        public string ServerAddress { get; set; }

        public ImplantServiceSocket()
        {
        }
        public virtual void Start()
        {

        }
        public virtual void Stop()
        {

        }
        public virtual bool Send(string data = "")
        {
            return false;
        }
    }
}
