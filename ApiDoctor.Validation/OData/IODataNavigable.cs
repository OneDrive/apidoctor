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

namespace ApiDoctor.Validation.OData
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using ApiDoctor.Validation.Error;

    public interface IODataNavigable
    {
        /// <summary>
        /// Returns the next target pasted on the value of a component of the URI
        /// </summary>
        IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx, IssueLogger logger, bool isLastSegment);

        /// <summary>
        /// Returns the next component assuming that an entitytype key is provided
        /// </summary>
        IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx, IssueLogger issues);

        string TypeIdentifier { get; }
    }

    public class ODataCollection : IODataNavigable
    {
        public string TypeIdentifier { get; internal set; }

        public ODataCollection(string typeIdentifier)
        {
            this.TypeIdentifier = typeIdentifier;
        }

        public IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx, IssueLogger issues, bool isLastSegment)
        {
            // NavigationByUriComponent for a collection means that either we have a hard coded key in the example path,
            // or this is a function/action bound to a collection.

            long testLong;
            Guid testGuid;
            if (Guid.TryParse(component, out testGuid) ||
                long.TryParse(component, out testLong) ||
                component.IsLikelyBase64Encoded())
            {
                issues.Warning(ValidationErrorCode.AmbiguousExample,
                    $"Assuming {component} under {this.TypeIdentifier} is a hard-coded key in the example path. Please fix to be a placeholder.");
                return this.NavigateByEntityTypeKey(edmx, issues);
            }

            var result = this.NavigateByFunction(component, edmx, isLastSegment);
            if (result != null)
            {
                return result;
            }

            // if the segment is itself a fully-qualified type, then it's a cast operator and should return a casted collection.
            if (component.Contains("."))
            {
                result = edmx.LookupNavigableType(component);
                if (result != null)
                {
                    return new ODataCollection(component);
                }
            }

            return null;
        }

        public IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx, IssueLogger issues)
        {
            return edmx.ResourceWithIdentifier<IODataNavigable>(this.TypeIdentifier);
        }
    }

    public static class OdataNavigableExtensionMethods
    {
        public static IODataNavigable NavigateByFunction(this IODataNavigable source, string component, EntityFramework edmx, bool isLastSegment)
        {
            var matches =
                edmx.DataServices.Schemas.SelectMany(s => s.Functions).
                    Where(f =>
                        f.IsBound &&
                        (f.Name == component || f.ParameterizedName == component) &&
                        f.ReturnType?.Type != null &&
                        f.Parameters.Any(p => p.Name == "bindingParameter" && p.Type.TypeOnly() == source.TypeIdentifier.TypeOnly())).
                    ToList();

            if (matches.Any())
            {
                foreach (var m in matches)
                {
                    // if its the last segment, it may not really be composable. So default to what already is already there.
                    m.IsComposable |= !isLastSegment;
                }

                var match = matches.First().ReturnType.Type;
                return edmx.ResourceWithIdentifier<IODataNavigable>(match);
            }

            var otherCaseName =
                edmx.DataServices.Schemas.SelectMany(s => s.Functions).
                    Where(f =>
                        f.IsBound &&
                        (f.Name.IEquals(component) || f.ParameterizedName.IEquals(component)) &&
                        f.Parameters.Any(p => p.Name == "bindingParameter" && p.Type.TypeOnly() == source.TypeIdentifier.TypeOnly())).
                    Select(f => f.Name.IEquals(component) ? f.Name : f.ParameterizedName).
                    FirstOrDefault();

            if (otherCaseName != null)
            {
                throw new ArgumentException($"ERROR: case mismatch between URL segment '{component}' and schema element '{otherCaseName}'");
            }

            return null;
        }
    }

    public class ODataSimpleType : IODataNavigable
    {
        public SimpleDataType Type { get; internal set; }

        public ODataSimpleType(SimpleDataType type)
        {
            this.Type = type;
        }

        public IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx, IssueLogger issues, bool isLastSegment)
        {
            throw new NotSupportedException($"simple type can't navigate to {component}");
        }

        public IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx, IssueLogger issues)
        {
            throw new NotSupportedException();
        }

        public string TypeIdentifier
        {
            get { return Type.ODataResourceName(); }
        }
    }

    public class ODataTargetInfo
    {
        public ODataTargetClassification Classification { get; set; }

        public string QualifiedType { get; set; }

        public string Name { get; set; }
    }


    public enum ODataTargetClassification
    {
        Unknown,
        EntityType,
        EntitySet,
        Action,
        Function,
        EntityContainer,
        SimpleType,
        ComplexType,
        NavigationProperty,
        TypeCast,
    }
}
