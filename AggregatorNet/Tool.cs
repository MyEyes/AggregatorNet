using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AggregatorNet
{
    [Serializable]
    public class Tool : APIObject
    {
        public string name { get; set; }
        public string hard_match_hash { get; set; }
        public string version { get; set; }
        public string description { get; set; }

        public Tool(Aggregator aggregator) : base(aggregator) { }

        public override int submit()
        {
            aggregator.QueueRequest("/api/tool/register", this);
            return -1;
        }

        public override string toJson()
        {
            return JsonSerializer.Serialize<Tool>(this);
        }
    }
}
