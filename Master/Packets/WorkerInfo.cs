using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Master.Packets
{
    class WorkerInfo
    {
        public WorkerStatus Status;
        public double OkPart;
        public string System;

        public WorkerInfo(SimplePacket packet)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(packet.Data), Encoding.UTF8);
            Status = (WorkerStatus)reader.ReadByte();
            OkPart = reader.ReadDouble();
            System = reader.ReadString();
        }
    }

    enum WorkerStatus : byte
    {
        None,
        Working,
        Error
    }
}
