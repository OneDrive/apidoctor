/*
 * Markdown Scanner
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

namespace ApiDocs.Validation.UnitTests
{
    using NUnit.Framework;
    using MultipartMime;

    [TestFixture]
    public class MultipartMimeTests
    {
        [Test]
        public void RoundtripTest()
        {
            MultipartMime.MultipartMimeContent message = new MultipartMime.MultipartMimeContent();
            message.Parts.Add(new MultipartMime.MessagePart { Id = "<metadata>", ContentType = new MimeContentType("application/json"), Body = "{\"foo\": \"bar\"}" });
            message.Parts.Add(new MultipartMime.MessagePart { Id = "<content", ContentType = new MimeContentType("text/plain"), Body = "This is test message content" });

            string body1 = message.ToString();

            MultipartMime.MultipartMimeContent message2 = new MultipartMime.MultipartMimeContent(message.ContentType, body1);
            string body2 = message2.ToString();

            Assert.AreEqual(body1.Length, body2.Length, "Body length changed between roundtrips.");
            Assert.AreEqual(body1, body2, "Body text was different between roundtrips.");
        }
    }
}
