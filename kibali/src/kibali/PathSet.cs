using System;
using System.Collections.Generic;
using System.Security;
using System.Text.Json;

namespace Kibali
{
    public class PathSet
    {
        public SortedSet<string> SchemeKeys { get; set; } = new SortedSet<string>();
        public SortedSet<string> Methods { get; set; } = new SortedSet<string>();
        public string AlsoRequires { get; set; }
        public SortedSet<string> ExcludedProperties { get; set; } = new SortedSet<string>();
        public SortedSet<string> IncludedProperties { get; set; } = new SortedSet<string>();
        

        public SortedDictionary<string, string> Paths
        {
            get
            {
                if (paths == null)
                {
                    paths = new SortedDictionary<string, string>();
                };
                return paths;
            }
            set { paths = value; }
        }
        private SortedDictionary<string, string> paths;

        public void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            if (!string.IsNullOrEmpty(AlsoRequires))
            {
                writer.WriteString("alsoRequires", AlsoRequires);
            }

            if (ExcludedProperties.Count > 0)
            {
                writer.WritePropertyName("excludedProperties");
                writer.WriteStartArray();
                foreach (var prop in ExcludedProperties)
                {
                    writer.WriteStringValue(prop);
                }
                writer.WriteEndArray();
            }

            if (IncludedProperties.Count > 0)
            {
                writer.WritePropertyName("includedProperties");
                writer.WriteStartArray();
                foreach (var prop in IncludedProperties)
                {
                    writer.WriteStringValue(prop);
                }
                writer.WriteEndArray();
            }



            writer.WritePropertyName("schemeKeys");
            writer.WriteStartArray();
            foreach (var scheme in SchemeKeys)
            {
                writer.WriteStringValue(scheme);
            }
            writer.WriteEndArray();


            writer.WritePropertyName("methods");
            writer.WriteStartArray();
            foreach (var method in Methods)
            {
                writer.WriteStringValue(method);
            }
            writer.WriteEndArray();

            writer.WritePropertyName("paths");
            writer.WriteStartObject();
            foreach (var path in Paths)
            {
                writer.WritePropertyName(path.Key);
                writer.WriteStringValue(path.Value);
            }
            writer.WriteEndObject();


            writer.WriteEndObject();
        }

        public static PathSet Load(JsonElement value)
        {
            var pathSet = new PathSet();
            ParsingHelpers.ParseMap(value, pathSet, handlers);
            return pathSet;
        }

        private static FixedFieldMap<PathSet> handlers = new()
        {
            { "alsoRequires", (o,v) => {o.AlsoRequires = v.GetString();  } },
            { "methods", (o,v) => {o.Methods = ParsingHelpers.GetOrderedHashSetOfString(v);  } },
            { "schemeKeys", (o,v) => {o.SchemeKeys = ParsingHelpers.GetOrderedHashSetOfString(v);  } },
            { "paths", (o,v) => {o.Paths = ParsingHelpers.GetOrderedMap(v, x => x.ToString()); } },
            { "includedProperties", (o,v) => {o.IncludedProperties = ParsingHelpers.GetOrderedHashSetOfString(v);  } },
            { "excludedProperties", (o,v) => {o.ExcludedProperties = ParsingHelpers.GetOrderedHashSetOfString(v);  } },
        };
    }

}
