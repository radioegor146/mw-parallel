using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Master.Packets
{
    class WorkerAuthRequest
    {
        public byte[] RandomBytes;

        public WorkerAuthRequest()
        {
            RandomBytes = new byte[16];
            RandomNumberGenerator.Create().GetBytes(RandomBytes);
        }

        public SimplePacket GetPacket()
        {
            return new SimplePacket()
            {
                Type = PacketType.WorkerAuthRequest,
                Data = RandomBytes
            };
        }
    }
}
