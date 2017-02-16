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


namespace ApiDocs.Validation.MultipartMime
{
    using Tags;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;

    public class MultipartMimeContent
    {
        /// <summary>
        /// Create a new multipart mime document
        /// </summary>
        public MultipartMimeContent()
        {
            ContentType = new MimeContentType("multipart/related");
            Boundary = Guid.NewGuid().ToString("d");
            Parts = new List<MessagePart>();
        }

        /// <summary>
        /// Parse an existing multipart mime body into parts
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="body"></param>
        public MultipartMimeContent(string contentType, string body)
        {
            ContentType = new MimeContentType(contentType);
            Parts = ParseBodyIntoParts(body, Boundary);
        }

        public MultipartMimeContent(MimeContentType contentType, string body)
        {
            ContentType = contentType;
            Parts = ParseBodyIntoParts(body, Boundary);
        }

        /// <summary>
        /// The boundary identifier for the multipart message
        /// </summary>
        public string Boundary
        {
            get { return ContentType.Arguments["boundary"]?.TextBetweenCharacters('"'); }
            set { ContentType.Arguments["boundary"] = $"\"{value}\""; }
        }

        /// <summary>
        /// The full value of the content-type header for this message
        /// </summary>
        public MimeContentType ContentType { get; set; }

        /// <summary>
        /// The actual MIME type for the multipart mime message, usually multipart/related
        /// </summary>
        public string MultipartType
        {
            get { return ContentType.MimeType; }
            set { ContentType.MimeType = value; }
        }


        public MessagePart PartWithId(string id)
        {
            return (from p in Parts where p.Id == id select p).FirstOrDefault();
        }

        public IEnumerable<MessagePart> PartsWithContentType(string contentType)
        {
            return (from p in Parts where p.ContentType.MimeType == contentType select p);
        }

        private static List<MessagePart> ParseBodyIntoParts(string body, string boundary)
        {
            var parts = new List<MessagePart>();

            StringReader reader = new StringReader(body);
            string nextLine = reader.ReadLine();
            StringBuilder partbody = new StringBuilder();
            while(nextLine != null)
            {
                if (nextLine.Equals($"--{boundary}"))
                {
                    // A new message part is starting
                    if (partbody.Length > 0)
                    {
                        // Need to remove the last empty line from partbody, because it was actually part of the signal that we're at the end
                        partbody.Length = partbody.Length - (2 * Environment.NewLine.Length);
                        parts.Add(MessagePart.Deserialize(partbody.ToString()));
                        partbody.Length = 0;
                    }
                }
                else if (nextLine.Equals($"--{boundary}--"))
                {
                    // End of the message has been reached
                    if (partbody.Length > 0)
                    {
                        // Need to remove the last empty line from partbody, because it was actually part of the signal that we're at the end
                        partbody.Length = partbody.Length - (2 * Environment.NewLine.Length);
                        parts.Add(MessagePart.Deserialize(partbody.ToString()));
                    }
                    break;
                }
                else
                {
                    partbody.AppendLine(nextLine);
                }
                nextLine = reader.ReadLine();
            }

            return parts;
        }

        public IList<MessagePart> Parts { get; private set;  }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var part in Parts)
            {
                sb.AppendLine($"--{Boundary}");
                sb.AppendLine(part.ToString());
            }
            sb.AppendLine($"--{Boundary}--");
            return sb.ToString();
        }

    }

    public class MessagePart
    {
        private const string ContentTypeHeader = "Content-Type";
        private const string ContentIdHeader = "Content-ID";

        public MimeContentType ContentType
        {
            get { return new MimeContentType(Headers[ContentTypeHeader]); }
            set { Headers[ContentTypeHeader] = value.ToString(); }
        }

        public string Id
        {
            get { return Headers[ContentIdHeader]; }
            set { Headers[ContentIdHeader] = value; }
        }
        public Dictionary<string, string> Headers { get; private set; }
        public string Body { get; set; }

        public MessagePart()
        {
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// Convert this MessagePart into a wire-format MIME message part, combinding headers + Body
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var header in Headers)
            {
                sb.AppendLine($"{header.Key}: {header.Value}");
            }
                
            sb.AppendLine();
            sb.AppendLine(Body);
            return sb.ToString();
        }

        public static MessagePart Deserialize(string serializedMessage)
        {
            MessagePart message = new MultipartMime.MessagePart();

            StringReader reader = new StringReader(serializedMessage);
            while(true)
            {
                string nextLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(nextLine))
                    break;

                // Parse nextLine as header
                var index = nextLine.IndexOf(':');
                var name = nextLine.Substring(0, index);
                var value = nextLine.Substring(index + 1).Trim();
                message.Headers[name] = value;
            }
            message.Body = reader.ReadToEnd();

            return message;
        }
    }
}
