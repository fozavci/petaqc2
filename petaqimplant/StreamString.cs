using System;
using System.IO;
using System.Text;

// Send & Receive from the Microsoft examples
// https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication
// Defines the data protocol for reading and writing strings on our stream

namespace PetaqImplant
{
    public class StreamString
    {
        private Stream ioStream;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
        }

        public string ReadString()
        {
            string output = "";
            int len;
            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            var inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);
            if (inBuffer.Length != 0)
            {
                output = Encoding.UTF8.GetString(inBuffer);
            }
            return output;
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = Encoding.UTF8.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }
}
