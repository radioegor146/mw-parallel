using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Worker
{
    enum LogLevel
    {
        None,
        Log,
        Debug
    } 

    class Logger
    {
        private LogLevel level;
        private FileStream fileStream;
        private StreamWriter mainWriter;

        public Logger(string filename, LogLevel level = LogLevel.Log)
        {
            this.level = level;
            if (level == LogLevel.None)
                return;
            fileStream = new FileStream(filename, FileMode.Create);
            mainWriter = new StreamWriter(fileStream);
            mainWriter.AutoFlush = true;
        }

        public void Log(string text)
        {
            if (level >= LogLevel.Log)
                lock (mainWriter)
                {
                    mainWriter.WriteLine($"[{DateTime.Now}] [LOG]: {text}");
                    Console.WriteLine($"[{DateTime.Now}] [LOG]: {text}");
                }
        }

        public void Debug(string text)
        {
            if (level >= LogLevel.Debug)
                lock (mainWriter)
                {
                    mainWriter.WriteLine($"[{DateTime.Now}] [DEBUG]: {text}");
                    Console.WriteLine($"[{DateTime.Now}] [DEBUG]: {text}");
                }
        }

        public void SetLogLevel(LogLevel level)
        {
            this.level = level;
        }
    }
}
