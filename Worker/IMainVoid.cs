using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Worker
{
    /// <summary>
    /// Main worker void interface
    /// </summary>
    public interface IMainVoid
    {
        /// <summary>
        /// Main void
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="progressCallback">Callback for sending progress</param>
        /// <returns>Output data</returns>
        byte[] DoIt(byte[] input, Action<double> progressCallback);

        /// <summary>
        /// Called when starting to send config
        /// </summary>
        /// <param name="config">Config</param>
        void Setup(JObject config);
    }
}
