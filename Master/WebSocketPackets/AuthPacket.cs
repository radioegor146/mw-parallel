using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Master.WebSocketPackets
{
    class AuthPacket
    {
        public byte[] PasswordData;

        public void DeserializeJson(string json)
        {
            PasswordData = JObject.Parse(json)["password"].Value<byte[]>();
        }
    }
}
