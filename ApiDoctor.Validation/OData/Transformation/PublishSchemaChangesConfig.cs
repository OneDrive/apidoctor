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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDoctor.Validation.OData.Transformation
{
    public class PublishSchemaChangesConfigFile : Config.ConfigFile
    {
        [JsonProperty("publishSchemaChanges")]
        public PublishSchemaChanges SchemaChanges { get; set; }

        public override bool IsValid
        {
            get
            {
                return SchemaChanges != null;
            }
        }
    }

    public class PublishSchemaChanges
    {
        [JsonProperty("name")]
        public string TransformationName { get; set; }

        [JsonProperty("namespacesToPublish")]
        public string[] NamespacesToPublish { get; set; }

        [JsonProperty("namespaces")]
        public Dictionary<string, SchemaModifications> Schemas { get; set; }
    }

    /// <summary>
    /// Properties in CommonModificationProperties are copied to all objects if applicable.
    /// </summary>
    public class CommonModificationProperties : BaseModifications
    {
        [JsonProperty("order")]
        public int? CollectionIndex { get; set; }
    }

    /// <summary>
    /// Properties in BaseModifications are NOT copied to the target of the modification. These are used as control properties for 
    /// behavior outside of modifying the actual instance of the target object.
    /// </summary>
    public class BaseModifications
    {
        /// <summary>
        /// Indicates that the target element should be removed after the schema
        /// </summary>
        [JsonProperty("remove")]
        public bool Remove { get; set; }

        /// <summary>
        /// Indicates that the target element should be added if missing from the schema
        /// </summary>
        [JsonProperty("add")]
        public bool Add { get; set; }

        /// <summary>
        /// Indiciates the version(s) of output schema that should include the target element.
        /// </summary>
        [JsonProperty("versions")]
        public string[] AvailableInVersions { get; set; }

    }

    public class SchemaModifications : CommonModificationProperties
    {
        [JsonProperty("entityType")]
        public Dictionary<string, EntityTypeModification> EntityTypes { get; set; }

        [JsonProperty("complexType")]
        public Dictionary<string, ComplexTypeModification> ComplexTypes { get; set; }

        [JsonProperty("entityContainer")]
        public Dictionary<string, EntityContainerModification> EntityContainers { get; set; }

        [JsonProperty("function")]
        public Dictionary<string, FunctionModification> Functions { get; set; }

        [JsonProperty("action")]
        public Dictionary<string, FunctionModification> Actions { get; set; }

        [JsonProperty("term")]
        public Dictionary<string, TermModification> Terms { get; set; }

        [JsonProperty("annotation")]
        public Dictionary<string, AnnotationModification> Annotations { get; set; }
    }

    public class TermModification : CommonModificationProperties
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("appliesTo")]
        public string AppliesTo { get; set; }

        [JsonProperty("ags:WorkloadTermNamespace")]
        public string GraphWorkloadTermNamespace { get; set; }

        [JsonProperty("annotation")]
        public Dictionary<string, AnnotationModification> Annotations { get; set; }
    }

    public class AnnotationModification : CommonModificationProperties
    {
        [JsonProperty("string")]
        public string String { get; set; }

        [JsonProperty("bool")]
        public bool? Bool { get; set; }
    }

    public class FunctionModification : CommonModificationProperties
    {
        [JsonProperty("isBound")]
        public bool? IsBound { get; set; }

        [JsonProperty("isComposable")]
        public bool? IsComposable { get; set; }

        [JsonProperty("parameter")]
        public Dictionary<string, ParameterModification> Parameters { get; set; }

        [JsonProperty("returnType")]
        public ReturnTypeModification ReturnType { get; set; }
    }

    public class ReturnTypeModification : CommonModificationProperties
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("nullable")]
        public bool? Nullable { get; set; }

        [JsonProperty("unicode")]
        public bool? Unicode { get; set; }
    }

    public class ParameterModification : CommonModificationProperties
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("nullable")]
        public bool? Nullable { get; set; }

        [JsonProperty("unicode")]
        public bool? Unicode { get; set; }

    }

    public class EntityContainerModification : CommonModificationProperties
    {
        [JsonProperty("entitySet")]
        public Dictionary<string, EntitySetModification> EntitySets { get; set; }

        [JsonProperty("singleton")]
        public Dictionary<string, EntitySetModification> Singletons { get; set; }
    }

    public class EntitySetModification : CommonModificationProperties
    {
        [JsonProperty("entityType")]
        public string EntityType { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("navigationPropertyBinding")]
        public Dictionary<string, NavigationPropertyBindingModification> NavigationPropertyBinding { get; set; }
    }

    public class NavigationPropertyBindingModification : CommonModificationProperties
    {
        [JsonProperty("target")]
        public string Target { get; set; }
    }

    public class ComplexTypeModification : CommonModificationProperties
    {
        [JsonProperty("graphName")]
        public string GraphEntityTypeName { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, PropertyModification> Properties { get; set; }

        [JsonProperty("baseType")]
        public string BaseType { get; set; }

        [JsonProperty("openType")]
        public bool? OpenType { get; set; }

    }

    public class EntityTypeModification : ComplexTypeModification
    {
        [JsonProperty("ags:IsMaster")]
        public bool? GraphIsMaster { get; set; }
        [JsonProperty("ags:AddressUrl")]
        public string GraphAddressUrl { get; set; }
        [JsonProperty("ags:AddressUrlMsa")]
        public string GraphAddressUrlMsa { get; set; }
        [JsonProperty("ags:AddressContainsEntitySetSegment")]
        public bool? GraphAddressContainsEntitySetSegment { get; set; }
        [JsonProperty("ags:InstantOnUrl")]
        public string GraphInstantOnUrl { get; set; }

        [JsonProperty("key")]
        public KeyModification Key { get; set; }

        [JsonProperty("navigationProperties")]
        public Dictionary<string, PropertyModification> NavigationProperties { get; set; }
    }

    public class KeyModification : CommonModificationProperties
    {
        [JsonProperty]
        public PropertyRefModification PropertyRef { get; set; }
    }

    public class PropertyRefModification : CommonModificationProperties
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class PropertyModification : CommonModificationProperties
    {
        [JsonProperty("ags:CreateVirtualNavigationProperty")]
        public bool? CreateVirtualNavigationProperty { get; set; }

        [JsonProperty("ags:VirtualNavigationPropertyName")]
        public string VirtualNavigationPropertyName { get; set; }

        [JsonProperty("ags:TargetEntityType")]
        public string TargetEntityType { get; set; }

        [JsonProperty("ags:KeyPropertyPath")]
        public string KeyPropertyPath { get; set; }

        [JsonProperty("containsTarget")]
        public bool? ContainsTarget { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("nullable")]
        public bool? Nullable { get; set; }
        [JsonProperty("isBound")]
        public bool? IsBound {get;set;}

        [JsonProperty("graphName")]
        public string GraphPropertyName { get; set; }
    }

    public class EnumerationTypeModification : CommonModificationProperties
    {
        [JsonProperty("underlyingType")]
        public string UnderlyingType { get; set; }

        [JsonProperty("isFlags")]
        public bool? IsFlags { get; set; }

        [JsonProperty("member")]
        public Dictionary<string, MemberModification> Members { get; set; }
    }

    public class MemberModification : CommonModificationProperties
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
