using System;
using System.Text.Json;

namespace Kibali
{
    public class Scheme
    {
        public string AdminDisplayName { get; set; }
        public string AdminDescription { get; set; }
        public string UserDisplayName { get; set; }
        public string UserDescription { get; set; }
        public bool RequiresAdminConsent { get; set; }
        public bool IsPreauthorizationOnly { get; set; }
        public int PrivilegeLevel { get; set; }

        public void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            if (!string.IsNullOrEmpty(AdminDisplayName)) writer.WriteString("adminDisplayName", AdminDisplayName);
            if (!string.IsNullOrEmpty(AdminDescription)) writer.WriteString("adminDescription", AdminDescription);
            if (!string.IsNullOrEmpty(UserDisplayName)) writer.WriteString("userDisplayName", UserDisplayName);
            if (!string.IsNullOrEmpty(UserDescription)) writer.WriteString("userDescription", UserDescription);
            if (RequiresAdminConsent) writer.WriteBoolean("requiresAdminConsent", RequiresAdminConsent);
            if (PrivilegeLevel != 0) writer.WriteNumber("privilegeLevel", PrivilegeLevel);
            if (IsPreauthorizationOnly) writer.WriteBoolean("isPreauthorizationOnly", true);
          
            writer.WriteEndObject();
        }


        public static Scheme Load(JsonElement value)
        {
            var scheme = new Scheme();
            ParsingHelpers.ParseMap(value, scheme, handlers);
            return scheme;
        }

        private static readonly FixedFieldMap<Scheme> handlers = new()
        {
            { "adminDisplayName", (o,v) => {o.AdminDisplayName = v.GetString();  } },
            { "adminDescription", (o,v) => {o.AdminDescription = v.GetString();  } },
            { "userDisplayName", (o,v) => {o.UserDisplayName = v.GetString();  } },
            { "userDescription", (o,v) => {o.UserDescription = v.GetString();  } },
            { "requiresAdminConsent", (o,v) => {o.RequiresAdminConsent = v.GetBoolean();  } },
            { "privilegeLevel", (o,v) => {o.PrivilegeLevel = v.GetInt32();  } },
            { "isPreauthorizationOnly", (o,v) => { o.IsPreauthorizationOnly = v.GetBoolean(); } },
        };
    }

}
