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

namespace ApiDocs.ConsoleApp
{
    using System.Collections.Generic;
    using System.Linq;
    using ApiDocs.Validation;
    using ApiDocs.Validation.Error;

    public class CheckResults
    {
        public int SuccessCount { get; set; }
        public int WarningCount { get; set; }
        public int FailureCount { get; set; }

        public int TotalCount { get { return this.SuccessCount + this.WarningCount + this.FailureCount; } }

        public bool WereFailures { get { return this.FailureCount != 0; } }

        public double PercentSuccessful
        {
            get { return 100.0 * ((double)this.SuccessCount / this.TotalCount); }
        }

        public IEnumerable<ValidationError> Errors { get; set; }


        public void IncrementResultCount(IEnumerable<ValidationError> output)
        {
            var errors = output as ValidationError[] ?? output.ToArray();
            if (errors.WereErrors())
            {
                this.FailureCount++;
            }
            else if (errors.WereWarnings())
            {
                this.WarningCount++;
            }
            else
            {
                this.SuccessCount++;
            }
        }

        public void ConvertWarningsToSuccess()
        {
            this.SuccessCount += this.WarningCount;
            this.WarningCount = 0;
        }

        public static CheckResults operator +(CheckResults c1, CheckResults c2)
        {
            var result = new CheckResults
            {
                FailureCount = c1.FailureCount + c2.FailureCount,
                WarningCount = c1.WarningCount + c2.WarningCount,
                SuccessCount = c1.SuccessCount + c2.SuccessCount
            };

            var allErrors = new List<ValidationError>();
            if (null != c1.Errors)
                allErrors.AddRange(c1.Errors);
            if (null != c2.Errors)
                allErrors.AddRange(c2.Errors);

            if (allErrors.Count > 0)
                result.Errors = allErrors;

            return result;
        }

        public void PrintToConsole(bool addNewLine = true)
        {
            if (addNewLine)
            {
                FancyConsole.WriteLine();
                FancyConsole.Write("Runs completed. ");
            }

            const string percentCompleteFormat = "{0:0.00}% passed";
            FancyConsole.Write(
                this.PercentSuccessful == 100.0 ? FancyConsole.ConsoleSuccessColor : FancyConsole.ConsoleWarningColor,
                percentCompleteFormat,
                this.PercentSuccessful);

            if (this.FailureCount > 0 || this.WarningCount > 0)
            {
                FancyConsole.Write(" (");
                if (this.FailureCount > 0)
                    FancyConsole.Write(FancyConsole.ConsoleErrorColor, "{0} errors", this.FailureCount);
                if (this.WarningCount > 0 && this.FailureCount > 0)
                    FancyConsole.Write(", ");
                if (this.WarningCount > 0)
                    FancyConsole.Write(FancyConsole.ConsoleWarningColor, "{0} warnings", this.WarningCount);
                if ( (this.WarningCount > 0 || this.FailureCount > 0) && this.SuccessCount > 0)
                    FancyConsole.Write(", ");
                if (this.SuccessCount > 0)
                    FancyConsole.Write(FancyConsole.ConsoleSuccessColor, "{0} successful", this.SuccessCount);
                FancyConsole.Write(")");
            }
            FancyConsole.WriteLine();
        }

        /// <summary>
        /// Record the results from a validation test run into this object
        /// </summary>
        /// <param name="results"></param>
        internal void RecordResults(ValidationResults results, CheckServiceOptions options)
        {
            foreach (var result in results.Results)
            {
                if ((result.Outcome & ValidationOutcome.Error) > 0)
                {
                    FailureCount++;
                }
                else if ((result.Outcome & ValidationOutcome.Warning) > 0)
                {
                    if (options.IgnoreWarnings || options.SilenceWarnings)
                        SuccessCount++;
                    else 
                        WarningCount++;
                }
                else if ((result.Outcome & ValidationOutcome.Passed) > 0)
                {
                    SuccessCount++;
                }
            }
        }
    }
}
