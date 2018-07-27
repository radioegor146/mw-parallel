using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Master.WebSocketPackets
{
    class CPanelTaskChanged : IWebSocketPacket
    {
        [JsonProperty(PropertyName = "event")]
        public EventType EventType = EventType.CPanelTaskResult;

        [JsonProperty(PropertyName = "event_type")]
        public TaskEventType TaskEventType;

        [JsonProperty(PropertyName = "task")]
        public TaskInfo Task;

        public string SerializeJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public void DeserializeJson(string json)
        {

        }
    }

    enum TaskEventType
    {
        AddedToEnd,
        AddedToStart,
        WorkerSet,
        TaskReady
    }
}
