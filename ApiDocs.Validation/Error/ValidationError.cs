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

namespace ApiDocs.Validation.Error
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public enum ValidationErrorCode
    {
        Unknown,
        ConsolidatedError,
        JsonParserException,
        JsonErrorObject,
        MissingCollectionProperty,
        CollectionArrayEmpty,
        CollectionArrayNotEmpty,
        RequiredPropertiesMissing,
        AdditionalPropertyDetected,
        ExpectedTypeDifferent,
        ExpectedArrayValue,
        ExpectedNonArrayValue,
        ResourceTypeNotFound,
        NoCustomMembersFound,
        CustomValidationNotSupported,
        ResponseResourceTypeMissing,
        ArrayTypeMismatch,

        HttpStatusCodeDifferent,
        HttpStatusMessageDifferent,
        HttpRequiredHeaderMissing,
        HttpHeaderValueDifferent,
        HttpBodyExpected,
        HttpResponseFormatInvalid,

        ErrorOpeningFile,
        ErrorReadingFile,
        ErrorCopyingFile,
        ExtraFileDetected,

        MarkdownParserError,

        MissingLinkSourceId,
        LinkValidationSkipped,
        LinkDestinationNotFound,
        LinkDestinationOutsideDocSet,
        LinkFormatInvalid,

        MissingRequiredArguments,
        MissingAccessToken,
        JsonErrorObjectExpected,
        RequestWasEmptyOrNull,
        InvalidRequestFormat,
        UnsupportedContentType,
        RewriteRequestFailure,
        HttpParserError,
        BadPathTargetValue,
        ParameterParserError,
        MethodDisabled,
        OutputValueKeyNameIncorrect,
        HttpRequestAndMethodSpecified,
        OutputValueValueIncorrect,
        HttpRequestParameterInvalid,
        HttpRequestMissingAllowedStatusCodes,
        TypeConversionFailure,
        OrphanedDocumentPage,
        UnmappedDocumentElements,
        MissingCustomMembers,
        NullPropertyValue,
        InvalidContentType,
        InvalidHttpMethod,
        UrlLengthExceedsMaximum,
        NonStandardHeaderUsed,
        MissingHeaderBlock,
        InvalidDateTimeString,
        InvalidEnumeratedValueString,
        InvalidUrlString,
        InvalidExpectationKey,
        ExpectationConditionFailed,
        NoDocumentsFound,
        MissingResourceName,
        UnsupportedLanguage,
        AllScenariosDisabled,
        ExceptionWhileValidatingMethod,
        RequestWasRetried,
        ProhibitedDocumentHeaderFound,
        ExtraDocumentHeaderFound,
        RequiredDocumentHeaderMissing,
        DocumentHeaderInWrongPosition,
        SkippedSimilarErrors,
        RequiredScopesMissing,
        AnnotationParserException,
        DuplicateMethodIdentifier,
        ContentFormatException
    }

    public class ValidationError
    {
        protected ValidationError()
        {
            
        }

        public ValidationError(ValidationErrorCode code, string source, string messageformat, params object[] formatParams)
        {
            this.Code = code;
            this.Source = source;
            try
            {
                this.Message = string.Format(messageformat, formatParams);
            }
            catch
            {
                this.Message = messageformat;
            }
        }

        public static ValidationError CreateError(bool isWarning, ValidationErrorCode code, string source, string messageFormat, params object[] formatParams)
        {
            if (isWarning)
                return new ValidationWarning(code, source, messageFormat, formatParams);
            else
                return new ValidationError(code, source, messageFormat, formatParams);
        }

        public ValidationErrorCode Code { get; set; }

        public string Message { get; set; }

        public string Source { get; set; }

        public ValidationError[] InnerErrors { get; set; }

        public virtual bool IsWarning { get { return false; } }

        public virtual bool IsError { get { return true; } }

        public virtual bool IsWarningOrError
        {
            get { return IsWarning | IsError; }
        }

        /// <summary>
        /// Returns a log-ready string that includes information about the specific error/warning/message.
        /// </summary>
        public string ErrorText 
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                if (this.IsWarning)
                {
                    sb.Append("Warning: ");
                }
                else if (this.IsError)
                {
                    sb.Append("Error: ");
                }
                
                if (!string.IsNullOrEmpty(this.Source))
                {
                    sb.Append(this.Source);
                    sb.Append(": ");
                }
                sb.Append(this.Message);

                if (null != this.InnerErrors && this.InnerErrors.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine(this.InnerErrors.ErrorsToString("   "));
                }
                
                return sb.ToString();
            }
        }

        public static ValidationError NewConsolidatedError(ValidationErrorCode code, ValidationError[] errors, string message, params object[] parameters)
        {
            var error = errors.All(err => err.IsWarning) ? new ValidationWarning(code, null, message, parameters) : new ValidationError(code, null, message, parameters);

            error.InnerErrors = errors;
            return error;
        }
    }

    public static class ValidationErrorExtensions
    {
        public static string ErrorsToString(this IEnumerable<ValidationError> errors, string linePrefix = "", int linesBetweenErrors = 0)
        {
            var messages = from e in errors select string.Concat(linePrefix, e.ErrorText);

            string lineEnding = Environment.NewLine;
            if (linesBetweenErrors > 0)
            {
                StringBuilder sb = new StringBuilder(lineEnding);
                for (int i = 0; i < linesBetweenErrors; i++)
                {
                    sb.AppendLine();
                }
                lineEnding = sb.ToString();
            }
            return messages.ComponentsJoinedByString(lineEnding);
        }
    }
}
