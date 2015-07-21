namespace ApiDocs.Validation
{
    using System.Collections.Generic;
    using System.Linq;
    using ApiDocs.Validation.Config;
    using ApiDocs.Validation.Params;
    using Newtonsoft.Json;

    public class ScenarioFile : ConfigFile
    {
        [JsonProperty("scenarios")]
        public ScenarioDefinition[] Scenarios { get; set; }

        public override bool IsValid
        {
            get
            {
                return this.Scenarios != null;
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

            if (method.Scenarios != null && method.Scenarios.Count > 0)
            {
                query = query.Union(method.Scenarios);
            }

            return query.ToArray();
        }
    }
}
