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
    public class PropertyKind : APIObject
    {
        public string name { get; set; }
        public string description { get; set; }
        public bool is_matching { get; set; }

        private Dictionary<string, Property> propertyValues = new Dictionary<string, Property>();

        public PropertyKind(Aggregator aggregator, string name, string description, bool is_matching=false):base(aggregator)
        {
            this.name = name;
            this.description = description;
            this.is_matching = is_matching;
        }

        public Property Create(string value)
        {
            if(propertyValues.ContainsKey(value))
                return propertyValues[value];
            Property newValue = new Property(this.aggregator, this, value);
            propertyValues.Add(value, newValue);
            return newValue;
        }

        public override int submit()
        {
            aggregator.QueueRequest("/api/property/register_kind", this);
            return -1;
        }

        public override string toJson()
        {
            return JsonSerializer.Serialize<PropertyKind>(this);
        }
    }
}
