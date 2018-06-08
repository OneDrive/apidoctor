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

namespace ApiDoctor.Validation
{
    using System;
    using System.Collections.Generic;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.Json;
    using Newtonsoft.Json.Linq;

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
        /// True if the function has an overload without this parameter.
        /// </summary>
        public bool? Optional { get; set; }

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

        public JToken ToExampleJToken()
        {
            if (this.Type != null)
            {
                switch (this.Type.Type)
                {
                    case SimpleDataType.Boolean:
                        return new JValue(true);
                    case SimpleDataType.Byte:
                        return new JValue(byte.MaxValue);
                    case SimpleDataType.Date:
                        return new JValue(DateTime.Now.ToString("yyyy-MM-dd"));
                    case SimpleDataType.DateTimeOffset:
                        return new JValue(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                    case SimpleDataType.Double:
                    case SimpleDataType.Float:
                    case SimpleDataType.Single:
                        return new JValue(2.75);
                    case SimpleDataType.Duration:
                        return new JValue("PT2H");
                    case SimpleDataType.Guid:
                        return new JValue(Guid.NewGuid());
                    case SimpleDataType.Int16:
                    case SimpleDataType.Int32:
                    case SimpleDataType.Int64:
                        return new JValue(12356);
                    case SimpleDataType.String:
                        return new JValue("string");
                }

                if (this.Type.CustomTypeName != null)
                {
                    var obj = new JObject();
                    obj.Add("@odata.type", new JValue(this.Type.CustomTypeName));
                    return obj;
                }
            }

            return null;
        }

        /// <summary>
        /// Merge values from the param object into this object.
        /// </summary>
        /// <param name="param"></param>
        internal void AddMissingDetails(ParameterDefinition param, IssueLogger issues)
        {
            if (!this.Required.HasValue && param.Required.HasValue)
                this.Required = param.Required;

            this.IsNavigatable = this.IsNavigatable | param.IsNavigatable;

            if (this.Type != param.Type)
            {
                if (this.Type.IsObject)
                {
                    if (!this.Type.IsCollection && param.Type.IsCollection)
                    {
                        issues.Error(ValidationErrorCode.ExpectedTypeDifferent,
                            $"Type mismatch between example and table. Parameter name: {this.Name}; example type ({this.Type}) is not a collection, while the table description type ({param.Type}) is.");
                    }

                    if (this.Type.CustomTypeName == null && param.Type.CustomTypeName != null)
                    {
                        this.Type = param.Type;
                    }
                    else if (this.Type.CustomTypeName != null && param.Type.CustomTypeName != null)
                    {
                        issues.Warning(ValidationErrorCode.ExpectedTypeDifferent,
                            $"Type mismatch between example and table. Parameter name: { this.Name}; example type: ({ this.Type.CustomTypeName}); table type: ({ param.Type.CustomTypeName})");
                    }
                }
                else if (this.Type.IsCollection)
                {
                    if (!param.Type.IsCollection)
                    {
                        issues.Error(ValidationErrorCode.ExpectedTypeDifferent,
                            $"Type mismatch between example and table. Parameter name: {this.Name}; example type ({this.Type}) is a collection, while the table description type ({param.Type}) is not.");
                    }

                    if (param.Type.CollectionResourceType == SimpleDataType.Object && !string.IsNullOrEmpty(this.Type.CustomTypeName) && this.Type.CustomTypeName != param.Type.CustomTypeName)
                    {
                        issues.Warning(ValidationErrorCode.ExpectedTypeDifferent,
                            $"Inconsistent types between parameter ({this.Type.CustomTypeName}) and table ({param.Type.CustomTypeName})");
                    }
                    else if (this.Type.CollectionResourceType != param.Type.CollectionResourceType)
                    {
                        if (this.Type.CollectionResourceType == SimpleDataType.String && param.Type.IsEnum)
                        {
                            // we allow json examples of enums to look like strings because we don't have a better way of representing them right now.
                        }
                        else
                        {
                            issues.Warning(ValidationErrorCode.ExpectedTypeDifferent,
                                $"Inconsistent types between parameter ({this.Type.CollectionResourceType}) and table ({param.Type.CollectionResourceType})");
                        }
                    }

                    // table should be authoritative.
                    this.Type = param.Type;
                }
                else
                {
                    if (param.Type.IsLessSpecificThan(this.Type) &&
                        param.Type.JsonLooksDifferentFrom(this.Type))
                    {
                        issues.Warning(ValidationErrorCode.ExpectedTypeDifferent,
                            $"Parameter '{param.Name}' type changed from {this.Type.Type} --> {param.Type.Type} because the latter was in the table description. Update the resource to match.");
                    }

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
