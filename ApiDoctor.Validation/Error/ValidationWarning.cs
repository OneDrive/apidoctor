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

namespace ApiDoctor.Validation.Error
{
    public class ValidationWarning : ValidationError
    {

        public ValidationWarning(ValidationErrorCode code, string source, string sourceFile, string format, params object[] formatParams)
            : base(code, source, sourceFile, format, formatParams)
        {

        }

        public override bool IsWarning { get { return true; } }

        public override bool IsError { get { return false; } }
    }


    public class UndocumentedPropertyWarning : ValidationWarning
    {
        public UndocumentedPropertyWarning(string source, string sourceFile, string propertyName, ParameterDataType propertyType, string resourceName)
            : base(ValidationErrorCode.AdditionalPropertyDetected, source, sourceFile, "Undocumented property '{0}' [{1}] was not expected on resource {2}.", propertyName, propertyType, resourceName)
        {
            this.PropertyName = propertyName;
            this.PropertyType = propertyType;
            this.ResourceName = resourceName;
        }

        public string PropertyName { get; private set; }
        public ParameterDataType PropertyType { get; private set; }
        public string ResourceName { get; private set; }
    }
}
