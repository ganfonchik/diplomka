using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplomka
{
    public class LogRecord
    {
        public DateTime Timestamp { get; set; }
        public double? Temp { get; set; }
        public double? Humidity { get; set; }
        public double? Light { get; set; }
        public double? Co2 { get; set; }
        public double? Water { get; set; }
        public double? Sound { get; set; }
    }
}
