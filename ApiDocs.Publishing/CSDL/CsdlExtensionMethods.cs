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
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using Validation.Http;
    using Validation.OData;
    using Validation.OData.Transformation;
    using Validation.Utility;
    using System.Linq;

    internal static class CsdlExtensionMethods
    {

        public static string RequestUriPathOnly(this MethodDefinition method, string baseUrlToRemove)
        {
            var path = method.Request.FirstLineOnly().TextBetweenCharacters(' ', '?').TrimEnd('/');

            if (null != baseUrlToRemove && path.StartsWith(baseUrlToRemove))
            {
                path = path.Substring(baseUrlToRemove.Length);
            }

            // Normalize variables in the request path
            path = path.ReplaceTextBetweenCharacters('{', '}', "var");
            path = path.ReplaceTextBetweenCharacters('<', '>', "{var}", true, true);        // Fix Graph docs placeholder

            // Rewrite path syntax into what it logically means
            path = path.ReplaceTextBetweenCharacters(':', ':', "/children/{var}", requireSecondChar: false, removeTargetChars: true);

            return path;
        }

        public static string HttpMethodVerb(this MethodDefinition method)
        {
            HttpParser parser = new HttpParser();
            var request = parser.ParseHttpRequest(method.Request);
            return request.Method;

        }

        internal static void AppendWithCondition(this System.Text.StringBuilder sb, bool condition, string text, string prefixIfExistingContent = null)
        {
            if (condition)
            {
                if (sb.Length > 0 && prefixIfExistingContent != null)
                    sb.Append(prefixIfExistingContent);
                sb.Append(text);
            }
        }

        /// <summary>
        /// Merge two EntityFramework instances together into the first framework
        /// </summary>
        /// <param name="framework1"></param>
        /// <param name="framework2"></param>
        internal static EntityFramework MergeWith(this EntityFramework framework1, EntityFramework framework2)
        {
            ObjectGraphMerger<EntityFramework> merger = new ObjectGraphMerger<EntityFramework>(framework1, framework2);
            var edmx = merger.Merge();

            // Clean up bindingParameters on actions and methods to be consistently the same
            foreach(var schema in edmx.DataServices.Schemas)
            {
                foreach (var action in schema.Actions)
                {
                    foreach(var param in action.Parameters.Where(x => x.Name == "bindingParameter" || x.Name == "this")) {
                        param.Name = "bindingParameter";
                        param.Nullable = null;
                    }
                }
                foreach(var func in schema.Functions)
                {
                    foreach (var param in func.Parameters.Where(x => x.Name == "bindingParameter" || x.Name == "this")) {
                        param.Name = "bindingParameter";
                        param.Nullable = null;
                    }
                }
            }

            return edmx;


        }

       

       
    }
}
