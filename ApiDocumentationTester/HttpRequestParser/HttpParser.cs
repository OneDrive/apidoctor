using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ApiDocumentationTester.HttpRequestParser
{
    internal class HttpParser
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
                        if (components.Length < 2) throw new ArgumentException("requestString does not contain a proper HTTP request first line.");

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
                        int split = line.IndexOf(": ");
                        if (split < 1) throw new ArgumentException("requestString contains an invalid header definition");

                        var headerName = line.Substring(0, split);
                        var headerValue = line.Substring(split + 1);
                        request.Headers.Add(headerName, headerValue);

                        break;

                    case ParserMode.Body:
                        request.Body = line + Environment.NewLine + reader.ReadToEnd();
                        break;
                }
            }

            return request;
        }

        private enum ParserMode
        {
            FirstLine,
            Headers,
            Body
        }
    }
}
