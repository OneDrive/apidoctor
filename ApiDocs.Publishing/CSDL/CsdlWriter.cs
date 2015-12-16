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
        private string[] validNamespaces;

        public CsdlWriter(DocSet docs, string[] namespacesToExport)
            : base(docs)
        {
            validNamespaces = namespacesToExport;
        }

        public override async Task PublishToFolderAsync(string outputFolder)
        {
            // Step 1: Generate an EntityFramework OM from the documentation
            EntityFramework framework = CreateEntityFrameworkFromDocs();

            // Step 2: Generate XML representation of EDMX
            var xmlData = ODataParser.GenerateEdmx(framework);

            // Step 3: Write the XML to disk
            var outputDir = new System.IO.DirectoryInfo(outputFolder);
            outputDir.Create();

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
                var targetSchema = FindOrCreateSchemaForNamespace(resource.Name.NamespaceOnly(), edmx);
                if (targetSchema != null)
                {
                    AddResourceToSchema(targetSchema, resource, edmx);
                }
            }

            // Figure out the EntityCollection
            BuildEntityCollection(edmx);

            // Add actions to the collection
            BuildActionsAndFunctions(edmx);

            return edmx;
        }

        /// <summary>
        /// Scan the MethodDefintions in the documentation and create actions and functions in the 
        /// EntityFramework for matching call patterns.
        /// </summary>
        /// <param name="edmx"></param>
        private void BuildActionsAndFunctions(EntityFramework edmx)
        {
            Dictionary<string, MethodDefinition> uniqueRequestPaths = GetUniqueRequestPaths();

            foreach (var path in uniqueRequestPaths.Keys)
            {
                var method = uniqueRequestPaths[path];

                ODataTargetInfo requestTarget = null;
                try
                {
                    requestTarget = ParseRequestTargetType(path, method, edmx);
                    if (requestTarget.Classification == ODataTargetClassification.Unknown &&
                        !string.IsNullOrEmpty(requestTarget.Name) &&
                        requestTarget.QualifiedType != null)
                    {
                        // Create a new action (not idempotent) / function (idempotent) based on this request method!
                        ActionOrFunctionBase target = null;
                        if (method.RequestMetadata.IsIdempotent)
                        {
                            target = new Function();
                        }
                        else
                        {
                            target = new Validation.OData.Action();
                        }

                        var schemaName = requestTarget.Name.NamespaceOnly();
                        target.Name = requestTarget.Name.TypeOnly();
                        target.IsBound = true;
                        target.Parameters.Add(new Parameter { Name = "bindingParameter", Type = requestTarget.QualifiedType, Nullable = false });
                        if (method.RequestBodyParameters.Count > 0)
                        {
                            // TODO: Convert RequestBodyParameters into Parameters
                        }
                    
                        if (method.ExpectedResponseMetadata.Type != null)
                        {
                            target.ReturnType = new ReturnType { Type = method.ExpectedResponseMetadata.Type.ODataResourceName(), Nullable = false };
                        }

                        var schema = FindOrCreateSchemaForNamespace(schemaName, edmx, true);
                        if (target is Function)
                            schema.Functions.Add((Function)target);
                        else
                            schema.Actions.Add((Validation.OData.Action)target);
                    }
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Walks the requestPath through the resources / entities defined in the edmx and resolves
        /// the type of request represented by the path
        /// </summary>
        /// <param name="requestPath"></param>
        /// <param name="requestMethod"></param>
        /// <param name="edmx"></param>
        /// <returns></returns>
        private static ODataTargetInfo ParseRequestTargetType(string requestPath, MethodDefinition requestMethod, EntityFramework edmx)
        {
            string[] requestParts = requestPath.Substring(1).Split('/');

            EntityContainer entryPoint = (from s in edmx.DataServices.Schemas
                                          where s.EntityContainers.Count > 0
                                          select s.EntityContainers.FirstOrDefault()).SingleOrDefault();

            if (entryPoint == null) throw new InvalidOperationException("Couldn't locate an EntityContainer to begin target resolution");

            IODataNavigable currentObject = entryPoint;

            for(int i=0; i<requestParts.Length; i++)
            {
                string uriPart = requestParts[i];
                IODataNavigable nextObject = null;
                if (uriPart == "{var}")
                {
                    nextObject = currentObject.NavigateByEntityTypeKey();
                }
                else
                {
                    // BUG: This is flawed, because even if we're navigation to a Collection(item), we just get item.
                    nextObject = currentObject.NavigateByUriComponent(uriPart, edmx);
                }

                if (nextObject == null && i == requestParts.Length - 1)
                {
                    // The last component wasn't known already, so that means we have a new thing.
                    return new ODataTargetInfo
                    {
                        Name = uriPart,
                        Classification = ODataTargetClassification.Unknown,
                        QualifiedType = edmx.LookupIdentifierForType(currentObject)
                    };
                }
                else if (nextObject == null)
                {
                    throw new InvalidOperationException("Uri path requires navigating into unknown targets.");
                }
                currentObject = nextObject;
            }

            var response = new ODataTargetInfo
            {
                Name = requestParts.Last(),
                QualifiedType = edmx.LookupIdentifierForType(currentObject)
            };

            if (currentObject is EntityType)
                response.Classification = ODataTargetClassification.EntityType;
            if (currentObject is EntitySet)
                response.Classification = ODataTargetClassification.EntitySet;
            if (currentObject is Validation.OData.Action)
                response.Classification = ODataTargetClassification.Action;
            if (currentObject is Function)
                response.Classification = ODataTargetClassification.Function;
            if (currentObject is EntityContainer)
                response.Classification = ODataTargetClassification.EntityContainer;

            return response;
        }



        // EntitySet is something in the format of /name/{var}
        private readonly static System.Text.RegularExpressions.Regex EntitySetPathRegEx = new System.Text.RegularExpressions.Regex(@"^\/(\w*)\/{var}$");
        // Singleton is something in the format of /name
        private readonly static System.Text.RegularExpressions.Regex SingletonPathRegEx = new System.Text.RegularExpressions.Regex(@"^\/(\w*)$");

        /// <summary>
        /// Parse the URI paths for methods defined in the documentation and construct an entity container that contains these
        /// entity sets / singletons in the largest namespace.
        /// </summary>
        /// <param name="edmx"></param>
        private void BuildEntityCollection(EntityFramework edmx)
        {
            Dictionary<string, MethodDefinition> uniqueRequestPaths = GetUniqueRequestPaths();
            var resourcePaths = uniqueRequestPaths.Keys.OrderBy(x => x).ToArray();

            EntityContainer container = new EntityContainer();
            foreach (var path in resourcePaths)
            {

                if (EntitySetPathRegEx.IsMatch(path))
                {
                    var name = EntitySetPathRegEx.Match(path).Groups[1].Value;
                    container.EntitySets.Add(new EntitySet { Name = name, EntityType = uniqueRequestPaths[path].ExpectedResponseMetadata.ResourceType });
                }
                else if (SingletonPathRegEx.IsMatch(path))
                {
                    var name = SingletonPathRegEx.Match(path).Groups[1].Value;
                    container.Singletons.Add(new Singleton { Name = name, Type = uniqueRequestPaths[path].ExpectedResponseMetadata.ResourceType });
                }
            }

            // TODO: Allow the default schema name to be specified instead of inferred
            var largestSchema = (from x in edmx.DataServices.Schemas
                                 orderby x.Entities.Count descending
                                 select x).First();
            container.Name = largestSchema.Namespace;
            largestSchema.EntityContainers.Add(container);
        }

        private Dictionary<string, MethodDefinition> cachedUniqueRequestPaths = null;

        /// <summary>
        /// Return a dictionary of the unique request paths in the 
        /// documentation and the method definitions that defined them.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, MethodDefinition> GetUniqueRequestPaths()
        {
            if (cachedUniqueRequestPaths == null)
            {
                Dictionary<string, MethodDefinition> uniqueRequestPaths = new Dictionary<string, MethodDefinition>();
                foreach (var m in Documents.Methods)
                {
                    if (m.ExpectedResponseMetadata.ExpectError)
                    {
                        // Ignore thigns that are expected to error
                        continue;
                    }

                    var path = m.RequestUriPathOnly();
                    if (!path.StartsWith("/"))
                    {
                        // Ignore aboslute URI paths
                        continue;
                    }
                    uniqueRequestPaths[path] = m;

                    Console.WriteLine("{0} :: {1} --> {2}", path, m.RequestMetadata.ResourceType, m.ExpectedResponseMetadata.ResourceType);
                }
                cachedUniqueRequestPaths = uniqueRequestPaths;
            }
            return cachedUniqueRequestPaths;
        }

        /// <summary>
        /// Find an existing schema definiton or create a new one in an entity framework for a given namespace.
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="edmx"></param>
        /// <returns></returns>
        private Schema FindOrCreateSchemaForNamespace(string ns, EntityFramework edmx, bool overrideNamespaceFilter = false)
        {
            // Check to see if this is a namespace that should be exported.
            if (!overrideNamespaceFilter && validNamespaces != null && !validNamespaces.Contains(ns))
            {
                return null;
            }

            if (ns.Equals("odata", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var matchingSchema = (from s in edmx.DataServices.Schemas
                                 where s.Namespace == ns
                                 select s).FirstOrDefault();

            if (null != matchingSchema)
                return matchingSchema;

            var newSchema = new Schema() { Namespace = ns };
            edmx.DataServices.Schemas.Add(newSchema);
            return newSchema;
        }


        private void AddResourceToSchema(Schema schema, ResourceDefinition resource, EntityFramework edmx)
        {
            ComplexType type;
            if (!string.IsNullOrEmpty(resource.KeyPropertyName))
            {
                var entity = new EntityType();
                entity.Key = new Key { PropertyRef = new PropertyRef { Name = resource.KeyPropertyName } };
                entity.NavigationProperties = (from p in resource.Parameters
                                               where p.IsNavigatable
                                               select ConvertParameterToProperty<NavigationProperty>(p)).ToList();
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
                               where !p.IsNavigatable && !p.Name.StartsWith("@")
                               select ConvertParameterToProperty<Property>(p) ).ToList();

            var annotations = (from p in resource.Parameters where p.Name.StartsWith("@") select p);
            ParseInstanceAnnotations(annotations, resource, edmx);
        }


        private void ParseInstanceAnnotations(IEnumerable<ParameterDefinition> annotations, ResourceDefinition containedResource, EntityFramework edmx)
        {
            foreach (var prop in annotations)
            {
                var qualifiedName = prop.Name.Substring(1);
                var ns = qualifiedName.NamespaceOnly();
                var localName = qualifiedName.TypeOnly();

                Term term = new Term { Name = localName, AppliesTo = containedResource.Name, Type = prop.Type.ODataResourceName() };
                if (!string.IsNullOrEmpty(prop.Description))
                {
                    term.Annotations.Add(new Annotation { Term = Annotation.LongDescription, String = prop.Description });
                }

                var targetSchema = FindOrCreateSchemaForNamespace(ns, edmx, overrideNamespaceFilter: true);
                if (null != targetSchema)
                {
                    targetSchema.Terms.Add(term);
                }
            }
        }


        private static T ConvertParameterToProperty<T>(ParameterDefinition param) where T : Property, new()
        {
            return new T()
            {
                Name = param.Name,
                Nullable = (param.Required.HasValue ? !param.Required.Value : false),
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
                case SimpleDataType.Stream:
                    return "Edm.Stream";
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
