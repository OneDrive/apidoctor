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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ApiDoctor.Validation.Error;
using MarkdownDeep;

namespace ApiDoctor.Validation
{
    public class SupplementalFile : DocFile
    {
        private static HashSet<string> LikelyUrlAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "src", "url",
        };

        public SupplementalFile(string basePath, string relativePath, DocSet parent)
            :base(basePath, relativePath, parent)
        {
        }

        protected override string GetContentsOfFile(string tags, IssueLogger issues)
        {
            return File.ReadAllText(Path.Combine(this.FullPath));
        }

        public override bool Scan(string tags, IssueLogger issues)
        {
            try
            {
                var content = this.GetContentsOfFile(tags, issues);
                var xd = XDocument.Parse(content);
                this.MarkdownLinks = AllAttribues(xd.Root).
                    Where(a => LikelyUrlAttributes.Contains(a.Name.ToString()) && !string.IsNullOrEmpty(a.Value)).
                    Select(url => new LinkInfo { Definition = new LinkDefinition(url.Value, Fixup(url.Value)), Text = PrintNode(url.Parent) }).
                    Cast<MarkdownDeep.ILinkInfo>().
                    ToList();
                this.HasScanRun = true;
                return true;
            }
            catch (IOException ioex)
            {
                issues.Error(ValidationErrorCode.ErrorOpeningFile, $"Error reading file contents.", ioex);
                return false;
            }
            catch (Exception ex)
            {
                issues.Error(ValidationErrorCode.ErrorReadingFile, $"Error reading file contents.", ex);
                return false;
            }
        }

        private IEnumerable<XAttribute> AllAttribues(XElement element)
        {
            foreach (var att in element.Attributes())
            {
                yield return att;
            }

            foreach (var el in element.Elements())
            {
                foreach (var att in AllAttribues(el))
                {
                    yield return att;
                }
            }
        }

        private string Fixup(string url)
        {
            if (url.StartsWith("/en-us/", StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring(6);
            }

            if (!url.EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
                !url.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) &&
                !url.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                url = url + ".md";
            }

            return url;
        }

        private string PrintNode(XElement node)
        {
            var sb = new StringBuilder($"{Environment.NewLine}<{node.Name}");
            foreach (var attribute in node.Attributes())
            {
                sb.Append($" {attribute.Name}=\"{attribute.Value}\"");
            }

            if (node.Elements().Any())
            {
                sb.AppendLine(">");
            }
            else
            {
                sb.AppendLine("/>");
            }
 
            return sb.ToString();
        }

        private class LinkInfo : MarkdownDeep.ILinkInfo
        {
            public LinkDefinition Definition { get; set; }

            public string Text { get; set; }
        }
    }
}
