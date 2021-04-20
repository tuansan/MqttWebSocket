using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MqttWebSocket.Mqtt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MqttWebSocket.Configuration
{
    public static class ConfigureWebSocketEndpoint
    {
        public static void UseMqttSettings(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var mqttSettings = new MqttSettingsModel();
            configuration.Bind("MQTT", mqttSettings);
            services.AddSingleton(mqttSettings);
            services.AddSingleton<IMqttService, MqttService>();
        }

        public static void UseWebSocketEndpointApplicationBuilder(
            this IApplicationBuilder application,
            MqttSettingsModel mqttSettings,
            IMqttService mqttService)
        {
            mqttService.StartAsync();

            if (mqttSettings.WebSocketEndPoint?.Enabled != true || string.IsNullOrEmpty(mqttSettings.WebSocketEndPoint.Path))
                return;

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(mqttSettings.WebSocketEndPoint.KeepAliveInterval)
            };

            if (mqttSettings.WebSocketEndPoint.AllowedOrigins?.Any() == true)
            {
                foreach (var item in mqttSettings.WebSocketEndPoint.AllowedOrigins)
                {
                    webSocketOptions.AllowedOrigins.Add(item);
                }
            }

            application.UseWebSockets(webSocketOptions);

            application.Use(async (context, next) =>
            {
                if (context.Request.Path == mqttSettings.WebSocketEndPoint.Path)
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        await mqttService.RunWebSocketConnectionAsync(context);
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                else
                {
                    await next().ConfigureAwait(false);
                }
            });
        }
    }
}