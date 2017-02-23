using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLI.Data.DataCollectionHandler
{
    public class Payload
    {
        public string error_code { get; set; }
        public string error_reason { get; set; }
        public string timestamp { get; set; }
        public string name { get; set; }
        public int? type { get; set; }
        public string sample_format { get; set; }
        public string sample { get; set; }
    }

    public class OgeroutMsg
    {
        public string ogerout_msg_type { get; set; }
        public Payload payload { get; set; }
    }

}
