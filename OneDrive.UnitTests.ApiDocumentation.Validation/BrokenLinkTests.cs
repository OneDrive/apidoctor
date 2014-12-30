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
            Assert.IsEmpty(errors);

            Assert.IsTrue(file.ValidateNoBrokenLinks(false, out errors));
            Assert.IsEmpty(errors);
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
            Assert.IsEmpty(errors);

            Assert.IsFalse(file.ValidateNoBrokenLinks(false, out errors));
            Assert.AreEqual(1, errors.Length);
            Assert.IsTrue(errors.First().Code == ValidationErrorCode.MissingLinkSourceId);
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
            Assert.IsEmpty(errors);

            Assert.IsFalse(file.ValidateNoBrokenLinks(false, out errors));
            Assert.AreEqual(1, errors.Length);
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
            Assert.IsEmpty(errors);

            Assert.IsFalse(file.ValidateNoBrokenLinks(false, out errors));
            Assert.AreEqual(2, errors.Length);
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
            ParseMarkdownForBlocksAndLinks(m_Markdown);
            return ParseCodeBlocks(out errors);
        }

        public Func<string, bool> IsLinkValid { get; set; }

        protected override LinkValidationResult VerifyRelativeLink(System.IO.FileInfo sourceFile, string linkUrl, string docSetBasePath)
        {
            if (null != IsLinkValid)
            {
                return IsLinkValid(linkUrl) ? LinkValidationResult.Valid : LinkValidationResult.FileNotFound;
            }

            return LinkValidationResult.Valid;
        }


    }
}
