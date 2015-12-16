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

namespace ApiDocs.Validation.OData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    public static class ExtensionMethods
    {
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
        /// Resovles a fully qualified type identifier within a collection of schema.
        /// </summary>
        /// <param name="schemas"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        internal static ComplexType FindTypeWithIdentifier(this IEnumerable<Schema> schemas, string identifier)
        {
            // onedrive.item
            int splitIndex = identifier.LastIndexOf('.');
            if (splitIndex == -1)
                throw new ArgumentException("identifier should be of format {schema}.{type}");

            string schemaName = identifier.Substring(0, splitIndex);
            string typeName = identifier.Substring(splitIndex + 1);

            // Resolve the schema first
            var schema = (from s in schemas
                          where s.Namespace == schemaName
                          select s).FirstOrDefault();
            if (null == schema)
            {
                return null;
            }

            // Look for a matching complex type
            var matchingComplexType = (from ct in schema.ComplexTypes
                                       where ct.Name == typeName
                                       select ct).FirstOrDefault();
            if (null != matchingComplexType)
            {
                return matchingComplexType;
            }

            // Look for a matching entity type
            var matchingEntityType = (from et in schema.Entities
                                      where et.Name == typeName
                                      select et).FirstOrDefault();
            return matchingEntityType;
        }

        internal static ComplexType FindTypeWithIdentifier(this EntityFramework edmx, string identifier)
        {
            if (identifier.StartsWith("Collection("))
            {
                var innerId = identifier.Substring(11, identifier.Length - 12);
                return edmx.FindTypeWithIdentifier(innerId);
            }

            return edmx.DataServices.Schemas.FindTypeWithIdentifier(identifier);
        }

        public static string LookupIdentifierForType(this EntityFramework edmx, IODataNavigable type)
        {
            foreach (var schema in edmx.DataServices.Schemas)
            {

                if (type is EntityType)
                {
                    foreach (var et in schema.Entities)
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
    }
}
