namespace ApiDocs.Validation.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    public class HttpRequest
    {
        public HttpRequest()
        {
            this.Headers = new WebHeaderCollection();
        }

        public string Method { get; set; }
        public string Url { get; set; }
        public string Body { get; set; }

        public string Accept
        {
            get
            {
                return this.Headers["Accept"];
            }
            set
            {
                this.Headers["Accept"] = value;
            }
        }

        public string Authorization
        {
            get { return this.Headers["Authorization"]; }
            set { this.Headers["Authorization"] = value; }
        }

        public string ContentType
        {
            get { return this.Headers["content-type"]; }
            set { this.Headers["content-type"] = value; }
        }

        public bool IsMatchingContentType(string expectedContentType)
        {
            if (string.IsNullOrEmpty(this.ContentType))
                return false;

            string[] contentTypeParts = this.ContentType.Split(new char[] { ';' });
            return contentTypeParts.Length > 0 && contentTypeParts[0].Equals(expectedContentType, StringComparison.OrdinalIgnoreCase);
        }

        public WebHeaderCollection Headers { get; private set; }

        public HttpWebRequest PrepareHttpWebRequest(string baseUrl)
        {
            var effectiveUrl = baseUrl;
            if (this.Url.StartsWith(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                effectiveUrl = this.Url;
            }
            else
            {
                effectiveUrl += this.Url;
            }

            HttpWebRequest request = WebRequest.CreateHttp(effectiveUrl);
            request.AllowAutoRedirect = false;
            request.Method = this.Method;
            request.KeepAlive = true;
            request.ServicePoint.Expect100Continue = false;

            foreach (var key in this.Headers.AllKeys)
            {
                switch (key.ToLower())
                {
                    case "accept":
                        request.Accept = this.Headers[key];
                        break;
                    case "content-type":
                        request.ContentType = this.Headers[key];
                        break;
                    case "content-length":
                        // Don't set these headers
                        break;
                    case "if-modified-since":
                        request.IfModifiedSince = this.Headers[key].Equals("current_time") ? DateTime.Now : Convert.ToDateTime(this.Headers[key]);
                        break;
                    default:
                        request.Headers.Add(key, this.Headers[key]);
                        break;
                }
            }

            if (this.Body != null)
            {
                using (var stream = request.GetRequestStream())
                {
                    var writer = new StreamWriter(stream);
                    writer.Write(this.Body);
                    writer.Flush();
                }
            }

            if (null != ValidationConfig.AdditionalHttpHeaders)
            {
                foreach (var header in ValidationConfig.AdditionalHttpHeaders)
                {
                    request.Headers.Add(header);
                }
            }


            return request;
        }

        public string FullHttpText()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.Method);
            sb.Append(" ");
            sb.Append(this.Url);
            sb.Append(" ");
            sb.AppendLine("HTTP/1.1");
            foreach (string header in this.Headers.AllKeys)
            {
                if (header.Equals("authorization", StringComparison.OrdinalIgnoreCase) && this.Headers[header].Length > 30)
                {
                    sb.AppendFormat("{0}: {1}...", header, this.Headers[header].Substring(0, 30));
                }
                else
                {
                    sb.AppendFormat("{0}: {1}", header, this.Headers[header]);
                }

                sb.AppendLine();
            }
            sb.AppendLine();
            sb.Append(this.Body);
            
            return sb.ToString();
        }


        public async Task<HttpResponse> GetResponseAsync(string baseUrl, int retryCount = 0)
        {
            var webRequest = this.PrepareHttpWebRequest(baseUrl);
            var response = await Http.HttpResponse.ResponseFromHttpWebResponseAsync(webRequest);

            if (IsRetryableError(response))
            {
                if (retryCount < ValidationConfig.RetryAttemptsOnServiceUnavailableResponse)
                {
                    // Do "full jitter" back off
                    await BackoffHelper.Default.FullJitterBackoffDelayAsync(retryCount);

                    // Try the request again
                    return await this.GetResponseAsync(baseUrl, retryCount + 1);
                }
            }
            response.RetryCount = retryCount;
            return response;
        }

        public static bool IsRetryableError(Http.HttpResponse response)
        {
            return (response.StatusCode >= 500 && response.StatusCode < 600);
        }
    }
}
