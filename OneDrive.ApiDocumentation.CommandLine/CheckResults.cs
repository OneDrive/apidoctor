using OneDrive.ApiDocumentation.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.ConsoleApp
{
    public class CheckResults
    {
        public int SuccessCount { get; set; }
        public int WarningCount { get; set; }
        public int FailureCount { get; set; }

        public int TotalCount { get { return SuccessCount + WarningCount + FailureCount; } }

        public bool WereFailures { get { return FailureCount != 0; } }

        public double PercentSuccessful
        {
            get { return 100.0 * ((double)SuccessCount / TotalCount); }
        }

        public IEnumerable<ValidationError> Errors { get; set; }


        public void IncrementResultCount(IEnumerable<ValidationError> output)
        {
            if (output.WereErrors())
            {
                FailureCount++;
            }
            else if (output.WereWarnings())
            {
                WarningCount++;
            }
            else
            {
                SuccessCount++;
            }
        }

        public void ConvertWarningsToSuccess()
        {
            SuccessCount += WarningCount;
            WarningCount = 0;
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
            if (PercentSuccessful == 100.0)
                FancyConsole.Write(FancyConsole.ConsoleSuccessColor, percentCompleteFormat, PercentSuccessful);
            else
                FancyConsole.Write(FancyConsole.ConsoleWarningColor, percentCompleteFormat, PercentSuccessful);

            if (FailureCount > 0 || WarningCount > 0)
            {
                FancyConsole.Write(" (");
                if (FailureCount > 0)
                    FancyConsole.Write(FancyConsole.ConsoleErrorColor, "{0} errors", FailureCount);
                if (WarningCount > 0 && FailureCount > 0)
                    FancyConsole.Write(", ");
                if (WarningCount > 0)
                    FancyConsole.Write(FancyConsole.ConsoleWarningColor, "{0} warnings", WarningCount);
                if (WarningCount > 0 || FailureCount > 0 && SuccessCount > 0)
                    FancyConsole.Write(", ");
                if (SuccessCount > 0)
                    FancyConsole.Write(FancyConsole.ConsoleSuccessColor, "{0} successful", SuccessCount);
                FancyConsole.Write(")");
            }
            FancyConsole.WriteLine();
        }
    }
}
