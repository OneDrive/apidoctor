namespace ApiDocs.Validation.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;

    public class HttpParser
    {
        
        
        /// <summary>
        /// Converts a raw HTTP request into an HttpWebRequest instance
        /// </summary>
        /// <param name="requestString"></param>
        /// <returns></returns>
        public HttpRequest ParseHttpRequest(string requestString)
        {
            StringReader reader = new StringReader(requestString);
            string line;
            ParserMode mode = ParserMode.FirstLine;

            HttpRequest request = new HttpRequest();

            while( (line = reader.ReadLine()) != null)
            {
                switch (mode)
                {
                    case ParserMode.FirstLine:
                        var components = line.Split(' ');
                        if (components.Length < 2) 
                            throw new ArgumentException("requestString does not contain a proper HTTP request first line.");

                        request.Method = components[0];
                        request.Url = components[1];

                        mode = ParserMode.Headers;
                        break;

                    case ParserMode.Headers:
                        if (string.IsNullOrEmpty(line))
                        {
                            mode = ParserMode.Body;
                            continue;
                        }

                        // Parse each header
                        int split = line.IndexOf(": ", StringComparison.Ordinal);
                        if (split < 1) 
                            throw new ArgumentException("requestString contains an invalid header definition");

                        var headerName = line.Substring(0, split);
                        var headerValue = line.Substring(split + 1);
                        request.Headers.Add(headerName, headerValue);

                        break;

                    case ParserMode.Body:
                        var restOfBody = reader.ReadToEnd() ?? string.Empty;

                        // normalize line endings to CRLF, which is required for headers, etc.
                        restOfBody = restOfBody.Replace("\r\n", "\n").Replace("\n", "\r\n");
                        request.Body = string.Concat(line, "\r\n", restOfBody);
                        break;
                }
            }

            return request;
        }

        public HttpResponse ParseHttpResponse(string responseString)
        {
            StringReader reader = new StringReader(responseString);
            string line;
            ParserMode mode = ParserMode.FirstLine;

            HttpResponse response = new HttpResponse() { Headers = new WebHeaderCollection() };

            while ((line = reader.ReadLine()) != null)
            {
                switch (mode)
                {
                    case ParserMode.FirstLine:
                        var components = line.Split(' ');
                        if (components.Length < 3) throw new ArgumentException("responseString does not contain a proper HTTP request first line.");

                        response.HttpVersion = components[0];
                        response.StatusCode = int.Parse(components[1]);
                        response.StatusMessage = components.ComponentsJoinedByString(" ", 2);

                        mode = ParserMode.Headers;
                        break;

                    case ParserMode.Headers:
                        if (string.IsNullOrEmpty(line))
                        {
                            mode = ParserMode.Body;
                            continue;
                        }

                        // Parse each header
                        int split = line.IndexOf(": ", StringComparison.Ordinal);
                        if (split < 1) throw new ArgumentException("requestString contains an invalid header definition");

                        var headerName = line.Substring(0, split);
                        var headerValue = line.Substring(split + 1);
                        response.Headers.Add(headerName, headerValue);

                        break;

                    case ParserMode.Body:
                        response.Body = line + Environment.NewLine + reader.ReadToEnd();
                        break;
                }
            }

            return response;
        }

        private enum ParserMode
        {
            FirstLine,
            Headers,
            Body
        }
    }
}
