using System.Text.Json;

namespace Kibali
{
    public class ProvisioningInfo
    {
        public string Id { get; set; }

        public string Scheme { get; set; }

        public bool IsHidden { get; set; }

        public bool IsEnabled { get; set; }

        public string Environment { get; set; }

        public string ResourceAppId { get; set; }


        public void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            if (!string.IsNullOrWhiteSpace(Id)) writer.WriteString("id", Id);

            if (!string.IsNullOrWhiteSpace(Scheme)) writer.WriteString("scheme", Scheme);

            if (IsHidden) writer.WriteBoolean("isHidden", IsHidden);

            if (IsEnabled) writer.WriteBoolean("isEnabled", IsEnabled);

            if (!string.IsNullOrWhiteSpace(Environment)) writer.WriteString("environment", Environment);

            if (!string.IsNullOrWhiteSpace(ResourceAppId)) writer.WriteString("resourceAppId", ResourceAppId);

            writer.WriteEndObject();
        }

        public static ProvisioningInfo Load(JsonElement value)
        {
            var provisioningInfo = new ProvisioningInfo();
            ParsingHelpers.ParseMap(value, provisioningInfo, handlers);
            return provisioningInfo;
        }

        private static readonly FixedFieldMap<ProvisioningInfo> handlers = new()
        {
            { "id", (o,v) => { o.Id = v.GetString(); } },
            { "scheme", (o,v) => { o.Scheme = v.GetString(); } },
            { "isHidden", (o,v) => { o.IsHidden = v.GetBoolean(); } },
            { "isEnabled", (o,v) => { o.IsEnabled = v.GetBoolean(); } },
            { "environment", (o,v) => { o.Environment = v.GetString(); } },
            { "resourceAppId", (o,v) => { o.ResourceAppId= v.GetString(); } },
        };
    }
}
