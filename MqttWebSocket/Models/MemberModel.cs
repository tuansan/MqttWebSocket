using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace MqttWebSocket.Models
{
    public class MemberModel
    {
        public string Id { get; set; }
        public string Topic { get; set; }
        public string Guid { get; set; }
    }
}
