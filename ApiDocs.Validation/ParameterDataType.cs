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
    using System.Linq;

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

        public ParameterDataType(ParameterDataType existingType, IEnumerable<ParameterDefinition> customMembers)
        {
            this.Type = existingType.Type;
            this.CollectionResourceType = existingType.CollectionResourceType;
            this.CustomTypeName = existingType.CustomTypeName;
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
            if (type.IsCollection)
            {
                throw new ArgumentException("Cannot create a collection for a collection");
            }

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
        public string CustomTypeName { get; private set; }

        public bool IsObject
        {
            get { return this.Type == SimpleDataType.Object; }
        }

        public bool IsCollection
        {
            get { return this.Type == SimpleDataType.Collection; }
        }

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

        #region Helper Methods
        /// <summary>
        /// Returns true if the current ParameterDataType instance is less
        /// qualified than the type provided in the arguments.
        /// {complex type} > {simple type} > {string} > {object}
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal bool IsLessSpecificThan(ParameterDataType type)
        {
            return this.SpecificityLevel() < type.SpecificityLevel();
        }

        private int SpecificityLevel()
        {
            if (this.IsObject && !string.IsNullOrEmpty(this.CustomTypeName))
                return 10;
            if (this.IsCollection && this.CollectionResourceType == SimpleDataType.Object && !string.IsNullOrEmpty(this.CustomTypeName))
                return 10;

            SimpleDataType type = this.IsCollection ? this.CollectionResourceType : this.Type;
            if (type == SimpleDataType.Int64)
                return 4;
            else if (type == SimpleDataType.String)
                return 3;
            else if (type != SimpleDataType.None && type != SimpleDataType.Object && type != SimpleDataType.Collection)
                return 5;

            return 0;
        }


        public static ParameterDataType ChooseBest(ParameterDataType a, ParameterDataType b)
        {
            if (b.IsLessSpecificThan(a))
                return a;
            else
                return b;
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
        Stream,

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
