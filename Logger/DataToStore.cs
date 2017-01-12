using Logger.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logger
{
    
    public class DataToStore
    {
        public string SourceTime { get; set; }
        public string LoggerTime { get; set; }
        public string Key { get; set; }
        public Severity LogSeverity { get; set; }
        public string Message { get; set; }
        public string AdditonalMessage { get; set; }
        public string ExtraString{ get; set; }
        public string MachineName { get; set; }


    }
}
