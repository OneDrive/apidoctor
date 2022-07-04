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

using System.ComponentModel;

namespace ApiDoctor.Validation.OData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    public static class ExtensionMethods
    {
        private static readonly Dictionary<string, SimpleDataType> ODataSimpleTypeMap = new Dictionary<string, SimpleDataType>()
        {
            { "Edm.String", SimpleDataType.String },
            { "Edm.Int64", SimpleDataType.Int64 },
            { "Edm.Int32", SimpleDataType.Int32 },
            { "Edm.Int16", SimpleDataType.Int16 },
            { "Edm.Boolean", SimpleDataType.Boolean },
            { "Edm.Byte", SimpleDataType.Byte },
            { "Edm.Date", SimpleDataType.Date},
            { "Edm.DateTimeOffset", SimpleDataType.DateTimeOffset },
            { "Edm.Duration", SimpleDataType.Duration },
            { "Edm.Double", SimpleDataType.Double },
            { "Edm.Float", SimpleDataType.Float },
            { "Edm.Guid", SimpleDataType.Guid },
            { "Edm.TimeOfDay", SimpleDataType.TimeOfDay},
            { "Edm.TimeSpan", SimpleDataType.TimeSpan },
            { "Edm.Stream", SimpleDataType.Stream },
            { "Edm.Object", SimpleDataType.Object },
            { "Edm.Single", SimpleDataType.Single },
            { "Edm.Binary", SimpleDataType.Binary },
        };

        internal static bool ToBoolean(this string source)
        {
            if (string.IsNullOrEmpty(source)) return false;

            bool value;
            if (Boolean.TryParse(source, out value))
                return value;

            throw new ArgumentException(string.Format("Failed to convert {0} into a boolean", source));
        }

        internal static string AttributeValue(this XElement xml, XName attributeName)
        {
            var attribute = xml.Attribute(attributeName);
            if (null == attribute)
                return null;
            return attribute.Value;
        }

        /// <summary>
        /// Resolves a fully qualified type identifier within a collection of schema. 
        /// Will match on ComplexType, EntityType, Action, or Function.
        /// </summary>
        public static object FindTypeWithIdentifier(this IEnumerable<Schema> schemas, string identifier)
        {
            if (identifier == null)
            {
                return null;
            }

            bool isCollection = UnwrapCollectionIfNeeded(ref identifier);

            // namespace.name
            int splitIndex = identifier.LastIndexOf('.');
            if (splitIndex == -1)
            {
                throw new ArgumentException("identifier should be of format {schema}.{type}");
            }

            string schemaName = identifier.Substring(0, splitIndex);
            string typeName = identifier.Substring(splitIndex + 1);

            // Resolve the schema first
            var schema = schemas.FirstOrDefault(s => s.Namespace == schemaName || s.Alias == schemaName);
            if (schema == null)
            {
                return null;
            }

            // now see if a data type matches
            var match =
                schema.ComplexTypes.FirstOrDefault(ct => ct.Name == typeName) as object ??
                schema.EntityTypes.FirstOrDefault(et => et.Name == typeName) as object ??
                schema.Enumerations.FirstOrDefault(e => e.Name == typeName) as object;

            if (match != null)
            {
                if (isCollection)
                {
                    return new ODataCollection(identifier);
                }

                return match;
            }

            // now look for matching actions or functions.
            match =
                schema.Actions.FirstOrDefault(a => a.Name == typeName) as object ??
                schema.Functions.FirstOrDefault(f => f.Name == typeName) as object;

            if (match != null)
            {
                if (isCollection)
                {
                    throw new ArgumentException("Can't have a collection of functions or actions");
                }

                return match;
            }

            return null;
        }

        private static bool UnwrapCollectionIfNeeded(ref string identifier)
        {
            bool isCollection = false;
            if (identifier.IsCollection())
            {
                identifier = identifier.Substring(11, identifier.Length - 12);
                isCollection = true;
            }

            return isCollection;
        }

        public static T ResourceWithIdentifier<T>(this IEnumerable<Schema> schemas, string identifier) where T : class
        {
            var type = schemas.FindTypeWithIdentifier(identifier);
            if (type != null && type is T)
            {
                return (T)type;
            }

            bool isCollection = UnwrapCollectionIfNeeded(ref identifier);

            SimpleDataType simpleType = identifier.ToODataSimpleType();
            if (simpleType != SimpleDataType.Object)
            {
                if (isCollection)
                {
                    return new ODataCollection(identifier) as T;
                }

                return new ODataSimpleType(simpleType) as T;
            }

            throw new KeyNotFoundException("Unable to find type identifier '" + identifier + "' as '" + typeof(T).Name + "'.");
        }

        /// <summary>
        /// Look up a type in the EntityFramework based on the type identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="edmx"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static T ResourceWithIdentifier<T>(this EntityFramework edmx, string identifier) where T : class
        {
            return edmx.DataServices.Schemas.ResourceWithIdentifier<T>(identifier);
        }


        public static IODataNavigable LookupNavigableType(this List<Schema> schemas, string identifier)
        {
            var foundType = schemas.FindTypeWithIdentifier(identifier);
            if (null != foundType)
            {
                return (IODataNavigable)foundType;
            }

            SimpleDataType simpleType = identifier.ToODataSimpleType();
            if (simpleType != SimpleDataType.Object)
                return new ODataSimpleType(simpleType);

            throw new InvalidOperationException("Could not resolve type identifier: " + identifier);
        }

        internal static IODataNavigable LookupNavigableType(this EntityFramework edmx, string identifier)
        {
            var foundType = edmx.DataServices.Schemas.FindTypeWithIdentifier(identifier);
            if (null != foundType)
            {
                return (IODataNavigable)foundType;
            }

            SimpleDataType simpleType = identifier.ToODataSimpleType();
            if (simpleType != SimpleDataType.Object)
                return new ODataSimpleType(simpleType);

            throw new InvalidOperationException("Could not resolve type identifier: " + identifier);
        }

        /// <summary>
        /// Resolve an IODataNavigable instance into a type identifier string.
        /// </summary>
        /// <param name="edmx"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string LookupIdentifierForType(this EntityFramework edmx, IODataNavigable type)
        {
            if (type is ODataSimpleType)
            {
                return ((ODataSimpleType)type).Type.ODataResourceName();
            }
            else if (type is ODataCollection)
            {
                return "Collection(" + ((ODataCollection)type).TypeIdentifier + ")";
            }

            foreach (var schema in edmx.DataServices.Schemas)
            {
                if (type is EntityType)
                {
                    foreach (var et in schema.EntityTypes)
                    {
                        if (et == type)
                            return schema.Namespace + "." + et.Name;
                    }
                }
                else if (type is ComplexType)
                {
                    foreach (var ct in schema.ComplexTypes)
                    {
                        if (ct == type)
                            return schema.Namespace + "." + ct.Name;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns "oneDrive" for "oneDrive.item" input.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string NamespaceOnly(this string type)
        {
            if (type.IsCollection())
                type = type.ElementName();

            var trimPoint = type.LastIndexOf('.');
            if (trimPoint >= 0)
                return type.Substring(0, trimPoint);

            throw new InvalidOperationException("Type doesn't appear to have a namespace assocaited with it: " + type);
        }

        public static bool HasNamespace(this string type)
        {
            var trimPoint = type.LastIndexOf('.');
            return trimPoint != -1;
        }


        /// <summary>
        /// Returns "item" for "oneDrive.item" input.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string TypeOnly(this string type)
        {
            if (type.IsCollection())
                type = type.ElementName(); 

            var trimPoint = type.LastIndexOf('.');
            return type.Substring(trimPoint + 1);
        }

        /// <summary>
        /// Convert a ParameterDataType instance into the OData equivelent.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ODataResourceName(this ParameterDataType type)
        {

            if (type.Type == SimpleDataType.Object && !string.IsNullOrEmpty(type.CustomTypeName))
            {
                return type.CustomTypeName;
            }

            if (type.Type == SimpleDataType.Collection)
            {
                return string.Format(
                    "Collection({0})",
                    type.CollectionResourceType.ODataResourceName(type.CustomTypeName));
            }

            return type.Type.ODataResourceName();
        }

        /// <summary>
        /// Convert a simple type into OData equivelent. If Object is specified, a customDataType can be returned instead.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="customDataType"></param>
        /// <returns></returns>
        public static string ODataResourceName(this SimpleDataType type, string customDataType = null)
        {
            if (type == SimpleDataType.Object && !string.IsNullOrEmpty(customDataType))
                return customDataType;

            string typeName = (from kv in ODataSimpleTypeMap where kv.Value == type select kv.Key).SingleOrDefault();
            if (null == typeName)
            {
                throw new NotSupportedException(string.Format("Attempted to convert an unsupported SimpleDataType into OData: {0}", type));
            }
            return typeName;
        }

        /// <summary>
        /// Convert from a typeName like Edm.String back into the internal SimpleDataType enum.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static SimpleDataType ToODataSimpleType(this string typeName)
        {
            SimpleDataType? dataType =
                (from kv in ODataSimpleTypeMap where kv.Key == typeName select kv.Value).SingleOrDefault();

            if (dataType.GetValueOrDefault() != default(SimpleDataType))
            {
                return dataType.Value;
            }

            return SimpleDataType.Object;
        }

        public static bool IsCollection(this string typeName)
        {
            return typeName.StartsWith($"{ODataParser.CollectionPrefix}", StringComparison.Ordinal) && typeName.EndsWith(")", StringComparison.Ordinal);
        }

        public static string ElementName(this string collection)
        {
            if (!collection.IsCollection())
            {
                return collection;
            }

            return collection.Substring(ODataParser.CollectionPrefix.Length, collection.Length - ODataParser.CollectionPrefix.Length -1);
        }

    }
}
