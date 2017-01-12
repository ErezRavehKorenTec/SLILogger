using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logger
{
    public class ErrorConfig : BaseLoggerConfig
    {
        public bool ImmediateFlush { get; set; }
    }
}
