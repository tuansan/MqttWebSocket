using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MqttWebSocket.Mqtt;
using System;
using System.Linq;
using System.Net;
using System.Text;

namespace MqttWebSocket.Configuration
{
    public static class ConfigureMqtt
    {
        public static void UseMqtt(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var mqttSettings = new MqttSettings();
            configuration.Bind("MQTT", mqttSettings);
            services.AddSingleton(mqttSettings);
            services.AddSingleton<IMqttService, MqttService>();
        }

        public static void UseMqttEndpoint(
            this IApplicationBuilder application,
            MqttSettings mqttSettings,
            IMqttService mqttService)
        {
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