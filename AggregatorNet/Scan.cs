using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace AggregatorNet
{
    [Serializable]
    public class Scan
    {
        public string tool_hash { get; set; }
        public string scan_hash { get; set; }
        public string scan_soft_hash { get; set; }
        public string arguments { get; set; }
    }
}
