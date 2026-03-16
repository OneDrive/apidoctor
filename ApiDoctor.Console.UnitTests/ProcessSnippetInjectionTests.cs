namespace ApiDoctor.Console.UnitTests
{
    using System;
    using System.Linq;
    using ApiDoctor.ConsoleApp;
    using NUnit.Framework;

    [TestFixture]
    public class ProcessSnippetInjectionTests
    {
        private const string SnippetContent = "# [C#](#tab/csharp)\r\n[!INCLUDE [snippet](snippets/csharp.md)]\r\n\r\n---";

        /// <summary>
        /// Builds a minimal markdown file with a method code block and optional
        /// existing tab section. Uses a DELETE request to avoid the GET→msgraph-interactive
        /// rewrite logic, keeping test assertions simpler.
        /// </summary>
        private static string[] BuildFileContents(
            bool withExistingTabs = false,
            bool withTrailingContent = true,
            bool withHttpTab = false)
        {
            var lines = new System.Collections.Generic.List<string>
            {
                "## Delete resource [test-method]",                       // 0
                "<!-- { \"blockType\": \"request\", \"name\": \"test-method\" } -->", // 1
                "```http",                                                 // 2
                "DELETE /me/resource",                                     // 3
                "```",                                                     // 4
            };

            if (withHttpTab)
            {
                // Insert HTTP tab header before the comment line
                lines.Insert(1, "# [HTTP](#tab/http)");
                // Now comment is at index 2, etc.
            }

            if (withExistingTabs)
            {
                lines.Add("# [C#](#tab/csharp)");
                lines.Add("[!INCLUDE [old-snippet](old.md)]");
                lines.Add("");
                lines.Add("---");
                lines.Add("");  // blank line after tab section end
            }

            if (withTrailingContent)
            {
                lines.Add("## Next section");
                lines.Add("Some content here.");
            }

            return lines.ToArray();
        }

        [Test]
        public void ReturnsNull_WhenMethodIdentifierNotFound()
        {
            var fileContents = BuildFileContents();

            var result = Program.ProcessSnippetInjection(
                fileContents,
                "nonexistent-method",
                "DELETE /me/resource",
                SnippetContent);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ReturnsNull_WhenHttpRequestLineNotFound()
        {
            var fileContents = BuildFileContents();

            var result = Program.ProcessSnippetInjection(
                fileContents,
                "test-method",
                "POST /me/something-else",
                SnippetContent);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void FirstTabInsertion_InjectsHttpTabAndSnippets()
        {
            var fileContents = BuildFileContents();

            var result = Program.ProcessSnippetInjection(
                fileContents,
                "test-method",
                "DELETE /me/resource",
                SnippetContent);

            Assert.That(result, Is.Not.Null);
            var resultArray = result.ToArray();

            // Should contain the HTTP tab header
            Assert.That(resultArray, Has.Some.Contains("# [HTTP](#tab/http)"));

            // Should contain our snippet content
            Assert.That(string.Join("\n", resultArray), Does.Contain("[!INCLUDE [snippet](snippets/csharp.md)]"));

            // Trailing content should be preserved
            Assert.That(resultArray, Has.Some.EqualTo("## Next section"));
        }

        [Test]
        public void ExistingTabSection_ReplacesSnippets()
        {
            var fileContents = BuildFileContents(withExistingTabs: true, withHttpTab: true);

            var newSnippet = "# [Python](#tab/python)\r\n[!INCLUDE [snippet](snippets/python.md)]\r\n\r\n---";
            var result = Program.ProcessSnippetInjection(
                fileContents,
                "test-method",
                "DELETE /me/resource",
                newSnippet);

            Assert.That(result, Is.Not.Null);
            var joined = string.Join("\n", result);

            // Old snippet should be gone
            Assert.That(joined, Does.Not.Contain("old.md"));

            // New snippet should be present
            Assert.That(joined, Does.Contain("snippets/python.md"));
        }

        [Test]
        public void EmptySnippets_WithExistingHttpTab_RemovesHttpTab()
        {
            var fileContents = BuildFileContents(withExistingTabs: true, withHttpTab: true);

            var result = Program.ProcessSnippetInjection(
                fileContents,
                "test-method",
                "DELETE /me/resource",
                "");

            Assert.That(result, Is.Not.Null);
            var resultArray = result.ToArray();

            // HTTP tab should be removed
            Assert.That(resultArray, Has.None.Contains("# [HTTP](#tab/http)"));
        }

        [Test]
        public void SnippetInjection_AtEndOfFile_DoesNotThrow()
        {
            // This is the exact scenario that caused the original crash:
            // When the code block is at the very end of the file with no trailing content,
            // insertionLine + 1 would be out of bounds.
            var fileContents = BuildFileContents(withTrailingContent: false);

            Assert.DoesNotThrow(() =>
            {
                var result = Program.ProcessSnippetInjection(
                    fileContents,
                    "test-method",
                    "DELETE /me/resource",
                    SnippetContent);

                // Force enumeration to trigger any deferred exceptions
                result?.ToArray();
            });
        }

        [Test]
        public void SnippetInjection_AtEndOfFile_InjectsCorrectly()
        {
            var fileContents = BuildFileContents(withTrailingContent: false);

            var result = Program.ProcessSnippetInjection(
                fileContents,
                "test-method",
                "DELETE /me/resource",
                SnippetContent);

            Assert.That(result, Is.Not.Null);
            var joined = string.Join("\n", result);
            Assert.That(joined, Does.Contain("[!INCLUDE [snippet](snippets/csharp.md)]"));
        }

        [Test]
        public void ExistingTabSection_AtEndOfFile_ReplacesWithoutThrow()
        {
            // End-of-file with existing tabs and no trailing content
            var fileContents = BuildFileContents(withExistingTabs: true, withHttpTab: true, withTrailingContent: false);

            var newSnippet = "# [Go](#tab/go)\r\n[!INCLUDE [snippet](snippets/go.md)]\r\n\r\n---";
            Assert.DoesNotThrow(() =>
            {
                var result = Program.ProcessSnippetInjection(
                    fileContents,
                    "test-method",
                    "DELETE /me/resource",
                    newSnippet);

                var resultArray = result?.ToArray();
                if (resultArray != null)
                {
                    var joined = string.Join("\n", resultArray);
                    Assert.That(joined, Does.Not.Contain("old.md"));
                    Assert.That(joined, Does.Contain("snippets/go.md"));
                }
            });
        }
    }
}
