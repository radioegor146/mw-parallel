using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Worker
{
    public interface IMainVoid
    {
        byte[] DoIt(byte[] input, Action<double> progressCallback);

        void Setup(JObject config);
    }
}
