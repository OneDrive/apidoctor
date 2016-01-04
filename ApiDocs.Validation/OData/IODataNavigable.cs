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

    public interface IODataNavigable
    {
        /// <summary>
        /// Returns the next target pasted on the value of a component of the URI
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx);

        /// <summary>
        /// Returns the next component assuming that an entitytype key is provided
        /// </summary>
        /// <returns></returns>
        IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx);

        string TypeIdentifier { get; }
    }

    public class ODataCollection : IODataNavigable
    {
        public string TypeIdentifier { get; internal set; }

        public ODataCollection(string typeIdentifier)
        {
            this.TypeIdentifier = typeIdentifier;
        }

        public IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx)
        {
            // NavigationByUriComponent for a collection means that we have a hard coded key in the example path.
            return this.NavigateByEntityTypeKey(edmx);
        }

        public IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx)
        {
            return edmx.ResourceWithIdentifier<IODataNavigable>(this.TypeIdentifier);
        }
    }

    public class ODataSimpleType : IODataNavigable
    {
        public SimpleDataType Type { get; internal set; }

        public ODataSimpleType(SimpleDataType type)
        {
            this.Type = type;
        }

        public IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx)
        {
            throw new NotSupportedException();
        }

        public IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx)
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
        NavigationProperty
    }
}
