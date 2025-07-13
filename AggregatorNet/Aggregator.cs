using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Runtime;
using System.Text.Json.Serialization;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net;

namespace AggregatorNet
{
    public class Aggregator
    {
        string baseUri;
        RequestQueue requestQueue;
        Dictionary<string, PropertyKind> propertyKinds = new Dictionary<string, PropertyKind>();
        public Aggregator(string base_uri, string user, string pass)
        {
            this.baseUri = base_uri;
            requestQueue = new RequestQueue(base_uri, user, pass);
        }

        public JsonDocument PostRequest(string endpoint, APIObject obj, string user = null, string pass = null)
        {
            return requestQueue.PostRequest(endpoint, obj, user, pass);
        }
        public JsonDocument PostRequestRaw(string endpoint, string json, string user=null, string pass=null)
        {
            return requestQueue.PostRequestRaw(endpoint, json, user, pass);
        }

        public void QueueRequest(string endpoint, APIObject obj, string user = null, string pass = null)
        {
            //We can enqueue requests that depend on other requests being processed first,
            //because the single worker thread guarantees in order execution of the queue
            requestQueue.EnqueueRequest(endpoint, obj, user, pass);
        }

        public void QueueRequestRaw(string endpoint, string json, string user= null, string pass = null)
        {
            //We can enqueue requests that depend on other requests being processed first,
            //because the single worker thread guarantees in order execution of the queue
            requestQueue.EnqueueRequestRaw(endpoint, json, user, pass);
        }
        /// <summary>
        /// You MUST call this to cause the worker thread to terminate after finishing all requests.
        /// .NET does not guarantee that finalizers are called on process exit anymore, so this can't
        /// be handled by declaring a finalizer on the Aggregator class
        /// </summary>
        public void FinishQueue()
        {
            requestQueue.FinishQueue();
        }

        public Scan StartScan(Tool tool, string soft_hash, string hard_hash, string arguments)
        {
            Scan scan = new Scan(this);
            scan.arguments = arguments;
            scan.tool_hash = tool.hard_match_hash;
            scan.scan_hash = hard_hash;
            scan.scan_soft_hash = soft_hash;
            this.QueueRequest("/api/scan/start", scan);

            return scan;
        }

        public void StopScan(Scan scan)
        {
            this.QueueRequestRaw("/api/scan/stop", scan.toJson());
        }

        public PropertyKind CreatePropertyKind(string name, string description, bool is_matching=false)
        {
            if(propertyKinds.ContainsKey(name))
                return propertyKinds[name];
            PropertyKind kind = new PropertyKind(this, name, description, is_matching);
            propertyKinds.Add(name, kind);
            return kind;
        }

        public PropertyKind GetPropertyKind(string name)
        {
            if (propertyKinds.ContainsKey(name))
                return propertyKinds[name];
            return null;
        }

        public Subject CreateSubject(string name, string hard_hash, List<Property> properties, string version = "1.0", string host=null)
        {
            if (host == null)
                host = Environment.MachineName;
            Subject subj = new Subject(this);
            subj.hash = hard_hash;
            subj.name = name;
            subj.SetProperties(properties);
            this.QueueRequest("/api/subject/create", subj);

            return subj;
        }

        public void SubmitResult(Result result)
        {
            this.QueueRequest("/api/scan/submit", result);
        }

        public Tool CreateTool(string name, string description, string version = "1.0")
        {
            Tool tool = new Tool(this);
            tool.name = name;
            tool.description = description;
            tool.hard_match_hash = HashHelper.GetSHA256StringFromFile(System.Reflection.Assembly.GetEntryAssembly().Location);
            tool.version = version;
            this.QueueRequest("/api/tool/register", tool);

            return tool;
        }
    }
}
