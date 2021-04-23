using Microsoft.AspNetCore.Http;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Client.Publishing;
using MQTTnet.Implementations;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MqttWebSocket.Configuration;
using MqttWebSocket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqttWebSocket.Mqtt
{
    public class MqttService : IMqttService
    {
        private readonly MqttSettings _mqttSettings;
        private IMqttServer MqttServer { get; }
        private MqttWebSocketServerAdapter Adapter { get; }
        private IList<MemberModel> List { get; }
        private int Count;

        public MqttService(MqttSettings mqttSettings)
        {
            _mqttSettings = mqttSettings;

            var mqttFactory = new MqttFactory();

            Adapter = new MqttWebSocketServerAdapter(mqttFactory.DefaultLogger);

            var adapters = new List<IMqttServerAdapter> { Adapter };

            if (mqttSettings.TcpEndPoint?.Enabled == true)
                adapters.Add(new MqttTcpServerAdapter(mqttFactory.DefaultLogger)
                {
                    TreatSocketOpeningErrorAsWarning = true // Opening other ports than for HTTP is not allows in Azure App Services.
                });

            MqttServer = mqttFactory.CreateMqttServer(adapters);

            List = new List<MemberModel>();
        }

        public Task StartAsync()
        {
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithConnectionValidator(ConnectionValidator)
                .WithApplicationMessageInterceptor(ApplicationMessageInterceptor)
                .WithSubscriptionInterceptor(SubscriptionInterceptor)
                .WithConnectionBacklog(_mqttSettings.ConnectionBacklog)
                .WithDefaultEndpointPort(_mqttSettings.TcpEndPoint.Port).Build();

            MqttServer.ClientDisconnectedHandler = new MqttServerClientDisconnectedHandlerDelegate(ClientDisconnectedHandler);

            MqttServer.StartAsync(optionsBuilder);

            return Task.CompletedTask;
        }

        private void ConnectionValidator(MqttConnectionValidatorContext c)
        {
            if (c.Username != "admin" || c.Password != "123456")
                c.ReasonCode = MqttConnectReasonCode.NotAuthorized;
            else
            {
                Console.WriteLine($"{c.ClientId} connection validator for c.Endpoint: {c.Endpoint}");
                Console.WriteLine($"{++Count} online");

                Guid g = Guid.NewGuid();

                List.Add(new MemberModel
                {
                    Id = c.ClientId,
                    Topic = g.ToString(),
                    Guid = g
                });
            }
        }

        private void ClientDisconnectedHandler(MqttServerClientDisconnectedEventArgs d)
        {
            List.Remove(List.FirstOrDefault(s => s.Id == d.ClientId));
            Console.WriteLine($"{--Count} online");
        }

        private void ApplicationMessageInterceptor(MqttApplicationMessageInterceptorContext context)
        {
            var oldData = context.ApplicationMessage.Payload;
            string text = Encoding.UTF8.GetString(oldData);

            Console.WriteLine($"{context.ApplicationMessage.Topic}: {text}");
            //context.ApplicationMessage.Topic = "text10";
        }

        private void SubscriptionInterceptor(MqttSubscriptionInterceptorContext s)
        {
            if (s.TopicFilter.Topic.Equals("#"))
                s.AcceptSubscription = false;

            if (Guid.TryParse(s.TopicFilter.Topic, out Guid g))
                if (g != List.FirstOrDefault(q => q.Id == s.ClientId)?.Guid)
                    s.AcceptSubscription = false;
        }

        public async Task RunWebSocketConnectionAsync(HttpContext context)
        {
            string subProtocol = null;
            if (context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var requestedSubProtocolValues))
            {
                subProtocol = MqttSubProtocolSelector.SelectSubProtocol(requestedSubProtocolValues);
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol).ConfigureAwait(false);

            await Adapter.RunWebSocketConnectionAsync(webSocket, context).ConfigureAwait(false);
        }

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            return MqttServer.PublishAsync(applicationMessage);
        }
    }
}