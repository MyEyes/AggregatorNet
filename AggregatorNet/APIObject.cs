using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AggregatorNet
{

    public class APIObjectJsonIdConverter : JsonConverter<APIObject>
    {
        //Not implemented because we will never have to parse on this end
        public override APIObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(APIObject).IsAssignableFrom(typeToConvert);
        }

        public override void Write(Utf8JsonWriter writer, APIObject value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.id);
        }
    }

    [JsonConverter(typeof(APIObjectJsonIdConverter))]
    public class APIObject
    {
        public int id {  get; set; }
        protected Aggregator aggregator;

        public APIObject(Aggregator aggregator) { id = -1; this.aggregator = aggregator; }

        public int resolve()
        {
            if (id >= 0)
            {
                return id;
            }
            return submit();
        }

        public virtual int submit()
        {
            throw new NotImplementedException("Please overwrite \"submit\"");
        }

        public virtual string toJson()
        {
            throw new NotImplementedException("Please overwrite \"toJson\"");
        }
    }
}
