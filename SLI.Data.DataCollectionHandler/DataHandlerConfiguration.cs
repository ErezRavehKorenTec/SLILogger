using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLI.Data.DataCollectionHandler
{
    public class DataHandlerConfiguration
    {
        public int DataHandlerListeningPort { get; set; }
        public int TimeToTrigerKeepAliveCheck { get; set; }

    }
}
