using ApiDoctor.Validation.Http;
using ApiDoctor.Validation.UnitTests.Properties;
using NUnit.Framework;

namespace ApiDoctor.Validation.UnitTests
{
    [TestFixture]
    public class HttpParserTests
    {
        public static string FullHttpRequest => Resources.ExampleRequest;


        [Theory]
        [TestCase(
            @"GET https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'",
            "https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'")]
        public void ParseOdataUrl(string odataUrl, string actualUrl)
        {
            var request = HttpParser.ParseHttpRequest(odataUrl);
            Assert.That(actualUrl, Is.EqualTo(request.Url), "Parsed Url should be equal to request header url");
        }

        [Test]
        [TestCase(
            @"GET HTTP/2.0 https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'",
            "HTTP/2.0")]
        public void HttpVersionShouldBeRespected(string odataUrl, string httpVersion)
        {
            var request = HttpParser.ParseHttpRequest(odataUrl);
            Assert.That(httpVersion, Is.EqualTo(request.HttpVersion), "When HttpVersion is specified, should be respected");
        }

        [Test]
        [TestCase(
            @"GET https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'")]
        public void HttpVersionShouldDefaultToHttp1(string odataUrl)
        {
            var request = HttpParser.ParseHttpRequest(odataUrl);
            Assert.That("HTTP/1.1", Is.EqualTo(request.HttpVersion), "When HttpVersion is not specified, default to HTTP/1.1");
        }

        [Test]
        public void ParseHttpRequest()
        {
            var exampleRequest = FullHttpRequest;
            var parsedRequest = HttpParser.ParseHttpRequest(exampleRequest);
            Assert.That(parsedRequest, Is.Not.Null);
        }

        [Test]
        [TestCase(
            @"GET https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'",
            "GET")]
        public void ParseOdataMethod(string odataUrl, string method)
        {
            var request = HttpParser.ParseHttpRequest(odataUrl);
            Assert.That(method, Is.EqualTo(request.Method), "Parsed Method should be equal to request header method");
        }
    }
}