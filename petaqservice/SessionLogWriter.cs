using System;
using System.Text;
using System.IO;

namespace PetaqService
{
    public class SessionLogWriter : TextWriter
    {
        public SessionLogWriter()
        {
        }
        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }
        public override void Write(string value)
        {
            // encrypt the data before sending
            value = Common.Encrypt(value);



        }
        public override void WriteLine(string value)
        {
            // remove \n before sending
            // value = value.Remove(value.Length - 1);

            // use Write as it will encrypt and send to the service connected
            Write(value);
        }
    }
    
}
