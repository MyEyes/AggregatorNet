using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AggregatorNet
{
    public class Result
    {
        public string scan_hash { get; set; }
        public string subject_hash { get; set; }
        public string hash { get; set; }
        public string soft_hash { get; set; }
        public string risk { get; set; }
        public string text { get; set; }

        public Result(string softId, string hardId, string risk, string text)
        {
            this.hash = HashHelper.GetSHA256String(hardId);
            this.soft_hash = HashHelper.GetSHA256String(softId);
            this.risk = risk;
            this.text = text;
        }
    }
}
