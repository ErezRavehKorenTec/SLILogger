using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Log;
using System.Threading;
using Log.Enums;

namespace SLILogger
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Logger log = new Logger();
            var thread1 = Task.Run(() => WriteDubug(log));
            //var thread2 = Task.Run(() => WriteDubug(log));
            var thread3 = Task.Run(() => WriteError(log));
            Task.WaitAll(new[] { thread1, /*thread2,*/thread3 });
        }
        private static void WriteError(Logger log)
        {
            while (true)
            {
                var logMessage = LogMessage.CreateNewByText("this is test error", SeverityLevels.Error, DateTime.Now);
                logMessage.ExtraString = "Time = " + DateTime.Now;
                log.PublishMessage(logMessage);
            }
        }

        private static void WriteDubug(Logger log)
        {
            while (true)
            {
                var logMessage = LogMessage.CreateNewByText("this is test debug", SeverityLevels.Debug, DateTime.Now);
                logMessage.ExtraString = "Time = " + DateTime.Now;
                log.PublishMessage(logMessage);
            }
        }
    }
}
