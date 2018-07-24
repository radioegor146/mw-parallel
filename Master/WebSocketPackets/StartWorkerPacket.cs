using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Master.WebSocketPackets
{
    [Serializable]
    class StartWorkerPacket : IWebSocketPacket
    {
        [JsonProperty(PropertyName = "event")]
        public EventType Type = EventType.StartWorker;

        [JsonProperty(PropertyName = "workers")]
        public WorkerInfo[] Workers;

        public void DeserializeJson(string json)
        {

        }

        public string SerializeJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    [Serializable]
    class WorkerInfo
    {
        [JsonProperty(PropertyName = "worker_id")]
        public string WorkerId;

        [JsonProperty(PropertyName = "worker_progress")]
        public double WorkerProgress;

        [JsonProperty(PropertyName = "worker_status")]
        public ClientWorkerStatus WorkerStatus;

        [JsonProperty(PropertyName = "worker_system")]
        public string WorkerSystem;
    }

}
