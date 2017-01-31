using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLI.Logger
{
    public class ManyFilesConfig : BaseLoggerConfig
    {
        public int AmountOfFiles { get; set; }
        public int SizeThresholdInBytes { get; set; }
    }
}
