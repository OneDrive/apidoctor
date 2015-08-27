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

        public void PrintToConsole()
        {
            FancyConsole.WriteLine();
            FancyConsole.Write("Runs completed. ");

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
                if (this.WarningCount > 0 || this.FailureCount > 0 && this.SuccessCount > 0)
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
