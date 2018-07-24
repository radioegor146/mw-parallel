using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Worker
{
    public interface IMainVoid
    {
        byte[] DoIt(byte[] input, Action<double> progressCallback);
    }
}
