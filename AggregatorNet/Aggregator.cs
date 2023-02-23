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
        string user;
        string pass;

        string token;
        public Aggregator(string base_uri, string user, string pass)
        {
            this.baseUri = base_uri;
            this.user = user;
            this.pass = pass;
            ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;

        }
        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            if (error == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }

            return true;
        }
        public JsonDocument PostRequest(string endpoint, string json, string user=null, string pass=null)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, this.baseUri+endpoint);
            if (user == null && pass == null)
                request.Headers.Add("Authorization", "Bearer " + token);
            else
                request.Headers.Add("Authorization", "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(user + ":" + pass)));
            request.Content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(json));
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var resp = client.SendAsync(request);
            resp.Wait();
            var data = resp.Result.Content.ReadAsStringAsync();
            data.Wait(); 
            var content = data.Result;
            return JsonSerializer.Deserialize<JsonDocument>(content);
        }

        private void Reauthenticate()
        {
            Console.WriteLine("Authenticating");
            var response = this.PostRequest("/api/auth/tokens", "{}", this.user, this.pass);
            try
            {
                var error = response.RootElement.GetProperty("error");
                if (error.ValueKind != JsonValueKind.Null)
                {
                    Console.WriteLine("Couldn't authenticate");
                    Console.WriteLine(error.GetString());
                    return;
                }
            }
            catch(Exception)
            { }
            var token = response.RootElement.GetProperty("token");
            if(token.ValueKind == JsonValueKind.Null)
            {
                Console.WriteLine("No token");
            }
            else
            {
                this.token = token.GetString();
            }
        }

        public Scan StartScan(Tool tool, string soft_hash, string hard_hash, string arguments)
        {
            if (this.token == null)
                this.Reauthenticate();
            Scan scan = new Scan();
            scan.arguments = arguments;
            scan.tool_hash = tool.hard_match_hash;
            scan.scan_hash = hard_hash;
            scan.scan_soft_hash = soft_hash;
            var response = this.PostRequest("/api/scan/start", JsonSerializer.Serialize<Scan>(scan));

            try
            {
                var error = response.RootElement.GetProperty("error");
                if (error.ValueKind != JsonValueKind.Null)
                {
                    Console.WriteLine("Couldn't start scan. Error:");
                    Console.WriteLine(error.GetString());
                    return null;
                }
            }
            catch (Exception)
            { }

            return scan;
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
                        this.Reauthenticate();
                        return false;
                    }
                    else
                        return true;
                }
            }
            catch (Exception) { }
            return true;
        }

        public void StopScan(Scan scan)
        {
            for (int x = 0; x < 2; x++)
            {
                var response = this.PostRequest("/api/scan/stop", JsonSerializer.Serialize<Scan>(scan));

                try
                {
                    if (!CheckErrorAndReauthenticate(response))
                        continue;
                    var error = response.RootElement.GetProperty("error");
                    
                    if (error.ValueKind != JsonValueKind.Null)
                    {
                        Console.WriteLine("Couldn't stop scan. Error:");
                        Console.WriteLine(error.GetString());
                    }
                    return;
                }
                catch (Exception)
                { }
            }
        }

        public Subject CreateSubject(string name, string path, string soft_hash, string hard_hash, string version = "1.0", string host=null)
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

            for (int x = 0; x < 2; x++)
            {
                var response = this.PostRequest("/api/subject/create", JsonSerializer.Serialize<Subject>(subj));

                try
                {
                    if (!CheckErrorAndReauthenticate(response))
                        continue;
                    var error = response.RootElement.GetProperty("error");
                    if (error.ValueKind != JsonValueKind.Null)
                    {
                        Console.WriteLine("Couldn't create subject. Error:");
                        Console.WriteLine(error.GetString());
                    }
                }
                catch (Exception)
                { }
                return subj;
            }
            return subj;
        }

        public void SubmitResult(Scan scan, Subject subj, Result result)
        {
            result.scan_hash = scan.scan_hash;
            result.subject_hash = subj.hash;

            for (int x = 0; x < 2; x++)
            {
                var response = this.PostRequest("/api/scan/submit", JsonSerializer.Serialize<Result>(result));
                try
                {
                    if (!CheckErrorAndReauthenticate(response))
                        continue;
                    var error = response.RootElement.GetProperty("error");
                    if (error.ValueKind != JsonValueKind.Null)
                    {
                        Console.WriteLine("Couldn't submit result. Error:");
                        Console.WriteLine(error.GetString());
                    }
                }
                catch (Exception)
                { }
                return;
            }
        }

        public Tool CreateTool(string name, string description, string version="1.0")
        {
            if (this.token == null)
                this.Reauthenticate();
            Tool tool = new Tool();
            tool.name = name;
            tool.description = description;
            tool.soft_match_hash = HashHelper.GetSHA256String(tool.name);
            tool.hard_match_hash = HashHelper.GetSHA256StringFromFile(System.Reflection.Assembly.GetEntryAssembly().Location);
            tool.version = version;
            string json = JsonSerializer.Serialize(tool);
            var response = this.PostRequest("/api/tool/register", json);

            try
            {
                var error = response.RootElement.GetProperty("error");
                if (error.ValueKind != JsonValueKind.Null)
                {
                    Console.WriteLine("Couldn't register tool. Error:");
                    Console.WriteLine(error.GetString());
                    return null;
                }
            }
            catch (Exception)
            { }

            return tool;
        }
    }
}
