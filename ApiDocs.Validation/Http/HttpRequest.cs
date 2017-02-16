/*
 * Markdown Scanner
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDocs.Validation.Http
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Collections.Generic;
    using Params;

    public class HttpRequest
    {

        public static HttpLog.HttpLogGenerator HttpLogSession { get; set; }

        public HttpRequest()
        {
            this.Headers = new WebHeaderCollection();

        }

        public string Method { get; set; }
        public string Url { get; set; }
        public string Body { get; set; }
        public byte[] BodyBytes { get; set; }
        
        internal DateTimeOffset StartTime { get; set; }
        internal Uri LastEffectiveUri { get; set; }

        public string Accept
        {
            get { return this.Headers["Accept"]; }
            set { this.Headers["Accept"] = value; }
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

        public ICredentials Credentials { get; set; }

        public static bool ContentTypeMatchesValue(string expected, string headerValue)
        {
            if (string.IsNullOrEmpty(expected))
                return false;
            if (string.IsNullOrEmpty(headerValue))
                return false;

            string[] contentTypeParts = headerValue.Split(';');
            return contentTypeParts.Length > 0 && contentTypeParts[0].Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsMatchingContentType(string expectedContentType)
        {
            return ContentTypeMatchesValue(expectedContentType, this.ContentType);
        }

        public WebHeaderCollection Headers { get; private set; }

        public static readonly string[] IgnoredHeaders = { "content-length" };

        private Uri GenerateAbsoluteUrl(string baseUrl, bool forceBaseUrl = false)
        {
            if (forceBaseUrl && string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException(
                    "baseUrl",
                    "Cannot specify forceBaseUrl = true and not provide a baseUrl.");
            }

            Uri effectiveUrl;
            // See if this.Url is a relative URL or a fully qualified URL
            if (!Uri.TryCreate( forceBaseUrl ? baseUrl + this.Url : this.Url, UriKind.Absolute, out effectiveUrl))
            {
                // This correctly fails on Windows for relative urls, but fails
                // on platforms that allow a "/" in their file URIs (like Mac)
                return GenerateAbsoluteUrl(baseUrl, true);
            }

            if (effectiveUrl.Scheme != "https" && effectiveUrl.Scheme != "http")
            {
                if (!forceBaseUrl)
                {
                    return GenerateAbsoluteUrl(baseUrl, true);
                }
                else
                {
                    throw new InvalidOperationException("Couldn't generate a valid absolute web URL using the baseUrl.");
                }
            }
            return effectiveUrl;
        }

        /// <summary>
        /// Serialize the HTTP request to an output stream
        /// </summary>
        /// <param name="requestStream"></param>
        /// <returns></returns>
        internal async Task WriteToStreamAsync(Stream requestStream)
        {
            using (TextWriter writer = new StreamWriter(requestStream, new UTF8Encoding(false), 4096, true))
            {
                await writer.WriteAsync(FullHttpText(showFullAuthorizationHeader: true));
                await writer.FlushAsync();
            }
        }

        public HttpWebRequest PrepareHttpWebRequest(string baseUrl)
        {
            var effectiveUrl = GenerateAbsoluteUrl(baseUrl);
            this.LastEffectiveUri = effectiveUrl;

            HttpWebRequest request = WebRequest.CreateHttp(effectiveUrl);
            request.AllowAutoRedirect = false;
            request.Method = this.Method;
            request.KeepAlive = true;
            request.ServicePoint.Expect100Continue = false;
            request.Credentials = this.Credentials;

            foreach (var key in this.Headers.AllKeys)
            {
                if (IgnoredHeaders.Contains(key.ToLower()))
                {
                    continue;
                }
                
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

            if (this.Body != null && this.BodyBytes == null)
            {
                using (var stream = request.GetRequestStream())
                {
                    var writer = new StreamWriter(stream);
                    writer.Write(this.Body);
                    writer.Flush();
                }
            }
            else if (this.Body == null && this.BodyBytes != null)
            {
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(this.BodyBytes, 0, this.BodyBytes.Length);
                }
            }
            else if (this.Body != null && this.BodyBytes != null)
            {
                throw new InvalidOperationException("Body and BodyBytes cannot both be set on the same request");
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

        public string FullHttpText(bool showFullAuthorizationHeader = false)
        {
            if (this.BodyBytes != null)
                Headers[HttpRequestHeader.ContentLength] = this.BodyBytes.Length.ToString();
            else if (this.Body != null)
                Headers[HttpRequestHeader.ContentLength] = Encoding.UTF8.GetByteCount(this.Body).ToString();

            StringBuilder sb = new StringBuilder();
            sb.Append(this.Method);
            sb.Append(" ");
            if (this.LastEffectiveUri != null)
            {
                sb.Append(this.LastEffectiveUri.ToString());
            }
            else
            {
                sb.Append(this.Url);
            }
            sb.Append(" ");
            sb.AppendLine("HTTP/1.1");
            foreach (string header in this.Headers.AllKeys)
            {
                if (!showFullAuthorizationHeader && header.Equals("authorization", StringComparison.OrdinalIgnoreCase) && this.Headers[header].Length > 30)
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
            if (this.BodyBytes != null)
            {
                sb.Append("... binary content ...");
            }
            else
            {
                sb.Append(this.Body);
            }

            return sb.ToString();
        }


        public async Task<HttpResponse> GetResponseAsync(IServiceAccount account, int retryCount = 0)
        {
            var baseUrl = account.BaseUrl;
            this.RewriteRequestBodyNamespaces(account);

            var webRequest = this.PrepareHttpWebRequest(baseUrl);
            this.StartTime = DateTimeOffset.UtcNow;

            HttpResponse response = await HttpResponse.ResponseFromHttpWebResponseAsync(webRequest);
            TimeSpan duration = DateTimeOffset.UtcNow.Subtract(this.StartTime);

            response.RewriteResponseBodyNamespaces(account);

            var logger = HttpRequest.HttpLogSession;
            if (null != logger)
            {
                await logger.RecordSessionAsync(this, response, duration);
            }

            if (ShouldRetryRequest(response))
            {
                if (retryCount < ValidationConfig.RetryAttemptsOnServiceUnavailableResponse)
                {
                    // Do "full jitter" back off
                    await BackoffHelper.Default.FullJitterBackoffDelayAsync(retryCount);

                    // Try the request again
                    return await this.GetResponseAsync(account, retryCount + 1);
                }
            }
            response.RetryCount = retryCount;
            return response;
        }

        public List<HttpStatusCode> RetryOnStatusCode { get; set; }
        public string HttpVersion { get; internal set; }

        public bool ShouldRetryRequest(HttpResponse response)
        {
            return (response.StatusCode >= 500 && response.StatusCode < 600) || (null != RetryOnStatusCode && RetryOnStatusCode.Contains((HttpStatusCode)response.StatusCode));
        }

        
    }
}
