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
    public class Tag : APIObject
    {
        public string shortname { get; set; }
        public string color { get; set; }
        public string name { get; set; }
        public string description { get; set; }

        public Tag(Aggregator aggregator, string shortname, string color, string name, string description):base(aggregator)
        {
            this.shortname = shortname;
            this.color = color;
            this.name = name;
            this.description = description;
        }

        public override int submit()
        {
            aggregator.QueueRequest("/api/tag/register", this);
            return -1;
        }

        public override string toJson()
        {
            return JsonSerializer.Serialize<Tag>(this);
        }
    }
}
