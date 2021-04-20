using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Client.Publishing;
using MQTTnet.Implementations;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MqttWebSocket.Configuration;

namespace MqttWebSocket.Mqtt
{
    public class MqttService : IMqttService
    {
        private readonly MqttSettingsModel _mqttSettings;
        private IMqttServer _mqttServer { get; }
        private MqttWebSocketServerAdapter _adapter { get; }

        public MqttService(MqttSettingsModel mqttSettings)
        {
            _mqttSettings = mqttSettings;

            var mqttFactory = new MqttFactory();

            _adapter = new MqttWebSocketServerAdapter(mqttFactory.DefaultLogger);

            var adapters = new List<IMqttServerAdapter> { _adapter };

            if (mqttSettings.TcpEndPoint?.Enabled == true)
                adapters.Add(new MqttTcpServerAdapter(mqttFactory.DefaultLogger)
                {
                    TreatSocketOpeningErrorAsWarning = true // Opening other ports than for HTTP is not allows in Azure App Services.
                });

            _mqttServer = mqttFactory.CreateMqttServer(adapters);
        }

        public Task StartAsync()
        {
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

                    Console.WriteLine($"{context.ApplicationMessage.Topic}: {text}");
                    //context.ApplicationMessage.Payload = mergedData;
                    //context.ApplicationMessage.Topic = "text10";
                })
                .WithSubscriptionInterceptor(s =>
                {
                    if (s.TopicFilter.Topic.Equals("#")) s.CloseConnection = true;
                })
                .WithConnectionBacklog(_mqttSettings.ConnectionBacklog)
                .WithDefaultEndpointPort(_mqttSettings.TcpEndPoint.Port).Build();

            _mqttServer.StartAsync(optionsBuilder);

            return Task.CompletedTask;
        }

        public async Task RunWebSocketConnectionAsync(HttpContext context)
        {
            string subProtocol = null;
            if (context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var requestedSubProtocolValues))
            {
                subProtocol = MqttSubProtocolSelector.SelectSubProtocol(requestedSubProtocolValues);
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol).ConfigureAwait(false);

            await _adapter.RunWebSocketConnectionAsync(webSocket, context).ConfigureAwait(false);
        }

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            return _mqttServer.PublishAsync(applicationMessage);
        }
    }
}