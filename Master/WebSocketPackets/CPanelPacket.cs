using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Master.WebSocketPackets
{
    class CPanelPacket
    {
        public CPanelPacketType PacketType;

        public string JsonData;

        public void DeserializeJson(string json)
        {
            JsonData = json;
            PacketType = (CPanelPacketType)JObject.Parse(json)["type"].Value<int>();
        }
    }

    enum CPanelPacketType
    {
        Nop,
        AuthRequest,
        NewTask
    }
}
