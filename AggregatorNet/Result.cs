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
    public class Result : APIObject
    {
        private Scan _scan;
        public int scan_id { get; set; }
        private Subject _subject;
        public int subject_id { get; set; }
        public string hash { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        private List<Property> _properties;
        public List<int> properties { get; set; }
        private List<Tag> _tags;
        public List<int> tags { get; set; }

        public Result(Aggregator aggregator, Scan scan, Subject subject, string hardId, string risk, string title, string description, List<Property> properties, List<Tag> tags):base(aggregator)
        {
            this.hash = HashHelper.GetSHA256String(hardId);
            this.title = title;
            this.description = description;
            this._scan = scan;
            this._subject = subject;
            this.SetTags(tags);
            this.SetProperties(properties);
        }

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
            if (_properties != null && (properties == null || _properties.Count != properties.Count))
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
            _scan.resolve();
            _subject.resolve();
            aggregator.QueueRequest("/api/scan/submit", this);
            return -1;
        }

        public override string toJson()
        {
            this.CreatePropertyIdArray();
            this.CreateTagsIdArray();
            this.scan_id = _scan.id;
            this.subject_id = this._subject.id;
            return JsonSerializer.Serialize<Result>(this);
        }
    }
}
