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

namespace ApiDocs.Validation
{
    using System.Collections.Generic;
    using ApiDocs.Validation.Json;

    /// <summary>
    /// Represents a parameter for a request
    /// </summary>
    public class ParameterDefinition : ItemDefinition
    {
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The type of object the parameter expects
        /// </summary>
        public ParameterDataType Type { get; set; }

        /// <summary>
        /// For Type values of Collection or Object, specify the resource name for the value.
        /// </summary>
        public string TypeResourceName { get; set; }

        /// <summary>
        /// The location of the parameter in the request/response
        /// </summary>
        public ParameterLocation Location { get; set; }

        /// <summary>
        /// True if the parameter is required to have a value.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// List of enumerated values for a parameter that uses enumeration
        /// </summary>
        public List<EnumerationDefinition> EnumeratedValues { get; set; }
    }
    
    public enum ParameterLocation
    {
        Path,
        QueryString,
        Header,
        JsonObject
    }

    public enum ParameterDataType
    {
        String,
        Boolean,
        Int32,
        Int64,
        DateTimeOffset,
        /// <summary>
        /// Specifies that the value is an array of another data type
        /// </summary>
        Collection,
        /// <summary>
        /// Specifies that the value is a resource defined elsewhere in the data set
        /// </summary>
        Resource,
        /// <summary>
        /// Specifies that the value is an undefined resource (generic object)
        /// </summary>
        Object,
        Double
    }
}
