﻿/*
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
    using System.Linq;

    using ApiDoctor.Validation.OData;

    public class ParameterDataType
    {
        #region Constructors
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
        public ParameterDataType(string customDataType, bool isCollection = false, bool isEnum = false)
        {
            // Remove the prefix '#' that @odata.type may include
            if (customDataType.StartsWith("#"))
                customDataType = customDataType.Substring(1);

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
            this.IsEnum = isEnum;
        }

        public ParameterDataType(ParameterDataType existingType, IEnumerable<ParameterDefinition> customMembers)
        {
            this.Type = existingType.Type;
            this.CollectionResourceType = existingType.CollectionResourceType;
            this.CustomTypeName = existingType.CustomTypeName;
            this.IsEnum = existingType.IsEnum;
            if (null != customMembers)
            {
                this.CustomMembers = customMembers.ToList();
            }
        }

        public ParameterDataType(IEnumerable<ParameterDefinition> customMembers)
        {
            this.Type = SimpleDataType.Object;
            if (null != customMembers)
            {
                this.CustomMembers = customMembers.ToList();
            }

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
                CollectionResourceType = type.Type,
                CollectionDimensions = type.IsCollection ? 2 : 1,
                IsEnum = type.IsEnum,
            };
        }

        public ParameterDataType()
        {

        }
        #endregion

        #region Public Properties
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
        public string CustomTypeName { get; set; }

        public bool IsObject
        {
            get { return this.Type == SimpleDataType.Object; }
        }

        public bool IsCollection
        {
            get { return this.Type == SimpleDataType.Collection; }
        }

        public int CollectionDimensions { get; set; }

        public bool IsEnum { get; private set; }

        /// <summary>
        /// A dictionary that reprensets the known type members of an undefined object type.
        /// </summary>
        public List<ParameterDefinition> CustomMembers { get; private set; }

        #endregion

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

        public override int GetHashCode()
        {
            return this.Type.GetHashCode();
        }
        #endregion

        public override string ToString()
        {
            if (this.IsObject)
            {
                if (!string.IsNullOrEmpty(this.CustomTypeName))
                    return this.CustomTypeName;
            }
            else if (this.IsCollection)
            {
                if (string.IsNullOrEmpty(this.CustomTypeName))
                    return "Collection(" + this.CollectionResourceType.ToString() + ")";
                else
                    return "Collection(" + this.CustomTypeName + ")";
            }

            return this.Type.ToString();
        }

        /// <summary>
        /// Get's the MarkDown for a type
        /// </summary>
        /// <returns></returns>
        public string GetMarkDown()
        {
            if (this.IsObject)
            {
                string typeName = CustomTypeName.TypeOnly();
                return $"[{typeName}]({typeName}.md)";
            }
            if (this.IsCollection)
            {
                if (!string.IsNullOrEmpty(this.CustomTypeName))
                {
                    string typeName = CustomTypeName.TypeOnly();
                    return $"[{typeName}]({typeName}.md) collection";
                }
                if (CollectionResourceType == SimpleDataType.Object)
                {
                    string typeName = CollectionResourceType.ODataResourceName().TypeOnly();
                    return $"[{typeName}]({typeName}.md) collection";
                }
                else
                {
                    return $"{CollectionResourceType} collection";
                }
            }

            // Primitive type
            return this.Type.ToString();
        }

        #region Helper Methods

        /// <summary>
        /// Returns true if the two types are perceivably different in json.
        /// For example, Int64 and Int16 generally look the same.
        /// Double and Int32 generally look different.
        /// Guid and String generally look different. And so on.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal bool JsonLooksDifferentFrom(ParameterDataType type)
        {
            if (type == null)
            {
                return true;
            }

            if (this.IsCollection)
            {
                if (type.IsCollection)
                {
                    return new ParameterDataType(this.CollectionResourceType).JsonLooksDifferentFrom(new ParameterDataType(type.CollectionResourceType));
                }
                else
                {
                    return true;
                }
            }
            else if (type.IsCollection)
            {
                return true;
            }

            if (this.Type == type.Type)
            {
                return false;
            }

            switch (this.Type)
            {
                case SimpleDataType.Boolean:
                    return type.Type != SimpleDataType.Boolean;
                case SimpleDataType.Byte:
                case SimpleDataType.Int16:
                case SimpleDataType.Int32:
                case SimpleDataType.Int64:
                    return
                        type.Type != SimpleDataType.Byte &&
                        type.Type != SimpleDataType.Int16 &&
                        type.Type != SimpleDataType.Int32 &&
                        type.Type != SimpleDataType.Int64;
                case SimpleDataType.Double:
                case SimpleDataType.Float:
                case SimpleDataType.Single:
                    return
                        type.Type != SimpleDataType.Double &&
                        type.Type != SimpleDataType.Float &&
                        type.Type != SimpleDataType.Single;
                default:
                    // default to assuming the types look different
                    return true;
            }
        }

        /// <summary>
        /// Returns true if the current ParameterDataType instance is less
        /// qualified than the type provided in the arguments.
        /// {complex type} > {simple type} > {string} > {object}
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal bool IsLessSpecificThan(ParameterDataType type)
        {
            /*
                For JSON, we basically have the following hierarchy of data types:

                string
                -> GUID
                -> DateTimeOffset
                -> TimeSpan

                int64
                ->int32
                ->int16
                ->boolean

                float
                -> double

                object
                -> complex type

                stream

                We should never allow something from one tree to be considered less specific than something in a different tree.
            */

            if (type == null)
            {
                return false;
            }

            if (this.IsCollection)
            {
                if (type.IsCollection)
                {
                    return new ParameterDataType(this.CollectionResourceType).IsLessSpecificThan(new ParameterDataType(type.CollectionResourceType));
                }
                else
                {
                    return false;
                }
            }

            if (this.IsCollection != type.IsCollection)
            {
                return false;
            }

            if (this.Type == SimpleDataType.String &&
                (type.Type == SimpleDataType.Guid || type.Type == SimpleDataType.DateTimeOffset || type.Type == SimpleDataType.TimeSpan))
            {
                return true;
            }
            else if (this.Type == SimpleDataType.Int64 && (type.Type == SimpleDataType.Int32 || type.Type == SimpleDataType.Int16 || type.Type == SimpleDataType.Boolean))
            {
                return true;
            }
            else if (this.Type == SimpleDataType.Double && (type.Type == SimpleDataType.Float))
            {
                return true;
            }
            else if (this.Type == SimpleDataType.Object && type.Type == SimpleDataType.Object && this.CustomTypeName.IEquals("Edm.Object"))
            {
                return true;
            }

            return false;
        }

        public static ParameterDataType ChooseBest(ParameterDataType a, ParameterDataType b)
        {
            if (b.IsLessSpecificThan(a))
                return a;
            else
                return b;
        }

        public static string ChooseBest(string a, string b)
        {
            var paramA = a.ParseParameterDataType();
            var paramB = b.ParseParameterDataType();

            if (paramB.IsLessSpecificThan(paramA))
            {
                return a;
            }
            else
            {
                return b;
            }
        }
        #endregion

        #region Static property type definitions

        static ParameterDataType()
        {
            String = new ParameterDataType(SimpleDataType.String);
            GenericObject = new ParameterDataType(SimpleDataType.Object);
            GenericCollection = new ParameterDataType(SimpleDataType.Object, true);
            Boolean = new ParameterDataType(SimpleDataType.Boolean);
            Double = new ParameterDataType(SimpleDataType.Double);
            Float = new ParameterDataType(SimpleDataType.Float);
            Guid = new ParameterDataType(SimpleDataType.Guid);
            Int64 = new ParameterDataType(SimpleDataType.Int64);
            Int32 = new ParameterDataType(SimpleDataType.Int32);
            DateTimeOffset = new ParameterDataType(SimpleDataType.DateTimeOffset);
            JsonCollection = new ParameterDataType(SimpleDataType.Json, isCollection: false);
        }

        public static ParameterDataType String
        {
            get; private set;
        }
        public static ParameterDataType GenericObject
        {
            get; private set;
        }
        public static ParameterDataType GenericCollection
        {
            get; private set;
        }
        public static ParameterDataType Boolean
        {
            get; private set;
        }
        public static ParameterDataType Double
        {
            get; private set;
        }
        public static ParameterDataType Float
        {
            get; private set;
        }
        public static ParameterDataType Guid
        {
            get; private set;
        }
        public static ParameterDataType Int64
        {
            get; private set;
        }
        public static ParameterDataType Int32
        {
            get; private set;
        }
        public static ParameterDataType DateTimeOffset
        {
            get; private set;
        }
        public static ParameterDataType Json
        {
            get; private set;
        }
        public static ParameterDataType JsonCollection
        {
            get; private set;
        }

        #endregion
    }

    public enum SimpleDataType
    {
        None,

        String,
        Boolean,
        Byte,

        Int16,
        Int32,
        Int64,

        Float,
        Double,

        Date,
        DateTimeOffset,
        Duration,
        Guid,
        TimeOfDay,
        TimeSpan,
        Stream,

        /// <summary>
        /// Specifies that the value is an array of another data type
        /// </summary>
        Collection,

        /// <summary>
        /// Specifies that the value is an undefined resource (generic object)
        /// </summary>
        Object,

        Single,

        Binary,
        /// <summary>
        /// Specified Json (including Json collections)
        /// </summary>
        Json
    }
}
