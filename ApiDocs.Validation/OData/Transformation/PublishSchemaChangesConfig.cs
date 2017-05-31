using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.OData.Transformation
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

    public class BaseModifications
    {
        [JsonProperty("remove")]
        public bool Remove { get; set; }

        [JsonProperty("versions")]
        public string[] AvailableInVersions { get; set; }
    }

    public class SchemaModifications : BaseModifications
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

    public class TermModification : BaseModifications
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

    public class AnnotationModification : BaseModifications
    {
        [JsonProperty("string")]
        public string String { get; set; }

        [JsonProperty("bool")]
        public bool? Bool { get; set; }
    }

    public class FunctionModification : BaseModifications
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

    public class ReturnTypeModification : BaseModifications
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("nullable")]
        public bool? Nullable { get; set; }

        [JsonProperty("unicode")]
        public bool? Unicode { get; set; }
    }

    public class ParameterModification : BaseModifications
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("nullable")]
        public bool? Nullable { get; set; }

        [JsonProperty("unicode")]
        public bool? Unicode { get; set; }

        [JsonProperty("order")]
        public int? ParameterIndex { get; set; }

    }

    public class EntityContainerModification : BaseModifications
    {
        [JsonProperty("entitySet")]
        public Dictionary<string, EntitySetModification> EntitySets { get; set; }

        [JsonProperty("singleton")]
        public Dictionary<string, EntitySetModification> Singletons { get; set; }
    }

    public class EntitySetModification : BaseModifications
    {
        [JsonProperty("entityType")]
        public string EntityType { get; set; }

        [JsonProperty("navigationPropertyBinding")]
        public Dictionary<string, NavigationPropertyBindingModification> NavigationPropertyBinding { get; set; }
    }

    public class NavigationPropertyBindingModification : BaseModifications
    {
        [JsonProperty("target")]
        public string Target { get; set; }
    }

    public class ComplexTypeModification : BaseModifications
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

    public class KeyModification : BaseModifications
    {
        [JsonProperty]
        public PropertyRefModification PropertyRef { get; set; }
    }

    public class PropertyRefModification : BaseModifications
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class PropertyModification : BaseModifications
    {
        [JsonProperty("ags:CreateVirutalNavigationProperty")]
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

    public class EnumerationTypeModification : BaseModifications
    {
        [JsonProperty("underlyingType")]
        public string UnderlyingType { get; set; }

        [JsonProperty("isFlags")]
        public bool? IsFlags { get; set; }

        [JsonProperty("member")]
        public Dictionary<string, MemberModification> Members { get; set; }
    }

    public class MemberModification : BaseModifications
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
