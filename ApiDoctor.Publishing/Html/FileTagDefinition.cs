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

namespace ApiDoctor.Publishing.Html
{
    using System.Collections.Generic;
    using System.IO;
    using ApiDoctor.Validation;
    using Mustache;

    public class FileTagDefinition : TagDefinition
    {
        public FileTagDefinition()
            : base("url")
        {
        }

        public string RootDestinationFolder { get; set; }
        public string DestinationFile { get; set; }

        public override void GetText(TextWriter writer, Dictionary<string, object> arguments, Scope context)
        {
            var filenameToReplace = arguments["filename"] as string;
            if (null != filenameToReplace)
            {
                var relativeFileUrl = DocSet.RelativePathToRootFromFile(
                    this.DestinationFile,
                    Path.Combine(this.RootDestinationFolder, filenameToReplace),
                    true);
                writer.Write(relativeFileUrl);
            }
        }

        protected override bool GetHasContent()
        {
            return false;
        }

        protected override IEnumerable<TagParameter> GetParameters()
        {
            return new TagParameter[] { new TagParameter("filename") { IsRequired = true } };
        }


        public override IEnumerable<TagParameter> GetChildContextParameters()
        {
            return new TagParameter[] { new TagParameter("filename") };
        }
    }
}
