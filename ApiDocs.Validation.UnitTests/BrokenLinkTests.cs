namespace ApiDocs.Validation.UnitTests
{
    using System;
    using System.IO;
    using System.Linq;
    using ApiDocs.Validation.Error;
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
            ValidationError[] errors;

            Assert.IsTrue(file.Scan(out errors));
            Assert.IsEmpty(errors.WarningsOrErrorsOnly());

            Assert.IsTrue(file.ValidateNoBrokenLinks(false, out errors));
            Assert.IsEmpty(errors.WarningsOrErrorsOnly());
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

            ValidationError[] errors;

            Assert.IsTrue(file.Scan(out errors));
            var realErrors = from e in errors where e.IsWarning || e.IsError select e;
            Assert.IsEmpty(realErrors);

            Assert.IsFalse(file.ValidateNoBrokenLinks(false, out errors));
            realErrors = from e in errors where e.IsWarning || e.IsError select e;
            Assert.AreEqual(1, realErrors.Count());
            Assert.IsTrue(realErrors.First().Code == ValidationErrorCode.MissingLinkSourceId);
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

            ValidationError[] errors;

            Assert.IsTrue(file.Scan(out errors));
            Assert.IsEmpty(errors.WarningsOrErrorsOnly());

            Assert.IsFalse(file.ValidateNoBrokenLinks(false, out errors));
            Assert.AreEqual(1, errors.WarningsOrErrorsOnly().Count());
            Assert.IsTrue(errors.First().Code == ValidationErrorCode.LinkDestinationNotFound);
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

            ValidationError[] errors;

            Assert.IsTrue(file.Scan(out errors));
            Assert.IsEmpty(errors.WarningsOrErrorsOnly());

            Assert.IsFalse(file.ValidateNoBrokenLinks(false, out errors));
            Assert.AreEqual(2, errors.WarningsOrErrorsOnly().Count());
            Assert.IsTrue(errors[0].Code == ValidationErrorCode.MissingLinkSourceId);
            Assert.IsTrue(errors[1].Code == ValidationErrorCode.LinkDestinationNotFound);
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

        public new bool Scan(out ValidationError[] errors)
        {
            this.HasScanRun = true;
            this.TransformMarkdownIntoBlocksAndLinks(this.Markdown);
            return this.ParseMarkdownBlocks(out errors);
        }

        public Func<string, bool> IsLinkValid { get; set; }


        protected override LinkValidationResult VerifyRelativeLink(FileInfo sourceFile, string linkUrl, string docSetBasePath, out string relativeFileName)
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
