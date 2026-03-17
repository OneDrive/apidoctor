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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using ApiDoctor.Validation.OData;
    using NUnit.Framework;

    [TestFixture]
    public class StringSuggestionsTests
    {
        [Test]
        public void SuggestStringFromCollection_ExactMatch_ReturnsMatchWithScoreZero()
        {
            var result = StringSuggestions.SuggestStringFromCollection("hello", new[] { "hello", "world", "foo" });

            Assert.That(result, Does.StartWith("hello"));
            Assert.That(result, Does.Contain("score: 0"));
        }

        [Test]
        public void SuggestStringFromCollection_OneCharacterDifference_ReturnsBestCandidate()
        {
            var result = StringSuggestions.SuggestStringFromCollection(
                "helo",
                new[] { "world", "hello", "foo" });

            Assert.That(result, Does.StartWith("hello"));
        }

        [Test]
        public void SuggestStringFromCollection_MultipleCandidates_ReturnsBestScore()
        {
            // "cat" vs "bat" → dist 1, "car" → dist 1, "hello" → dist 4
            // First with score 1 wins (bat)
            var result = StringSuggestions.SuggestStringFromCollection(
                "cat",
                new[] { "bat", "car", "hello" });

            Assert.That(result, Does.StartWith("bat"));
            Assert.That(result, Does.Contain("score: 1"));
        }

        [Test]
        public void SuggestStringFromCollection_EmptyCollection_ReturnsNullAndMaxScore()
        {
            var result = StringSuggestions.SuggestStringFromCollection(
                "test",
                Enumerable.Empty<string>());

            // suggestionText is null; null + string in C# produces " (score: ...)"
            Assert.That(result, Does.Contain($"score: {int.MaxValue}"));
        }

        [Test]
        public void SuggestStringFromCollection_SingleCandidateThatMatches_ReturnsThatCandidate()
        {
            var result = StringSuggestions.SuggestStringFromCollection("abc", new[] { "abc" });

            Assert.That(result, Does.StartWith("abc"));
            Assert.That(result, Does.Contain("score: 0"));
        }

        [Test]
        public void SuggestStringFromCollection_TransposedCharacters_ReturnsSuggestion()
        {
            // Damerau-Levenshtein: transposition counts as 1 edit
            var result = StringSuggestions.SuggestStringFromCollection(
                "teh",
                new[] { "the", "tea", "hen" });

            Assert.That(result, Does.StartWith("the"));
        }

        [Test]
        public void SuggestStringFromCollection_StringsOverHundredCharsDifferent_ReturnsMaxScore()
        {
            // MAX_SUGGESTION_SCORE is 100; a length difference > 100 returns int.MaxValue immediately
            var longString = new string('z', 102);
            var result = StringSuggestions.SuggestStringFromCollection("a", new[] { longString });

            Assert.That(result, Does.Contain($"score: {int.MaxValue}"));
        }
    }

    [TestFixture]
    public class XmlParseHelperTests
    {
        // Schema has [XmlRoot("Schema", Namespace = ODataParser.EdmNamespace)]
        // ComplexType has [XmlRoot("ComplexType", ...)]

        [Test]
        public void XmlElementName_TypeWithXmlRootAttribute_ReturnsCorrectName()
        {
            var name = typeof(Schema).XmlElementName();
            Assert.That(name, Is.EqualTo("Schema"));
        }

        [Test]
        public void XmlElementName_ComplexType_ReturnsCorrectName()
        {
            var name = typeof(ComplexType).XmlElementName();
            Assert.That(name, Is.EqualTo("ComplexType"));
        }

        [Test]
        public void XmlElementName_TypeWithoutXmlRootAttribute_ThrowsInvalidOperationException()
        {
            // System.String has no XmlRootAttribute
            Assert.That(() => typeof(string).XmlElementName(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("Missing XmlTagName"));
        }

        [Test]
        public void ThrowIfWrongElement_MatchingElementName_DoesNotThrow()
        {
            var xml = new XElement("Schema");
            Assert.DoesNotThrow(() => typeof(Schema).ThrowIfWrongElement(xml));
        }

        [Test]
        public void ThrowIfWrongElement_WrongElementName_ThrowsWithExpectedAndActual()
        {
            var xml = new XElement("NotSchema");
            var ex = Assert.Throws<InvalidOperationException>(
                () => typeof(Schema).ThrowIfWrongElement(xml));
            Assert.That(ex.Message, Does.Contain("Schema"));
            Assert.That(ex.Message, Does.Contain("NotSchema"));
        }

        [Test]
        public void ThrowIfWrongElement_ComplexTypeElementMatches_DoesNotThrow()
        {
            var xml = new XElement("ComplexType");
            Assert.DoesNotThrow(() => typeof(ComplexType).ThrowIfWrongElement(xml));
        }
    }
}
