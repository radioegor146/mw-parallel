using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Worker.Packets
{
    class WorkerInfo
    {
        public WorkerStatus Status;
        public double OkPart;
        public string System;

        public WorkerInfo() { }

        public byte[] GetPacket()
        {
            MemoryStream packetData = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(packetData);
            writer.Write((byte)Status);
            writer.Write(OkPart);
            writer.Write(System);
            MemoryStream returnData = new MemoryStream();
            StreamUtils.WritePacket(returnData, new SimplePacket()
            {
                Data = packetData.ToArray(),
                Type = PacketType.WorkerInfo
            });
            return returnData.ToArray();
        }
    }

    enum WorkerStatus : byte
    {
        None,
        Working,
        Error
    }
}
