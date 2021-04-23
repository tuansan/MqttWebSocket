using Microsoft.AspNetCore.Http;
using MQTTnet;
using MQTTnet.Client.Publishing;
using System.Threading.Tasks;

namespace MqttWebSocket.Mqtt
{
    public interface IMqttService
    {
        Task StartAsync();

        Task RunWebSocketConnectionAsync(HttpContext context);

        Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage);
    }
}