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

        public Logger(string filename, LogLevel level = LogLevel.Log, bool writeToFile = false)
        {
            this.level = level;
            if (level == LogLevel.None)
                return;
            if (!writeToFile)
                return;
            int logid = 0;
            try
            {
                fileStream = new FileStream(filename, FileMode.Create);
            }
            catch
            {
                Log($"Can't create logfile \"{filename}\"");
                bool success = false;
                while (!success)
                {
                    try
                    {
                        logid++;
                        fileStream = new FileStream(filename + "-" + logid, FileMode.Create);
                        success = true;
                    }
                    catch
                    {
                        Log($"Can't create logfile \"{filename + "-" + logid}\"");
                    }
                }
            }
            mainWriter = new StreamWriter(fileStream);
            mainWriter.AutoFlush = true;
        }

        public void Log(string text)
        {
            if (level >= LogLevel.Log)
            {
                if (mainWriter != null)
                    lock (mainWriter)
                        mainWriter?.WriteLine($"[{DateTime.Now}] [LOG]: {text}");
                Console.WriteLine($"[{DateTime.Now}] [LOG]: {text}");
            }
        }

        public void Debug(string text)
        {
            if (level >= LogLevel.Debug)
            {
                if (mainWriter != null)
                    lock (mainWriter)
                        mainWriter?.WriteLine($"[{DateTime.Now}] [DEBUG]: {text}");
                Console.WriteLine($"[{DateTime.Now}] [DEBUG]: {text}");
            }
        }

        public void SetLogLevel(LogLevel level)
        {
            this.level = level;
        }
    }
}
