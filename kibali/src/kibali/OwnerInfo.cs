using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kibali
{
    public class OwnerInfo
    {
        public string OwnerSecurityGroup { get; set; }

        public void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            if (!string.IsNullOrWhiteSpace(OwnerSecurityGroup)) writer.WriteString("ownerSecurityGroup", OwnerSecurityGroup);

            writer.WriteEndObject();
        }

        public static OwnerInfo Load(JsonElement value)
        {
            var ownerInfo = new OwnerInfo();
            ParsingHelpers.ParseMap(value, ownerInfo, handlers);
            return ownerInfo;
        }

        private static readonly FixedFieldMap<OwnerInfo> handlers = new()
        {
            { "ownerSecurityGroup", (o,v) => { o.OwnerSecurityGroup= v.GetString(); } },
        };
    }
}
