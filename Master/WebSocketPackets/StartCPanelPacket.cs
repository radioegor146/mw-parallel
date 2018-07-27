using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Master.WebSocketPackets
{
    [Serializable]
    class StartCPanelPacket : IWebSocketPacket
    {
        [JsonProperty(PropertyName = "event")]
        public EventType Type = EventType.StartWorker;

        [JsonProperty(PropertyName = "workers")]
        public WorkerInfo[] Workers;

        [JsonProperty(PropertyName = "tasks_in_queue")]
        public TaskInfo[] InQueueTasks;

        [JsonProperty(PropertyName = "tasks_ready")]
        public TaskInfo[] ReadyTasks;

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

        [JsonProperty(PropertyName = "current_task")]
        public TaskInfo CurrentTask;
    }

    [Serializable]
    class TaskInfo
    {
        [JsonProperty(PropertyName = "task_state")]
        public TaskStatus Status;

        [JsonProperty(PropertyName = "task_id")]
        public int TaskId;

        [JsonProperty(PropertyName = "worker_id")]
        public string WorkerId;

        [JsonProperty(PropertyName = "input_data")]
        public byte[] InputData;

        [JsonProperty(PropertyName = "output_data")]
        public byte[] OutputData;

        public TaskInfo(Task task, TaskStatus status)
        {
            TaskId = task.Id;
            WorkerId = task.WorkerId;
            InputData = task.InputData;
            OutputData = task.OutputData;
            Status = status;
        }
    }

    enum TaskStatus
    {
        InQueue,
        Processing,
        Ready
    }
}
