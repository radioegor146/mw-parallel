using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Master.WebSocketPackets
{
    class Packet
    {
        public ControlPacketType PacketType;

        public string JsonData;

        public void DeserializeJson(string json)
        {
            JsonData = json;
            PacketType = (ControlPacketType)JObject.Parse(json)["type"].Value<int>();
        }
    }

    enum ControlPacketType
    {
        Nop,
        AuthRequest,
        NewTask,
        AbortWorker,
        RemoveTask
    }
}
