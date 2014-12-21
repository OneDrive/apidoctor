using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApiDocumentationTester
{
    public class JsonValidator
    {

        private Dictionary<string, JsonSchema> m_RegisteredSchema = new Dictionary<string,JsonSchema>();

        public JsonSchema[] RegisteredSchema
        {
            get { return m_RegisteredSchema.Values.ToArray(); }
        }

        public void RegisterJsonResource(string name, string json)
        {
            //try
            //{
                var schema = new JsonSchema(json) { ResourceName = name };
                m_RegisteredSchema[name] = schema;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Failed to register schema for {0}: {1}", name, ex.Message);
            //}
        }
        
        /// <summary>
        /// Validates the value of actualJson according to an implicit schmea defined by expectedJson
        /// </summary>
        /// <param name="expectedJson"></param>
        /// <param name="actualJson"></param>
        /// <returns></returns>
        public bool ValidateJson(string resourceType, string json, bool jsonIsCollection, out ValidationError[] errors)
        {
            var schema = m_RegisteredSchema[resourceType];
            return ValidateJson(schema, json, jsonIsCollection, out errors);
        }

        public bool ValidateJson(JsonSchema schema, string json, bool jsonIsCollection, out ValidationError[] errors)
        {
            return schema.ValidateJson(json, out errors, m_RegisteredSchema, jsonIsCollection);
        }
        
    }


   
}
