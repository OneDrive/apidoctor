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
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using ApiDocs.ConsoleApp.AppVeyor;
    using ApiDocs.Validation;

    public static class TestReport
    {
        private const string TestFrameworkName = "apidocs";
        private static BuildWorkerApi BuildWorkerApi { get { return Program.BuildWorker; } }
        private static readonly Dictionary<string, long> TestStartTimes = new Dictionary<string, long>();
        private static readonly Dictionary<string, string> TestStartFilename = new Dictionary<string, string>();

        public static void StartTest(string testName, string filename = null, bool skipPrintingHeader = false)
        {
            if (!skipPrintingHeader)
            {
                FancyConsole.WriteLine();
                FancyConsole.Write(FancyConsole.ConsoleHeaderColor, "Starting test: ");
                FancyConsole.Write(FancyConsole.ConsoleDefaultColor, testName);
            }

            if (null != filename)
            {
                if (!skipPrintingHeader)
                    FancyConsole.Write(" [{0}]", filename);
                TestStartFilename[testName] = filename;
            }
            if (!skipPrintingHeader)
                FancyConsole.WriteLine();

            TestStartTimes[testName] = DateTimeOffset.Now.Ticks;
        }


        public static async Task FinishTestAsync(string testName, TestOutcome outcome, string message = null, string filename = null, string stdOut = null, bool printFailuresOnly = false)
        {
            var endTime = DateTimeOffset.Now.Ticks;

            TimeSpan duration;
            long startTime;
            if (TestStartTimes.TryGetValue(testName, out startTime))
            {
                duration = new TimeSpan(endTime - startTime);
                TestStartTimes.Remove(testName);
            }
            else
            {
                duration = TimeSpan.Zero;
            }

            if (null == filename)
            {
                TestStartFilename.TryGetValue(testName, out filename);
                TestStartFilename.Remove(testName);
            }

            if (!printFailuresOnly || outcome != TestOutcome.Passed)
            {
                FancyConsole.Write("Test {0} complete.", testName);
                switch (outcome)
                {
                    case TestOutcome.Failed:
                        FancyConsole.Write(ConsoleColor.Red, " Failed: {0}", message);
                        break;
                    case TestOutcome.Passed:
                        FancyConsole.Write(ConsoleColor.Green, " Passed: {0}", message);
                        break;
                    default:
                        FancyConsole.Write(" {0}: {1}", outcome, message);
                        break;
                }
                FancyConsole.WriteLine(" duration: {0}", duration);
            }

            await BuildWorkerApi.RecordTestAsync(testName, TestFrameworkName, outcome: outcome, durationInMilliseconds: (long)duration.TotalMilliseconds, errorMessage: message, filename: filename, stdOut: stdOut);
        }


        internal static async Task LogMessageAsync(string message, MessageCategory category = MessageCategory.Information, string details = null)
        {
            await BuildWorkerApi.AddMessageAsync(message, category, details);
        }

        internal static async Task LogMethodTestResults(Validation.MethodDefinition method, IServiceAccount account, Validation.ValidationResults results)
        {
            foreach (var scenario in results.Results)
            {
                if (scenario.Outcome == ValidationOutcome.None)
                    continue;

                StringBuilder stdout = new StringBuilder();

                string message = null;
                if (scenario.Errors.Count > 0)
                {
                    stdout.AppendFormat("Scenario: {0}", scenario.Name);
                    foreach (var error in scenario.Errors)
                    {
                        stdout.AppendLine(error.ErrorText);

                        if (error.IsError && message == null)
                        {
                            message = error.ErrorText.FirstLineOnly();
                        }
                    }

                    stdout.AppendFormat(
                        "Scenario finished with outcome: {0}. Duration: {1}",
                        scenario.Outcome,
                        scenario.Duration);
                }

                string testName = string.Format(
                    "{0}: {1} [{2}]",
                    method.Identifier,
                    scenario.Name,
                    account.Name);
                
                string filename = method.SourceFile.DisplayName;
                TestOutcome outcome = ToTestOutcome(scenario.Outcome);

                await BuildWorkerApi.RecordTestAsync(
                        testName,
                        TestFrameworkName,
                        outcome: outcome,
                        durationInMilliseconds: (long)scenario.Duration.TotalMilliseconds,
                        errorMessage: message ?? scenario.Outcome.ToString(),
                        filename: filename,
                        stdOut: stdout.ToString());
            }
        }

        private static TestOutcome ToTestOutcome(ValidationOutcome outcome)
        {
            if ((outcome & ValidationOutcome.Error) > 0)
                return TestOutcome.Failed;
            if ((outcome & ValidationOutcome.Skipped) > 0)
                return TestOutcome.Skipped;
            if ((outcome & ValidationOutcome.Passed) > 0)
                return TestOutcome.Passed;

            return TestOutcome.Inconclusive;
        }
    }

}
