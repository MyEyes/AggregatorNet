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
        public Aggregator(string base_uri, string user, string pass)
        {
            this.baseUri = base_uri;
            requestQueue = new RequestQueue(base_uri, user, pass);
        }
        public JsonDocument PostRequest(string endpoint, string json, string user = null, string pass = null)
        {
            return requestQueue.PostRequest(endpoint, json, user, pass);
        }

        public void QueueRequest(string endpoint, string json, string user = null, string pass = null)
        {
            //We can enqueue requests that depend on other requests being processed first,
            //because the single worker thread guarantees in order execution of the queue
            requestQueue.EnqueueRequest(endpoint, json, user, pass);
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
            Scan scan = new Scan();
            scan.arguments = arguments;
            scan.tool_hash = tool.hard_match_hash;
            scan.scan_hash = hard_hash;
            scan.scan_soft_hash = soft_hash;
            this.QueueRequest("/api/scan/start", JsonSerializer.Serialize<Scan>(scan));

            return scan;
        }

        public void StopScan(Scan scan)
        {
            this.QueueRequest("/api/scan/stop", JsonSerializer.Serialize<Scan>(scan));
        }

        public Subject CreateSubject(string name, string path, string soft_hash, string hard_hash, string version = "1.0", string host = null)
        {
            if (host == null)
                host = Environment.MachineName;
            Subject subj = new Subject();
            subj.hash = hard_hash;
            subj.host = host;
            subj.version = version;
            subj.name = name;
            subj.soft_hash = soft_hash;
            subj.path = path;
            this.QueueRequest("/api/subject/create", JsonSerializer.Serialize<Subject>(subj));

            return subj;
        }

        public void SubmitResult(Scan scan, Subject subj, Result result)
        {
            result.scan_hash = scan.scan_hash;
            result.subject_hash = subj.hash;
            this.QueueRequest("/api/scan/submit", JsonSerializer.Serialize<Result>(result));
        }

        public Tool CreateTool(string name, string description, string version = "1.0")
        {
            Tool tool = new Tool();
            tool.name = name;
            tool.description = description;
            tool.soft_match_hash = HashHelper.GetSHA256String(tool.name);
            tool.hard_match_hash = HashHelper.GetSHA256StringFromFile(System.Reflection.Assembly.GetEntryAssembly().Location);
            tool.version = version;
            string json = JsonSerializer.Serialize(tool);
            this.QueueRequest("/api/tool/register", json);

            return tool;
        }
    }
}
