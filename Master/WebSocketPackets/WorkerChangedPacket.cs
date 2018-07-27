using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Master.WebSocketPackets
{
    enum EventType
    {
        WorkerConnected,
        WorkerDisconnected,
        WorkerStatusChange,
        WorkerError,
        WorkerTaskReady,
        GeneralInfo,
        StartWorker,
        CPanelInfo,
        CPanelTaskResult,
        RSAKp
    }

    [Serializable]
    class WorkerChangedPacket : IWebSocketPacket
    {
        [JsonProperty(PropertyName = "event")]
        public EventType EventType;

        [JsonProperty(PropertyName = "worker_id")]
        public string WorkerId;

        [JsonProperty(PropertyName = "worker_progress")]
        public double WorkerProgress;

        [JsonProperty(PropertyName = "worker_status")]
        public ClientWorkerStatus WorkerStatus;

        [JsonProperty(PropertyName = "worker_system")]
        public string WorkerSystem = "";

        public void DeserializeJson(string json)
        {

        }

        public string SerializeJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
