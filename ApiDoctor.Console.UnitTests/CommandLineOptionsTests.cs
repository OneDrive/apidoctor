/*
 * API Doctor
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the ""Software""), to deal in
 * the Software without restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDoctor.Console.UnitTests
{
    using System.Linq;
    using ApiDoctor.ConsoleApp;
    using NUnit.Framework;
    using static ApiDoctor.ConsoleApp.PublishOptions;

    [TestFixture]
    public class CommandLineOptionsVerbTests
    {
        // Verb constants are the public contract between the CLI and routing logic.
        // These tests exist to catch accidental renames that would silently break the CLI.

        [Test] public void VerbPrint_IsCorrect()            => Assert.That(CommandLineOptions.VerbPrint, Is.EqualTo("print"));
        [Test] public void VerbCheckLinks_IsCorrect()       => Assert.That(CommandLineOptions.VerbCheckLinks, Is.EqualTo("check-links"));
        [Test] public void VerbDocs_IsCorrect()             => Assert.That(CommandLineOptions.VerbDocs, Is.EqualTo("check-docs"));
        [Test] public void VerbService_IsCorrect()          => Assert.That(CommandLineOptions.VerbService, Is.EqualTo("check-service"));
        [Test] public void VerbPublish_IsCorrect()          => Assert.That(CommandLineOptions.VerbPublish, Is.EqualTo("publish"));
        [Test] public void VerbMetadata_IsCorrect()         => Assert.That(CommandLineOptions.VerbMetadata, Is.EqualTo("check-metadata"));
        [Test] public void VerbAbout_IsCorrect()            => Assert.That(CommandLineOptions.VerbAbout, Is.EqualTo("about"));
        [Test] public void VerbCheckAll_IsCorrect()         => Assert.That(CommandLineOptions.VerbCheckAll, Is.EqualTo("check-all"));
        [Test] public void VerbPublishMetadata_IsCorrect()  => Assert.That(CommandLineOptions.VerbPublishMetadata, Is.EqualTo("publish-edmx"));
        [Test] public void VerbGenerateDocs_IsCorrect()     => Assert.That(CommandLineOptions.VerbGenerateDocs, Is.EqualTo("generate-docs"));
        [Test] public void VerbGenerateSnippets_IsCorrect() => Assert.That(CommandLineOptions.VerbGenerateSnippets, Is.EqualTo("generate-snippets"));
        [Test] public void VerbGeneratePermissionFiles_IsCorrect() => Assert.That(CommandLineOptions.VerbGeneratePermissionFiles, Is.EqualTo("generate-permission-files"));
    }

    [TestFixture]
    public class BaseOptionsTests
    {
        [Test]
        public void PageParameterDict_NullParameters_ReturnsNull()
        {
            var opts = new PrintOptions { AdditionalPageParameters = null };
            Assert.That(opts.PageParameterDict, Is.Null);
        }

        [Test]
        public void PageParameterDict_EmptyParameters_ReturnsNull()
        {
            var opts = new PrintOptions { AdditionalPageParameters = "" };
            Assert.That(opts.PageParameterDict, Is.Null);
        }

        [Test]
        public void PageParameterDict_SingleParam_ReturnsSingleEntry()
        {
            var opts = new PrintOptions { AdditionalPageParameters = "env=prod" };
            var dict = opts.PageParameterDict;

            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict["env"], Is.EqualTo("prod"));
        }

        [Test]
        public void PageParameterDict_MultipleParams_ReturnsAllEntries()
        {
            var opts = new PrintOptions { AdditionalPageParameters = "env=prod&version=v1.0&debug=true" };
            var dict = opts.PageParameterDict;

            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(dict["env"], Is.EqualTo("prod"));
            Assert.That(dict["version"], Is.EqualTo("v1.0"));
            Assert.That(dict["debug"], Is.EqualTo("true"));
        }

        [Test]
        public void PageParameterDict_IsCaseInsensitive()
        {
            var opts = new PrintOptions { AdditionalPageParameters = "Env=prod" };
            var dict = opts.PageParameterDict;

            Assert.That(dict["env"], Is.EqualTo("prod"));
            Assert.That(dict["ENV"], Is.EqualTo("prod"));
        }

        [Test]
        public void HasRequiredProperties_BaseOptions_AlwaysReturnsTrue()
        {
            var opts = new AboutOptions();
            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.True);
            Assert.That(missing, Is.Empty);
        }
    }

    [TestFixture]
    public class PrintOptionsTests
    {
        [Test]
        public void HasRequiredProperties_NoFlagsSet_ReturnsFalseWithMessage()
        {
            var opts = new PrintOptions { DocumentationSetPath = "." };
            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.False);
            Assert.That(missing, Has.Length.EqualTo(1));
        }

        [Test]
        public void HasRequiredProperties_PrintFilesSet_ReturnsTrue()
        {
            var opts = new PrintOptions { DocumentationSetPath = ".", PrintFiles = true };
            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.True);
            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void HasRequiredProperties_PrintResourcesSet_ReturnsTrue()
        {
            var opts = new PrintOptions { DocumentationSetPath = ".", PrintResources = true };
            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasRequiredProperties_PrintMethodsSet_ReturnsTrue()
        {
            var opts = new PrintOptions { DocumentationSetPath = ".", PrintMethods = true };
            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasRequiredProperties_PrintAccountsSet_ReturnsTrue()
        {
            var opts = new PrintOptions { DocumentationSetPath = ".", PrintAccounts = true };
            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasRequiredProperties_MultipleFlags_ReturnsTrue()
        {
            var opts = new PrintOptions { DocumentationSetPath = ".", PrintFiles = true, PrintMethods = true };
            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.True);
            Assert.That(missing, Is.Empty);
        }
    }

    [TestFixture]
    public class FixDocsOptionsTests
    {
        [Test]
        public void Matches_NullRawMatches_ReturnsEmptyHashSet()
        {
            var opts = new FixDocsOptions { RawMatches = null };
            Assert.That(opts.Matches, Is.Empty);
        }

        [Test]
        public void Matches_EmptyRawMatches_ReturnsEmptyHashSet()
        {
            var opts = new FixDocsOptions { RawMatches = "" };
            Assert.That(opts.Matches, Is.Empty);
        }

        [Test]
        public void Matches_WhitespaceOnly_ReturnsEmptyHashSet()
        {
            var opts = new FixDocsOptions { RawMatches = "   " };
            Assert.That(opts.Matches, Is.Empty);
        }

        [Test]
        public void Matches_CommaSeparated_ReturnsAllValues()
        {
            var opts = new FixDocsOptions { RawMatches = "user,message,event" };
            Assert.That(opts.Matches, Is.EquivalentTo(new[] { "user", "message", "event" }));
        }

        [Test]
        public void Matches_SemicolonSeparated_ReturnsAllValues()
        {
            var opts = new FixDocsOptions { RawMatches = "user;message;event" };
            Assert.That(opts.Matches, Is.EquivalentTo(new[] { "user", "message", "event" }));
        }

        [Test]
        public void Matches_TrimsWhitespace()
        {
            var opts = new FixDocsOptions { RawMatches = " user , message , event " };
            Assert.That(opts.Matches, Is.EquivalentTo(new[] { "user", "message", "event" }));
        }

        [Test]
        public void Matches_IsCaseInsensitive()
        {
            var opts = new FixDocsOptions { RawMatches = "User" };
            Assert.That(opts.Matches.Contains("user"), Is.True);
            Assert.That(opts.Matches.Contains("USER"), Is.True);
        }

        [Test]
        public void Matches_SingleValue_ReturnsSingleEntry()
        {
            var opts = new FixDocsOptions { RawMatches = "user" };
            Assert.That(opts.Matches.Count, Is.EqualTo(1));
            Assert.That(opts.Matches.Contains("user"), Is.True);
        }
    }

    [TestFixture]
    public class PublishOptionsTests
    {
        [Test]
        public void FilesToPublish_NullSourceFiles_ReturnsEmptyArray()
        {
            var opts = new PublishOptions { SourceFiles = null };
            Assert.That(opts.FilesToPublish, Is.Empty);
        }

        [Test]
        public void FilesToPublish_SemicolonSeparated_SplitsCorrectly()
        {
            var opts = new PublishOptions { SourceFiles = "a.md;b.md;c.md" };
            Assert.That(opts.FilesToPublish, Is.EqualTo(new[] { "a.md", "b.md", "c.md" }));
        }

        [Test]
        public void FilesToPublish_SingleFile_ReturnsSingleEntry()
        {
            var opts = new PublishOptions { SourceFiles = "readme.md" };
            Assert.That(opts.FilesToPublish, Is.EqualTo(new[] { "readme.md" }));
        }

        [Test]
        public void FilesToPublish_RoundTrip_SetterAndGetter()
        {
            var opts = new PublishOptions();
            opts.FilesToPublish = new[] { "a.md", "b.md" };
            Assert.That(opts.FilesToPublish, Is.EqualTo(new[] { "a.md", "b.md" }));
        }

        [Test]
        public void HasRequiredProperties_MarkdownFormat_ReturnsTrue()
        {
            var opts = new PublishOptions
            {
                DocumentationSetPath = ".",
                OutputDirectory = "out",
                Format = PublishFormat.Markdown
            };

            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.True);
            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void HasRequiredProperties_MustacheWithoutTemplate_ReturnsFalse()
        {
            var opts = new PublishOptions
            {
                DocumentationSetPath = ".",
                OutputDirectory = "out",
                Format = PublishFormat.Mustache,
                TemplatePath = null
            };

            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.False);
            Assert.That(missing, Contains.Item("template"));
        }

        [Test]
        public void HasRequiredProperties_MustacheWithTemplate_ReturnsTrue()
        {
            var opts = new PublishOptions
            {
                DocumentationSetPath = ".",
                OutputDirectory = "out",
                Format = PublishFormat.Mustache,
                TemplatePath = "./templates"
            };

            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasRequiredProperties_Swagger2WithAllFields_ReturnsTrue()
        {
            var opts = new PublishOptions
            {
                DocumentationSetPath = ".",
                OutputDirectory = "out",
                Format = PublishFormat.Swagger2,
                Title = "My API",
                Description = "API description",
                Version = "v1.0"
            };

            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasRequiredProperties_Swagger2MissingTitle_ReturnsFalse()
        {
            var opts = new PublishOptions
            {
                DocumentationSetPath = ".",
                OutputDirectory = "out",
                Format = PublishFormat.Swagger2,
                Title = null,
                Description = "desc",
                Version = "v1.0"
            };

            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.False);
            Assert.That(missing, Contains.Item("swagger-title"));
        }

        [Test]
        public void HasRequiredProperties_Swagger2AllFieldsMissing_ReportsAllThree()
        {
            var opts = new PublishOptions
            {
                DocumentationSetPath = ".",
                OutputDirectory = "out",
                Format = PublishFormat.Swagger2
            };

            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.False);
            Assert.That(missing, Contains.Item("swagger-title"));
            Assert.That(missing, Contains.Item("swagger-description"));
            Assert.That(missing, Contains.Item("swagger-version"));
        }
    }

    [TestFixture]
    public class GeneratePermissionFilesOptionsTests
    {
        [Test]
        public void HasRequiredProperties_BootstrappingOnly_NoFileRequired()
        {
            var opts = new GeneratePermissionFilesOptions
            {
                DocumentationSetPath = ".",
                BootstrappingOnly = true,
                PermissionsSourceFile = null
            };

            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.True);
            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void HasRequiredProperties_NotBootstrapping_WithFile_ReturnsTrue()
        {
            var opts = new GeneratePermissionFilesOptions
            {
                DocumentationSetPath = ".",
                BootstrappingOnly = false,
                PermissionsSourceFile = "permissions.json"
            };

            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasRequiredProperties_NotBootstrapping_NoFile_ReturnsFalse()
        {
            var opts = new GeneratePermissionFilesOptions
            {
                DocumentationSetPath = ".",
                BootstrappingOnly = false,
                PermissionsSourceFile = null
            };

            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.False);
            Assert.That(missing, Has.Length.EqualTo(1));
        }

        [Test]
        public void HasRequiredProperties_NotBootstrapping_WhitespaceFile_ReturnsFalse()
        {
            var opts = new GeneratePermissionFilesOptions
            {
                DocumentationSetPath = ".",
                BootstrappingOnly = false,
                PermissionsSourceFile = "   "
            };

            var result = opts.HasRequiredProperties(out var missing);

            Assert.That(result, Is.False);
        }
    }

    [TestFixture]
    public class PublishMetadataOptionsTests
    {
        [Test]
        public void GetOptions_MapsAllPropertiesCorrectly()
        {
            var opts = new PublishMetadataOptions
            {
                DocumentationSetPath = "/docs",
                OutputDirectory = "/out",
                SourceMetadataPath = "/source.edmx",
                SecondSourceMetadataPath = "/merge.edmx",
                SortOutput = true,
                TransformOutput = "myTransform",
                Version = "v1.0",
                SkipMetadataGeneration = true,
                ValidateSchema = true,
                AttributesOnNewLines = true,
                EntityContainerName = "MyContainer",
                ShowSources = true
            };

            var result = opts.GetOptions();

            Assert.That(result.OutputDirectoryPath, Is.EqualTo("/out"));
            Assert.That(result.SourceMetadataPath, Is.EqualTo("/source.edmx"));
            Assert.That(result.MergeWithMetadataPath, Is.EqualTo("/merge.edmx"));
            Assert.That(result.Sort, Is.True);
            Assert.That(result.TransformOutput, Is.EqualTo("myTransform"));
            Assert.That(result.DocumentationSetPath, Is.EqualTo("/docs"));
            Assert.That(result.Version, Is.EqualTo("v1.0"));
            Assert.That(result.SkipMetadataGeneration, Is.True);
            Assert.That(result.ValidateSchema, Is.True);
            Assert.That(result.AttributesOnNewLines, Is.True);
            Assert.That(result.EntityContainerName, Is.EqualTo("MyContainer"));
            Assert.That(result.ShowSources, Is.True);
        }

        [Test]
        public void GetOptions_NamespacesNull_MapsToNull()
        {
            var opts = new PublishMetadataOptions { OutputDirectory = "/out", Namespaces = null };
            var result = opts.GetOptions();
            Assert.That(result.Namespaces, Is.Null);
        }

        [Test]
        public void GetOptions_NamespacesCommaSeparated_SplitsCorrectly()
        {
            var opts = new PublishMetadataOptions { OutputDirectory = "/out", Namespaces = "microsoft.graph,microsoft.graph.beta" };
            var result = opts.GetOptions();
            Assert.That(result.Namespaces, Is.EqualTo(new[] { "microsoft.graph", "microsoft.graph.beta" }));
        }

        [Test]
        public void GetOptions_NamespacesSemicolonSeparated_SplitsCorrectly()
        {
            var opts = new PublishMetadataOptions { OutputDirectory = "/out", Namespaces = "microsoft.graph;microsoft.graph.beta" };
            var result = opts.GetOptions();
            Assert.That(result.Namespaces, Is.EqualTo(new[] { "microsoft.graph", "microsoft.graph.beta" }));
        }
    }
}
