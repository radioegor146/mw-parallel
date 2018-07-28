using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Worker.Packets
{
    class Signal
    {
        public SignalEnum Type;
        public byte[] Data;

        public Signal() { }

        public Signal(SimplePacket packet)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(packet.Data), Encoding.UTF8);
            Type = (SignalEnum)reader.ReadByte();
            Data = reader.ReadBytes(packet.Data.Length - 1);
        }

        public SimplePacket GetPacket()
        {
            MemoryStream data = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(data, Encoding.UTF8);
            writer.Write((byte)Type);
            writer.Write(Data);
            return new SimplePacket()
            {
                Type = PacketType.Signal,
                Data = data.ToArray()
            };
        }
    }

    public enum SignalEnum
    {
        None,
        Abort,
        Data
    }
}
