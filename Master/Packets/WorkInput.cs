using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Master.Packets
{
    class WorkInput
    {
        public byte[] Data;

        public WorkInput(byte[] data)
        {
            Data = data;
        }

        public SimplePacket ToPacket()
        {
            return new SimplePacket()
            {
                Type = PacketType.WorkInput,
                Data = Data
            };
        }
    }
}
