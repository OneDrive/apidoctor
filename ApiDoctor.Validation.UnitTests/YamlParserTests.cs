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

namespace ApiDoctor.Validation.UnitTests
{
    using System.Linq;
    using ApiDoctor.Validation;
    using ApiDoctor.Validation.Error;
    using NUnit.Framework;

    [TestFixture]
    public class YamlParserTests
    {
        private static readonly string yamlWithMultiLineArray = @"title: ""Define the /me as singleton""
description: ""These are things I had to add in the docs to make sure the Markdown-Scanner""
ms.localizationpriority: medium
author: """"
ms.prod: """"
doc_type: conceptualPageType
toc.keywords:
- foo
- bar
";

        // Missing closing double-quote on title property
        private static readonly string malformedYaml = @"title: ""Define the /me as singleton
description: ""These are things I had to add in the docs to make sure the Markdown-Scanner""
ms.localizationpriority: medium
author: """"
ms.prod: """"
doc_type: conceptualPageType
toc.keywords:
- foo
- bar
";

        [Test]
        public void YamlWithMultiLineArrayParses()
        {
            // Arrange
            _ = new DocSet();
            var issues = new IssueLogger();

            // Act
            DocFile.ParseYamlMetadata(yamlWithMultiLineArray, issues);

            // Assert
            Assert.That(!issues.Issues.WereErrors());
        }

        [Test]
        public void MalformedYamlGeneratesError()
        {
            // Arrange
            _ = new DocSet();
            var issues = new IssueLogger();

            // Act
            DocFile.ParseYamlMetadata(malformedYaml, issues);

            // Assert
            Assert.That(issues.Issues.WereErrors());
            var error = issues.Issues.FirstOrDefault();
            Assert.That(error != null);
            Assert.That(error.IsError);
            Assert.That(error.Message == "Incorrect YAML header format");
        }
    }
}