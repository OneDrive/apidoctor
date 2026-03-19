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

        [Test]
        public void ExistingTabSection_DashesAsLastLine_DoesNotThrow()
        {
            // Reproduces the pipeline crash: "---" is the very last line
            // with no trailing blank line, causing currentIndex + 1 to be
            // out of bounds at the FindEndOfTabSection check.
            var fileContents = new[]
            {
                "## Delete resource [test-method]",
                "# [HTTP](#tab/http)",
                "<!-- { \"blockType\": \"request\", \"name\": \"test-method\" } -->",
                "```http",
                "DELETE /me/resource",
                "```",
                "# [C#](#tab/csharp)",
                "[!INCLUDE [old-snippet](old.md)]",
                "",
                "---",   // last line — no trailing blank line
            };

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

        [Test]
        public void CommentOnFirstLine_ReturnsNull()
        {
            // The backward scan loop uses `index > 0` so it never checks
            // index 0. When the comment is on the first line, the scan
            // doesn't find it and the method returns null (no injection).
            var fileContents = new[]
            {
                "<!-- { \"blockType\": \"request\", \"name\": \"test-method\" } -->",
                "```http",
                "DELETE /me/resource",
                "```",
                "## Next section",
            };

            var result = Program.ProcessSnippetInjection(
                fileContents,
                "test-method",
                "DELETE /me/resource",
                SnippetContent);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ExistingTabSection_DoesNotDisplaceFollowingHeader()
        {
            // Reproduces an issue where "### Response" was displaced from after
            // the tab terminator to before the snippet tabs, breaking the document.
            var fileContents = new[]
            {
                "### Request",
                "The following example shows a request.",
                "# [HTTP](#tab/http)",
                "<!-- { \"blockType\": \"request\", \"name\": \"test-method\" } -->",
                "```http",
                "DELETE /me/resource",
                "```",
                "",
                "# [JavaScript](#tab/javascript)",
                "[!INCLUDE [sample-code](old-js.md)]",
                "[!INCLUDE [sdk-documentation](sdk-doc.md)]",
                "",
                "---",
                "",
                "### Response",
                "",
                "The following example shows the response.",
            };

            var newSnippet = "# [JavaScript](#tab/javascript)\r\n[!INCLUDE [sample-code](new-js.md)]\r\n\r\n---";
            var result = Program.ProcessSnippetInjection(
                fileContents,
                "test-method",
                "DELETE /me/resource",
                newSnippet);

            Assert.That(result, Is.Not.Null);
            var resultArray = result.ToArray();
            var joined = string.Join("\n", resultArray);

            // New snippet should be present
            Assert.That(joined, Does.Contain("new-js.md"));

            // Normalize to per-line strings so ordering checks are accurate even if
            // the injected snippet was inserted as a single multi-line string.
            var normalizedLines = joined
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .ToArray();

            // "### Response" must come AFTER the tab terminator "---", not before the snippet tabs.
            // Search from the code block area to avoid matching YAML frontmatter "---".
            var codeBlockIndex = Array.FindIndex(normalizedLines, l => l.Contains("```"));
            var tabTerminatorIndex = Array.FindIndex(normalizedLines, codeBlockIndex, l => l.Trim() == "---");
            var responseIndex = Array.FindIndex(normalizedLines, l => l.TrimStart().StartsWith("### Response"));
            Assert.That(responseIndex, Is.GreaterThan(tabTerminatorIndex),
                "### Response header must remain after the tab section terminator");
        }
    }
}
