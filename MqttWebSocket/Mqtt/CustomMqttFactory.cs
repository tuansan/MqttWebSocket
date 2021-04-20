using System.Collections.Generic;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Server;

namespace MqttWebSocket.Mqtt
{
    public class CustomMqttFactory
    {
        public CustomMqttFactory()
        {
            var mqttFactory = new MqttFactory();

            Adapter = new MqttWebSocketServerAdapter(mqttFactory.DefaultLogger);

            var adapters = new List<IMqttServerAdapter>
            {
                new MqttTcpServerAdapter(mqttFactory.DefaultLogger)
                {
                    TreatSocketOpeningErrorAsWarning = true // Opening other ports than for HTTP is not allows in Azure App Services.
                },
                Adapter
            };

            MqttServer = mqttFactory.CreateMqttServer(adapters);
        }

        public MqttWebSocketServerAdapter Adapter { get; }
        public IMqttServer MqttServer { get; }
    }
}