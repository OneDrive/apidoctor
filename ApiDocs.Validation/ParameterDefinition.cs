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
    using System;
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
        /// The location of the parameter in the request/response
        /// </summary>
        public ParameterLocation Location { get; set; }

        /// <summary>
        /// True if the parameter is required to have a value.
        /// </summary>
        public bool? Required { get; set; }

        /// <summary>
        /// List of enumerated values for a parameter that uses enumeration
        /// </summary>
        public List<EnumerationDefinition> EnumeratedValues { get; set; }

        /// <summary>
        /// Text value of the original value of this parameter.
        /// </summary>
        public string OriginalValue { get; set; }

        /// <summary>
        /// Indicates that the parameter should be represented as a navigation property
        /// </summary>
        public bool IsNavigatable { get; internal set; }

        /// <summary>
        /// Merge values from the param object into this object.
        /// </summary>
        /// <param name="param"></param>
        internal void AddMissingDetails(ParameterDefinition param)
        {
            if (!this.Required.HasValue && param.Required.HasValue)
                this.Required = param.Required;

            this.IsNavigatable = this.IsNavigatable | param.IsNavigatable;

            if (this.Type != param.Type)
            {
                if (this.Type.IsObject)
                {
                    if (this.Type.CustomTypeName == null && param.Type.CustomTypeName != null)
                    {
                        this.Type = param.Type;
                    }
                }
                else if (this.Type.IsCollection)
                {
                    if (this.Type.CollectionResourceType == SimpleDataType.Object && this.Type.CustomTypeName == null &&
                    param.Type.CollectionResourceType == SimpleDataType.Object && param.Type.CustomTypeName != null)
                    {
                        this.Type = param.Type;
                    }
                }
                else if (this.Type.IsLessSpecificThan(param.Type))
                {
                    // TODO: This should probably be logged out as documentation warnings!
                    System.Diagnostics.Debug.WriteLine(
                        "Parameter '{2}' type changed, {0} --> {1}",
                        this.Type.Type,
                        param.Type.Type, 
                        param.Name);
                    this.Type = param.Type;
                }
            }

            if (string.IsNullOrEmpty(this.Title))
            {
                this.Title = param.Title;
            }
            if (string.IsNullOrEmpty(this.Description))
            {
                this.Description = param.Description;
            }
            if (param.EnumeratedValues != null)
            {
                this.EnumeratedValues.AddRange(param.EnumeratedValues);
            }
        }
    }
    
    public enum ParameterLocation
    {
        Path,
        QueryString,
        Header,
        JsonObject
    }

 
}
