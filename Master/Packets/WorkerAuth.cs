using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Master.Packets
{
    class WorkerAuth
    {
        public byte[] Password;

        public WorkerAuth(SimplePacket packet)
        {
            Password = packet.Data;
        }

        public bool CheckPassword(byte[] passwordData)
        {
            if (passwordData.Length != Password.Length)
                return false;
            for (int i = 0; i < passwordData.Length; i++)
                if (passwordData[i] != Password[i])
                    return false;
            return true;
        }
    }
}
