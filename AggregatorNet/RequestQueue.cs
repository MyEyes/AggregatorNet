using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace AggregatorNet
{
    internal class RequestQueue
    {
        string baseUri;
        string user;
        string pass;

        string token;

        HttpClientHandler httpHandler;
        HttpClient httpClient;
        AuthenticationHeaderValue authHeader = null;
        ConcurrentQueue<RequestObject> requests;
        Thread workerThread;
        AutoResetEvent enqueueSignal = new AutoResetEvent(false);
        bool running = true;

        public RequestQueue(string base_uri, string user, string pass)
        {
            this.baseUri = base_uri;
            this.user = user;
            this.pass = pass;
            httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback += ValidateRemoteCertificate;
            httpClient = new HttpClient(httpHandler);
            requests = new ConcurrentQueue<RequestObject>();
            workerThread = new Thread(RequestWorker);
            Reauthenticate();
            workerThread.Start();
        }

        public void FinishQueue()
        {
            if (running)
            {
                running = false;
                enqueueSignal.Set();
            }
            workerThread.Join();
        }

        private bool Reauthenticate()
        {
            //Console.WriteLine("Authenticating");
            var response = this.PostRequestRaw("/api/auth/tokens", "{}", this.user, this.pass);
            try
            {
                var error = response.RootElement.GetProperty("error");
                if (error.ValueKind != JsonValueKind.Null)
                {
                    Console.WriteLine("Couldn't authenticate");
                    Console.WriteLine(error.GetString());
                    return false;
                }
            }
            catch (Exception)
            { }
            var token = response.RootElement.GetProperty("token");
            if (token.ValueKind == JsonValueKind.Null)
            {
                Console.WriteLine("No token");
                return false;
            }
            this.token = token.GetString();
            this.authHeader = new AuthenticationHeaderValue("Bearer", this.token);
            return true;
        }

        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            if (error == SslPolicyErrors.None)
            {
                return true;
            }
            return true;
        }

        private HttpRequestMessage CreateRequestRaw(string endpoint, string json, string user = null, string pass = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, this.baseUri + endpoint);
            if (user == null && pass == null)
                request.Headers.Authorization = this.authHeader;
            else
                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(user + ":" + pass)));
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return request;
        }

        public RequestObject CreateRequest(string endpoint, APIObject obj, string user = null, string pass = null)
        {
            return new RequestObject(endpoint, obj);
        }
        public JsonDocument PostRequestRaw(string endpoint, string json, string user = null, string pass = null)
        {
            HttpRequestMessage request = CreateRequestRaw(endpoint, json, user, pass);
            return ProcessRequestRaw(request);
        }

        public JsonDocument PostRequest(string endpoint, APIObject obj, string user = null, string pass = null)
        {
            RequestObject requestObj = CreateRequest(endpoint, obj, user, pass);
            return ProcessRequestRaw(requestObj.httpRequest);
        }

        public void EnqueueRequest(string endpoint, APIObject obj, string user = null, string pass = null)
        {
            EnqueueRequest(CreateRequest(endpoint, obj, user, pass));
        }

        public void EnqueueRequestRaw(string endpoint, string json, string user = null, string pass = null)
        {
            EnqueueRequestRaw(CreateRequestRaw(endpoint, json, user, pass));
        }
        public void EnqueueRequestRaw(HttpRequestMessage request)
        {
            var req = new RequestObject("", null);
            req.httpRequest = request;
            EnqueueRequest(req);
        }

        public void EnqueueRequest(RequestObject request)
        {
            requests.Enqueue(request);
            enqueueSignal.Set();
        }

        public JsonDocument ProcessRequestRaw(HttpRequestMessage request)
        {
            var resp = httpClient.SendAsync(request);
            resp.Wait();
            var data = resp.Result.Content.ReadAsStringAsync();
            data.Wait();
            var content = data.Result;
            return JsonSerializer.Deserialize<JsonDocument>(content);
        }

        public JsonDocument ProcessRequest(RequestObject request)
        {
            //This way we can defer creation until right before we send it, so if we always resolve ids first there shouldn't be an issue.
            if (request.httpRequest == null && request.id_receiver != null)
                request.httpRequest = CreateRequestRaw(request.endpoint, request.id_receiver.toJson());
            var resp = httpClient.SendAsync(request.httpRequest);
            resp.Wait();
            var data = resp.Result.Content.ReadAsStringAsync();
            data.Wait();
            var content = data.Result;
            var parsedData = JsonSerializer.Deserialize<JsonDocument>(content);
            try
            {
                if (request.id_receiver != null)
                {
                    var id_val = parsedData.RootElement.GetProperty("id");
                    if (id_val.ValueKind == JsonValueKind.Number)
                        request.id_receiver.id = id_val.GetInt32();
                }
            }
            catch (Exception ex) {/*Do nothing, error handling happens deeper*/ }
            return parsedData;
        }
        private bool CheckErrorAndReauthenticate(JsonDocument response)
        {
            try
            {
                var error = response.RootElement.GetProperty("error");
                if (error.ValueKind == JsonValueKind.String)
                {
                    if (error.GetString() == "401")
                    {
                        if (this.Reauthenticate())
                            return false;
                        else
                            throw new Exception("Couldn't reauthenticate");
                    }
                    else
                        return true;
                }
            }
            catch (Exception) { }
            return true;
        }

        public void RequestWorker(Object stateInfo)
        {
            RequestObject requestObject;
            while(running || requests.Count>0) //Keep running until all requests are done
            {
                if (!requests.TryDequeue(out requestObject)) //Try to get from queue and wait on enqueueing signal if we couldn't dequeue
                {
                    enqueueSignal.WaitOne();
                    continue;
                }
                try
                {
                    var response = ProcessRequest(requestObject);
                if (!CheckErrorAndReauthenticate(response)) //This only returns true if we got an error that indicates our auth token is invalid and we've reauthenticated
                {
                    //At this point we should be reauthenticated, so replace old auth header
                        var repeatedMessage = new HttpRequestMessage(HttpMethod.Post, requestObject.httpRequest.RequestUri);
                    repeatedMessage.Headers.Authorization = this.authHeader;
                        repeatedMessage.Content = requestObject.httpRequest.Content;
                        requestObject.httpRequest = repeatedMessage;
                        response = ProcessRequest(requestObject);
                }
                JsonElement error;
                if (response.RootElement.TryGetProperty("error", out error))
                {
                    if (error.ValueKind != JsonValueKind.Null)
                    {
                        Console.WriteLine(error.GetString());
                    }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("Ran into an issue while submitting request:", e);
                    throw e;
                }
            }
        }
    }
}
