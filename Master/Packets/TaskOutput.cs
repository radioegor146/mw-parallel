using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Master.Packets
{
    class TaskOutput
    {
        public byte[] Data;

        public TaskOutput(byte[] data)
        {
            Data = data;
        }
    }
}
