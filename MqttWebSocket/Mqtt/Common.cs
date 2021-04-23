using System.Text;
using Newtonsoft.Json;

namespace MqttWebSocket.Mqtt
{
    public static class Common
    {
        public static byte[] GetBytePayload(this object obj)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }

    }
}
