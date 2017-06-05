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
    using Validation.Config;
    using Validation.OData.Transformation;

    public class CsdlWriter : DocumentPublisher
    {
        private readonly CsdlWriterOptions options;

        public CsdlWriter(DocSet docs, string[] namespacesToExport, string baseUrl, string preexistingEdmxFile, MetadataFormat format = MetadataFormat.Default, bool sortOutput = false)
            : base(docs)
        {
            this.options = new CsdlWriterOptions()
            {
                Namespaces = namespacesToExport,
                BaseUrl = baseUrl,
                SourceMetadataPath = preexistingEdmxFile,
                Formats = format,
                Sort = sortOutput
            };
        }

        public CsdlWriter(DocSet docs, CsdlWriterOptions options)
            : base(docs)
        {
            this.options = options;
        }

        public override async Task PublishToFolderAsync(string outputFolder)
        {
            string outputFilenameSuffix = null;

            // Step 1: Generate an EntityFramework OM from the documentation and/or template file
            EntityFramework framework = CreateEntityFrameworkFromDocs();
            if (null == framework)
                return;

            // Step 1a: Apply an transformations that may be defined in the documentation
            if (!string.IsNullOrEmpty(options.TransformOutput))
            {
                PublishSchemaChangesConfigFile transformations = DocSet.TryLoadConfigurationFiles<PublishSchemaChangesConfigFile>(options.DocumentationSetPath).Where(x => x.SchemaChanges.TransformationName == options.TransformOutput).FirstOrDefault();
                if (null == transformations)
                {
                    throw new KeyNotFoundException($"Unable to locate a transformation set named {options.TransformOutput}. Aborting.");
                }

                string[] versionsToPublish = options.Version?.Split(new char[] { ',', ' '});
                framework.ApplyTransformation(transformations.SchemaChanges, versionsToPublish);
                if (!string.IsNullOrEmpty(options.Version))
                {
                    outputFilenameSuffix = $"-{options.Version}";
                }
            }

            if (options.Sort)
            {
                // Sorts the objects in collections, so that we have consistent output regardless of input
                framework.SortObjectGraph();
            }

            if (options.ValidateSchema)
            {
                framework.ValidateSchemaTypes();
            }

            // Step 2: Generate XML representation of EDMX
            string xmlData = null;
            if (options.Formats.HasFlag(MetadataFormat.EdmxOutput))
            {
                xmlData = ODataParser.Serialize<EntityFramework>(framework, options.AttributesOnNewLines);
            }
            else if (options.Formats.HasFlag(MetadataFormat.SchemaOutput))
            {
                xmlData = ODataParser.Serialize<Schema>(framework.DataServices.Schemas.First(), options.AttributesOnNewLines);
            }

            // Step 3: Write the XML to disk

            var outputFullName = GenerateOutputFileFullName(options.SourceMetadataPath, outputFolder, outputFilenameSuffix);
            Console.WriteLine($"Publishing metadata to {outputFullName}");

            using (var writer = System.IO.File.CreateText(outputFullName))
            {
                await writer.WriteAsync(xmlData);
                await writer.FlushAsync();
                writer.Close();
            }
        }

        private string GenerateOutputFileFullName(string templateFilename, string outputFolderPath, string filenameSuffix)
        {
            var outputDir = new System.IO.DirectoryInfo(outputFolderPath);
            outputDir.Create();

            filenameSuffix = filenameSuffix ?? "";

            string filename = null;
            if (!string.IsNullOrEmpty(templateFilename))
            {
                filename = $"{System.IO.Path.GetFileNameWithoutExtension(templateFilename)}{filenameSuffix}{System.IO.Path.GetExtension(templateFilename)}";
            }
            else
            {
                filename = $"metadata{filenameSuffix}.xml";
            }

            var outputFullName = System.IO.Path.Combine(outputDir.FullName, filename);
            return outputFullName;
        }

        private EntityFramework CreateEntityFrameworkFromDocs()
        {
            EntityFramework edmx = new EntityFramework();
            if (!string.IsNullOrEmpty(options.SourceMetadataPath))
            {
                try
                {
                    if (!System.IO.File.Exists(options.SourceMetadataPath))
                    {
                        throw new System.IO.FileNotFoundException($"Unable to locate source file: {options.SourceMetadataPath}");
                    }

                    using (System.IO.FileStream stream = new System.IO.FileStream(options.SourceMetadataPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    {
                        if (options.Formats.HasFlag(MetadataFormat.EdmxInput))
                        {
                            edmx = ODataParser.Deserialize<EntityFramework>(stream);
                        }
                        else if (options.Formats.HasFlag(MetadataFormat.SchemaInput))
                        {
                            var schema = ODataParser.Deserialize<Schema>(stream);
                            edmx = new EntityFramework();
                            edmx.DataServices.Schemas.Add(schema);
                        }
                        else
                        {
                            throw new InvalidOperationException("Source file was specified but no format for source file was provided.");
                        }
                    }
                    if (options.Namespaces != null && options.Namespaces.Any())
                    {
                        var schemas = edmx.DataServices.Schemas.ToArray();
                        foreach(var s in schemas)
                        {
                            if (!options.Namespaces.Contains(s.Namespace))
                            {
                                edmx.DataServices.Schemas.Remove(s);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to deserialize source template file: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"{ex.InnerException.Message}");
                    }
                    return null;
                }
            }

            bool generateNewElements = !options.SkipMetadataGeneration;

            // Add resources
            if (Documents.Files.Any())
            {
                foreach (var resource in Documents.Resources)
                {
                    var targetSchema = FindOrCreateSchemaForNamespace(resource.Name.NamespaceOnly(), edmx, generateNewElements: generateNewElements);
                    if (targetSchema != null)
                    {
                        AddResourceToSchema(targetSchema, resource, edmx, generateNewElements: generateNewElements);
                    }
                }

                // Figure out the EntityCollection
                this.BuildEntityContainer(edmx, options.BaseUrl);

                // Add actions to the collection
                this.ProcessRestRequestPaths(edmx, options.BaseUrl);
            }

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
                    // TODO: If we have an input Edmx, we may already know what this so we don't need to infer anything.
                    // if that is the case, we should just update it with anything else we know from the documentation.
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
        private void BuildEntityContainer(EntityFramework edmx, string baseUrlToRemove)
        {
            // Check to see if an entitycontainer already exists
            foreach (var s in edmx.DataServices.Schemas)
            {
                if (s.EntityContainers.Any())
                    return;
            }

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
                                 orderby x.EntityTypes.Count descending
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
        private Schema FindOrCreateSchemaForNamespace(string ns, EntityFramework edmx, bool overrideNamespaceFilter = false, bool generateNewElements = true)
        {
            // Check to see if this is a namespace that should be exported.
            if (!overrideNamespaceFilter && options.Namespaces != null && !options.Namespaces.Contains(ns))
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

            if (generateNewElements)
            {
                var newSchema = new Schema() { Namespace = ns };
                edmx.DataServices.Schemas.Add(newSchema);
                return newSchema;
            }

            return null;
        }


        private void AddResourceToSchema(Schema schema, ResourceDefinition resource, EntityFramework edmx, bool generateNewElements = true)
        {
            ComplexType type = null;
            var typeName = resource.Name.TypeOnly();

            // First check to see if there is an existing resource that matches this resource in the framework already
            var existingEntity = (from e in schema.EntityTypes where e.Name == typeName select e).SingleOrDefault();
            if (existingEntity != null)
            {
                type = existingEntity;    
            }

            // If that didn't work, look for a complex type that matches
            if (type == null)
            {
                var existingComplexType = (from e in schema.ComplexTypes where e.Name == typeName select e).SingleOrDefault();
                if (existingComplexType != null)
                {
                    type = existingComplexType;
                }
            }

            // Finally go ahead and create a new resource in the schema if we didn't find a matching one
            if (null == type && generateNewElements)
            {
                type = CreateResourceInSchema(schema, resource);
            }
            else if (null == type)
            {
                Console.WriteLine($"Type {resource.Name} was in the documentation but not found in the schema.");
            }

            if (null != type)
            {
                LogIfDifferent(type.Name, resource.Name.TypeOnly(), $"Schema type {type.Name} is different than the documentation name {resource.Name.TypeOnly()}.");
                LogIfDifferent(type.OpenType, resource.OriginalMetadata.IsOpenType, $"Schema type {type.Name} has a different OpenType value {type.OpenType} than the documentation {resource.OriginalMetadata.IsOpenType}.");
                LogIfDifferent(type.BaseType, resource.OriginalMetadata.BaseType, $"Schema type {type.Name} has a different BaseType value {type.BaseType} than the documentation {resource.OriginalMetadata.BaseType}.");

                AddDocPropertiesToSchemaResource(type, resource, edmx, generateNewElements);
            }
        }

        private void AddDocPropertiesToSchemaResource(ComplexType schemaType, ResourceDefinition docResource, EntityFramework edmx, bool generateNewElements)
        {
            var docProps = (from p in docResource.Parameters
                            where !p.IsNavigatable && !p.Name.StartsWith("@")
                            select p).ToList();
            MergePropertiesIntoSchema(schemaType.Name, schemaType.Properties, docProps, edmx, generateNewElements);

            var schemaEntity = schemaType as EntityType;
            if (null != schemaEntity)
            {
                var docNavigationProps = (from p in docResource.Parameters where p.IsNavigatable && !p.Name.StartsWith("@") select p);
                MergePropertiesIntoSchema(schemaEntity.Name, schemaEntity.NavigationProperties, docNavigationProps, edmx, generateNewElements);
            }

            var docInstanceAnnotations = (from p in docResource.Parameters where p.Name != null && p.Name.StartsWith("@") select p);
            MergeInstanceAnnotationsAndRecordTerms(schemaType.Name, docInstanceAnnotations, docResource, edmx, generateNewElements);
        }

        private static void MergePropertiesIntoSchema<TProp>(string typeName, List<TProp> schemaProps, IEnumerable<ParameterDefinition> docProps, EntityFramework edmx, bool generateNewElements)
            where TProp : Property, new()
        {
            var documentedProperties = docProps.ToDictionary(x => x.Name, x => x);
            foreach(var schemaProp in schemaProps)
            {
                ParameterDefinition documentedVersion = null;
                if (documentedProperties.TryGetValue(schemaProp.Name, out documentedVersion))
                {
                    // Compare / update schema with data from documentation
                    var docProp = ConvertParameterToProperty<Property>(typeName, documentedVersion);
                    LogIfDifferent(schemaProp.Nullable, docProp.Nullable, $"Type {typeName}: Property {docProp.Name} has a different nullable value than documentation.");
                    LogIfDifferent(schemaProp.TargetEntityType, docProp.TargetEntityType, $"Type {typeName}: Property {docProp.Name} has a different target entity type than documentation.");
                    LogIfDifferent(schemaProp.Type, docProp.Type, $"Type {typeName}: Property {docProp.Name} has a different Type value than documentation ({schemaProp.Type},{docProp.Type}).");
                    LogIfDifferent(schemaProp.Unicode, docProp.Unicode, $"Type {typeName}: Property {docProp.Name} has a different unicode value than documentation ({schemaProp.Unicode},{docProp.Unicode}).");
                    documentedProperties.Remove(documentedVersion.Name);

                    AddDescriptionAnnotation(typeName, schemaProp, documentedVersion);
                }
                else
                {
                    // Log out that this property wasn't in the documentation
                    Console.WriteLine($"UndocumentedProperty: {typeName} defines {schemaProp.Name} in the schema but has no matching documentation.");
                }
            }

            if (generateNewElements)
            {
                foreach(var newPropDef in documentedProperties.Values)
                {
                    // Create new properties based on the documentation
                    var newProp = ConvertParameterToProperty<TProp>(typeName, newPropDef);
                    schemaProps.Add(newProp);
                }
            }
        }

        private static void LogIfDifferent(object schemaValue, object documentationValue, string errorString)
        {
            if ( (schemaValue == null && documentationValue != null) || 
                 (schemaValue != null && documentationValue == null) ||
                 (schemaValue != null && documentationValue != null && !schemaValue.Equals(documentationValue)))
            {
                Console.WriteLine(errorString);
            }
        }

        private static ComplexType CreateResourceInSchema(Schema schema, ResourceDefinition resource)
        {
            ComplexType type;
            // Create a new entity or complex type for this resource
            if (!string.IsNullOrEmpty(resource.KeyPropertyName))
            {
                var entity = new EntityType();
                entity.Key = new Key { PropertyRef = new PropertyRef { Name = resource.KeyPropertyName } };
                entity.NavigationProperties = (from p in resource.Parameters
                                               where p.IsNavigatable
                                               select ConvertParameterToProperty<NavigationProperty>(entity.Name, p)).ToList();
                schema.EntityTypes.Add(entity);
                type = entity;
            }
            else
            {
                type = new ComplexType();
                schema.ComplexTypes.Add(type);
            }

            return type;
        }

        private void MergeInstanceAnnotationsAndRecordTerms(string typeName, IEnumerable<ParameterDefinition> annotations, ResourceDefinition containedResource, EntityFramework edmx, bool generateNewElements)
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

                var targetSchema = FindOrCreateSchemaForNamespace(ns, edmx, overrideNamespaceFilter: true, generateNewElements: generateNewElements);
                if (null != targetSchema)
                {
                    targetSchema.Terms.Add(term);
                }
            }
        }


        private static T ConvertParameterToProperty<T>(string typeName, ParameterDefinition param) where T : Property, new()
        {
            var prop = new T()
            {
                Name = param.Name,
                Nullable = (param.Required.HasValue ? !param.Required.Value : false),
                Type = param.Type.ODataResourceName()
            };

            // Add description annotation
            AddDescriptionAnnotation(typeName, prop, param);
            return prop;
        }

        private static void AddDescriptionAnnotation<T>(string typeName, T targetProperty, ParameterDefinition sourceParameter, string termForDescription = Term.LongDescriptionTerm) where T: Property, IODataAnnotatable
        {
            if (!string.IsNullOrEmpty(sourceParameter.Description))
            {
                if (targetProperty.Annotation == null)
                {
                	targetProperty.Annotation = new List<Annotation>();
                }

                // Check to see if there already is a term with Description
                var descriptionTerm = targetProperty.Annotation.Where(t => t.Term == termForDescription).FirstOrDefault();
                if (descriptionTerm != null)
                {
                    LogIfDifferent(descriptionTerm.String, sourceParameter.Description, $"Type {typeName} has a different value for term '{termForDescription}' than the documentation.");
                }
                else
                {
                	targetProperty.Annotation.Add(
                    new Annotation()
                    {
                        Term = termForDescription,
                        String = sourceParameter.Description
                    });
                }
            }
            else
            {
                Console.WriteLine($"Description was null for property: {typeName}.{sourceParameter.Name}.");
            }
        }
    }


    public class CsdlWriterOptions
    {
        public string OutputDirectoryPath { get; set; }
        public string SourceMetadataPath { get; set; }
        public MetadataFormat Formats { get; set; }
        public string[] Namespaces { get; set; }
        public bool Sort { get; set; }
        public string BaseUrl { get; set; }
        public string TransformOutput { get; set; }
        public string DocumentationSetPath { get; set; }
        public string Version { get; set; }
        public bool SkipMetadataGeneration { get; set; }
        public bool ValidateSchema { get; set; }
        public bool AttributesOnNewLines { get; set; }
    }

    [Flags]
    public enum MetadataFormat
    {
        Default      = EdmxInput | EdmxOutput,
        EdmxInput    = 1 << 0,
        EdmxOutput   = 1 << 1,
        SchemaInput  = 1 << 2,
        SchemaOutput = 1 << 3
    }
}
