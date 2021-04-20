using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MQTTnet;
using MQTTnet.Client.Publishing;

namespace MqttWebSocket.Mqtt
{
    public interface IMqttService
    {
        Task StartAsync();
        Task RunWebSocketConnectionAsync(HttpContext context);
        Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage);
    }
}
