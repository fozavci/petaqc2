using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;

using Microsoft.Extensions.Hosting;


namespace PetaqService
{
    public class Startup
    {
        private const int receiveChunkSize = 300000;



        public void ConfigureServices(IServiceCollection services)
        {
           // Used for debugging Websockets
           services.AddLogging(builder =>
           {
               builder.ClearProviders();
               //builder.AddConsole()
                   //.AddDebug()
                   //.AddFilter<ConsoleLoggerProvider>(category: null, level: LogLevel.Debug)
                   //.AddFilter<DebugLoggerProvider>(category: null, level: LogLevel.Debug);
           });

        }


        public void Configure(IApplicationBuilder app,  Microsoft.AspNetCore.Hosting.IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            
            // Use this for the static files to serve
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                        Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot/tools")),
                RequestPath = new PathString("/tools"),
                ServeUnknownFileTypes = true
            });



            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var webSocketOptions = new WebSocketOptions() 
            {
                // Keepalive may be important for some detections
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = receiveChunkSize
            };

            app.UseWebSockets(webSocketOptions);



            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        // tasking
                        var completion = new TaskCompletionSource<object>();
                        // getting the socket
                        var socket = await context.WebSockets.AcceptWebSocketAsync();
                        // handling the implant, adding direct route while creating the object
                        ImplantServiceSocket implant = ImplantManagement.CreateImplant(null, socket);
                        // set the IP
                        implant.implantIP = context.Connection.RemoteIpAddress.ToString();
                        // set the socket link URI
                        string headuri = "ws://";
                        if ( context.Request.IsHttps ) { headuri = "wss://"; }
                        implant.link_uri = headuri + context.Connection.LocalIpAddress+ ":" + context.Connection.LocalPort + context.Request.Path;
                        Program.petaconsole.ResetMenu();
                        // start receiving and sending async
                        await Task.WhenAll(implant.Receive(), implant.Send());
                        await completion.Task;

                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });
            app.UseFileServer();

        }
        public static Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    }

}
