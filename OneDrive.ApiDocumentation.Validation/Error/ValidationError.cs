namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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
        NonStandardHeaderUsed

    }

    public class ValidationError
    {
        protected ValidationError()
        {
            
        }

        public ValidationError(ValidationErrorCode code, string source, string messageformat, params object[] formatParams)
        {
            Code = code;
            Source = source;
            Message = string.Format(messageformat, formatParams);
        }

        public ValidationErrorCode Code { get; set; }

        public string Message { get; set; }

        public string Source { get; set; }

        public ValidationError[] InnerErrors { get; set; }

        public virtual bool IsWarning { get { return false; } }

        public virtual bool IsError { get { return true; } }

        /// <summary>
        /// Returns a log-ready string that includes information about the specific error/warning/message.
        /// </summary>
        public string ErrorText 
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                if (IsWarning)
                {
                    sb.Append("Warning: ");
                }
                else if (IsError)
                {
                    sb.Append("Error: ");
                }
                
                if (!string.IsNullOrEmpty(Source))
                {
                    sb.Append(Source);
                    sb.Append(": ");
                }
                sb.Append(Message);

                if (null != InnerErrors && InnerErrors.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine(InnerErrors.ErrorsToString("   "));
                }
                
                return sb.ToString();
            }
        }

        public static ValidationError NewConsolidatedError(ValidationErrorCode code, ValidationError[] errors, string message, params object[] parameters)
        {
            ValidationError error = null;
            if (errors.All(err => err.IsWarning))
            {
                error = new ValidationWarning(code, null, message, parameters);
            }
            else
            {
                error = new ValidationError(code, null, message, parameters);
            }

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
