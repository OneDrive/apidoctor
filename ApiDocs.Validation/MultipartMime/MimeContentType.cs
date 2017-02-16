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
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Tags;

    public class MimeContentType
    {
        public MimeContentType()
        {
            this.Arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public MimeContentType(string contentType)
        {
            this.Arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ParseContentTypeString(contentType);
        }

        public string MimeType { get; set; }

        public Dictionary<string, string> Arguments { get; private set; }

        public void ParseContentTypeString(string input)
        {
            if (input == null)
                return;

            string[] contentTypeParts = input.Split(';');
            if (contentTypeParts.Length > 0)
                MimeType = contentTypeParts[0];
            for(int i=1; i<contentTypeParts.Length; i++)
            {
                // Parse arguments
                var argument = contentTypeParts[i];
                int index = argument.IndexOf('=');
                if (index > 0)
                {
                    Arguments[argument.Substring(0, index).Trim()] = argument.Substring(index + 1).Trim();
                }
                else
                {
                    Arguments[argument] = string.Empty;
                }
            }
        }

        public override string ToString()
        {
            StringBuilder args = new StringBuilder();
            foreach(var arg in Arguments)
            {
                args.Append($"; {arg.Key}={arg.Value}");
            }

            return MimeType + args.ToString();
        }
    }
}
