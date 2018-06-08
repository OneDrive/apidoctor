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
    using System.Collections.Generic;
    using ApiDoctor.Validation.Utility;
    using Newtonsoft.Json;
    using NUnit.Framework;

    [TestFixture]
    public class ObjectGraphMergerTests
    {

        


        [Test]
        public void MergeSimpleTypes()
        {
            MergableClass[] itemsToMerge = new MergableClass[]
            {
                new MergableClass { Name = "Ryan", IsAuthor = true, ExpectedToBeNull = "foobar" },
                new MergableClass { Name = "Ryan", IsAuthor = false, ExpectedToBeNull = "baz" }
            };

            var merger = new ObjectGraphMerger<MergableClass>(itemsToMerge);
            var result = merger.Merge();

            Assert.IsNotNull(result);
            Assert.AreEqual("Ryan", result.Name);
            Assert.AreEqual(true, result.IsAuthor);
            Assert.IsNull(result.ExpectedToBeNull);
        }

        [Test]
        public void MergeCollection()
        {
            var itemsToMerge1 = new MergableClass[]
            {
                new MergableClass { Name = "Ryan", IsAuthor = true, ExpectedToBeNull = "foobar" },
                new MergableClass { Name = "Brad", IsAuthor = false, ExpectedToBeNull = "baz" }
            };

            var itemsToMerge2 = new MergableClass[]
            {
                new MergableClass { Name = "Ryan", IsAuthor = false, ExpectedToBeNull = "baz" },
                new MergableClass { Name = "Daron", IsAuthor = false, ExpectedToBeNull = "baz" }
            };

            var graphsToMerge = new MergableCollectionPropertyClass[]
            {
                new MergableCollectionPropertyClass { Authors = itemsToMerge1 },
                new MergableCollectionPropertyClass { Authors = itemsToMerge2 }

            };

            var merger = new ObjectGraphMerger<MergableCollectionPropertyClass>(graphsToMerge);
            var result = merger.Merge();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Authors);
            Assert.AreEqual(3, result.Authors.Length);

            foreach(var author in result.Authors)
            {
                switch(author.Name)
                {
                    case "Ryan":
                        Assert.IsTrue(author.IsAuthor);
                        break;
                    case "Daron":
                        Assert.IsFalse(author.IsAuthor);
                        break;
                    case "Brad":
                        Assert.IsFalse(author.IsAuthor);
                        break;
                    default:
                        Assert.Fail("Unexpected author name was returned");
                        break;
                }
            }
        }

        [Mergable]
        private class MergableCollectionPropertyClass
        {
            public MergableClass[] Authors { get; set; }
        }


        [Mergable(CollectionIdentifier = "Name")]
        private class MergableClass
        {
            public string Name { get; set; }

            [MergePolicy(MergePolicy.PreferGreaterValue)]
            public bool IsAuthor { get; set; }

            [MergePolicy(MergePolicy.Ignore)]
            public string ExpectedToBeNull { get; set; }
        }
        
    }
}

