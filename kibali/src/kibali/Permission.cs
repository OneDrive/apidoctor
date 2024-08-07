using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Kibali
{
    public class Permission
    {
        public string Note { get; set; }
        public bool Implicit { get; set; } = false;
        public string PrivilegeLevel { get; set; }
        public SortedDictionary<string, Scheme> Schemes { get; set; } = new SortedDictionary<string, Scheme>();
        public List<PathSet> PathSets { get; set; } = new List<PathSet>();
        public OwnerInfo OwnerInfo { get; set; } = new();

        public void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            if (!string.IsNullOrWhiteSpace(Note)) writer.WriteString("note", Note);
            if (Implicit) writer.WriteBoolean("implicit", Implicit);

            writer.WritePropertyName("schemes");
            writer.WriteStartObject();
            foreach (var scheme in Schemes)
            {
                writer.WritePropertyName(scheme.Key.ToString());
                scheme.Value.Write(writer);
            }
            writer.WriteEndObject();

            writer.WritePropertyName("pathSets");
            writer.WriteStartArray();
            foreach (var pathSet in PathSets)
            {
                pathSet.Write(writer);
            }
            writer.WriteEndArray();

            writer.WritePropertyName("ownerInfo");
            OwnerInfo.Write(writer);

            writer.WriteEndObject();
        }

        internal static Permission Load(JsonElement value)
        {
            var permission = new Permission();
            ParsingHelpers.ParseMap(value, permission, handlers);
            return permission;
        }

        private static FixedFieldMap<Permission> handlers = new()
        {
            { "note", (o,v) => {o.Note = v.GetString();  } },
            { "privilegeLevel", (o,v) => {o.PrivilegeLevel= v.GetString();  } },
            { "implicit", (o,v) => {o.Implicit = v.GetBoolean();  } },
            { "pathSets", (o,v) => {o.PathSets = ParsingHelpers.GetList(v, PathSet.Load);  } },
            { "schemes", (o,v) => {o.Schemes = ParsingHelpers.GetOrderedMap(v, Scheme.Load);  } },
            { "ownerInfo", (o,v) => {o.OwnerInfo = OwnerInfo.Load(v);  } },
        };
    }

}
