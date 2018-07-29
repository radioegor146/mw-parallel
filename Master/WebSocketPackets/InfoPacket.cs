using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Master.WebSocketPackets
{
    class InfoPacket : IWebSocketPacket
    {
        [JsonProperty(PropertyName = "event")]
        public EventType EventType = EventType.CPanelInfo;

        [JsonProperty(PropertyName = "permission_level")]
        public PermissionLevel Level = PermissionLevel.None;

        public InfoPacket() { }

        public void DeserializeJson(string json)
        {
               
        }

        public string SerializeJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

}

namespace Master
{
    enum PermissionLevel
    {
        WrongPassword,
        None,
        Workers,
        WorkersTasks,
        CreateTasks,
        Admin
    }
}
