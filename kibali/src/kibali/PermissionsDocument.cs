using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kibali
{
    public class PermissionsDocument
    {
        private SortedDictionary<string, Permission> permissions = new SortedDictionary<string, Permission>();
        
        public SortedDictionary<string, Permission> Permissions { get => permissions; set => permissions = value;  }

        public async Task WriteAsync(FileStream outStream)
        {
            var writer = new Utf8JsonWriter(outStream, new JsonWriterOptions() { Indented = true });
            Write(writer);
            await writer.FlushAsync();
        }

        private void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("$schema", "https://microsoftgraph.github.io/msgraph-metadata/graph-permissions-schema.json");
            writer.WritePropertyName("permissions");
            writer.WriteStartObject();
            foreach (var permissionPair in this.permissions)
            {
                writer.WritePropertyName(permissionPair.Key);
                permissionPair.Value.Write(writer);
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }


        public static PermissionsDocument Load(string document) 
        {
            return Load(JsonDocument.Parse(document));
        }

        public static PermissionsDocument Load(Stream documentStream)
        {
            return Load(JsonDocument.Parse(documentStream));
        }
        public static PermissionsDocument Load(JsonDocument doc)
        {
            return Load(doc.RootElement);
        }

        public static PermissionsDocument LoadFromFolder(string documentPath)
        {
            var mergedDoc = new PermissionsDocument();
            var mergedPermissions = new Dictionary<string, Permission>();
            foreach (var permissionsFile in Directory.EnumerateFiles(documentPath, "*.json"))
            {
                if (Path.GetFileName(permissionsFile).Equals("provisioningInfo.json",StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                try
                {
                    using var stream = new FileStream(permissionsFile, FileMode.Open);
                    var doc = Load(stream);
                    mergedPermissions = mergedPermissions.Concat(doc.Permissions).ToDictionary(x => x.Key, x => x.Value);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Unable to parse json file {permissionsFile}. {ex.Message}");
                }
                
            }

            mergedDoc.Permissions = new SortedDictionary<string, Permission>(mergedPermissions);
            return mergedDoc;
        }

        public static PermissionsDocument Load(JsonElement value)
        {
            var permissionsDocument = new PermissionsDocument();
            ParsingHelpers.ParseMap(value, permissionsDocument, handlers);
            return permissionsDocument;
        }

        private static readonly FixedFieldMap<PermissionsDocument> handlers = new()
        {
            { "permissions", (d,v) => { d.Permissions = ParsingHelpers.GetOrderedMap(v,Permission.Load);  } },
            { "$schema", (d,v) => {  } }
        };
        
    }

        

    public enum SchemeType
    {
        DelegatedWork,
        DelegatedPersonal,
        Application,
        ResourceSpecificConsent
    }

}
