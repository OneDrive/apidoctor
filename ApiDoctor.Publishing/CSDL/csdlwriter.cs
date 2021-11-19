/*
 * API Doctor
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

namespace ApiDoctor.Publishing.CSDL
{
    using ApiDoctor.Validation;
    using ApiDoctor.Validation.Writers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ApiDoctor.Validation.OData;
    using Validation.Config;
    using Validation.OData.Transformation;
    using ApiDoctor.Validation.Http;
    using System.IO;
    using ApiDoctor.Validation.Error;

    public class CsdlWriter : DocumentPublisher
    {
        private readonly CsdlWriterOptions options;

        public CsdlWriter(DocSet docs, CsdlWriterOptions options)
            : base(docs)
        {
            this.options = options;
        }

        public override async Task PublishToFolderAsync(string outputFolder, IssueLogger issues)
        {
            string outputFilenameSuffix = "";

            // Step 1: Generate an EntityFramework OM from the documentation and/or template file
            EntityFramework framework = CreateEntityFrameworkFromDocs(issues);
            if (null == framework)
                return;

            if (!string.IsNullOrEmpty(options.MergeWithMetadataPath))
            {
                EntityFramework secondFramework = CreateEntityFrameworkFromDocs(issues, options.MergeWithMetadataPath, generateFromDocs: false);
                framework = framework.MergeWith(secondFramework);
                outputFilenameSuffix += "-merged";
            }

            // Step 1a: Apply an transformations that may be defined in the documentation
            if (!string.IsNullOrEmpty(options.TransformOutput))
            {
                PublishSchemaChangesConfigFile transformations = DocSet.TryLoadConfigurationFiles<PublishSchemaChangesConfigFile>(options.DocumentationSetPath).Where(x => x.SchemaChanges.TransformationName == options.TransformOutput).FirstOrDefault();
                if (null == transformations)
                {
                    throw new KeyNotFoundException($"Unable to locate a transformation set named {options.TransformOutput}. Aborting.");
                }

                string[] versionsToPublish = options.Version?.Split(new char[] { ',', ' ' });
                framework.ApplyTransformation(transformations.SchemaChanges, versionsToPublish);
                if (!string.IsNullOrEmpty(options.Version))
                {
                    outputFilenameSuffix += $"-{options.Version}";
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

            if (!string.IsNullOrEmpty(outputFolder))
            {
                var outputFullName = GenerateOutputFileFullName(options.SourceMetadataPath, outputFolder, outputFilenameSuffix);
                Console.WriteLine($"Publishing metadata to {outputFullName}");

                using (var writer = System.IO.File.CreateText(outputFullName))
                {
                    await writer.WriteAsync(xmlData);
                    await writer.FlushAsync();
                    writer.Close();
                }
            }
            else
            {
                issues.Message($"Skipping writing published metadata to disk. No output folder given.");
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

        public EntityFramework CreateEntityFrameworkFromDocs(IssueLogger issues, string sourcePath = null, bool? generateFromDocs = null)
        {
            sourcePath = sourcePath ?? options.SourceMetadataPath;

            EntityFramework edmx = new EntityFramework();
            if (!string.IsNullOrEmpty(sourcePath))
            {
                try
                {
                    if (!System.IO.File.Exists(sourcePath))
                    {
                        throw new System.IO.FileNotFoundException($"Unable to locate source file: {sourcePath}");
                    }

                    using (System.IO.FileStream stream = new System.IO.FileStream(sourcePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
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
                        foreach (var s in schemas)
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
                    issues.Error(ValidationErrorCode.Unknown, $"Unable to deserialize source template file", ex);
                    return null;
                }
            }

            bool generateNewElements = (generateFromDocs == null && !options.SkipMetadataGeneration) || (generateFromDocs.HasValue && generateFromDocs.Value);

            // Add resources 
            if (Documents.Files.Any())
            {
                foreach (var resource in this.Documents.Resources)
                {
                    var targetSchema = FindOrCreateSchemaForNamespace(resource.Name.NamespaceOnly(), edmx, generateNewElements: generateNewElements);
                    if (targetSchema != null)
                    {
                        AddResourceToSchema(targetSchema, resource, edmx, issues, generateNewElements: generateNewElements);
                    }
                }

                // Add new elements from the documentation if they don't already exist.
                if (generateNewElements)
                {
                    // Figure out the EntityCollection
                    this.BuildEntityContainer(edmx, DocSet.SchemaConfig.BaseUrls, issues);

                    // Add actions to the collection
                    this.ProcessRestRequestPaths(edmx, DocSet.SchemaConfig.BaseUrls, issues);

                    // add enums to the default schema
                    var defaultSchema = edmx.DataServices.Schemas.FirstOrDefault(s => s.Namespace == DocSet.SchemaConfig.DefaultNamespace);
                    foreach (var enumDefinition in Documents.Enums.GroupBy(e => e.TypeName))
                    {
                        var enumType = new EnumType
                        {
                            Name = enumDefinition.Key,
                            Members = enumDefinition.Distinct(new EnumComparer()).Select(e =>
                                new EnumMember
                                {
                                    Name = e.MemberName,
                                    Value = e.NumericValue.HasValue ? e.NumericValue.GetValueOrDefault().ToString() : null
                                }).ToList(),
                            IsFlags = enumDefinition.FirstOrDefault().IsFlags,
                        };

                        if (enumType.Members[0].Value == null)
                        {
                            for (int i = 0; i < enumType.Members.Count; i++)
                            {
                                if (enumType.Members[i].Value != null)
                                {
                                    issues.Warning(ValidationErrorCode.Unknown,
                                        $"Enum {enumType.Name} has some values specified and others unspecified.");
                                    continue;
                                }

                                enumType.Members[i].Value = i.ToString();
                            }
                        }

                        defaultSchema.Enumerations.Add(enumType);
                    }
                }
            }

            if (this.options.Annotations.HasFlag(AnnotationOptions.Independent))
            {
                // dig through all the inline annotations and hoist them out.
                foreach (var schema in edmx.DataServices.Schemas)
                {
                    var annotationsMap = schema.Annotations.ToDictionary(an => an.Target);
                    foreach (var complex in schema.ComplexTypes.Concat(schema.EntityTypes))
                    {
                        var fullName = complex.Namespace + "." + complex.Name;
                        MergeAnnotations(fullName, complex, annotationsMap);

                        foreach (var property in complex.Properties)
                        {
                            MergeAnnotations(fullName + "/" + property.Name, property, annotationsMap);
                        }

                        var entity = complex as EntityType;
                        if (entity != null && entity.NavigationProperties != null)
                        {
                            foreach (var property in entity.NavigationProperties)
                            {
                                MergeAnnotations(fullName + "/" + property.Name, property, annotationsMap);
                            }
                        }

                        Dictionary<string, ODataCapabilities> capabilitiesPerProperty = new Dictionary<string, ODataCapabilities>();
                        foreach (var annotation in complex.Contributors.Where(c => c.OriginalMetadata?.OdataAnnotations != null).SelectMany(c => c.OriginalMetadata.OdataAnnotations))
                        {
                            if (annotation?.Capabilities != null)
                            {
                                ODataCapabilities capabilities;
                                if (!capabilitiesPerProperty.TryGetValue(annotation.Property ?? "", out capabilities))
                                {
                                    capabilitiesPerProperty.Add(annotation.Property ?? "", annotation.Capabilities);
                                    continue;
                                }

                                annotation.Capabilities.MergeWith(capabilities, issues.For(complex.Name + "/annotations"));
                            }
                        }

                        foreach (var kvp in capabilitiesPerProperty)
                        {
                            var capabilities = kvp.Value;
                            var property = new Property { Name = kvp.Key, Annotation = new List<Annotation>() };

                            TryAddAnnotation(property, "ChangeTracking", "Supported", capabilities.ChangeTracking);
                            TryAddAnnotation(property, "Org.OData.Core.V1.Computed", null, capabilities.Computed);
                            TryAddAnnotation(property, "CountRestrictions", "Countable", capabilities.Countable);
                            TryAddAnnotation(property, "DeleteRestrictions", "Deletable", capabilities.Deletable);
                            TryAddAnnotation(property, "ExpandRestrictions", "Expandable", capabilities.Expandable);
                            TryAddAnnotation(property, "FilterRestrictions", "Filterable", capabilities.Filterable);
                            TryAddAnnotation(property, "InsertRestrictions", "Insertable", capabilities.Insertable);
                            TryAddAnnotation(property, "SearchRestrictions", "Searchable", capabilities.Searchable);
                            TryAddAnnotation(property, "SelectRestrictions", "Selectable", capabilities.Selectable);
                            TryAddAnnotation(property, "SkipSupported", null, capabilities.Skippable);
                            TryAddAnnotation(property, "SortRestrictions", "Sortable", capabilities.Sortable);
                            TryAddAnnotation(property, "TopSupported", null, capabilities.Toppable);
                            TryAddAnnotation(property, "UpdateRestrictions", "Updatable", capabilities.Updatable);

                            if (capabilities.Navigability != null || capabilities.Referenceable.HasValue)
                            {
                                var annotation = new Annotation
                                {
                                    Term = "Org.OData.Capabilities.V1.NavigationRestrictions",
                                    Records = new List<Record>(),
                                };

                                if (capabilities.Referenceable.HasValue)
                                {
                                    annotation.Records.Add(new Record
                                    {
                                        PropertyValues = new List<PropertyValue>
                                            {
                                                new PropertyValue
                                                {
                                                    Property ="Referenceable",
                                                    Bool = capabilities.Referenceable.GetValueOrDefault(),
                                                },
                                            }
                                    });
                                }

                                if (capabilities.Navigability != null)
                                {
                                    annotation.Records.Add(new Record
                                    {
                                        PropertyValues = new List<PropertyValue>
                                            {
                                                new PropertyValue
                                                {
                                                    Property ="Navigability",
                                                    EnumMember = string.IsNullOrEmpty(capabilities.Navigability)
                                                        ? string.Empty
                                                        : ("Org.OData.Capabilities.V1.NavigationType/" + capabilities.Navigability)
                                                },
                                            }
                                    });
                                }

                                property.Annotation.Add(annotation);
                            }

                            if (capabilities.Permissions != null)
                            {
                                var annotation = new Annotation
                                {
                                    Term = "Org.OData.Core.V1.Permissions",
                                    EnumMember = string.IsNullOrEmpty(capabilities.Permissions)
                                        ? string.Empty
                                        : ("Org.OData.Core.V1.Permission/" + capabilities.Permissions)
                                };

                                property.Annotation.Add(annotation);
                            }

                            MergeAnnotations((fullName + "/" + property.Name).TrimEnd('/'), property, annotationsMap);
                        }
                    }

                    if (this.options.Annotations.HasFlag(AnnotationOptions.HttpRequests))
                    {
                        foreach (var container in schema.EntityContainers)
                        {
                            var prefix = schema.Namespace + "." + container.Name;
                            foreach (var entitySet in container.EntitySets)
                            {
                                var annotationName = prefix + "/" + entitySet.Name;
                                AddHttpRequestsAnnotations(annotationsMap, entitySet.SourceMethods as MethodCollection, annotationName, issues.For(annotationName));
                            }

                            foreach (var singleton in container.Singletons)
                            {
                                var annotationName = prefix + "/" + singleton.Name;
                                AddHttpRequestsAnnotations(annotationsMap, singleton.SourceMethods as MethodCollection, annotationName, issues.For(annotationName));
                            }
                        }

                        foreach (var operation in schema.Actions.Cast<ActionOrFunctionBase>().Concat(schema.Functions))
                        {
                            var target =
                                (operation.Parameters.FirstOrDefault(p => p.Name.IEquals("bindingParameter")).Type ?? schema.Namespace) +
                                "/" +
                                operation.Name;

                            AddHttpRequestsAnnotations(annotationsMap, operation.SourceMethods as MethodCollection, target, issues.For(target));
                        }
                    }

                    schema.Annotations = annotationsMap.Values.ToList();

                    if (this.options.Annotations.HasFlag(AnnotationOptions.OnlyAnnotations))
                    {
                        schema.ComplexTypes = null;
                        schema.EntityContainers = null;
                        schema.EntityTypes = null;
                        schema.Enumerations = null;
                        schema.Functions = null;
                        schema.Actions = null;
                        schema.Terms = null;
                    }
                }
            }

            return edmx;
        }

        private static void TryAddAnnotation(Property annotatable, string term, string property, bool? value)
        {
            if (value.HasValue)
            {
                var annotation = new Annotation
                {
                    Term = term.Contains(".") ? term : $"Org.OData.Capabilities.V1.{term}"
                };

                if (property != null)
                {
                    annotation.Records = new List<Record>
                    {
                        new Record
                        {
                            PropertyValues = new List<PropertyValue>
                            {
                                new PropertyValue { Property=property, Bool = value.GetValueOrDefault()},
                            }
                        }
                    };
                }
                else
                {
                    annotation.Bool = value.GetValueOrDefault();
                }

                annotatable.Annotation.Add(annotation);
            }
        }

        private static void AddHttpRequestsAnnotations(Dictionary<string, Annotations> annotationsMap, MethodCollection methods, string target, IssueLogger issues)
        {
            if (methods != null)
            {
                var annotatable = new Property();
                AddHttpRequestsAnnotations(annotatable, methods, issues);
                MergeAnnotations(target, annotatable, annotationsMap);
            }
        }

        private static void AddHttpRequestsAnnotations(IODataAnnotatable annotatable, MethodCollection methods, IssueLogger issues)
        {
            if (methods != null)
            {
                var annotation = new Annotation
                {
                    Term = "Org.OData.Core.V1.HttpRequests",
                    Collection = new RecordCollection
                    {
                        Records = new List<Record>(),
                    }
                };

                foreach (var method in methods)
                {
                    try
                    {
                        var record = GenerateHttpRequestMethodAnnotation(method, issues);
                        annotation.Collection.Records.Add(record);
                    }
                    catch (Exception ex)
                    {
                        issues.Error(ValidationErrorCode.Unknown,
                            $"Exception in {method.SourceFile.DisplayName}!", ex);
                    }
                }

                if (annotatable.Annotation == null)
                {
                    annotatable.Annotation = new List<Annotation>();
                }

                annotatable.Annotation.Add(annotation);
            }
        }

        private static Record GenerateHttpRequestMethodAnnotation(MethodDefinition method, IssueLogger issues)
        {
            HttpRequest request;
            HttpParser.TryParseHttpRequest(method.Request, out request, issues);
            HttpResponse response;
            HttpParser.TryParseHttpResponse(method.ExpectedResponse, out response, issues);
            var record = new Record
            {
                PropertyValues = new List<PropertyValue>
                {
                    new PropertyValue
                    {
                        Property = "Description",
                        String = method.Description.ToStringClean(),
                    },
                    new PropertyValue { Property = "MethodDescription", String = method.Title },
                    new PropertyValue { Property = "MethodType", String = method.HttpMethodVerb()},
                    new PropertyValue
                    {
                        Property = "CustomHeaders",
                        Collection = new RecordCollection
                        {
                            Records = (method.Parameters?.Where(p=>p.Location == ParameterLocation.Header)?.Select(p =>
                                new Record
                                {
                                    PropertyValues = new List<PropertyValue>
                                    {
                                        new PropertyValue { Property = "Name", String = p.Name },
                                        new PropertyValue { Property = "Description", String = p.Description.ToStringClean() },
                                        new PropertyValue { Property = "Required", Bool = p.Required.GetValueOrDefault() },
                                    }
                                }))?.ToList()
                        },
                    },
                    new PropertyValue
                    {
                        Property = "CustomQueryOptions",
                        Collection = new RecordCollection
                        {
                            Records = (method.Parameters?.Where(p=>p.Location == ParameterLocation.QueryString)?.Select(p =>
                                new Record
                                {
                                    PropertyValues = new List<PropertyValue>
                                    {
                                        new PropertyValue { Property = "Name", String = p.Name },
                                        new PropertyValue { Property = "Description", String = p.Type.ToStringClean() },
                                        new PropertyValue { Property = "Required", Bool = p.Required.GetValueOrDefault() },
                                    }
                                }))?.ToList()
                        },
                    },
                    new PropertyValue
                    {
                        Property = "HttpResponses",
                        Collection = new RecordCollection
                        {
                            Records = new List<Record>
                            {
                                new Record
                                {
                                    PropertyValues = new List<PropertyValue>
                                    {
                                        new PropertyValue { Property = "ResponseCode", String  = response?.StatusCode.ToString() },
                                        new PropertyValue
                                        {
                                            Property = "Examples" ,
                                            Collection = new RecordCollection
                                            {
                                                Records = new List<Record>
                                                {
                                                    new Record
                                                    {
                                                        Type = "Org.OData.Core.V1.InlineExample",
                                                        PropertyValues = new List<PropertyValue>
                                                        {
                                                            new PropertyValue { Property = "InlineValue", String  = response?.Body, },
                                                            new PropertyValue { Property = "Description", String = response?.ContentType },
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new PropertyValue
                    {
                        Property = "SecuritySchemes",
                        Collection = new RecordCollection
                        {
                            Records = (method?.SourceFile?.AuthScopes?.Where(scp=>scp?.Scope != null && !scp.Scope.IContains("not supported")).Select(scp =>
                                new Record
                                {
                                    PropertyValues = new List<PropertyValue>
                                    {
                                        new PropertyValue { Property = "AuthorizationSchemeName", String = scp.Title },
                                        new PropertyValue
                                        {
                                            Property = "RequiredScopes",
                                            Collection = new RecordCollection { Strings = scp.Scope?.Split(new[] {',',';',' ' }, StringSplitOptions.RemoveEmptyEntries).ToList() }
                                        }
                                    }
                                }))?.ToList()
                        },
                    },
                },
            };

            return record;
        }

        private static void MergeAnnotations(string fullName, IODataAnnotatable annotatable, Dictionary<string, Annotations> schemaLevelAnnotations)
        {
            if (annotatable.Annotation?.Count > 0)
            {
                Annotations annotations;
                if (schemaLevelAnnotations.TryGetValue(fullName, out annotations))
                {
                    foreach (var annotation in annotatable.Annotation)
                    {
                        var existingAnnotation = annotations.AnnotationList.FirstOrDefault(a => a.Term == annotation.Term);
                        if (existingAnnotation != null && annotation.Collection != null && annotation.Collection.Records?.Count > 0)
                        {
                            if (existingAnnotation.Collection == null)
                            {
                                existingAnnotation.Collection = new RecordCollection();
                            }

                            if (existingAnnotation.Collection.Records == null)
                            {
                                existingAnnotation.Collection.Records = new List<Record>();
                            }

                            existingAnnotation.Collection.Records.AddRange(annotation.Collection.Records);
                        }
                        else
                        {
                            annotations.AnnotationList.Add(annotation);
                        }
                    }
                }
                else
                {
                    schemaLevelAnnotations[fullName] = new Annotations
                    {
                        Target = fullName,
                        AnnotationList = annotatable.Annotation,
                    };
                }

                annotatable.Annotation = null;
            }
        }

        /// <summary>
        /// Scan the MethodDefintions in the documentation and create actions and functions in the
        /// EntityFramework for matching call patterns.
        /// </summary>
        /// <param name="edmx"></param>
        private void ProcessRestRequestPaths(EntityFramework edmx, string[] baseUrlsToRemove, IssueLogger issues)
        {
            Dictionary<string, MethodCollection> uniqueRequestPaths = GetUniqueRequestPaths(baseUrlsToRemove, issues);
            List<string> pathsToProcess = uniqueRequestPaths.Keys.OrderBy(p => p.Count(c => c == '/')).ThenBy(p => p).ToList();

            for (int attempts = 1; attempts < 10 && pathsToProcess.Count > 0; attempts++)
            {
                Dictionary<string, Exception> pathsToRetry = new Dictionary<string, Exception>();
                foreach (var path in pathsToProcess)
                {
                    var methodCollection = uniqueRequestPaths[path];

                    ODataTargetInfo requestTarget = null;
                    try
                    {
                        // TODO: If we have an input Edmx, we may already know what this so we don't need to infer anything.
                        // if that is the case, we should just update it with anything else we know from the documentation.
                        requestTarget = ParseRequestTargetType(path, methodCollection, edmx, issues);
                        if (requestTarget.Classification == ODataTargetClassification.Unknown &&
                            !string.IsNullOrEmpty(requestTarget.Name) &&
                            requestTarget.QualifiedType != null)
                        {
                            CreateNewActionOrFunction(edmx, methodCollection, requestTarget.Name, requestTarget.QualifiedType, issues.For(requestTarget.Name));
                        }
                        else if (requestTarget.Classification == ODataTargetClassification.EntityType)
                        {
                            // We've learned more about this entity type, let's add that information to the state
                            AppendToEntityType(edmx, requestTarget, methodCollection);
                        }
                        else if (requestTarget.Classification == ODataTargetClassification.NavigationProperty)
                        {
                            // TODO: Record somewhere the operations that are available on this NavigationProperty
                            AppendToNavigationProperty(edmx, requestTarget, methodCollection, issues);
                        }
                        else
                        {
                            // TODO: Are there interesting things to learn here?
                            //Console.WriteLine("Found type {0}: {1}", requestTarget.Classification, path);
                        }
                    }
                    catch (Exception ex)
                    {
                        pathsToRetry.Add(path, ex);
                        continue;
                    }
                }

                if (pathsToRetry.Count < pathsToProcess.Count)
                {
                    pathsToProcess = pathsToRetry.Keys.ToList();
                }
                else
                {
                    issues.Message($"Failed to resolve the following paths after {attempts} attempts:");
                    foreach (var kvp in pathsToRetry)
                    {
                        issues.Warning(ValidationErrorCode.Unknown, $"Couldn't serialize request for path {kvp.Key} into EDMX", kvp.Value);
                    }

                    break;
                }
            }
        }

        private void AppendToNavigationProperty(EntityFramework edmx, ODataTargetInfo navigationProperty, MethodCollection methods, IssueLogger issues)
        {
            EntityType parentType = edmx.ResourceWithIdentifier<EntityType>(navigationProperty.QualifiedType);

            var matchingProperty =
                parentType.NavigationProperties.FirstOrDefault(np => np.Name == navigationProperty.Name) ??
                parentType.Properties.FirstOrDefault(p => p.Name == navigationProperty.Name);
            if (null != matchingProperty)
            {
                // TODO: Append information from methods into this navigation property
                StringBuilder sb = new StringBuilder();
                const string seperator = ", ";
                sb.AppendWithCondition(methods.GetAllowed, "GET", seperator);
                sb.AppendWithCondition(methods.PostAllowed, "POST", seperator);
                sb.AppendWithCondition(methods.PutAllowed, "PUT", seperator);
                sb.AppendWithCondition(methods.DeleteAllowed, "DELETE", seperator);
                issues.Message($"Collection '{navigationProperty.QualifiedType + "/" + navigationProperty.Name}' supports: ({sb})");
            }
            else
            {
                issues.Error(ValidationErrorCode.RequiredPropertiesMissing,
                    $"EntityType '{navigationProperty.QualifiedType}' doesn't have a matching Property '{navigationProperty.Name}' but a request exists for this. Sounds like a documentation error.");
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
        }

        private void CreateNewActionOrFunction(
            EntityFramework edmx,
            MethodCollection methodCollection,
            string name,
            string boundToType,
            IssueLogger issues,
            bool recursing = false)
        {
            if (name.IEquals("$ref") ||
                name.IEquals("$value"))
            {
                issues.Message("Ignoring $ref and $value. No schema changes needed.");
                return;
            }

            if (name.StartsWith("{"))
            {
                var currentType = edmx.DataServices.Schemas.FindTypeWithIdentifier(boundToType) as ComplexType;
                if (currentType?.OpenType == true)
                {
                    issues.Message($"Ignoring {name} navigation into open type. No schema changes needed.");
                }
                else
                {
                    issues.Warning(ValidationErrorCode.Unknown, $"Don't know what to do with {name}; it's not a function");
                }

                return;
            }

            // Create a new action (not idempotent) / function (idempotent) based on this request method!
            ActionOrFunctionBase target = null;
            if (methodCollection.AllMethodsIdempotent)
            {
                target = new Validation.OData.Function
                {
                    IsComposable = methodCollection.Any(mc => mc.RequestMetadata.IsComposable),
                };
            }
            else
            {
                target = new Validation.OData.Action();
            }

            if (this.options.ShowSources)
            {
                target.SourceFiles = string.Join(";", new HashSet<string>(methodCollection.Select(m => m.SourceFile).Select(f => Path.GetFileName(f.FullPath)).ToList()));
            }

            if (this.options.Annotations.HasFlag(AnnotationOptions.HttpRequests))
            {
                AddHttpRequestsAnnotations(target, methodCollection, issues.For(name));
            }

            target.SourceMethods = methodCollection;

            target.Name = name.TypeOnly();
            target.IsBound = true;
            target.Parameters.Add(new Parameter { Name = "bindingParameter", Type = boundToType, IsNullable = false });
            foreach (var param in methodCollection.Where(m => m.HttpMethodVerb() == "POST").SelectMany(m => m.RequestBodyParameters))
            {
                try
                {
                    var existingParam = target.Parameters.FirstOrDefault(p => p.Name.IEquals(param.Name));
                    if (existingParam != null)
                    {
                        existingParam.Type = ParameterDataType.ChooseBest(existingParam.Type, param.Type.ODataResourceName());
                    }
                    else
                    {
                        target.Parameters.Add(
                            new Parameter
                            {
                                Name = param.Name,
                                Type = param.Type.ODataResourceName(),
                                IsNullable = (param.Required.HasValue ? param.Required.Value : false)
                            });
                    }
                }
                catch (Exception ex)
                {
                    issues.Error(ValidationErrorCode.Unknown, $"Exception when adding parameter {param.Name} with type {param.Type}", ex);
                    throw;
                }
            }

            if (target.Name.Contains("("))
            {
                target.ParameterizedName = target.Name;
                var pathParameters = target.Name.TextBetweenCharacters('(', ')').Split(',');
                target.Name = target.Name.Substring(0, target.Name.IndexOf('('));
                foreach (var param in pathParameters.Where(p => !string.IsNullOrEmpty(p)))
                {
                    var paramName = param.Split('=')[0];
                    var matchingParamDef = methodCollection.SelectMany(m => m.Parameters).FirstOrDefault(p => p.Name == paramName);
                    if (matchingParamDef == null)
                    {
                        issues.Error(ValidationErrorCode.Unknown,
                            $"Couldn't find definition for parameter {param} in {name} after looking in {string.Join(",", methodCollection.Select(m => m.SourceFile?.DisplayName))}");
                    }
                    else
                    {
                        target.Parameters.Add(
                            new Parameter
                            {
                                Name = matchingParamDef.Name,
                                Type = matchingParamDef.Type.ODataResourceName(),
                                IsNullable = (matchingParamDef.Required.GetValueOrDefault())
                            });

                        // if we have an optional param, recursively call again without it
                        if (matchingParamDef.Optional.GetValueOrDefault() == true)
                        {
                            var overloadName = target.ParameterizedName.Replace($"{paramName}={{var}}", string.Empty);
                            CreateNewActionOrFunction(edmx, methodCollection, overloadName, boundToType, issues);
                        }
                    }
                }

                if (!recursing)
                {
                    // note: this is very rough and brittle. only intended for limited use right now.
                    // if there are other sample calling patterns for this function, create bindings for them too.
                    foreach (var sample in methodCollection.SelectMany(mc => mc.SourceFile.Samples).SelectMany(s => s.Samples))
                    {
                        if (sample.EndsWith(")") && sample.Contains("("))
                        {
                            var overloadName = target.ParameterizedName.ReplaceTextBetweenCharacters('(', ')', sample.TextBetweenCharacters('(', ')'));
                            CreateNewActionOrFunction(edmx, methodCollection, overloadName, boundToType, issues, recursing: true);
                        }
                        else if (sample.EndsWith(target.Name))
                        {
                            // overload with no parameters
                            var overloadName = target.Name;
                            CreateNewActionOrFunction(edmx, methodCollection, overloadName, boundToType, issues, recursing: true);
                        }
                    }
                }
            }

            if (methodCollection.ResponseType != null)
            {
                target.ReturnType = new ReturnType { Type = methodCollection.ResponseType.ODataResourceName(), Nullable = false };
                var targetAction = target as Validation.OData.Action;
                if (targetAction != null &&
                    target.Parameters.FirstOrDefault(p => p.Name == "bindingParameter")?.Type?.StartsWith("Collection(") == false)
                {
                    targetAction.EntitySetPath = "bindingParameter";
                }
            }

            var schemaName = name.HasNamespace() ? name.NamespaceOnly() : edmx.DataServices.Schemas.FirstOrDefault()?.Namespace;
            var schema = FindOrCreateSchemaForNamespace(schemaName, edmx, true);

            // first see if this action or function is already bound to a base type with the same params.
            // if so, this definition is redundant.
            foreach (var existingFunction in schema.Functions.Cast<ActionOrFunctionBase>().Concat(schema.Actions).ToList())
            {
                if (existingFunction.CanSubstituteFor(target, edmx))
                {
                    return;
                }

                // on the other hand, if the opposite is true...
                if (target.CanSubstituteFor(existingFunction, edmx))
                {
                    if (target is Function)
                    {
                        schema.Functions.Remove((Function)existingFunction);
                    }
                    else
                    {
                        schema.Actions.Remove((Validation.OData.Action)existingFunction);
                    }
                }
            }

            if (target is Function)
            {
                schema.Functions.Add((Function)target);
            }
            else
            {
                schema.Actions.Add((Validation.OData.Action)target);
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
        private static ODataTargetInfo ParseRequestTargetType(string requestPath, MethodCollection requestMethodCollection, EntityFramework edmx, IssueLogger issues)
        {
            string[] requestParts = requestPath.Substring(1).Split(new char[] { '/' });

            EntityContainer entryPoint = (from s in edmx.DataServices.Schemas
                                          where s.EntityContainers.Count > 0
                                          select s.EntityContainers.FirstOrDefault()).SingleOrDefault();

            if (entryPoint == null) throw new InvalidOperationException("Couldn't locate an EntityContainer to begin target resolution");

            IODataNavigable currentObject = entryPoint;
            IODataNavigable previousObject = null;

            for (int i = 0; i < requestParts.Length; i++)
            {
                bool isLastSegment = i == requestParts.Length - 1;
                string uriPart = requestParts[i];
                IODataNavigable nextObject = null;
                if (uriPart == "{var}" &&
                    (currentObject as ComplexType)?.OpenType != true)
                {
                    try
                    {
                        nextObject = currentObject.NavigateByEntityTypeKey(edmx, issues.For($"ExampleRequest_{string.Join("/", requestParts.Take(i + 1))}"));
                    }
                    catch (Exception ex)
                    {
                        throw new NotSupportedException("Unable to navigation into EntityType by key: " + currentObject.TypeIdentifier + " (" + ex.Message + ")");
                    }
                }
                else
                {
                    nextObject = currentObject.NavigateByUriComponent(
                        uriPart,
                        edmx,
                        issues.For($"ExampleRequest_{string.Join("/", requestParts.Take(i + 1))}"),
                        isLastSegment);
                }

                if (nextObject == null && isLastSegment)
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
                    throw new InvalidOperationException(
                        $"Uri path requires navigating into unknown object hierarchy: missing property '{uriPart}' on '{currentObject.TypeIdentifier}'. Possible issues:\r\n" +
                        $"\t 1) Doc bug where '{uriPart}' isn't defined on the resource.\r\n" +
                        $"\t 2) Doc bug where '{uriPart}' is an example key and should instead be replaced with a placeholder like {{item-id}} or declared in the sampleKeys annotation.\r\n" +
                        $"\t 3) Doc bug where '{currentObject.TypeIdentifier}' is supposed to be an entity type, but is being treated as a complex because it (and its ancestors) are missing the keyProperty annotation.\r\n");
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
                    if (response.Name.Contains("."))
                    {
                        response.Classification = ODataTargetClassification.TypeCast;
                    }
                    else
                    {
                        response.Classification = ODataTargetClassification.NavigationProperty;
                    }

                    response.QualifiedType = edmx.LookupIdentifierForType(previousObject);
                }
                else
                {
                    response.Classification = ODataTargetClassification.EntitySet;
                }
            }
            else if (currentObject is ComplexType)
            {
                response.Classification = ODataTargetClassification.ComplexType;
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
        private void BuildEntityContainer(EntityFramework edmx, string[] baseUrlsToRemove, IssueLogger issues)
        {
            // Check to see if an entitycontainer already exists
            foreach (var s in edmx.DataServices.Schemas)
            {
                if (s.EntityContainers.Any())
                    return;
            }

            Dictionary<string, MethodCollection> uniqueRequestPaths = GetUniqueRequestPaths(baseUrlsToRemove, issues);
            var resourcePaths = uniqueRequestPaths.Keys.OrderBy(x => x).ToArray();

            EntityContainer container = new EntityContainer();
            foreach (var path in resourcePaths)
            {
                try
                {
                    var methodCollection = uniqueRequestPaths[path];

                    if (EntitySetPathRegEx.IsMatch(path))
                    {
                        var name = EntitySetPathRegEx.Match(path).Groups[1].Value;
                        var entitySet = new EntitySet
                        {
                            Name = name,
                            EntityType = methodCollection.ResponseType?.ODataResourceName(),
                            SourceMethods = methodCollection,
                        };

                        if (this.options.Annotations.HasFlag(AnnotationOptions.HttpRequests))
                        {
                            AddHttpRequestsAnnotations(entitySet, methodCollection, issues.For(path + "/HttpRequestsAnnotations"));
                        }

                        container.EntitySets.Add(entitySet);
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
                        var singleton = new Singleton
                        {
                            Name = name,
                            Type = methodCollection.ResponseType.ODataResourceName(),
                            SourceMethods = methodCollection,
                        };

                        if (this.options.Annotations.HasFlag(AnnotationOptions.HttpRequests))
                        {
                            AddHttpRequestsAnnotations(singleton, methodCollection, issues.For(path + "/HttpRequestsAnnotations"));
                        }

                        container.Singletons.Add(singleton);
                    }
                }
                catch (Exception ex)
                {
                    issues.Error(ValidationErrorCode.Unknown, $"BuildEntityContainer: error in path {path}", ex);
                }
            }

            Schema defaultSchema;
            if (DocSet.SchemaConfig.DefaultNamespace != null)
            {
                defaultSchema = edmx.DataServices.Schemas.Single(s => s.Namespace.IEquals(DocSet.SchemaConfig.DefaultNamespace));
            }
            else
            {
                // if the default schema isn't specified, assume the most common schema in the docs is the default
                defaultSchema = edmx.DataServices.Schemas.OrderByDescending(s => s.EntityTypes.Count).First();
            }

            container.Name = this.options.EntityContainerName ?? defaultSchema.Namespace;
            defaultSchema.EntityContainers.Add(container);
        }

        private Dictionary<string, MethodCollection> cachedUniqueRequestPaths { get; set; }

        /// <summary>
        /// Return a dictionary of the unique request paths in the
        /// documentation and the method definitions that defined them.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, MethodCollection> GetUniqueRequestPaths(string[] baseUrlsToRemove, IssueLogger issues)
        {
            if (cachedUniqueRequestPaths == null)
            {
                Dictionary<string, MethodCollection> uniqueRequestPaths = new Dictionary<string, MethodCollection>();
                foreach (var m in Documents.Methods)
                {
                    if (m.RequestMetadata?.OpaqueUrl == true)
                    {
                        // ignore requests with opaque urls, as they don't contribute to the schema
                        continue;
                    }

                    if (m.ExpectedResponseMetadata != null && m.ExpectedResponseMetadata.ExpectError)
                    {
                        // Ignore things that are expected to error
                        continue;
                    }

                    var path = m.RequestUriPathOnly(baseUrlsToRemove, issues);
                    if (!path.StartsWith("/"))
                    {
                        // Ignore absolute URI paths
                        continue;
                    }

                    if (m.RequestMetadata.Tags != null &&
                        DocSet.SchemaConfig.SupportedTags != null &&
                        !DocSet.SchemaConfig.SupportedTags.Intersect(m.RequestMetadata.TagList).Any())
                    {
                        // ignore tagged request examples if the current service doesn't support that tag
                        continue;
                    }

                    issues.Message($"Converted '{m.Request.FirstLineOnly()}' into generic form '{path}'");

                    if (!uniqueRequestPaths.ContainsKey(path))
                    {
                        uniqueRequestPaths.Add(path, new MethodCollection());
                    }
                    uniqueRequestPaths[path].Add(m);

                    issues.Message($"{path} :: {m.RequestMetadata.ResourceType} --> {m.ExpectedResponseMetadata?.ResourceType}");
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

        private void AddResourceToSchema(
            Schema schema,
            ResourceDefinition resource,
            EntityFramework edmx,
            IssueLogger issues,
            bool generateNewElements = true)
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
                type = CreateResourceInSchema(schema, resource, issues);
            }
            else if (null == type)
            {
                issues.Error(ValidationErrorCode.ResourceTypeNotFound, $"Type {resource.Name} was in the documentation but not found in the schema.");
            }

            if (null != type)
            {
                LogIfDifferent(type.Name, resource.Name.TypeOnly(), issues, $"Schema type {type.Name} is different than the documentation name {resource.Name.TypeOnly()}.");
                LogIfDifferent(type.OpenType, resource.OriginalMetadata.IsOpenType, issues, $"Schema type {type.Name} has a different OpenType value {type.OpenType} than the documentation {resource.OriginalMetadata.IsOpenType}.");
                LogIfDifferent(type.BaseType, resource.OriginalMetadata.BaseType, issues, $"Schema type {type.Name} has a different BaseType value {type.BaseType} than the documentation {resource.OriginalMetadata.BaseType}.");

                AddDocPropertiesToSchemaResource(type, resource, edmx, generateNewElements, issues);
            }
        }

        private void AddDocPropertiesToSchemaResource(
            ComplexType schemaType,
            ResourceDefinition docResource,
            EntityFramework edmx,
            bool generateNewElements,
            IssueLogger issues)
        {
            var docProps = (from p in docResource.Parameters
                            where !p.IsNavigatable &&
                                  !p.Name.StartsWith("@") &&
                                  (docResource.ResolvedBaseTypeReference == null ||
                                   !docResource.ResolvedBaseTypeReference.HasOrInheritsProperty(p.Name))
                            select p).ToList();
            MergePropertiesIntoSchema(schemaType.Name, schemaType.Properties, docProps, edmx, generateNewElements, issues.For(schemaType.Name), schemaType.Contributors);

            var schemaEntity = schemaType as EntityType;

            var docNavigationProps = (from p in docResource.Parameters
                                      where p.IsNavigatable &&
                                            !p.Name.StartsWith("@") &&
                                            (docResource.ResolvedBaseTypeReference == null ||
                                             !docResource.ResolvedBaseTypeReference.HasOrInheritsProperty(p.Name))
                                      select p);

            if (schemaEntity != null)
            {
                MergePropertiesIntoSchema(schemaEntity.Name, schemaEntity.NavigationProperties, docNavigationProps, edmx, generateNewElements, issues.For(schemaEntity.Name), schemaEntity.Contributors);
            }

            if (schemaType.BaseType != docResource.BaseType)
            {
                issues.Warning(ValidationErrorCode.Unknown, $"Resource {schemaType.Name} has multiple declarations with mismatched BaseTypes.");

                if (schemaType.BaseType == null)
                {
                    schemaType.BaseType = docResource.BaseType;
                }
            }

            if (schemaType.OpenType != docResource.OriginalMetadata?.IsOpenType && docResource.OriginalMetadata?.IsOpenType == true)
            {
                issues.Warning(ValidationErrorCode.Unknown, $"Resource { schemaType.Name} has multiple declarations with mismatched OpenType declarations.");
                schemaType.OpenType = true;
            }

            var docInstanceAnnotations = (from p in docResource.Parameters where p.Name != null && p.Name.StartsWith("@") select p);
            MergeInstanceAnnotationsAndRecordTerms(schemaType.Name, docInstanceAnnotations, docResource, edmx, generateNewElements);

            schemaType.Contributors.Add(docResource);
        }

        private void MergePropertiesIntoSchema<TProp>(
            string typeName,
            List<TProp> schemaProps,
            IEnumerable<ParameterDefinition> docProps,
            EntityFramework edmx,
            bool generateNewElements,
            IssueLogger issues,
            HashSet<ResourceDefinition> allContributors = null)
            where TProp : Property, new()
        {
            var documentedProperties = docProps.ToDictionary(x => x.Name, x => x);
            foreach (var schemaProp in schemaProps)
            {
                ParameterDefinition documentedVersion = null;
                if (documentedProperties.TryGetValue(schemaProp.Name, out documentedVersion))
                {
                    // Compare / update schema with data from documentation
                    var docProp = ConvertParameterToProperty<Property>(typeName, documentedVersion, issues.For(schemaProp.Name));
                    LogIfDifferent(schemaProp.Nullable, docProp.Nullable, issues, $"Type {typeName}: Property {docProp.Name} has a different nullable value than documentation.");
                    LogIfDifferent(schemaProp.TargetEntityType, docProp.TargetEntityType, issues, $"Type {typeName}: Property {docProp.Name} has a different target entity type than documentation.");
                    LogIfDifferent(schemaProp.Type, docProp.Type, issues, $"Type {typeName}: Property {docProp.Name} has a different Type value than documentation ({schemaProp.Type},{docProp.Type}).");
                    LogIfDifferent(schemaProp.Unicode, docProp.Unicode, issues, $"Type {typeName}: Property {docProp.Name} has a different unicode value than documentation ({schemaProp.Unicode},{docProp.Unicode}).");
                    documentedProperties.Remove(documentedVersion.Name);

                    AddDescriptionAnnotation(typeName, schemaProp, documentedVersion, issues);
                }
                else
                {
                    // Log out that this property wasn't in the documentation
                    if (allContributors == null ||
                        allContributors.All(c => c.Parameters.All(p => p.Name != schemaProp.Name)))
                    {
                        issues.Warning(ValidationErrorCode.RequiredPropertiesMissing,
                            $"UndocumentedProperty: {typeName} defines {schemaProp.Name} in the schema but has no matching documentation.");
                    }
                }
            }

            if (generateNewElements)
            {
                foreach (var newPropDef in documentedProperties.Values)
                {
                    // Create new properties based on the documentation
                    var newProp = ConvertParameterToProperty<TProp>(typeName, newPropDef, issues.For(newPropDef.Name));
                    schemaProps.Add(newProp);
                }
            }
        }

        private static void LogIfDifferent(object schemaValue, object documentationValue, IssueLogger issues, string errorString)
        {
            if ((schemaValue == null && documentationValue != null) ||
                 (schemaValue != null && documentationValue == null) ||
                 (schemaValue != null && documentationValue != null && !schemaValue.Equals(documentationValue)))
            {
                issues.Warning(ValidationErrorCode.Unknown, errorString);
            }
        }

        private ComplexType CreateResourceInSchema(Schema schema, ResourceDefinition resource, IssueLogger issues)
        {
            var entityKey = resource.ExplicitOrInheritedKeyPropertyName;
            ComplexType type;

            // Create a new entity or complex type for this resource
            if (!string.IsNullOrEmpty(entityKey))
            {
                var entity = new EntityType
                {
                    BaseType = resource.BaseType,
                    HasStream = resource.OriginalMetadata.IsMediaEntity,
                    Name = resource.Name.TypeOnly(),
                    Namespace = resource.Name.NamespaceOnly(),
                    OpenType = resource.OriginalMetadata.IsOpenType,
                };

                if (resource.ResolvedBaseTypeReference == null ||
                    entityKey != resource.ResolvedBaseTypeReference.ExplicitOrInheritedKeyPropertyName)
                {
                    entity.Key = new Key { PropertyRef = new PropertyRef { Name = entityKey } };
                }

                entity.NavigationProperties = (from p in resource.Parameters
                                               where p.IsNavigatable &&
                                                    (resource.ResolvedBaseTypeReference == null ||
                                                    !resource.HasOrInheritsProperty(p.Name))
                                               select ConvertParameterToProperty<NavigationProperty>(entity.Name, p, issues.For(p.Name))).ToList();
                schema.EntityTypes.Add(entity);
                type = entity;
            }
            else
            {
                type = new ComplexType
                {
                    BaseType = resource.BaseType,
                    Name = resource.Name.TypeOnly(),
                    Namespace = resource.Name.NamespaceOnly(),
                    OpenType = resource.OriginalMetadata.IsOpenType,
                };

                schema.ComplexTypes.Add(type);
            }

            type.Abstract = resource.Abstract;
            type.Contributors.Add(resource);

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
                    term.Annotations.Add(new Annotation { Term = Term.LongDescriptionTerm, String = prop.Description.ToStringClean() });
                }

                var targetSchema = FindOrCreateSchemaForNamespace(ns, edmx, overrideNamespaceFilter: true, generateNewElements: generateNewElements);
                if (null != targetSchema)
                {
                    targetSchema.Terms.Add(term);
                }
            }
        }

        private T ConvertParameterToProperty<T>(string typeName, ParameterDefinition param, IssueLogger issues) where T : Property, new()
        {
            var prop = new T()
            {
                Name = param.Name,
                Nullable = (param.Required.HasValue ? !param.Required.Value : false),
                Type = param.Type.ODataResourceName()
            };

            // Add description annotation
            AddDescriptionAnnotation(typeName, prop, param, issues);
            return prop;
        }

        private void AddDescriptionAnnotation<T>(
            string typeName,
            T targetProperty,
            ParameterDefinition sourceParameter,
            IssueLogger issues,
            string termForDescription = Term.DescriptionTerm) where T : Property, IODataAnnotatable
        {
            if (this.options.Annotations == AnnotationOptions.None)
            {
                return;
            }

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
                    LogIfDifferent(descriptionTerm.String, sourceParameter.Description, issues, $"Type {typeName} has a different value for term '{termForDescription}' than the documentation.");
                }
                else
                {
                    targetProperty.Annotation.Add(
                    new Annotation()
                    {
                        Term = termForDescription,
                        String = sourceParameter.Description.ToStringClean(),
                    });
                }
            }
        }

        private class EnumComparer : IEqualityComparer<EnumerationDefinition>
        {
            public bool Equals(EnumerationDefinition x, EnumerationDefinition y)
            {
                if (object.ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return x.MemberName.Equals(y.MemberName);
            }

            public int GetHashCode(EnumerationDefinition obj)
            {
                return obj?.MemberName.GetHashCode() ?? 0;
            }
        }
    }


    public class CsdlWriterOptions
    {
        public string OutputDirectoryPath { get; set; }
        public string SourceMetadataPath { get; set; }
        public string MergeWithMetadataPath { get; set; }
        public MetadataFormat Formats { get; set; }
        public string[] Namespaces { get; set; }
        public bool Sort { get; set; }
        public string TransformOutput { get; set; }
        public string DocumentationSetPath { get; set; }
        public string Version { get; set; }
        public bool SkipMetadataGeneration { get; set; }
        public AnnotationOptions Annotations { get; set; }
        public bool ValidateSchema { get; set; }
        public bool AttributesOnNewLines { get; set; }
        public string EntityContainerName { get; set; }
        public bool ShowSources { get; set; }
    }

    [Flags]
    public enum MetadataFormat
    {
        Default = EdmxInput | EdmxOutput,
        EdmxInput = 1 << 0,
        EdmxOutput = 1 << 1,
        SchemaInput = 1 << 2,
        SchemaOutput = 1 << 3
    }

    [Flags]
    public enum AnnotationOptions
    {
        None = 0x00,
        Properties = 0x01,
        Capabilities = 0x02,
        HttpRequests = 0x04,
        Independent = 0x08,
        AllAnnotations = Properties | Capabilities | HttpRequests,
        OnlyAnnotations = AllAnnotations | Independent,
    }
}
