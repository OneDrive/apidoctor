// ---------------------------------------------------------------------------
//  <copyright file="SampleJsonGenerator.cs" company="Microsoft Corporation">
//     Copyright © Microsoft Corporation. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------

namespace ApiDocs.DocumentationGeneration
{
    using System;
    using System.Collections.Generic;

    using ApiDocs.Validation.OData;

    using Newtonsoft.Json.Linq;

    public static class SampleJsonGenerator
    {
        public static JObject GetSampleJson(EntityFramework entityFramework, ComplexType type)
        {
            JObject sample = new JObject(new JProperty("@odata.type", $"microsoft.graph.{type.Name}"));

            foreach (Property property in type.Properties)
            {
                sample.Add(property.Name, GetSampleJson(entityFramework, property.Name, property.Type));
            }

            return sample;
        }

        public static JToken GetSampleJson(EntityFramework entityFramework, EnumType enumtype)
        {
            var index = Math.Abs(enumtype.Name.GetHashCode() % enumtype.Members.Count);
            return enumtype.Members[index].Name;
        }

        private static JToken GetSampleJson(EntityFramework entityFramework, string name, string type)
        {
            if (type.StartsWith("Collection("))
            {
                JArray array = new JArray();
                var elementType = type.Substring("Collection(".Length).TrimEnd(')');
                array.Add(GetSampleJson(entityFramework, name, elementType));

                return array;
            }

            if (type.StartsWith("Edm."))
            {
                switch (type)
                {
                    case "Edm.String":
                        if (name == "id")
                        {
                            return "String (identifier)";
                        }

                        if (name.EndsWith("Url") || name.EndsWith("Uri"))
                        {
                            return "https://example.com/" + name;
                        }

                        return name + " value";
                    case "Edm.Boolean":
                        return name.GetHashCode() % 2 == 1;
                    case "Edm.Int32":
                        return Math.Abs(name.GetHashCode() % 100);
                    default:
                        return type;
                }
            }

            var reference = entityFramework.DataServices.Schemas.FindTypeWithIdentifier(type);

            // Complex type covers both complex types and entities
            var complexType = reference as ComplexType;
            if (complexType != null)
            {
                return GetSampleJson(entityFramework, complexType);
            }

            var enumtype = reference as EnumType;
            if (enumtype != null)
            {
                return GetSampleJson(entityFramework, enumtype);
            }

            return "Unknown Type: " + type;
        }
    }
}