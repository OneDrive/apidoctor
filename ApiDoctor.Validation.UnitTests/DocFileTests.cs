using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApiDoctor.Validation.Error;
using NUnit.Framework;

namespace ApiDoctor.Validation.UnitTests
{
    [TestFixture]
    public class DocFileTests
    {
        [Test]
        public void FileNameShouldNotContainerUnderscore()
        {
            DocSet docSet = new DocSet();
            DocFileForTesting file = new DocFileForTesting(string.Empty, "test_file.md", "TestFile", docSet);
            var issues = new IssueLogger();
            file.Scan(string.Empty, issues);

            Assert.IsNotEmpty(issues.Errors);
            Assert.IsTrue(issues.Errors.Any(x => x.Code == ValidationErrorCode.IllegalCharacterInFileName));
        }
    }
}
