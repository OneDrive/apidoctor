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
        private readonly string[] validNamespaces;
        private readonly string baseUrl;

        public CsdlWriter(DocSet docs, string[] namespacesToExport, string baseUrl)
            : base(docs)
        {
            this.validNamespaces = namespacesToExport;
            this.baseUrl = baseUrl;
        }

        public override async Task PublishToFolderAsync(string outputFolder)
        {
            // Step 1: Generate an EntityFramework OM from the documentation
            EntityFramework framework = CreateEntityFrameworkFromDocs(this.baseUrl);

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

        private EntityFramework CreateEntityFrameworkFromDocs(string baseUrlToRemove)
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
            this.BuildEntityCollection(edmx, baseUrlToRemove);

            // Add actions to the collection
            this.ProcessRestRequestPaths(edmx, baseUrlToRemove);

            return edmx;
        }

        /// <summary>
        /// Scan the MethodDefintions in the documentation and create actions and functions in the 
        /// EntityFramework for matching call patterns.
        /// </summary>
        /// <param name="edmx"></param>
        private void ProcessRestRequestPaths(EntityFramework edmx, string baseUrlToRemove)
        {
            Dictionary<string, MethodCollection> uniqueRequestPaths = GetUniqueRequestPaths(baseUrlToRemove);

            foreach (var path in uniqueRequestPaths.Keys)
            {
                var methodCollection = uniqueRequestPaths[path];

                ODataTargetInfo requestTarget = null;
                try
                {
                    requestTarget = ParseRequestTargetType(path, methodCollection, edmx);
                    if (requestTarget.Classification == ODataTargetClassification.Unknown &&
                        !string.IsNullOrEmpty(requestTarget.Name) &&
                        requestTarget.QualifiedType != null)
                    {
                        CreateNewActionOrFunction(edmx, methodCollection, requestTarget);
                    }
                    else if (requestTarget.Classification == ODataTargetClassification.EntityType)
                    {
                        // We've learned more about this entity type, let's add that information to the state
                        AppendToEntityType(edmx, requestTarget, methodCollection);
                    }
                    else if (requestTarget.Classification == ODataTargetClassification.NavigationProperty)
                    {
                        // TODO: Record somewhere the operations that are available on this NavigationProperty
                        AppendToNavigationProperty(edmx, requestTarget, methodCollection);
                    }
                    else
                    {
                        // TODO: Are there interesting things to learn here?
                        Console.WriteLine("Found type {0}: {1}", requestTarget.Classification, path);
                    }
                }
                catch (Exception ex)
                {
                    // TODO: Log this out better than this.
                    Console.WriteLine("Couldn't serialize request for path {0} into EDMX: {1}", path, ex.Message);
                    continue;
                }
            }
        }

        private void AppendToNavigationProperty(EntityFramework edmx, ODataTargetInfo navigationProperty, MethodCollection methods)
        {
            EntityType parentType = edmx.ResourceWithIdentifier<EntityType>(navigationProperty.QualifiedType);

            NavigationProperty matchingProperty =
                parentType.NavigationProperties.FirstOrDefault(np => np.Name == navigationProperty.Name);
            if (null != matchingProperty)
            {
                // TODO: Append information from methods into this navigation property
                StringBuilder sb = new StringBuilder();
                const string seperator = ", ";
                sb.AppendWithCondition(methods.GetAllowed, "GET", seperator);
                sb.AppendWithCondition(methods.PostAllowed, "POST", seperator);
                sb.AppendWithCondition(methods.PutAllowed, "PUT", seperator);
                sb.AppendWithCondition(methods.DeleteAllowed, "DELETE", seperator);

                Console.WriteLine("Collection '{0}' supports: ({1})", navigationProperty.QualifiedType + "/" + navigationProperty.Name, sb);
            }
            else
            {
                Console.WriteLine(
                    "EntityType '{0}' doesn't have a matching navigationProperty '{1}' but a request exists for this. Sounds like a documentation error.",
                    navigationProperty.QualifiedType,
                    navigationProperty.Name);
            }
        }

        /// <summary>
        /// Use the properties of methodCollection to augment what we know about this entity type
        /// </summary>
        /// <param name="requestTarget"></param>
        /// <param name="methodCollection"></param>
        private void AppendToEntityType(EntityFramework edmx, ODataTargetInfo requestTarget, MethodCollection methodCollection)
        {
            StringBuilder sb = new StringBuilder();
            const string seperator = ", ";
            sb.AppendWithCondition(methodCollection.GetAllowed, "GET", seperator);
            sb.AppendWithCondition(methodCollection.PostAllowed, "POST", seperator);
            sb.AppendWithCondition(methodCollection.PutAllowed, "PUT", seperator);
            sb.AppendWithCondition(methodCollection.DeleteAllowed, "DELETE", seperator);

            Console.WriteLine("EntityType '{0}' supports: ({1})", requestTarget.QualifiedType, sb.ToString());
        }

        private void CreateNewActionOrFunction(EntityFramework edmx, MethodCollection methodCollection, ODataTargetInfo requestTarget)
        {
            // Create a new action (not idempotent) / function (idempotent) based on this request method!
            ActionOrFunctionBase target = null;
            if (methodCollection.AllMethodsIdempotent)
            {
                target = new Validation.OData.Function();
            }
            else
            {
                target = new Validation.OData.Action();
            }

            var schemaName = requestTarget.Name.NamespaceOnly();
            target.Name = requestTarget.Name.TypeOnly();
            target.IsBound = true;
            target.Parameters.Add(new Parameter { Name = "bindingParameter", Type = requestTarget.QualifiedType, Nullable = false });
            foreach (var param in methodCollection.RequestBodyParameters)
            {
                target.Parameters.Add(
                    new Parameter
                    {
                        Name = param.Name,
                        Type = param.Type.ODataResourceName(),
                        Nullable = (param.Required.HasValue ? param.Required.Value : false)
                    });
            }

            if (methodCollection.ResponseType != null)
            {
                target.ReturnType = new ReturnType { Type = methodCollection.ResponseType.ODataResourceName(), Nullable = false };
            }

            var schema = FindOrCreateSchemaForNamespace(schemaName, edmx, true);
            if (target is Function)
                schema.Functions.Add((Function)target);
            else
                schema.Actions.Add((Validation.OData.Action)target);
        }

        /// <summary>
        /// Walks the requestPath through the resources / entities defined in the edmx and resolves
        /// the type of request represented by the path
        /// </summary>
        /// <param name="requestPath"></param>
        /// <param name="requestMethod"></param>
        /// <param name="edmx"></param>
        /// <returns></returns>
        private static ODataTargetInfo ParseRequestTargetType(string requestPath, MethodCollection requestMethodCollection, EntityFramework edmx)
        {
            string[] requestParts = requestPath.Substring(1).Split(new char[] { '/'});

            EntityContainer entryPoint = (from s in edmx.DataServices.Schemas
                                          where s.EntityContainers.Count > 0
                                          select s.EntityContainers.FirstOrDefault()).SingleOrDefault();

            if (entryPoint == null) throw new InvalidOperationException("Couldn't locate an EntityContainer to begin target resolution");

            IODataNavigable currentObject = entryPoint;
            IODataNavigable previousObject = null;

            for(int i=0; i<requestParts.Length; i++)
            {
                string uriPart = requestParts[i];
                IODataNavigable nextObject = null;
                if (uriPart == "{var}")
                {
                    try
                    {
                        nextObject = currentObject.NavigateByEntityTypeKey(edmx);
                    }
                    catch (Exception ex)
                    {
                        throw new NotSupportedException("Unable to navigation into EntityType by key: " + currentObject.TypeIdentifier + " (" + ex.Message + ")");
                    }


                }
                else
                {
                    nextObject = currentObject.NavigateByUriComponent(uriPart, edmx);
                }

                if (nextObject == null && i == requestParts.Length - 1)
                {
                    // The last component wasn't known already, so that means we have a new thing.
                    // We assume that if the uriPart doesnt' have a namespace that this is a navigation property that isn't documented.

                    // TODO: We may need to be smarter about this if we allow actions without namespaces. If that's the case, we could look at the request
                    // method to figure out of this appears to be an action (POST?) or a navigationProperty (GET?)

                    return new ODataTargetInfo
                    {
                        Name = uriPart,
                        Classification = uriPart.HasNamespace() ? ODataTargetClassification.Unknown : ODataTargetClassification.NavigationProperty,
                        QualifiedType = edmx.LookupIdentifierForType(currentObject)
                    };
                }
                else if (nextObject == null)
                {
                    throw new InvalidOperationException(
                        string.Format("Uri path requires navigating into unknown object hierarchy: missing property '{0}' on '{1}'", uriPart, currentObject.TypeIdentifier));
                }
                previousObject = currentObject;
                currentObject = nextObject;
            }

            var response = new ODataTargetInfo
            {
                Name = requestParts.Last(),
                QualifiedType = edmx.LookupIdentifierForType(currentObject)
            };

            if (currentObject is EntityType)
                response.Classification = ODataTargetClassification.EntityType;
            else if (currentObject is EntityContainer)
                response.Classification = ODataTargetClassification.EntityContainer;
            else if (currentObject is ODataSimpleType)
                response.Classification = ODataTargetClassification.SimpleType;
            else if (currentObject is ODataCollection)
            {
                if (previousObject != entryPoint)
                {
                    response.Classification = ODataTargetClassification.NavigationProperty;
                    response.QualifiedType = edmx.LookupIdentifierForType(previousObject);
                }
                else
                {
                    response.Classification = ODataTargetClassification.EntitySet;
                }
            }
            else if (currentObject is ComplexType)
            {
                throw new NotSupportedException(string.Format("Encountered a ComplexType. This is probably a doc bug where type '{0}' should be defined with keyProperty to be an EntityType", currentObject.TypeIdentifier));
            }
            else
            {
                throw new NotSupportedException(string.Format("Unhandled object type: {0}", currentObject.GetType().Name));
            }

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
        private void BuildEntityCollection(EntityFramework edmx, string baseUrlToRemove)
        {
            Dictionary<string, MethodCollection> uniqueRequestPaths = GetUniqueRequestPaths(baseUrlToRemove);
            var resourcePaths = uniqueRequestPaths.Keys.OrderBy(x => x).ToArray();

            EntityContainer container = new EntityContainer();
            foreach (var path in resourcePaths)
            {
                if (EntitySetPathRegEx.IsMatch(path))
                {
                    var name = EntitySetPathRegEx.Match(path).Groups[1].Value;
                    container.EntitySets.Add(new EntitySet { Name = name, EntityType = uniqueRequestPaths[path].ResponseType.ODataResourceName() });
                }
                else if (SingletonPathRegEx.IsMatch(path))
                {
                    // Before we declare this a singleton, see if any other paths that have the same root match the entity set regex
                    var query = (from p in resourcePaths where p.StartsWith(path + "/") && EntitySetPathRegEx.IsMatch(p) select p);
                    if (query.Any())
                    {
                        // If there's a similar resource path that matches the entity, we don't declare a singleton.
                        continue;
                    }

                    var name = SingletonPathRegEx.Match(path).Groups[1].Value;
                    container.Singletons.Add(new Singleton { Name = name, Type = uniqueRequestPaths[path].ResponseType.ODataResourceName() });
                }
            }

            // TODO: Allow the default schema name to be specified instead of inferred
            var largestSchema = (from x in edmx.DataServices.Schemas
                                 orderby x.Entities.Count descending
                                 select x).First();
            container.Name = largestSchema.Namespace;
            largestSchema.EntityContainers.Add(container);
        }

        private Dictionary<string, MethodCollection> cachedUniqueRequestPaths { get; set; }
    
        /// <summary>
        /// Return a dictionary of the unique request paths in the 
        /// documentation and the method definitions that defined them.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, MethodCollection> GetUniqueRequestPaths(string baseUrlToRemove)
        {
            if (cachedUniqueRequestPaths == null)
            {
                Dictionary<string, MethodCollection> uniqueRequestPaths = new Dictionary<string, MethodCollection>();
                foreach (var m in Documents.Methods)
                {
                    if (m.ExpectedResponseMetadata.ExpectError)
                    {
                        // Ignore thigns that are expected to error
                        continue;
                    }

                    var path = m.RequestUriPathOnly(baseUrlToRemove);
                    if (!path.StartsWith("/"))
                    {
                        // Ignore aboslute URI paths
                        continue;
                    }

                    Console.WriteLine("Converted '{0}' into generic form '{1}'", m.Request.FirstLineOnly(), path);

                    if (!uniqueRequestPaths.ContainsKey(path))
                    {
                        uniqueRequestPaths.Add(path, new MethodCollection());
                    }
                    uniqueRequestPaths[path].Add(m);

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
            type.OpenType = resource.OriginalMetadata.IsOpenType;
            type.Properties = (from p in resource.Parameters
                               where !p.IsNavigatable && !p.Name.StartsWith("@")
                               select ConvertParameterToProperty<Property>(p) ).ToList();

            var annotations = (from p in resource.Parameters where p.Name != null && p.Name.StartsWith("@") select p);
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
                    term.Annotations.Add(new Annotation { Term = Term.LongDescriptionTerm, String = prop.Description });
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
            var prop = new T()
            {
                Name = param.Name,
                Nullable = (param.Required.HasValue ? !param.Required.Value : false),
                Type = param.Type.ODataResourceName()
            };

            // Add description annotation
            if (!string.IsNullOrEmpty(param.Description))
            {
                prop.Annotation = new List<Annotation>();
                prop.Annotation.Add(
                    new Annotation()
                    {
                        Term = Term.DescriptionTerm,
                        String = param.Description
                    });
            }
            return prop;
        }
    }

}
