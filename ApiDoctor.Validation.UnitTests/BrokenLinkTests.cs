﻿/*
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

namespace ApiDoctor.Validation.UnitTests
{
    using System;
    using System.IO;
    using System.Linq;
    using ApiDoctor.Validation.Error;
    using NUnit.Framework;

    [TestFixture]
    public class BrokenLinkTests
    {

        [Test]
        public void NoBrokenLinks()
        {
            string markdown = 
@"# Test file
[Basic Link](http://www.microsoft.com/).
[ID-based link][microsoft]
[Up one level](../anotherfile.md)

[microsoft]: http://www.microsoft.com
";
            TestableDocFile file = new TestableDocFile(markdown);
            var issues = new IssueLogger();

            Assert.That(file.Scan(string.Empty, issues));
            Assert.That(issues.Issues.WarningsOrErrorsOnly(), Is.Empty);

            Assert.That(file.ValidateNoBrokenLinks(false, issues, true));
            Assert.That(issues.Issues.WarningsOrErrorsOnly(), Is.Empty);
        }


        [Test]
        public void BrokenLinkInvalidId()
        {
            string markdown =
@"# Test file

This is a [basic Link](http://www.microsoft.com/).

This is a link to a named resource [ID-based link][microsoft-foo]

This link goes [up one level](../anotherfile.md)

[microsoft]: http://www.microsoft.com
";
            TestableDocFile file = new TestableDocFile(markdown);

            var issues = new IssueLogger();

            Assert.That(file.Scan(string.Empty, issues));
            var realErrors = from e in issues.Issues where e.IsWarning || e.IsError select e;
            Assert.That(realErrors, Is.Empty);

            Assert.That(file.ValidateNoBrokenLinks(false, issues, false), Is.False);
            realErrors = from e in issues.Issues where e.IsWarning || e.IsError select e;
            Assert.That(1, Is.EqualTo(realErrors.Count()));
            Assert.That(realErrors.First().Code == ValidationErrorCode.MissingLinkSourceId);
        }


        [Test]
        public void BrokenLinkNotFound()
        {
            string markdown =
@"# Test file

This is a [basic Link](http://www.microsoft.com/).

This is a link to a named resource [ID-based link][microsoft]

This link goes [up one level](../anotherfile.md)

[microsoft]: http://www.microsoft.com
";
            TestableDocFile file = new TestableDocFile(markdown)
            {
                IsLinkValid = new Func<string, bool>(
                    link =>
                    {
                        if (link == "../anotherfile.md")
                        {
                            return false;
                        }
                        return true;
                    })
            };

            var issues = new IssueLogger();

            Assert.That(file.Scan(string.Empty, issues));
            Assert.That(issues.Issues.WarningsOrErrorsOnly(), Is.Empty);

            Assert.That(file.ValidateNoBrokenLinks(false, issues, false), Is.False);
            Assert.That(1, Is.EqualTo(issues.Issues.WarningsOrErrorsOnly().Count()));
            Assert.That(issues.Issues.Any(i => i.Code == ValidationErrorCode.LinkDestinationNotFound));
        }

        [Test]
        public void BrokenLinkMultipleErrors()
        {
            var markdown =
@"# Test file

This is a [basic Link](http://www.microsoft.com/).

This is a link to a named resource [ID-based link][microsoft-foo]

This link goes [up one level](../anotherfile.md)

[microsoft]: http://www.microsoft.com
";
            TestableDocFile file = new TestableDocFile(markdown);
            file.IsLinkValid = link => link != "../anotherfile.md";

            var issues = new IssueLogger();

            Assert.That(file.Scan(string.Empty, issues));
            Assert.That(issues.Issues.WarningsOrErrorsOnly(), Is.Empty);

            Assert.That(file.ValidateNoBrokenLinks(false, issues, false), Is.False);
            Assert.That(2, Is.EqualTo(issues.Issues.WarningsOrErrorsOnly().Count()));
            Assert.That(issues.Issues.Any(i => i.Code == ValidationErrorCode.MissingLinkSourceId));
            Assert.That(issues.Issues.Any(i => i.Code == ValidationErrorCode.LinkDestinationNotFound));
        }
    }

    class TestableDocFile : DocFile
    {
        private string Markdown { get; set; }

        public TestableDocFile(string markdownContent)
        {
            this.Markdown = markdownContent;
            this.BasePath = "doesn't matter";
            this.FullPath = "doesn't matter";
            this.DisplayName = "testable_doc_file";
        }

        public override bool Scan(string tags, IssueLogger issues)
        {
            this.HasScanRun = true;
            this.TransformMarkdownIntoBlocksAndLinks(this.Markdown, tags);
            return this.ParseMarkdownBlocks(issues);
        }

        public Func<string, bool> IsLinkValid { get; set; }


        protected override LinkValidationResult VerifyRelativeLink(FileInfo sourceFile, string linkUrl, string docSetBasePath, out string relativeFileName, bool requireFilenameCaseMatch)
        {
            relativeFileName = null;
            if (null != this.IsLinkValid)
            {
                return this.IsLinkValid(linkUrl) ? LinkValidationResult.Valid : LinkValidationResult.FileNotFound;
            }

            return LinkValidationResult.Valid;
        }


    }
}
