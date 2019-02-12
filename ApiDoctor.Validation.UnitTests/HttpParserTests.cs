using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public HttpParserTests(ITestOutputHelper helper)
        {
            this.helper = helper;
            this.httpParser = new HttpParser();
        }

        private readonly HttpParser httpParser;

        [Theory]
        [InlineData(@"GET https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'", "https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'")]
        public void ParseOdataUrl(string odataUrl, string actualUrl)
        {
            var request = httpParser.ParseHttpRequest(odataUrl);
            request.Url.Should().Be(actualUrl, "Parsed Url should be equal to request header url");
        }

        [Theory]
        [InlineData(@"GET https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'", "GET")]
        public void ParseOdataMethod(string odataUrl, string method)
        {
            var request = httpParser.ParseHttpRequest(odataUrl);
            request.Method.Should().Be(method, "Parsed Method should be equal to request header method");
        }

        [Theory]
        [InlineData(@"GET https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'", "HTTP/1.1")]
        public void ParseDefaultHttpVersion(string odataUrl, string httpVersion)
        {
            var request = httpParser.ParseHttpRequest(odataUrl);
            request.HttpVersion.Should().Be(httpVersion, "When HttpVersion is not specified, default to HTTP/1.1");
        }

        [Theory]
        [InlineData(@"GET HTTP/1.1 https://graph.microsoft.com/beta/riskyUsers?$filter=riskLevel eq microsoft.graph.riskLevel'medium'", "HTTP/1.1")]
        public void ParseHttpVersion(string odataUrl, string httpVersion)
        {
            var request = httpParser.ParseHttpRequest(odataUrl);
            request.HttpVersion.Should().Be(httpVersion, "When HttpVersion is not specified, default to HTTP/1.1");
        }

        [Theory]
        [MemberData(nameof(FullHttpRequest))]
        public void ParseHttpRequest(string exampleRequest)
        {
            var parsedRequest = this.httpParser.ParseHttpRequest(exampleRequest);
            parsedRequest.Should().NotBe(null);
            var jsonData = JsonConvert.SerializeObject(parsedRequest, Formatting.Indented);
            this.helper.WriteLine(jsonData);
        }

        public static IEnumerable<object[]> FullHttpRequest =>
            new List<string[]>
            {
                new[] {Resources.ExampleRequest}
            };
    }
}
