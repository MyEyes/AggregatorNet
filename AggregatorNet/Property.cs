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
    public class Property : APIObject
    {
        public string value { get; set; }
        private PropertyKind _kind;
        public int kind { get; set; }

        public Property(Aggregator aggregator, PropertyKind kind, string value):base(aggregator)
        {
            this.value = value;
            this._kind = kind;
        }

        public override int submit()
        {
            this._kind.resolve();
            aggregator.QueueRequest("/api/property/add", this);
            return -1;
        }

        public override string toJson()
        {
            this.kind = this._kind.id;
            return JsonSerializer.Serialize<Property>(this);
        }
    }
}
