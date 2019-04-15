/*
 * API Doctor
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

namespace ApiDoctor.Validation.Http
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Web;

    public class HttpParser
    {
        /// <summary>
        ///     Converts a raw HTTP request into an HttpWebRequest instance.
        /// </summary>
        /// <param name="requestString"></param>
        /// <returns></returns>
        public HttpRequest ParseHttpRequest(string requestString)
        {
            if (string.IsNullOrWhiteSpace(requestString))
            {
                throw new ArgumentException("requestString was empty or whitespace only. Not a valid HTTP request.");
            }

            var mode = ParserMode.FirstLine;
            var request = new HttpRequest();
            var reader = new StringReader(requestString);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                switch (mode)
                {
                    case ParserMode.FirstLine:
                        //FirstLines in HttpRequests can be of the formats special case for the Odata Url with equality operators. 
                        //GET https://graph.microsoft.com/beta/accessReviews('2b83cc42-09db-46f6-8c6e-16fec466a82d')
                        //GET HTTP/1.1  https://graph.microsoft.com/beta/accessReviews('2b83cc42-09db-46f6-8c6e-16fec466a82d')
                        //GET https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'
                        //Removing the exception allows us to parse all three types of url.
                        var components = line.Split(' ');

                        if (components.Length < 2)
                            throw new HttpParserRequestException("RequestString does not contain a proper HTTP request first line.");
                        if (components[0].StartsWith("HTTP/"))
                            throw new HttpParserRequestException("RequestString contains an HTTP response.");

                        string url;
                        var httpVersion = components[1].StartsWith("HTTP/") ? components[1] : "HTTP/1.1";
                        if (components.Length > 3)
                        {
                            //Assume Odata Uri in the form https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'
                            var uri = new StringBuilder(components.Length);
                            for (var i = 1; i < components.Length; i++)
                            {
                                uri.Append(components[i]);
                                uri.Append(' ');
                            }
                            //Trim because some urls have unnecessary spaces after the url, leading to a mismatch downstream.
                            url = uri.ToString().Trim();
                        }
                        else
                        {
                            url = components[1];
                        }

                        request.Method = components[0];
                        request.Url = url;
                        request.HttpVersion = httpVersion;

                        mode = ParserMode.Headers;
                        break;

                    case ParserMode.Headers:
                        if (string.IsNullOrEmpty(line))
                        {
                            mode = ParserMode.Body;
                            continue;
                        }

                        // Parse each header
                        var split = line.IndexOf(": ", StringComparison.Ordinal);
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return request;
        }

        /// <summary>
        ///     Convert a raw HTTP response into an HttpResponse instance.
        /// </summary>
        /// <param name="responseString"></param>
        /// <returns></returns>
        public HttpResponse ParseHttpResponse(string responseString)
        {
            var reader = new StringReader(responseString);
            string line;
            var mode = ParserMode.FirstLine;

            var response = new HttpResponse { Headers = new WebHeaderCollection() };

            while ((line = reader.ReadLine()) != null)
                switch (mode)
                {
                    case ParserMode.FirstLine:
                        var components = line.Split(' ');
                        if (components.Length < 3)
                            throw new ArgumentException(
                                "responseString does not contain a proper HTTP request first line.");

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
                        var split = line.IndexOf(": ", StringComparison.Ordinal);
                        if (split < 1)
                            throw new ArgumentException(
                                $"Request contains an invalid header definition: \"{line}\". Missing whitespace between the headers and body?");

                        var headerName = line.Substring(0, split);
                        var headerValue = line.Substring(split + 1);
                        response.Headers.Add(headerName, headerValue);

                        break;

                    case ParserMode.Body:
                        response.Body = line + Environment.NewLine + reader.ReadToEnd();
                        break;
                }

            return response;
        }


        /// <summary>
        ///     Take query string formatted input (a=1&b=2) and return a dictionary
        ///     of values.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static NameValueCollection ParseQueryString(string input)
        {
            if (input != null && input[0] != '?') input = "?" + input;

            var output = HttpUtility.ParseQueryString(input);
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