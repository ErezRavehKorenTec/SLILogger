using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLI.Common
{
    public interface IData
    {
        string Key { get; }

        DataType Type { get; }

        object Value { get; }

        DateTime TimeStemp { get; }

        string Serialize();

    }
}
