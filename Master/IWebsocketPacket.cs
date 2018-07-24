using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Master
{
    interface IWebSocketPacket
    {
        string SerializeJson();

        void DeserializeJson(string json);
    }
}
