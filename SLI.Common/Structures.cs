using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLI.Common
{
    public enum DataType
    {
        External = 0,
        Internal = 1,
    }
    public class DefaultData : IData
    {
        public string Key { get; private set; }

        public object Value { get; private set; }

        public DataType Type { get; private set; }

        public DateTime TimeStemp { get; private set; }

        public DefaultData(string key, object value, DataType type) : this(key, value, type, DateTime.Now)
        {
        }

        public DefaultData(string key, object value, DataType type, DateTime time)
        {
            Key = key;
            Value = value;
            Type = type;
            TimeStemp = time;
        }

        public string Serialize()
        {
            return Key + ";" + Value;
        }
    }
}
