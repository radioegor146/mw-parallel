using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Worker
{
    class SimplePacket
    {
        public PacketType Type;
        public byte[] Data;

        public byte[] GetBytes()
        {
            if (Data.Length > 65536)
                Data = new byte[4] { 0xDE, 0xAD, 0xBE, 0xEF };
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
