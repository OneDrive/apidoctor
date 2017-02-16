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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using ApiDocs.Validation.Error;
    using Newtonsoft.Json;

    public class HttpResponse
    {
        private readonly string[] headersForPartialMatch = { "content-type" };

        public string HttpVersion { get; set; }
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public WebHeaderCollection Headers { get; set; }
        public string Body { get; set; }
        public TimeSpan CallDuration { get; set; }
        public bool WasSuccessful { get { return this.StatusCode >= 200 && this.StatusCode < 300; } }
        public string ContentType { get { return this.Headers["content-type"]; } }
        public int RetryCount { get; set; }

        public static async Task<HttpResponse> ResponseFromHttpWebResponseAsync(HttpWebRequest request)
        {
            long requestStartTicks = DateTime.UtcNow.Ticks;

            HttpWebResponse webResponse;
            try
            {
                webResponse = (HttpWebResponse)await request.GetResponseAsync();
            }
            catch (WebException webex)
            {
                webResponse = webex.Response as HttpWebResponse;
                if (null == webResponse)
                {
                    return new HttpResponse {
                        StatusCode = 504,
                        StatusMessage = "HttpResponseFailure " + webex.Message
                    };
                }
            }
            catch (Exception ex)
            {
                return new HttpResponse {
                    StatusCode = 504,
                    StatusMessage = "HttpResponseFailure " + ex.Message
                };
            }

            long requestFinalTicks = DateTime.UtcNow.Ticks;
            TimeSpan callDuration = new TimeSpan(requestFinalTicks - requestStartTicks);

            var response = await ConvertToResponseAsync(webResponse, callDuration);
            return response;
        }

        private static async Task<HttpResponse> ConvertToResponseAsync(HttpWebResponse webresp, TimeSpan? duration = null )
        {
            var resp = new HttpResponse()
            {
                HttpVersion = string.Format("HTTP/{0}.{1}", webresp.ProtocolVersion.Major, webresp.ProtocolVersion.Minor),
                StatusCode = (int)webresp.StatusCode,
                StatusMessage = webresp.StatusDescription,
                Headers = webresp.Headers
            };

            if (duration.HasValue)
            {
                resp.CallDuration = duration.Value;
            }

            var responseStream = webresp.GetResponseStream();
            if (null != responseStream)
            {
                using (var reader = new StreamReader(responseStream))
                {
                    resp.Body = await reader.ReadToEndAsync();
                }
            }
            return resp;
        }

        internal async Task WriteToStreamAsync(Stream responseStream)
        {
            using (TextWriter writer = new StreamWriter(responseStream, new UTF8Encoding(false), 4096, true))
            {
                await writer.WriteAsync(FullText(prettyPrintBody: false, useChunkedEncoding: true));
                await writer.FlushAsync();
            }
        }

        public string FullText(bool prettyPrintBody = true, bool useChunkedEncoding = false)
        {
            return this.FormatFullResponse(this.Body, prettyPrintBody, useChunkedEncoding);
        }

        public string FormatFullResponse(string body, bool prettyPrintJson, bool useChunkedEncodingIfPresent = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} {1} {2}", this.HttpVersion, this.StatusCode, this.StatusMessage);
            sb.AppendLine();


            bool isChunkedEncoding = false;
            bool isJson = false;
            if (null != this.Headers)
            {
                foreach (var key in this.Headers.AllKeys)
                {
                    sb.AppendFormat("{0}: {1}", key, this.Headers[key]);
                    sb.AppendLine();

                    if (key.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                    {
                        isJson = this.Headers[key].StartsWith("application/json");
                    }
                    if (key.Equals("transfer-encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        isChunkedEncoding = this.Headers[key].StartsWith("chunked");
                    }
                }
                sb.AppendLine();
            }

            // Pretty print JSON if that's what the body is
            if (isJson && prettyPrintJson)
            {
                try
                {
                    var jsonObject = JsonConvert.DeserializeObject(body);
                    sb.Append(JsonConvert.SerializeObject(jsonObject, Formatting.Indented));
                }
                catch (Exception)
                {
                    sb.Append(body);
                }
            }
            else if (useChunkedEncodingIfPresent && isChunkedEncoding)
            {
                // Write out the response body using chunked encoding semantics
                sb.AppendFormat("{0:x}", System.Text.Encoding.UTF8.GetByteCount(body));
                sb.AppendLine();
                sb.Append(body);
                sb.AppendLine();
                sb.AppendLine("0");
                sb.AppendLine();
            }
            else
            {
                sb.Append(body);
            }

            return sb.ToString();
        }
        

        /// <summary>
        /// Compares the headers in the current HttpResponse instance with the headers in the actual response
        /// and returns a set of validation errors for any important differences.
        /// </summary>
        /// <param name="actual">The actual response that is compared with the expected (current object) response.</param>
        /// <param name="errors">An array of errors that were generated by the validation</param>
        /// <returns>A boolean that is True if the response is valid (no significant errors or warnings detected) or False if errors exist.</returns>
        public bool ValidateResponseHeaders(HttpResponse actual, out ValidationError[] errors, IEnumerable<int> allowedStatusCodes = null)
        {
            if (actual == null) throw new ArgumentNullException("actual");
            
            List<ValidationError> errorList = new List<ValidationError>();

            // Check the HTTP status code
            bool usedAllowedStatusCode = false;
            if (this.StatusCode != actual.StatusCode)
            {
                if (null != allowedStatusCodes && allowedStatusCodes.Contains(actual.StatusCode))
                {
                    errorList.Add(new ValidationWarning(ValidationErrorCode.HttpStatusCodeDifferent, null, "Response uses an allowed status code that was different than the documentation indcaites: Expected status code: {0}, received: {1}.", this.StatusCode, actual.StatusCode));
                    usedAllowedStatusCode = true;
                }
                else
                {
                    errorList.Add(new ValidationError(ValidationErrorCode.HttpStatusCodeDifferent, null, "Expected status code: {0}, received: {1}.", this.StatusCode, actual.StatusCode));
                }
            }

            // Check the HTTP status message
            if (this.StatusMessage != actual.StatusMessage)
            {
                errorList.Add(ValidationError.CreateError(usedAllowedStatusCode, ValidationErrorCode.HttpStatusMessageDifferent, null, "Expected status message {0}, received: {1}.", this.StatusMessage, actual.StatusMessage));
            }

            // Check to see that expected headers were found in the response (headers listed in the 
            // expected response must be present in the actual response and contain the same value).
            List<string> otherResponseHeaderKeys = new List<string>();
            if (actual.Headers != null)
            {
                otherResponseHeaderKeys.AddRange(actual.Headers.AllKeys);
            }

            var comparer = new HeaderNameComparer();
            foreach(var expectedHeader in this.Headers.AllKeys)
            {
                if (!otherResponseHeaderKeys.Contains(expectedHeader, comparer))
                {
                    errorList.Add(new ValidationError(ValidationErrorCode.HttpRequiredHeaderMissing, null, "Response is missing header expected header: {0}.", expectedHeader));
                }
                else if (this.headersForPartialMatch.Contains(expectedHeader.ToLower()))
                {
                    var expectedValue = this.Headers[expectedHeader];
                    var actualValue = actual.Headers[expectedHeader];

                    if (!actualValue.ToLower().StartsWith(expectedValue))
                    {
                        errorList.Add(new ValidationError(ValidationErrorCode.HttpHeaderValueDifferent, null, "Header '{0}' has unexpected value '{1}' (expected {2})", expectedHeader, actualValue, expectedValue));
                    }
                }
            }

            errors = errorList.ToArray();
            return !errors.Any(x => x.IsError || x.IsWarning);
        }

        public bool IsMatchingContentType(string expectedContentType)
        {
            return HttpRequest.ContentTypeMatchesValue(expectedContentType, this.ContentType);
        }

        class HeaderNameComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return String.Equals(x, y, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                if (null != obj) return obj.GetHashCode();
                return 0;
            }
        }

    }
}
