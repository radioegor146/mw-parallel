using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Master.WebSocketPackets
{
    class CPanelTaskInputPacket
    {
        public byte[] Data;

        public void DeserializeJson(string json)
        {
            Data = Convert.FromBase64String(JObject.Parse(json)["data"].Value<string>());
        }
    }
}
