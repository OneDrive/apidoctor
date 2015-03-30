namespace OneDrive.ApiDocumentation.Validation.Http
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
            Headers = new WebHeaderCollection();
        }

        public string Method { get; set; }
        public string Url { get; set; }
        public string Body { get; set; }

        public string Accept
        {
            get
            {
                return Headers["Accept"];
            }
            set
            {
                Headers["Accept"] = value;
            }
        }

        public string Authorization
        {
            get { return Headers["Authorization"]; }
            set { Headers["Authorization"] = value; }
        }

        public string ContentType
        {
            get { return Headers["content-type"]; }
            set { Headers["content-type"] = value; }
        }

        public bool IsMatchingContentType(string expectedContentType)
        {
            if (string.IsNullOrEmpty(ContentType))
                return false;

            string[] contentTypeParts = ContentType.Split(new char[] { ';' });
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

            HttpWebRequest request = HttpWebRequest.CreateHttp(effectiveUrl);
            request.AllowAutoRedirect = false;
            request.Method = Method;
            request.KeepAlive = true;
            request.ServicePoint.Expect100Continue = false;

            foreach (var key in Headers.AllKeys)
            {
                switch (key.ToLower())
                {
                    case "accept":
                        request.Accept = Headers[key];
                        break;
                    case "content-type":
                        request.ContentType = Headers[key];
                        break;
                    case "content-length":
                        // Don't set these headers
                        break;
                    case "if-modified-since":
                        if (Headers[key].Equals("current_time"))
                        {
                            request.IfModifiedSince = DateTime.Now;
                        }
                        else
                        {
                            request.IfModifiedSince = Convert.ToDateTime(Headers[key]);
                        }
                        break;
                    default:
                        request.Headers.Add(key, Headers[key]);
                        break;
                }
                
            }

            if (Body != null)
            {
                using (var stream = request.GetRequestStream())
                {
                    var writer = new StreamWriter(stream);
                    writer.Write(Body);
                    writer.Flush();
                }
            }

            return request;
        }

        public string FullHttpText()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Method);
            sb.Append(" ");
            sb.Append(Url);
            sb.Append(" ");
            sb.AppendLine("HTTP/1.1");
            foreach (var header in Headers.AllKeys)
            {
                sb.AppendFormat("{0}: {1}", header, Headers[header]);
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.Append(Body);
            
            return sb.ToString();
        }
    }
}
