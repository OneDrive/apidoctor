using ApiDoctor.Validation.Error;
using ApiDoctor.Validation.UnitTests.Properties;
using FluentAssertions;
using NUnit.Framework;

namespace ApiDoctor.Validation.UnitTests
{
    [TestFixture]
    internal class YamlFrontMatterTests
    {
        [Test]
        public void ProcessedContentsShouldNotContainFrontMatter()
        {
            var (yamlFrontMatter, processedContents) = DocFile.ParseAndRemoveYamlFrontMatter(Resources.ExampleResourceWithYamlFrontMatter, new IssueLogger());
            yamlFrontMatter.Should().NotBeNullOrWhiteSpace();
            // Processed Content should not have pre-processed out yamlFrontMatter.
            processedContents.Should().NotContain(yamlFrontMatter);
        }
    }
}
