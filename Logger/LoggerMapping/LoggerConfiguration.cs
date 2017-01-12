using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logger.LoggerMapping
{
    public class LoggerConfiguration
    {
        public int TimeUntilFileWrite { get; set; }

        public int MaxEntriesInDictionary { get; set; }
        public bool FlushAllDictionaryAtOnce { get; set; }
    }
}
