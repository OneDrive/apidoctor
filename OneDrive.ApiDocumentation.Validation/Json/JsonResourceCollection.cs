namespace OneDrive.ApiDocumentation.Validation.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class JsonResourceCollection
    {
        private Dictionary<string, JsonSchema> m_RegisteredSchema = new Dictionary<string,JsonSchema>();

        public JsonSchema[] RegisteredSchema
        {
            get { return m_RegisteredSchema.Values.ToArray(); }
        }

        public void RegisterJsonResource(ResourceDefinition resource)
        {
            var schema = new JsonSchema(resource.JsonExample, resource.Metadata);
            m_RegisteredSchema[resource.Metadata.ResourceType] = schema;
        }
        
        /// <summary>
        /// Validates the value of actualJson according to an implicit schmea defined by expectedJson
        /// </summary>
        /// <param name="expectedJson"></param>
        /// <param name="actualJson"></param>
        /// <returns></returns>
        public bool ValidateJson(CodeBlockAnnotation annotation, string json, out ValidationError[] errors)
        {
            var resourceType = annotation.ResourceType;
            if (resourceType == "stream")
            {
                // No validation since we're streaming data
                errors = null;
                return true;
            }
            else
            {
                var schema = m_RegisteredSchema[resourceType];
                return ValidateJson(schema, json, annotation, out errors);
            }
        }

        public bool ValidateJson(JsonSchema schema, string json, CodeBlockAnnotation annotation, out ValidationError[] errors)
        {
            return schema.ValidateJson(json, out errors, m_RegisteredSchema, annotation);
        }

        internal void RegisterJsonResources(IEnumerable<ResourceDefinition> resources)
        {
            foreach (var resource in resources)
            {
                RegisterJsonResource(resource);
            }
        }

        internal void Clear()
        {
            m_RegisteredSchema.Clear();
        }
    }


   
}
