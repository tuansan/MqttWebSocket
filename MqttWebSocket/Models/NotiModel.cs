using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MqttWebSocket.Models
{
    public class NotiModel
    {
        public int connected { get; set; }
        public int disconnected { get; set; }
        public int warning { get; set; }
        public int over { get; set; }
    }
}
