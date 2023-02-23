using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AggregatorNet
{
    [Serializable]
    public class Tool
    {
        public string name { get; set; }
        public string soft_match_hash { get; set; }
        public string hard_match_hash { get; set; }
        public string version { get; set; }
        public string description { get; set; }
    }
}
