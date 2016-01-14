/*
 * Markdown Scanner
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDocs.Validation.OData
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Newtonsoft.Json;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    /// <summary>
    /// Converts OData input into json examples which can be validated against our 
    /// ResourceDefinitions in a DocSet.
    /// </summary>
    public class ODataParser
    {
        private static readonly IDictionary<string, object> ODataSimpleTypeExamples = new Dictionary<string, object>() {
            { "Edm.String", "string" },
            { "Edm.Boolean", false },
            { "Edm.Int64", 1234567890 },
            { "Edm.Int32", 1234 },
            { "Edm.Int16", 1234 },
            { "Edm.Double", 12.345678 },
            { "Edm.Decimal", 12.345678901234 },
            { "Edm.Float", 12.3456 },
            { "Edm.Byte", 12 },
            { "Edm.SByte", -12 },
            { "Edm.DateTime", "2014-01-01T00:00:00Z" },
            { "Edm.DateTimeOffset", "2014-01-01T00:00:00Z" },
            { "Edm.Time", "00:00:00Z" },
            { "Edm.Guid", "9F328426-8A81-40D1-8F35-D619AA90A12C" }
        };

        #region Static EDMX -> EntityFramework methods 
        public static EntityFramework DeserializeEntityFramework(string metadataContent)
        {
            XmlSerializer ser = new XmlSerializer(typeof(EntityFramework));

            StringReader reader = new StringReader(metadataContent);
            EntityFramework result = (EntityFramework)ser.Deserialize(reader);

            return result;
        }

        public static async Task<EntityFramework> ParseEntityFrameworkFromUrlAsync(Uri remoteUrl)
        {
            var request = WebRequest.CreateHttp(remoteUrl);
            var response = await request.GetResponseAsync();
            var httpResponse = response as HttpWebResponse;
            if (httpResponse == null)
                return null;

            string remoteMetadataContents = null;
            using (var stream = httpResponse.GetResponseStream())
            {
                if (null != stream)
                {
                    StreamReader reader = new StreamReader(stream);
                    remoteMetadataContents = await reader.ReadToEndAsync();
                }
            }

            return DeserializeEntityFramework(remoteMetadataContents);
        }

        public static async Task<EntityFramework> ParseEntityFrameworkFromFileAsync(string path)
        {
            StreamReader reader = new StreamReader(path);
            string localMetadataContents = await reader.ReadToEndAsync();

            return  DeserializeEntityFramework(localMetadataContents);
        }

        /// <summary>
        /// Convert resources found in the CSDL schema objects into ResourceDefintion instances
        /// that can be tested against the documentation.
        /// </summary>
        /// <param name="schemas"></param>
        /// <returns></returns>
        public static List<ResourceDefinition> GenerateResourcesFromSchemas(IEnumerable<Schema> schemas)
        {
            List<ResourceDefinition> resources = new List<ResourceDefinition>();
            
            foreach (var schema in schemas)
            {
                resources.AddRange(CreateResourcesFromSchema(schema, schemas));
            }

            return resources;
        }

        private static IEnumerable<ResourceDefinition> CreateResourcesFromSchema(Schema schema, IEnumerable<Schema> otherSchema)
        {
            List<ResourceDefinition> resources = new List<ResourceDefinition>();

            resources.AddRange(from ct in schema.ComplexTypes select ResourceDefinitionFromType(schema, otherSchema, ct));
            resources.AddRange(from et in schema.Entities select ResourceDefinitionFromType(schema, otherSchema, et));

            return resources;
        }

        private static ResourceDefinition ResourceDefinitionFromType(Schema schema, IEnumerable<Schema> otherSchema, ComplexType ct)
        {
            var annotation = new CodeBlockAnnotation() { ResourceType = string.Concat(schema.Namespace, ".", ct.Name), BlockType = CodeBlockType.Resource };
            var json = BuildJsonExample(ct, otherSchema);
            ResourceDefinition rd = new JsonResourceDefinition(annotation, json, null);
            return rd;
        }

        private static string BuildJsonExample(ComplexType ct, IEnumerable<Schema> otherSchema)
        {
            Dictionary<string, object> dict = BuildDictionaryExample(ct, otherSchema);
            return JsonConvert.SerializeObject(dict);
        }

        private static Dictionary<string, object> BuildDictionaryExample(ComplexType ct, IEnumerable<Schema> otherSchema)
        {
            return ct.Properties.Where(prop => prop.Type != "Edm.Stream").ToDictionary(prop => prop.Name, prop => ExampleOfType(prop.Type, otherSchema));
        }

        private static readonly string CollectionPrefix = "Collection(";
        private static object ExampleOfType(string typeIdentifier, IEnumerable<Schema> otherSchemas)
        {
            if (typeIdentifier.StartsWith(CollectionPrefix) && typeIdentifier.EndsWith(")"))
            {
                var arrayTypeIdentifier = typeIdentifier.Substring(CollectionPrefix.Length);
                arrayTypeIdentifier = arrayTypeIdentifier.Substring(0, arrayTypeIdentifier.Length - 1);

                var obj = ObjectExampleForType(arrayTypeIdentifier, otherSchemas);
                return new object[] { obj, obj };
            }

            return ObjectExampleForType(typeIdentifier, otherSchemas);
        }

        private static object ObjectExampleForType(string typeIdentifier, IEnumerable<Schema> otherSchemas)
        {
            if (ODataSimpleTypeExamples.ContainsKey(typeIdentifier))
                return ODataSimpleTypeExamples[typeIdentifier];

            // If that fails, the we need to locate the typeIdentifier in the schemas and
            // generate an example from that.
            ComplexType matchingType = null;
            try
            {
                matchingType = otherSchemas.ResourceWithIdentifier<ComplexType>(typeIdentifier);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception loading type with identifier: {0} - {1}", typeIdentifier, ex.Message);
            }

            if (null == matchingType)
            {
                Debug.WriteLine("Failed to find an example for type: " + typeIdentifier);
                return new { datatype = typeIdentifier };
            }
            else
                return BuildDictionaryExample(matchingType, otherSchemas);
        }

        #endregion

        #region EntityFramework -> EDMX methods

        public const string EdmxNamespace = "http://docs.oasis-open.org/odata/ns/edmx";
        public const string EdmNamespace = "http://docs.oasis-open.org/odata/ns/edm";

        /// <summary>
        /// Generates an entity framework XML file for a given input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GenerateEdmx(EntityFramework input)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter stringWriter = new StringWriter(sb);
            XmlWriter writer = XmlWriter.Create(
                stringWriter,
                new XmlWriterSettings { Encoding = new UTF8Encoding(false), OmitXmlDeclaration = true, Indent = true });

            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("edmx", ODataParser.EdmxNamespace);

            XmlSerializer ser = new XmlSerializer(typeof(EntityFramework));
            ser.Serialize(writer, input, ns);
            writer.Flush();

            return sb.ToString();
        }
        #endregion

    }
}
