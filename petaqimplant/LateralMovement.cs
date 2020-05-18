using System;
using System.Linq;
using System.Collections.Generic;

namespace PetaqImplant
{
    public class LateralMovement
    {

        //internal string kerberos_ticket { get; set; } // kerberos ticket in Base64
        //internal string domain { get; private set; } // domain
        //internal string username { get; set; } // username
        //internal string domainuser { get; private set; } // username
        //internal string password { get; set; } // password
        //internal string password_hash { get; set; } // password hash
        //internal string host { get; set; } // host to connect to
        //internal string command { get; set; } // command to execute
        //internal string data { get; set; } // data in Base64 to send
        //internal string technique { get; set; } // data in Base64 to send
        internal Dictionary<string, string> coptions = new Dictionary<string, string>();

        public LateralMovement()
        {
        }

        public virtual bool Connect(Dictionary<string, string> coptions)
        {
            //kerberos_ticket = coptions["kerberos_ticket"];
            //domain = coptions["domain"];
            //username = coptions["username"];
            //password = coptions["password"];
            //password_hash = coptions["password_hash"];
            //host = coptions["host"];
            //command = coptions["command"];
            //data = coptions["data"];
            //technique = coptions["technique"];
            this.coptions = coptions;
            if (coptions.ContainsKey("username")) {
                if (coptions.ContainsKey("domain"))
                {
                    coptions.Add("domainuser", coptions["username"] + "@" + coptions["domain"]);
                }
                else
                {
                    coptions.Add("domainuser", coptions["username"]);
                }
            }
            
            return Execute();
        }
        public virtual bool Execute()
        {
            return false;
        }
    }
}
