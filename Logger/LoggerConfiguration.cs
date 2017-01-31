using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLI.Logger
{
    public class LoggerConfiguration
    {
        public int TimeUntilFileWrite { get; set; }

        public int MaxEntriesInDictionary { get; set; }
    }
}
