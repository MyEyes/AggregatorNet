using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AggregatorNet
{
    [Serializable]
    public class Scan : APIObject
    {
        public string tool_hash { get; set; }
        public string scan_hash { get; set; }
        public string scan_soft_hash { get; set; }
        public string arguments { get; set; }

        public Scan(Aggregator aggregator):base(aggregator)
        {
        }

        public override int submit()
        {
            aggregator.QueueRequest("/api/scan/start", this);
            return -1;
        }

        public override string toJson()
        {
            return JsonSerializer.Serialize<Scan>(this);
        }
    }
}
