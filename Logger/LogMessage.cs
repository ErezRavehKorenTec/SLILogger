using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLI.Logger
{
    public class LogMessage
    {
        public static SLI_Message CreateNewByText(string message, SeverityLevels level, DateTime time)
        {
            return new SLI_Message(message, level, time);
        }
    }
    public class SLI_Message
    {
        public string AdditonalMessage { get; set; }
        public string ExtraString { get; set; }
        public string Message { get; set; }
        public SeverityLevels Level { get; set; }
        public DateTime Time { get; set; }

        public SLI_Message(string message, SeverityLevels level, DateTime time)
        {
            Message = message;
            Level = level;
            Time= time;
        }
    }

}
