using NUnit.Framework;
using OneDrive.ApiDocumentation.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.UnitTests.ApiDocumentation.Validation
{
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
            TestableDocFile file = new TestableDocFile(markdown);
            file.IsLinkValid = new Func<string, bool>(link =>
            {
                if (link == "../anotherfile.md")
                    return false;
                return true;
            });

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
            string markdown =
@"# Test file

This is a [basic Link](http://www.microsoft.com/).

This is a link to a named resource [ID-based link][microsoft-foo]

This link goes [up one level](../anotherfile.md)

[microsoft]: http://www.microsoft.com
";
            TestableDocFile file = new TestableDocFile(markdown);
            file.IsLinkValid = new Func<string, bool>(link =>
            {
                if (link == "../anotherfile.md")
                    return false;
                return true;
            });

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
        private string m_Markdown { get; set; }
        public TestableDocFile(string markdownContent)
        {
            m_Markdown = markdownContent;
            m_BasePath = "doesn't matter";
            FullPath = "doesn't matter";
            DisplayName = "testable_doc_file";
        }

        public new bool Scan(out ValidationError[] errors)
        {
            m_hasScanRun = true;
            TransformMarkdownIntoBlocksAndLinks(m_Markdown);
            return ParseMarkdownBlocks(out errors);
        }

        public Func<string, bool> IsLinkValid { get; set; }


        protected override DocFile.LinkValidationResult VerifyRelativeLink(System.IO.FileInfo sourceFile, string linkUrl, string docSetBasePath, out string relativeFileName)
        {
            relativeFileName = null;
            if (null != IsLinkValid)
            {
                return IsLinkValid(linkUrl) ? LinkValidationResult.Valid : LinkValidationResult.FileNotFound;
            }

            return LinkValidationResult.Valid;
        }


    }
}
