using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MqttWebSocket.Configuration;
using MqttWebSocket.Mqtt;

namespace MqttWebSocket
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddCors();
            services.AddControllersWithViews();
            services.UseMqttSettings(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, MqttSettingsModel mqttSettings, CustomMqttFactory mqttFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            //app.UseCors(x => x
            //    .AllowAnyOrigin()
            //    .AllowAnyMethod()
            //    .AllowAnyHeader());

            app.UseAuthorization();


            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithConnectionValidator(c =>
                {
                    Console.WriteLine($"{c.ClientId} connection validator for c.Endpoint: {c.Endpoint}");
                    c.ReasonCode = MqttConnectReasonCode.Success;
                })
                .WithApplicationMessageInterceptor(context =>
                {
                    var oldData = context.ApplicationMessage.Payload;
                    string text = Encoding.UTF8.GetString(oldData);

                    Console.WriteLine(context.ApplicationMessage.Topic + ": " + text);
                    //context.ApplicationMessage.Payload = mergedData;
                    //context.ApplicationMessage.Topic = "text10";
                })
                .WithSubscriptionInterceptor(s =>
                {
                    if (s.TopicFilter.Topic.Equals("#")) s.TopicFilter.Topic = null;
                })
                .WithConnectionBacklog(mqttSettings.ConnectionBacklog)
                .WithDefaultEndpointPort(mqttSettings.TcpEndPoint.Port);

            mqttFactory.MqttServer.StartAsync(optionsBuilder.Build()).Wait();

            app.UseWebSocketEndpointApplicationBuilder(mqttSettings, mqttFactory);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

    }
}
