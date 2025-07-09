using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiXunSeleniumTools
{
    public partial class ShiXunSeleniumManager
    {
        public void Log(LogLevel loglevel, string message)
        {
            // 沒有設定log路徑就不寫log
            if (String.IsNullOrEmpty(this.logFilePath))
                return;

            // 寫log
            if (loglevel >= logLevel)
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{loglevel}] {message}";
                File.AppendAllText(this.logFilePath, logEntry + Environment.NewLine);
                Console.WriteLine(logEntry);
            }
        }
    }
}
