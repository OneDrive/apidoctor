using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Kibali
{
    public class AcceptableClaim
    {
        public AcceptableClaim(string permission, bool least, string[] alsoRequires)
        {
            this.Permission = permission;
            this.AlsoRequires = alsoRequires;
            this.Least = least;
        }
        public string Permission { get; }

        public string[] AlsoRequires { get; }

        public bool Least { get; }

        public bool IsHidden { get; set; }

        public bool IsEnabled { get; set; }

        public List<string> SupportedEnvironments { get; set; }

        internal bool IsAuthorized(string[] providedPermissions)
        {
            return providedPermissions.Contains(this.Permission);  //TODO: add support for alsoRequires
        }

        internal void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("permission");
            writer.WriteStringValue(this.Permission);
            if (this.AlsoRequires.Any())
            {
                writer.WritePropertyName("alsoRequires");
                writer.WriteStringValue(string.Join(",", this.AlsoRequires));
            }
            writer.WriteEndObject();
        }
    }
}
