using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OneDrive.ApiDocumentation.Validation
{

    public class ScenarioFile : ConfigFile
    {
        [JsonProperty("scenarios")]
        public ScenarioDefinition[] Scenarios { get; set; }

        public override bool IsValid
        {
            get
            {
                return Scenarios != null;
            }
        }
    }

    public static class ScenarioExtensionMethods
    {
        public static ScenarioDefinition[] ScenariosForMethod(this IEnumerable<ScenarioDefinition> scenarios, MethodDefinition method)
        {
            var id = method.Identifier;
            var query = from p in scenarios
                        where p.MethodName == id
                        select p;

            return query.ToArray();
        }
    }

}
