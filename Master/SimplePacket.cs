using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Master
{
    class SimplePacket
    {
        public PacketType Type;
        public byte[] Data;

        public byte[] GetBytes()
        {
            MemoryStream memoryStream = new MemoryStream();
            StreamUtils.WritePacket(memoryStream, this);
            return memoryStream.ToArray();
        }
    }

    enum PacketType : short
    {
        Nop,
        TaskInput,
        TaskOutput,
        WorkerInfo,
        WorkerAuth,
        Signal
    }
}
