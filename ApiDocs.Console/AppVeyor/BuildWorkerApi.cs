namespace ApiDocs.ConsoleApp.AppVeyor
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class BuildWorkerApi
    {
        public Uri UrlEndPoint { get; set; }
        private static readonly JsonSerializerSettings CachedJsonSettings;

        static BuildWorkerApi()
        {
            CachedJsonSettings =  new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore };
            CachedJsonSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
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
                await this.PostToApiAsync("api/build/messages", body);
            }
            catch
            {
                // ignored
            }
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
                await this.PostToApiAsync("api/build/compilationmessages", body);
            }
            catch
            {
                // ignored
            }
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
                await this.PostToApiAsync("api/build/variables", body);
            }
            catch
            {
                // ignored
            }
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
                await this.PostToApiAsync("api/tests", body);
            }
            catch
            {
                // ignored
            }
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
                await this.PostToApiAsync("api/artifacts", body);
            }
            catch
            {
                // ignored
            }
        }

        private async Task PostToApiAsync(string path, object body)
        {
            if (this.UrlEndPoint == null) return;


            var targetUrl = new Uri(this.UrlEndPoint, path);
            var request = WebRequest.CreateHttp(targetUrl);
            request.Method = "POST";
            request.ContentType = "application/json";

            if (null != body)
            {
                using (var writer = new StreamWriter(await request.GetRequestStreamAsync()))
                {
                    var bodyString = JsonConvert.SerializeObject(body, CachedJsonSettings);
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
                if (httpResponse.StatusCode != HttpStatusCode.OK && httpResponse.StatusCode != HttpStatusCode.NoContent)
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
