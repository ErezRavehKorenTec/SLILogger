using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLI.Logger
{
    public class OneFileConfig : BaseLoggerConfig
    {
        public bool ImmediateFlush { get; set; }
    }
}
