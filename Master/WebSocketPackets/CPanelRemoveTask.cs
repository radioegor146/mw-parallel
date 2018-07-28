using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Master.WebSocketPackets
{
    class CPanelRemoveTask
    {
        public int TaskId;

        public void DeserializeJson(string json)
        {
            TaskId = JObject.Parse(json)["task_id"].Value<int>();
        }
    }
}
