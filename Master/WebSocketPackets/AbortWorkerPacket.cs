using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Master.WebSocketPackets
{
    class AbortWorkerPacket
    {
        public string WorkerId;

        public void DeserializeJson(string json)
        {
            WorkerId = JObject.Parse(json)["worker_id"].Value<string>();
        }
    }
}
