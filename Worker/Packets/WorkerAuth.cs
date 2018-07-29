using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Worker.Packets
{
    class WorkerAuth
    {
        public byte[] Password;

        public WorkerAuth()
        {

        }

        public byte[] GetPacket()
        {
            MemoryStream returnData = new MemoryStream();
            StreamUtils.WritePacket(returnData, new SimplePacket()
            {
                Data = Password,
                Type = PacketType.WorkerAuth
            });
            return returnData.ToArray();
        }
    }
}
