using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Master.Packets
{
    class WorkerAuthResponse
    {
        public byte[] Md5Bytes;

        public WorkerAuthResponse(SimplePacket packet)
        {
            Md5Bytes = packet.Data;
        }

        public bool CheckPassword(byte[] randomInput, string password)
        {
            byte[] NewHash = MD5.Create().ComputeHash(randomInput.Concat(Encoding.UTF8.GetBytes(password)).ToArray());
            if (NewHash.Length != Md5Bytes.Length)
                return false;
            for (int i = 0; i < NewHash.Length; i++)
                if (NewHash[i] != Md5Bytes[i])
                    return false;
            return true;
        }
    }
}
