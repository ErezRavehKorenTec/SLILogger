using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Logger;
using System.Threading;

namespace SLILogger
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var thread1 = Task.Run(() => WriteDubug());
            var thread3 = Task.Run(() => WriteError());
            Task.WaitAll(new[] { thread1, thread3 });
            //while (true)
            //    Logger.Logger.GetInstance().WriteToLog("", Logger.Enums.Severity.Error, "this is test");
        }

        private static void WriteError()
        {
            while (true)
            {

                Logger.Logger.GetInstance().WriteToLog("", Logger.Enums.Severity.Error, "this is test");
            }
        }

        private static void WriteDubug()
        {
            while (true)
            {

                Logger.Logger.GetInstance().WriteToLog("", Logger.Enums.Severity.Debug, "this is test to error");
            }
        }
    }
}
