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
    using System.Collections.Generic;

    public class HttpParser
    {
        
        
        /// <summary>
        /// Converts a raw HTTP request into an HttpWebRequest instance.
        /// </summary>
        /// <param name="requestString"></param>
        /// <returns></returns>
        public HttpRequest ParseHttpRequest(string requestString)
        {
            if (string.IsNullOrWhiteSpace(requestString))
            {
                throw new ArgumentException("requestString was empty or whitespace only. Not a valid HTTP request.");
            }

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
                            throw new HttpParserRequestException("RequestString does not contain a proper HTTP request first line.");
                        if (components.Length > 3)
                            throw new HttpParserRequestException("RequestString does not contain a proper HTTP request first line (more than the expected 3 components)");

                        if (components[0].StartsWith("HTTP/"))
                            throw new HttpParserRequestException("RequestString contains an HTTP response.");

                        request.Method = components[0];
                        request.Url = components[1];

                        if (components.Length >= 3)
                        {
                            request.HttpVersion = components[2];
                        }
                        else
                        {
                            request.HttpVersion = "HTTP/1.1";
                        }

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

        /// <summary>
        /// Convert a raw HTTP response into an HttpResponse instance.
        /// </summary>
        /// <param name="responseString"></param>
        /// <returns></returns>
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
                        if (split < 1) throw new ArgumentException($"Request contains an invalid header definition: \"{line}\". Missing whitespace between the headers and body?");

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


        /// <summary>
        /// Take query string formatted input (a=1&b=2) and return a dictionary
        /// of values.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static System.Collections.Specialized.NameValueCollection ParseQueryString(string input)
        {
            if (input != null && input[0] != '?')
            {
                input = "?" + input;
            }

            var output = System.Web.HttpUtility.ParseQueryString(input);
            return output;
        }

        private enum ParserMode
        {
            FirstLine,
            Headers,
            Body
        }
    }
}
