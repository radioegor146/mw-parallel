using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Worker
{
    class MainVoid : IMainVoid
    {
        public byte[] DoIt(byte[] input, Action<double> progressCallback)
        {
            for (int i = 0; i < 100; i++)
            {
                progressCallback(i / 100.0);
                Thread.Sleep(100);
            }
            return new byte[0];
        }
    }
}
