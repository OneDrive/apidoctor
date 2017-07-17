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

namespace ApiDocs.DocumentationGeneration.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using ApiDocs.DocumentationGeneration.Model;
    using ApiDocs.Validation.OData;

    public static class DocumentationExtensions
    {
        private const string CollectionPrefix = "Collection(";

        private const string DescriptionPlaceHolder = "**TODO: Add documentation**";

        public static string GetDescription(this Property property, EntityFramework entityFramework, ComplexType complexType)
        {
            var inlineAnnotation = property.Annotation.FirstOrDefault(a => a.Term == Term.DescriptionTerm);
            if (inlineAnnotation != null)
            {
                return inlineAnnotation.String;
            }

            var descriptionAnnotation = property.GetAnnotationsForProperty(complexType, entityFramework).FirstOrDefault(a => a.Term == Term.DescriptionTerm);

            if (descriptionAnnotation != null)
            {
                return descriptionAnnotation.String;
            }

            return DescriptionPlaceHolder;
        }


        public static string GetDescription(this ComplexType complexType, EntityFramework entityFramework)
        {
            var inlineAnnotation = complexType.Annotation.FirstOrDefault(a => a.Term == Term.DescriptionTerm);
            if (inlineAnnotation != null)
            {
                return inlineAnnotation.String;
            }
            var descriptionAnnotation = complexType.GetAnnotationsForTarget(entityFramework).FirstOrDefault(a => a.Term == Term.DescriptionTerm);

            if (descriptionAnnotation != null)
            {
                return descriptionAnnotation.String;
            }

            return DescriptionPlaceHolder;
        }

        public static List<Annotation> GetAnnotationsForProperty(this Property targetProperty, ComplexType complexType, EntityFramework entityFramework)
        {
            string targetType = entityFramework.LookupIdentifierForType(complexType);
            var targetNamespace = targetType.NamespaceOnly();
            var schema = entityFramework.DataServices.Schemas.FirstOrDefault(s => s.Namespace == targetNamespace);
            if (schema != null)
            {
                string target = $"{targetType}/{targetProperty.Name}";
                var annotations = schema.Annotations.FirstOrDefault(a => a.Target == target);
                if (annotations != null)
                {
                    return annotations.AnnotationList;
                }
            }
            return new List<Annotation>();
        }

        public static List<Annotation> GetAnnotationsForTarget<T>(this T target, EntityFramework entityFramework) where T: IODataNavigable, IODataAnnotatable
        {
            string targetType = entityFramework.LookupIdentifierForType(target);
            var targetNamespace = targetType.NamespaceOnly();
            var schema = entityFramework.DataServices.Schemas.FirstOrDefault(s => s.Namespace == targetNamespace);
            if (schema != null)
            {
                var annotations = schema.Annotations.FirstOrDefault(a => a.Target == targetType);
                if (annotations != null)
                {
                    return annotations.AnnotationList;
                }
            }
            return new List<Annotation>();
        }

        public static string GetTypeNameMarkdown(this Property property, EntityFramework entityFramework)
        {
            return GetTypeNameMarkdown(property.Type, entityFramework);
        }

        public static DocumentationComplexType ToDocumentationComplexType(this ComplexType type, EntityFramework entityFramework)
        {
            return new DocumentationComplexType(entityFramework, type);
        }

        public static DocumentationEntityType ToDocumentationEntityType(this EntityType type, EntityFramework entityFramework)
        {
            return new DocumentationEntityType(entityFramework, type);
        }

        public static DocumentationNavigationProperty ToDocumentationNavigationProperty(this NavigationProperty property, EntityFramework entityFramework, EntityType entityType)
        {
            return new DocumentationNavigationProperty(entityFramework, entityType, property);
        }

        public static DocumentationProperty ToDocumentationProperty(this Property property, EntityFramework entityFramework, ComplexType complexType)
        {
            return new DocumentationProperty(entityFramework, complexType, property);
        }

        private static string GetTypeNameMarkdown(string typeName, EntityFramework entityFramework)
        {
            string propertyType = typeName;
            if (propertyType.StartsWith(CollectionPrefix) && propertyType.EndsWith(")"))
            {
                propertyType = propertyType.Substring(CollectionPrefix.Length, propertyType.Length - CollectionPrefix.Length - 1);
                return GetTypeNameMarkdown(propertyType, entityFramework) + " collection";
            }

            IODataNavigable type = entityFramework.DataServices.Schemas.LookupNavigableType(propertyType);

            var simpleType = type as ODataSimpleType;
            if (simpleType != null)
            {
                return simpleType.Type.ToString();
            }

            return $"[{type.TypeIdentifier}]({type.TypeIdentifier}.md)";
        }
    }
}