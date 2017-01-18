using Log.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Log
{

    public class BaseLoggerConfig
    {
        public bool Implemented { get; set; }
        public int MemorySize { get; set; }
        public string ListenerName { get; set; }
        public string FilePath { get; set; }
        public SeverityLevels Severity { get; set; }
    }
}
