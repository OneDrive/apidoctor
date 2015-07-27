namespace ApiDocs.Validation.Http
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Linq;

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

            string[] contentTypeParts = this.ContentType.Split(';');
            return contentTypeParts.Length > 0 && contentTypeParts[0].Equals(expectedContentType, StringComparison.OrdinalIgnoreCase);
        }

        public WebHeaderCollection Headers { get; private set; }

        public static readonly string[] IgnoredHeaders = { "content-length" };

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
                if (IgnoredHeaders.Contains(key))
                    continue;
                
                if (WebHeaderCollection.IsRestricted(key))
                {
                    switch (key.ToLower())
                    {
                        case "accept":
                            request.Accept = this.Headers[key];
                            break;
                        case "content-type":
                            request.ContentType = this.Headers[key];
                            break;
                        case "connection":
                            request.Connection = this.Headers[key];
                            break;
                        case "date":
                            request.Date = DateTime.Parse(this.Headers[key]);
                            break;
                        case "expect":
                            request.Expect = this.Headers[key];
                            break;
                        case "host":
                            request.Host = this.Headers[key];
                            break;
                        case "if-modified-since":
                            request.IfModifiedSince = this.Headers[key].Equals("current_time") ? DateTime.Now : Convert.ToDateTime(this.Headers[key]);
                            break;
                        case "range":
                            AddRangeHeader(request, this.Headers[key]);
                            break;
                        case "referer":
                            request.Referer = this.Headers[key];
                            break;
                        case "transfer-encoding":
                            request.TransferEncoding = this.Headers[key];
                            break;
                        case "user-agent":
                            request.UserAgent = this.Headers[key];
                            break;
                        case "proxy-connection":
                        default:
                            throw new NotSupportedException(string.Format("Header {0} is a restricted header and is not supported.", key));
                    }
                }
                else
                {
                    request.Headers.Add(key, this.Headers[key]);
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

        /// <summary>
        /// Only handles single range requests that start with "bytes=".
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="value">Value.</param>
        private void AddRangeHeader(HttpWebRequest request, string value)
        {
            string specifier = "bytes";

            if (!value.StartsWith("bytes="))
            {
                throw new ArgumentException("value isn't a properly formated byte range header");
            }

            var range = value.Substring(specifier.Length + 1);
            var split = range.Split('-');
            if (split.Length != 2)
            {
                throw new ArgumentException("value isn't a properly formatted byte range header. Missing start or end range");
            }

            // -5000 => Last 5000 bytes of the file
            if (split[0].Length == 0 && split[1].Length != 0)
            {
                request.AddRange(-1 * Convert.ToInt64(split[1]), Convert.ToInt64(split[1]));
            }
            // 5000- => Get from 5000 byte to end of file
            else if (split[0].Length != 0 && split[1].Length == 0)
            {
                request.AddRange(Convert.ToInt64(split[0]));
            }
            // 5000-5006 => Get from 5000 - 5006
            else
            {
                request.AddRange(Convert.ToInt64(split[0]), Convert.ToInt64(split[1]));
            }
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
            var response = await HttpResponse.ResponseFromHttpWebResponseAsync(webRequest);

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

        public static bool IsRetryableError(HttpResponse response)
        {
            return (response.StatusCode >= 500 && response.StatusCode < 600);
        }
    }
}
