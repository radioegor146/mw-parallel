using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Worker.Packets
{
    class WorkOutput
    {
        public byte[] Data;

        public WorkOutput(byte[] data)
        {
            Data = data;
        }
    }
}
