using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AggregatorNet
{
    public class Subject
    {
        public string name { get; set; }
        public string soft_hash { get; set; }
        public string hash { get; set; }
        public string host { get; set; }
        public string path { get; set; }
        public string version { get; set; }
    }
}
