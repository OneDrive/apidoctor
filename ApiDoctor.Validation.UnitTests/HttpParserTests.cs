using System.Collections.Generic;
using ApiDoctor.Validation.Http;
using ApiDoctor.Validation.UnitTests.Properties;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace ApiDoctor.Validation.UnitTests
{
    public class HttpParserTests
    {
        private readonly ITestOutputHelper helper;
        private readonly HttpParser httpParser;

        public HttpParserTests(ITestOutputHelper helper)
        {
            this.helper = helper;
            httpParser = new HttpParser();
        }

        public static IEnumerable<object[]> FullHttpRequest =>
            new List<string[]>
            {
                new[] {Resources.ExampleRequest}
            };


        [Theory]
        [InlineData(
            @"GET https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'",
            "https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'")]
        public void ParseOdataUrl(string odataUrl, string actualUrl)
        {
            var request = httpParser.ParseHttpRequest(odataUrl);
            request.Url.Should().Be(actualUrl, "Parsed Url should be equal to request header url");
        }

        [Theory]
        [InlineData(
            @"GET https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'",
            "GET")]
        public void ParseOdataMethod(string odataUrl, string method)
        {
            var request = httpParser.ParseHttpRequest(odataUrl);
            request.Method.Should().Be(method, "Parsed Method should be equal to request header method");
        }

        [Theory]
        [InlineData(
            @"GET HTTP/2.0 https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'",
            "HTTP/2.0")]
        public void HttpVersionShouldBeRespected(string odataUrl, string httpVersion)
        {
            var request = httpParser.ParseHttpRequest(odataUrl);
            request.HttpVersion.Should().Be(httpVersion, "When HttpVersion is specified, should be respected");
        }

        [Theory]
        [InlineData(
            @"GET https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'")]
        public void HttpVersionShouldDefaultToHttp1(string odataUrl)
        {
            var request = httpParser.ParseHttpRequest(odataUrl);
            request.HttpVersion.Should().Be("HTTP/1.1", "When HttpVersion is not specified, default to HTTP/1.1");
        }

        [Theory]
        [MemberData(nameof(FullHttpRequest))]
        public void ParseHttpRequest(string exampleRequest)
        {
            var parsedRequest = httpParser.ParseHttpRequest(exampleRequest);
            parsedRequest.Should().NotBe(null);
            var jsonData = JsonConvert.SerializeObject(parsedRequest, Formatting.Indented);
            helper.WriteLine(jsonData);
        }
    }
}