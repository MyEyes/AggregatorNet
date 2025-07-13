using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AggregatorNet
{
    internal class RequestObject
    {
        public HttpRequestMessage httpRequest;
        public string endpoint;
        public APIObject id_receiver;

        public RequestObject(string endpoint, APIObject receiver) 
        {
            this.endpoint = endpoint;
            this.httpRequest = null;
            this.id_receiver = receiver;
        }
    }
}
