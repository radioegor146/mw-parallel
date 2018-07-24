using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Worker
{
    class Program
    {
        static ClientHandler clientHandler;
        static void Main(string[] args)
        {
            Console.TreatControlCAsInput = true;
            clientHandler = new ClientHandler(JObject.Parse(File.ReadAllText("config.json")));
            clientHandler.Start();
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        clientHandler.Stop();
                        break;
                    }
                }
            }
        }
    }
}
