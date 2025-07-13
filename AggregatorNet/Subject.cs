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
    public class Subject : APIObject
    {
        public string name { get; set; }
        public string hash { get; set; }
        private List<Property> _properties;
        public List<int> properties { get; set; }
        private List<Tag> _tags;
        public List<int> tags { get; set; }

        public Subject(Aggregator aggregator) : base(aggregator) { properties = new List<int>(); tags = new List<int>(); }


        public void SetProperties(List<Property> properties)
        {
            this._properties = properties;
            foreach (Property property in properties)
            {
                property.resolve();
            }
            this.properties = null; //Null properties so that they will be recalculated
        }
        private void CreatePropertyIdArray()
        {
            if (_properties != null && properties == null)
            {
                properties = new List<int>();
                foreach (Property property in _properties)
                {
                    properties.Add(property.id);
                }
            }
        }

        public void SetTags(List<Tag> tags)
        {
            if (tags == null)
            {
                this.tags = new List<int>();
                return;
            }

            this._tags = tags;
            foreach (Tag tag in tags)
            {
                tag.resolve();
            }
            this.tags = null; //Null properties so that they will be recalculated
        }


        private void CreateTagsIdArray()
        {
            if (_tags != null && tags == null)
            {
                tags = new List<int>();
                foreach (Tag tag in _tags)
                {
                    tags.Add(tag.id);
                }
            }
        }

        public override int submit()
        {
            aggregator.QueueRequest("/api/subject/create", this);
            return -1;
        }

        public override string toJson()
        {
            CreatePropertyIdArray();
            CreateTagsIdArray();
            return JsonSerializer.Serialize<Subject>(this);
        }
    }
}
