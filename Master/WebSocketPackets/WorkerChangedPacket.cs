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
        [EnumMember(Value = "worker_connected")]
        WorkerConnected,
        [EnumMember(Value = "worker_disconnected")]
        WorkerDisconnected,
        [EnumMember(Value = "worker_statuschange")]
        WorkerStatusChange,
        [EnumMember(Value = "worker_error")]
        WorkerError,
        [EnumMember(Value = "worker_taskready")]
        WorkerTaskReady,
        [EnumMember(Value = "general_info")]
        GeneralInfo,
        [EnumMember(Value = "start_workers")]
        StartWorker
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
