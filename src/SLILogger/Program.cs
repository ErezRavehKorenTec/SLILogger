using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using SLI.Logger;
using SLI.Data.DataCollectionHandler;

namespace SLILogger
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Logger.OnErrorEvent += new EventHandler<ErrorMessageEventArgs>(errorMessageReceive);
            Logger log = new Logger();
            RunServerLisener(log);
            var thread1 = Task.Run(() => RunServerLisener(log));
            //var thread1 = Task.Run(() => WriteDubug(log));
            //var thread3 = Task.Run(() => StayAlive());
            //var thread3 = Task.Run(() => WriteError(log));
            Task.WaitAll(new[] { thread1/*, thread2,thread3*/ });
            //while (true) ;

        }

        private static void StayAlive()
        {
            while (true)
            {
                Thread.Sleep(10000);
            }
        }

        private static void RunServerLisener(Logger log)
        {
            new DataCollectionHandler().StartListen(log);
            //new DataCollectionHandler().DataClientProccessThread(null);
        }

        private static void errorMessageReceive(object sender, ErrorMessageEventArgs e)
        {
            Console.WriteLine("Event Raised--"+e.Message);
        }

        private static void WriteError(Logger log)
        {
            while (true)
            {
                Thread.Sleep(100);
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
