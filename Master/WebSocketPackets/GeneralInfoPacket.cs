using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Master.WebSocketPackets
{
    [Serializable]
    class GeneralInfoPacket : IWebSocketPacket
    {
        [JsonProperty(PropertyName = "event")]
        public EventType EventType = EventType.GeneralInfo;

        [JsonProperty(PropertyName = "worker_count")]
        public int WorkerCount;

        [JsonProperty(PropertyName = "tasks_count")]
        public int TasksCount;

        public void DeserializeJson(string json)
        {

        }

        public string SerializeJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
