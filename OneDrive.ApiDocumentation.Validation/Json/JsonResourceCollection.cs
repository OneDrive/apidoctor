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
            var schema = new JsonSchema(resource);
            m_RegisteredSchema[resource.Metadata.ResourceType] = schema;
        }
        
        /// <summary>
        /// Validates the value of actualJson according to an implicit schmea defined by expectedJson
        /// </summary>
        /// <param name="expectedJson"></param>
        /// <param name="actualJson"></param>
        /// <returns></returns>
        public bool ValidateJsonExample(CodeBlockAnnotation annotation, string json, out ValidationError[] errors)
        {
            List<ValidationError> newErrors = new List<ValidationError>();

            var resourceType = annotation.ResourceType;
            if (resourceType == "stream")
            {
                // No validation since we're streaming data
                errors = null;
                return true;
            }
            else
            {
                JsonSchema schema;
                if (string.IsNullOrEmpty(resourceType))
                {
                    schema = JsonSchema.EmptyResponseSchema;
                }
                else if (!m_RegisteredSchema.TryGetValue(resourceType, out schema))
                {
                    newErrors.Add(new ValidationWarning(ValidationErrorCode.ResponseResourceTypeMissing, null, "Missing required resource: {0}. Validation based on json in method response.", resourceType));
                    // Create a new schema based on what's avaiable in the json
                    schema = new JsonSchema(json, new CodeBlockAnnotation { ResourceType = annotation.ResourceType });
                }

                ValidationError[] validationJsonOutput;
                ValidateJson(schema, json, annotation, out validationJsonOutput);

                newErrors.AddRange(validationJsonOutput);
                errors = newErrors.ToArray();
                return errors.Length == 0;
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
