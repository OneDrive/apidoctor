using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kibali
{
    public class PermissionsDeployment
    {
        public Dictionary<string, List<ProvisioningInfo>> Deployments { get; set; } = new();

        public static PermissionsDeployment Load(string document)
        {
            return Load(JsonDocument.Parse(document));
        }

        public static PermissionsDeployment Load(Stream documentStream)
        {
            return Load(JsonDocument.Parse(documentStream));
        }
        public static PermissionsDeployment Load(JsonDocument doc)
        {
            return Load(doc.RootElement);
        }

        public static PermissionsDeployment Load(JsonElement value)
        {
            var permissionsDeployment = new PermissionsDeployment();
            ParsingHelpers.ParseMap(value, permissionsDeployment, handlers);
            return permissionsDeployment;
        }

        private static readonly FixedFieldMap<PermissionsDeployment> handlers = new()
        {
            { "permissionDeployments", (d,v) => { d.Deployments = ParsingHelpers.GetMapOfLists(v,ProvisioningInfo.Load);  } },
        };
    }
}
