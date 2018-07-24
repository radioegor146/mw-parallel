using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace Worker.Packets
{
    class WorkerAuthResponse
    {
        public byte[] RandomBytes;
        public string Password;

        public WorkerAuthResponse()
        {

        }

        public byte[] GetPacket()
        {
            MemoryStream returnData = new MemoryStream();
            StreamUtils.WritePacket(returnData, new SimplePacket()
            {
                Data = MD5.Create().ComputeHash(RandomBytes.Concat(Encoding.UTF8.GetBytes(Password)).ToArray()),
                Type = PacketType.WorkerAuthResponse
            });
            return returnData.ToArray();
        }
    }
}
