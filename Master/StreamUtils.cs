using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Master
{
    class StreamUtils
    {
        public static SimplePacket ReadPacket(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            return new SimplePacket()
            {
                Type = (PacketType)ReadShort(stream),
                Data = ReadBytes(stream, data.Length - 2)
            };
        }

        public static short ReadShort(Stream stream)
        {
            byte[] shortBuf = new byte[2];
            for (int i = 0; i < 2; i++)
                shortBuf[i] = ReadByte(stream);
            return BitConverter.ToInt16(shortBuf, 0);
        }

        public static int ReadInt(Stream stream)
        {
            byte[] intBuf = new byte[4];
            for (int i = 0; i < 4; i++)
                intBuf[i] = ReadByte(stream);
            return BitConverter.ToInt32(intBuf, 0);
        }

        public static byte[] ReadBytes(Stream stream, int length)
        {
            byte[] buffer = new byte[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = ReadByte(stream);
            }
            return buffer;
        }

        public static byte ReadByte(Stream stream)
        {
            int t = stream.ReadByte();
            if (t == -1)
                throw new IOException("Stream ended");
            return (byte)t;
        }

        public static void WritePacket(Stream stream, SimplePacket packet)
        {
            WriteShort(stream, (short)packet.Type);
            stream.Write(packet.Data, 0, packet.Data.Length);
        }

        public static void WriteShort(Stream stream, short value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, 2);
        }

        public static void WriteInt(Stream stream, int value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, 4);
        }

        public static byte[] ReadByteArray(Stream stream)
        {
            return ReadBytes(stream, ReadInt(stream));
        }

        public static void WriteByteArray(Stream stream, byte[] data)
        {
            WriteInt(stream, data.Length);
            stream.Write(data, 0, data.Length);
        }
    }
}
