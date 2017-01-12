using Logger.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logger
{

    public class BaseLoggerConfig
    {
        public bool Implemented { get; set; }
        public int MemorySize { get; set; }
        public string ListenerName { get; set; }
        public string FilePath { get; set; }
        public Severity Severity { get; set; }
    }
}
