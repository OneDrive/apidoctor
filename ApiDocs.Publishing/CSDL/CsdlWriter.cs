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

namespace ApiDocs.Publishing.CSDL
{
    using ApiDocs.Validation;
    using ApiDocs.Validation.Writers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ApiDocs.Validation.OData;

    public class CsdlWriter : DocumentPublisher
    {
        public CsdlWriter(DocSet docs)
            : base(docs)
        {

        }

        public override async Task PublishToFolderAsync(string outputFolder)
        {
            // Step 1: Generate an EntityFramework OM from the documentation
            EntityFramework framework = CreateEntityFrameworkFromDocs();

            // Step 2: Generate XML representation of EDMX
            var xmlData = ODataParser.GenerateEdmx(framework);

            // Step 3: Write the XML to disk
            var outputFilename = System.IO.Path.Combine(outputFolder, "metadata.edmx");
            using (var writer = System.IO.File.CreateText(outputFilename))
            {
                await writer.WriteAsync(xmlData);
                await writer.FlushAsync();
                writer.Close();
            }
        }

        private EntityFramework CreateEntityFrameworkFromDocs()
        {
            var edmx = new EntityFramework();
            
            // Add resources
            foreach (var resource in Documents.Resources)
            {
                Schema parentSchema = FindOrCreateSchemaForNamespace(resource.Name.NamespaceOnly(), edmx);
                AddResourceToSchema(parentSchema, resource);
            }

            return edmx;
        }

        private static Schema FindOrCreateSchemaForNamespace(string ns, EntityFramework edmx)
        {
            var matchingSchema = (from s in edmx.DataServices.Schemas
                                 where s.Namespace == ns
                                 select s).FirstOrDefault();

            if (null != matchingSchema)
                return matchingSchema;

            var newSchema = new Schema() { Namespace = ns };
            edmx.DataServices.Schemas.Add(newSchema);
            return newSchema;
        }


        private static void AddResourceToSchema(Schema schema, ResourceDefinition resource)
        {
            ComplexType type;
            if (!string.IsNullOrEmpty(resource.KeyPropertyName))
            {
                var entity = new EntityType();
                entity.Key = new Key { PropertyRef = new PropertyRef { Name = resource.KeyPropertyName } };
                // TODO: Also need to add navigation properties
                schema.Entities.Add(entity);
                type = entity;
            }
            else
            {
                type = new ComplexType();
                schema.ComplexTypes.Add(type);
            }
            type.Name = resource.Name.TypeOnly();
            type.Properties = (from p in resource.Parameters
                               select ConvertParameterToProperty(p) ).ToList();
        }

        private static Property ConvertParameterToProperty(ParameterDefinition param)
        {
            return new Property()
            {
                Name = param.Name,
                Nullable = !param.Required,
                Type = param.Type.ODataResourceName()
            };
        }


    }

    internal static class ODataExtensionMethods
    {
        /// <summary>
        /// Returns "oneDrive" for "oneDrive.item" input.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string NamespaceOnly(this string type)
        {
            var trimPoint = type.LastIndexOf('.');
            return type.Substring(0, trimPoint);
        }


        /// <summary>
        /// Returns "item" for "oneDrive.item" input.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string TypeOnly(this string type)
        {
            var trimPoint = type.LastIndexOf('.');
            return type.Substring(trimPoint + 1);
        }

        /// <summary>
        /// Convert a ParameterDataType instance into the OData equivelent.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ODataResourceName(this ParameterDataType type)
        {

            if (type.Type == SimpleDataType.Object && !string.IsNullOrEmpty(type.CustomTypeName))
            {
                return type.CustomTypeName;
            }

            if (type.Type == SimpleDataType.Collection)
            {
                return string.Format("Collection({0})", type.CollectionResourceType.ODataResourceName(type.CustomTypeName));
            }

            return type.Type.ODataResourceName();
        }

        /// <summary>
        /// Convert a simple type into OData equivelent. If Object is specified, a customDataType can be returned instead.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="customDataType"></param>
        /// <returns></returns>
        public static string ODataResourceName(this SimpleDataType type, string customDataType = null)
        {
            switch (type)
            {
                case SimpleDataType.String:
                    return "Edm.String";
                case SimpleDataType.Int64:
                    return "Edm.Int64";
                case SimpleDataType.Int32:
                    return "Edm.Int32";
                case SimpleDataType.Boolean:
                    return "Edm.Boolean";
                case SimpleDataType.DateTimeOffset:
                    return "Edm.DateTimeOffset";
                case SimpleDataType.Double:
                    return "Edm.Double";
                case SimpleDataType.Float:
                    return "Edm.Float";
                case SimpleDataType.Guid:
                    return "Edm.Guid";
                case SimpleDataType.TimeSpan:
                    return "Edm.TimeSpan";
                case SimpleDataType.Object:
                    if (string.IsNullOrEmpty(customDataType))
                        return "Edm.Object";
                    else
                        return customDataType;
                default:
                    throw new NotSupportedException(string.Format("Attempted to convert an unsupported SimpleDataType into OData: {0}", type));
            }
        }
    }

}
