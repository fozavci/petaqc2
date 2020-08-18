using System;
using System.Text;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Security.Cryptography;

namespace PetaqImplant
{
    public class TTPs
    {

        public List<TTP> scenario { get; set; }
        public string scenarioID { get; set; }
        public string threat_actor { get; set; }
        public string[] ttps { get; set; }
    }

    public class TTP
    {

        //public string id { get; set; }
        public string mitreid { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string[] instructions { get; set; }
        public string[] original_instructions { get; set; }
        public string starttime { get; set; }
        public string stoptime { get; set; }
        public bool result { get; set; }
        public string output { get; set; }
    }

    public class Scenario
    {

        public static void Run(string scenario_b64)
        {
            TTPs scenario_torun;

            // Set the socket as the Console output
            Program.consoleIO = Console.Out;
            Console.SetOut(new SocketWriter());

            // parse the scenario in JSON format or return error message
            try
            {
                string scenario_text = Encoding.UTF8.GetString(Convert.FromBase64String(scenario_b64));
                scenario_torun = new JavaScriptSerializer().Deserialize<TTPs>(scenario_text);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Scenario parsing error:\n{0}", ex);
                return;
            }

            Console.WriteLine("Adversary emulation is starting...");

            foreach (var item in scenario_torun.scenario)
            {
                // set an output StringWriter for each TTP 
                StringWriter ttp_output = new StringWriter();

                ttp_output.WriteLine("TTP: {0}\nName: {1}\n", item.mitreid, item.name);

                try
                {                   
                    // set the start timestamp
                    item.starttime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    // run the instructions received
                    for (int i = 0; i < item.instructions.Length; i++)
                    {
                        // print the original instruction
                        ttp_output.WriteLine("Instruction: {0}", item.original_instructions[i]);
                        ttp_output.WriteLine("Start Timestamp: {0}\n", item.starttime);

                        // but exercute the updated instruction
                        PetaqImplant.Instructions.Instruct(item.instructions[i], ttp_output);

                        // recover the instruction to avoid high bandwith for file contents
                        item.instructions[i] = item.original_instructions[i];
                    }

                    // set the result
                    item.result = true;
                }
                catch (Exception ex)
                {
                    // save the error message to the TTP output
                    Console.WriteLine("Instruction failure:\n{0}", ex.ToString());

                    // set the result
                    item.result = false;
                }
                finally
                {
                    // set the stop timestamp
                    item.stoptime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    ttp_output.WriteLine("Stop Timestamp: {0}\n", item.stoptime);

                    // save the output to the TTP output
                    ttp_output.Close();
                    item.output = ttp_output.ToString();

                    // Set the socket as the Console output
                    Program.consoleIO = Console.Out;
                    Console.SetOut(new SocketWriter());

                    // Report the TTP result to the C2
                    Console.WriteLine(item.output);
                }


            }

            Console.WriteLine("Serialising the scenario report");
            string scenario_report = new JavaScriptSerializer().Serialize(scenario_torun);

            Console.WriteLine("Sending the report to the C2");
            string scenario_report_b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(scenario_report));
            Console.WriteLine("scenario_report "+scenario_report_b64);

        }
    }
}
