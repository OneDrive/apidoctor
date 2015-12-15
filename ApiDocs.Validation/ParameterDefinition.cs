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

        /// <summary>
        /// Merge values from the param object into this object.
        /// </summary>
        /// <param name="param"></param>
        internal void AddMissingDetails(ParameterDefinition param)
        {
            this.Required = param.Required;
            if (this.Type != param.Type)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Parameter type mismatch, was {0} but now {1}",
                    this.Type,
                    param.Type);
                this.Type = param.Type;
            }

            if (string.IsNullOrEmpty(this.TypeResourceName))
                this.TypeResourceName = param.TypeResourceName;
            if (string.IsNullOrEmpty(this.Title))
                this.Title = param.Title;
            if (string.IsNullOrEmpty(this.Description))
                this.Description = param.Description;

            if (param.EnumeratedValues != null)
                this.EnumeratedValues.AddRange(param.EnumeratedValues);
        }
    }
    
    public enum ParameterLocation
    {
        Path,
        QueryString,
        Header,
        JsonObject
    }

    public class ParameterDataType
    {

        /// <summary>
        /// Creates a new instance with a standard data type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isCollection"></param>
        public ParameterDataType(SimpleDataType type, bool isCollection = false)
        {
            if (type == SimpleDataType.Collection)
                throw new ArgumentException("type cannot be set to Collection with this constructor.");

            if (!isCollection)
            {
                this.Type = type;
            }
            else
            {
                this.Type = SimpleDataType.Collection;
                this.CollectionResourceType = type;
            }
        }

        /// <summary>
        /// Creates a new instance with a custom data type.
        /// </summary>
        /// <param name="customDataType"></param>
        /// <param name="isCollection"></param>
        public ParameterDataType(string customDataType, bool isCollection = false)
        {
            if (isCollection)
            {
                this.Type = SimpleDataType.Collection;
                this.CollectionResourceType = SimpleDataType.Object;
            }
            else
            {
                this.Type = SimpleDataType.Object;
            }
            this.CustomTypeName = customDataType;
        }

        /// <summary>
        /// Returns a new instance that represents a collection of a given type of object.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ParameterDataType CollectionOfType(ParameterDataType type)
        {
            return new ParameterDataType
            {
                Type = SimpleDataType.Collection,
                CustomTypeName = type.CustomTypeName,
                CollectionResourceType = type.Type
            };
        }

        public ParameterDataType()
        {

        }

        /// <summary>
        /// Specifies the type for this parameter among the known data types.
        /// </summary>
        public SimpleDataType Type { get; private set; }

        /// <summary>
        /// If the Type is a Collection, specifies the type of object inside the collection.
        /// </summary>
        public SimpleDataType CollectionResourceType { get; private set; }

        /// <summary>
        /// If Type or CollectionResourceType are Collection or Object, this property holds the name of the custom type.
        /// </summary>
        public string CustomTypeName { get; private set; }

        public bool IsObject
        {
            get { return this.Type == SimpleDataType.Object; }
        }

        public bool IsCollection
        {
            get { return this.Type == SimpleDataType.Collection; }
        }



        #region Equality Overloads

        public override bool Equals(object obj)
        {
            if (obj is ParameterDataType)
            {
                ParameterDataType other = (ParameterDataType)obj;
                return this.Type == other.Type && 
                       this.CollectionResourceType == other.CollectionResourceType &&
                       this.CustomTypeName == other.CustomTypeName;
            }
            return false;
        }

        public static bool operator ==(ParameterDataType a, ParameterDataType b)
        {
            if (Object.ReferenceEquals(a, b))
                return true;

            if ((object)a == null || (object)b == null)
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(ParameterDataType a, ParameterDataType b)
        {
            return !(a == b);
        }

        #endregion

        #region Helper Properties


        public static ParameterDataType String
        {
            get { return new ParameterDataType(SimpleDataType.String); }
        }

        public static ParameterDataType GenericObject
        {
            get { return new ParameterDataType(SimpleDataType.Object); }
        }

        public static ParameterDataType GenericCollection
        {
            get { return new ParameterDataType(SimpleDataType.Object, true); }
        }

        public static ParameterDataType Boolean
        {
            get { return new ParameterDataType(SimpleDataType.Boolean); }
        }

        public static ParameterDataType Double
        {
            get { return new ParameterDataType(SimpleDataType.Double); }
        }

        public static ParameterDataType Float
        {
            get { return new ParameterDataType(SimpleDataType.Float); }
        }

        public static ParameterDataType Guid
        {
            get { return new ParameterDataType(SimpleDataType.Guid); }
        }

        public static ParameterDataType Int64
        {
            get { return new ParameterDataType(SimpleDataType.Int64); }
        }

        public static ParameterDataType Int32
        {
            get { return new ParameterDataType(SimpleDataType.Int32); }
        }


        public static ParameterDataType DateTimeOffset
        {
            get { return new ParameterDataType(SimpleDataType.DateTimeOffset); }
        }

        #endregion
        }

    public enum SimpleDataType
    {
        None,
        String,
        Boolean,
        Int32,
        Int64,
        Float,
        Double,
        DateTimeOffset,
        Guid,
        TimeSpan,
        
        /// <summary>
        /// Specifies that the value is an array of another data type
        /// </summary>
        Collection,
        /// <summary>
        /// Specifies that the value is an undefined resource (generic object)
        /// </summary>
        Object
        
    }
}
