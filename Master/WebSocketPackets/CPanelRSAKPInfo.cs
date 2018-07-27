using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Master.WebSocketPackets
{
    class CPanelRSAKPInfo : IWebSocketPacket
    {
        [JsonProperty(PropertyName = "event")]
        public EventType EventType = EventType.RSAKp;

        [JsonProperty(PropertyName = "modulus")]
        public byte[] Modulus;

        [JsonProperty(PropertyName = "exponent")]
        public byte[] Exponent;

        public void DeserializeJson(string json)
        {

        }

        public string SerializeJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
