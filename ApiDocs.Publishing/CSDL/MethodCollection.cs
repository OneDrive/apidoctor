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

using System.Collections.Generic;
using System.Linq;

namespace ApiDocs.Publishing.CSDL
{
    /// <summary>
    /// Wrapper around a collection of methods that operate on the same REST path.
    /// </summary>
    internal class MethodCollection : List<Validation.MethodDefinition>
    {
        public bool PostAllowed
        {
            get { return HttpVerbAllowed("POST"); }
        }
        public bool GetAllowed
        {
            get { return HttpVerbAllowed("GET"); }
        }
        public bool DeleteAllowed
        {
            get { return HttpVerbAllowed("DELETE"); }
        }
        public bool PutAllowed
        {
            get { return HttpVerbAllowed("PUT"); }
        }

        /// <summary>
        /// Indicates that all methods in this collection are Idempotent methods
        /// </summary>
        public bool AllMethodsIdempotent
        {
            get { return this.All(x => x.RequestMetadata.IsIdempotent); }
        }

        /// <summary>
        /// Returns the request body parameters if the collection contains only a single request method.
        /// </summary>
        public List<Validation.ParameterDefinition> RequestBodyParameters
        {
            get
            {
                // TODO: For collections with multiple requests, we should create a union of these requests
                return this.First().RequestBodyParameters;
            }
        }

        /// <summary>
        /// Returns the response type if the collection contains only a single request method.
        /// </summary>
        public Validation.ParameterDataType ResponseType
        {
            get
            {
                // TODO: For collections with multiple requests, we should make sure the return types are consistent.
                return this.First().ExpectedResponseMetadata.Type;
            }
        }



        protected bool HttpVerbAllowed(string verb)
        {
            var query = from m in this
                        where m.HttpMethodVerb().Equals(verb, System.StringComparison.OrdinalIgnoreCase)
                        select verb;

            return query.Any();
        }


    }
}
