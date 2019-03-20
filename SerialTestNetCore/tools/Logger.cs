using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tools
{
    public class Logger
    {
        public static void Trace(string fmt, params object[] args)
        {
            Log(Level.Trace, fmt, args);
            string name = $"TraceLog{DateTime.Now.ToString("yy-MM-dd")}.txt";
            LogToFile(name, fmt, args);
        }

        public static void Debug(string fmt, params object[] args)
        {
            Log(Level.Debug, fmt, args);
        }

        public static void Info(string fmt, params object[] args)
        {
            Log(Level.Info, fmt, args);
            string name = $"InfoLog{DateTime.Now.ToString("yy-MM-dd")}.txt"; // static mCurTime would make log accumulate endlessly
            LogToFile(name, fmt, args);
        }

        public static void Warn(string fmt, params object[] args)
        {
            Log(Level.Warn, fmt, args);
            string name = $"WarnLog{DateTime.Now.ToString("yy-MM-dd")}.txt";
            LogToFile(name, fmt, args);
        }

        public static void Error(string fmt, params object[] args)
        {
            Log(Level.Error, fmt, args);
            string name = $"ErrorLog{DateTime.Now.ToString("yy-MM-dd")}.txt";
            LogToFile(name, fmt, args);
        }

        public static void Custom(string filename, string fmt, params object[] args)
        {
            Log(Level.Custom, fmt, args);
            string name = filename + DateTime.Now.ToString("yy-MM-dd")+ ".txt";
            LogToFile(name, fmt, args);
        }

        private static string GetFormatString(string fmt, params object[] args)
        {
            string str = String.Format(fmt, args);
            string ret = $"{DateTime.Now:yy-MM-dd HH:mm:ss}\t{str}";
            return ret;
        }
        private static void Log(Level level, string fmt, params object[] args)
        {
            if(level<MsLevel)
                return;
            string str = GetFormatString(fmt, args);
            Console.WriteLine(str);
        }

        private static void LogToFile(string name, string fmt, params object[] args)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
            string str = GetFormatString(fmt, args);
            str += "\n";
            SerFile.Append(path, str);
        }

        private enum Level
        {
            Trace = 0,
            Debug,
            Info,
            Warn,
            Error,
            Custom
        }

        private const Level MsLevel = Level.Trace;
        private static string mCurTime = DateTime.Now.ToString("yy-MM-dd");
    }
}
