using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AppVeyor
{

    public class BuildWorkerApi
    {
        public Uri UrlEndPoint { get; set; }
        private static JsonSerializerSettings jsonSettings;

        static BuildWorkerApi()
        {
            jsonSettings =  new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore };
            jsonSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
        }

        public BuildWorkerApi(Uri apiUrl)
        {
            this.UrlEndPoint = apiUrl;
        }

        public BuildWorkerApi()
        {
            this.UrlEndPoint = null;
        }

        public async Task AddMessageAsync(string message, MessageCategory category = MessageCategory.Information, string details = null)
        {
            try
            {
                var body = new { message = message, category = category, details = details };
                await PostToApi("api/build/messages", body);
            }
            catch { }
        }

        public async Task AddCompilationMessageAsync(string message, MessageCategory category = MessageCategory.Information, string details = null, string filename = null, int line = 0, int column = 0, string projectName = null, string projectFileName = null)
        {
            try
            {
                var body = new
                {
                    message = message,
                    category = category,
                    details = details,
                    fileName = filename,
                    line = line,
                    column = column,
                    projectName = projectName,
                    projectFileName = projectFileName
                };
                await PostToApi("api/build/compilationmessages", body);
            }
            catch { }
        }

        public async Task AddEnvironmentVariableAsync(string name, string value)
        {
            try
            {
                var body = new
                {
                    name = name,
                    value = value
                };
                await PostToApi("api/build/variables", body);
            }
            catch { }
        }

        public async Task RecordTestAsync(string testName, string testFramework = null, string filename = null, TestOutcome outcome = TestOutcome.None, long durationInMilliseconds = 0, string errorMessage = null, string errorStackTrace = null, string stdOut = null, string stdErr = null)
        {
            try
            {
                var body = new
                {
                    testName = testName,
                    testFramework = testFramework,
                    fileName = filename,
                    outcome = outcome,
                    durationMilliseconds = durationInMilliseconds,
                    ErrorMessage = errorMessage,
                    ErrorStackTrace = errorStackTrace,
                    StdOut = stdOut,
                    StdErr = stdErr
                };
                await PostToApi("api/tests", body);
            }
            catch { }
        }

        public async Task PushArtifactAsync(string path, string fileName, string name, ArtifactType type)
        {
            try
            {

                var body = new
                {
                    path = path,
                    fileName = fileName,
                    name = name,
                    type = type
                };
                await PostToApi("api/artifacts", body);
            }
            catch { }
        }

        private async Task PostToApi(string path, object body)
        {
            if (this.UrlEndPoint == null) return;


            var targetUrl = new Uri(UrlEndPoint, path);
            var request = HttpWebRequest.CreateHttp(targetUrl);
            request.Method = "POST";

            if (null != body)
            {
                using (var writer = new StreamWriter(await request.GetRequestStreamAsync()))
                {
                    var bodyString = JsonConvert.SerializeObject(body, jsonSettings);
                    await writer.WriteAsync(bodyString);
                    await writer.FlushAsync();
                }
            }

            HttpWebResponse httpResponse = null;
            try
            {
                var response = await request.GetResponseAsync();
                httpResponse = response as HttpWebResponse;
            }
            catch (WebException webEx)
            {
                httpResponse = webEx.Response as HttpWebResponse;
            }

            if (null != httpResponse)
            {
                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine("BuildWorkerApi response was {0}: {1}", (int)httpResponse.StatusCode, httpResponse.StatusDescription);
                }
                httpResponse.Dispose();
            }
        }
    }


    public enum MessageCategory
    {
        Information,
        Warning,
        Error
    }

    public enum TestOutcome
    {
        None,
        Running,
        Passed,
        Failed,
        Ignored,
        Skipped,
        Inconclusive,
        NotFound,
        Cancelled,
        NotRunnable
    }

    public enum ArtifactType
    {
        Auto,
        WebDeployPackage
    }
}
