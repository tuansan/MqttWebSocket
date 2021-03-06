using Microsoft.AspNetCore.Http;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Client.Publishing;
using MQTTnet.Implementations;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MqttWebSocket.Configuration;
using MqttWebSocket.Models;

namespace MqttWebSocket.Mqtt
{
    public class MqttService : IMqttService
    {
        #region Fields

        private IMqttServer MqttServer { get; }
        private MqttWebSocketServerAdapter Adapter { get; }
        private int Count;
        private readonly float[] GiaTri;
        private readonly Random rand = new();

        private const string _topicNoti = "Notifications";

        #endregion Fields

        #region Ctor

        public MqttService(MqttSettings mqttSettings)
        {
            var mqttFactory = new MqttFactory();

            Adapter = new MqttWebSocketServerAdapter(mqttFactory.DefaultLogger);

            var adapters = new List<IMqttServerAdapter> { Adapter };

            if (mqttSettings.TcpEndPoint?.Enabled == true)
                adapters.Add(new MqttTcpServerAdapter(mqttFactory.DefaultLogger)
                {
                    TreatSocketOpeningErrorAsWarning = true // Opening other ports than for HTTP is not allows in Azure App Services.
                });

            MqttServer = mqttFactory.CreateMqttServer(adapters);

            MqttServer.ClientDisconnectedHandler = new MqttServerClientDisconnectedHandlerDelegate(ClientDisconnectedHandler);

            MqttServer.StartAsync(new MqttServerOptionsBuilder()
                .WithConnectionValidator(ConnectionValidator)
                .WithApplicationMessageInterceptor(ApplicationMessageInterceptor)
                .WithSubscriptionInterceptor(SubscriptionInterceptor)
                .WithConnectionBacklog(mqttSettings.ConnectionBacklog)
                .WithDefaultEndpointPort(mqttSettings.TcpEndPoint.Port)
                .WithMaxPendingMessagesPerClient(mqttSettings.MaxPendingMessagesPerClient)
                .Build());

            _ = AutoPublishAsync();

            GiaTri = new float[8];
        }

        #endregion Ctor

        #region Utilities

        #region Common

        private async Task AutoPublishAsync()
        {
            while (true)
            {
                for (int i = 0; i < 8; i++)
                {
                    float num = rand.Next(0, 900);
                    if (true)//num % 2 == 0 || num is > 700 or < 200)
                    {
                        num -= GiaTri[i] < 500 ? 300 : GiaTri[i] > 700 ? 600 : 450;

                        GiaTri[i] += num / rand.Next(10, 30);

                        if (GiaTri[i] < 0) GiaTri[i] = 0;
                        if (GiaTri[i] > 1000) GiaTri[i] = 1000;

                        await PublishAsync(new MqttApplicationMessage()
                        {
                            Topic = "test",
                            Payload = new
                            {
                                Ten = $"test{i + 1}",
                                ThoiGianDocGiuLieu = DateTime.UtcNow,
                                GiaTri = GiaTri[i],
                                Quality = GetQuality(rand.Next(1, 11))
                            }.GetBytePayload()
                        });
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
            }
        }

        private async Task PublishNoti(NotiModel model)
        {
            await PublishAsync(new MqttApplicationMessage()
            {
                Topic = _topicNoti,
                Payload = model.GetBytePayload(),
                Retain = true,
                QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce
            });
        }

        private static string GetQuality(int num)
        {
            return num switch
            {
                1 => "Low",
                2 => "Low",
                3 => "Medium",
                4 => "Medium",
                5 => "Medium",
                6 => "Medium",
                _ => "Good"
            };
        }

        private MqttApplicationMessage ChangTopicMessage(MqttApplicationMessage message, string newTopic)
        {
            if (string.IsNullOrEmpty(newTopic))
                return message;
            return new MqttApplicationMessage()
            {
                Topic = newTopic,
                Payload = message.Payload,
                Retain = true,
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };
        }

        #endregion Common

        #region Conn

        private void ConnectionValidator(MqttConnectionValidatorContext c)
        {
            if (c.Username != "admin" || c.Password != "123456")
                c.ReasonCode = MqttConnectReasonCode.NotAuthorized;
            else
            {
                Console.WriteLine($"{c.ClientId} connection validator for c.Endpoint: {c.Endpoint}");
                Console.WriteLine($"{++Count} online");
            }
        }

        private void ClientDisconnectedHandler(MqttServerClientDisconnectedEventArgs d)
        {
            Console.WriteLine($"{--Count} online");
        }

        private void ApplicationMessageInterceptor(MqttApplicationMessageInterceptorContext context)
        {
            switch (context.ApplicationMessage.Topic)
            {
                case "test":
                    {
                        var dyn = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(context.ApplicationMessage.Payload));
                        // ReSharper disable once PossibleNullReferenceException
                        string topic = dyn["Ten"]?.Value;
                        if (!string.IsNullOrEmpty(topic))
                            PublishAsync(ChangTopicMessage(context.ApplicationMessage, topic));
                        break;
                    }
                case "testNoti":
                    _ = PublishNoti(new NotiModel()
                    {
                        connected = rand.Next(0, 10),
                        disconnected = 0
                    });
                    context.AcceptPublish = false;
                    break;
            }
        }

        private void SubscriptionInterceptor(MqttSubscriptionInterceptorContext s)
        {
            if (s.TopicFilter.Topic.Equals("#"))
                s.AcceptSubscription = false;
        }

        #endregion Conn

        #endregion Utilities

        #region Methods

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

        #endregion Methods
    }
}