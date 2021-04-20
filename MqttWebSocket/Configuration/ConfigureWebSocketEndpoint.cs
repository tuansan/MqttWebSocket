using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MqttWebSocket.Mqtt;

namespace MqttWebSocket.Configuration
{
    public static class ConfigureWebSocketEndpoint
    {
        public static void UseWebSocketEndpointApplicationBuilder(
            this IApplicationBuilder application,
            MqttSettingsModel mqttSettings,
            CustomMqttFactory mqttFactory)
        {
            if (mqttSettings?.WebSocketEndPoint?.Enabled != true)
            {
                return;
            }
            if (string.IsNullOrEmpty(mqttSettings.WebSocketEndPoint.Path))
            {
                return;
            }

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

            //start server
            var adapter = new MqttWebSocketServerAdapter(mqttFactory.Logger);

            mqttFactory.CreateMqttServer(new List<IMqttServerAdapter> { adapter }).StartAsync(optionsBuilder.Build()).GetAwaiter().GetResult();

            application.Use(async (context, next) =>
            {
                if (context.Request.Path == mqttSettings.WebSocketEndPoint.Path)
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        string subProtocol = null;
                        if (context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var requestedSubProtocolValues))
                        {
                            subProtocol = MqttSubProtocolSelector.SelectSubProtocol(requestedSubProtocolValues);
                        }

                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol).ConfigureAwait(false);
                        await adapter.RunWebSocketConnectionAsync(webSocket, context).ConfigureAwait(false);
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
