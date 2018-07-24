using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Master
{
    class Program
    {
        static MasterHandler masterHandler;

        static void Main(string[] args)
        {
            Console.TreatControlCAsInput = true;
            masterHandler = new MasterHandler(JObject.Parse(File.ReadAllText("config.json")));
            masterHandler.Start();
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        masterHandler.Stop();
                        break;
                    }
                }
            }
        }
    }
}
