using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Master
{
    class Task
    {
        public int Id;
        public string WorkerId;
        public byte[] InputData;
        public byte[] OutputData;

        public override bool Equals(object obj)
        {
            if (!(obj is Task))
                return false;
            return Id == ((Task)obj).Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
