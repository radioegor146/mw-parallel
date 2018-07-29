using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Master.Packets
{
    class TaskInput
    {
        public byte[] Data;

        public TaskInput(byte[] data)
        {
            Data = data;
        }

        public SimplePacket ToPacket()
        {
            return new SimplePacket()
            {
                Type = PacketType.TaskInput,
                Data = Data
            };
        }
    }
}
