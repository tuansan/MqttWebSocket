using System;

namespace MqttWebSocket.Models
{
    public class MemberModel
    {
        public string Id { get; set; }
        public string Topic { get; set; }
        public Guid Guid { get; set; }
    }
}
