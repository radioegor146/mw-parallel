using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Master
{
    class ClientInfo
    {
        public string Id;
        public JObject Config;
    }
}
