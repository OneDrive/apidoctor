using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.ConsoleApp
{
    public static class TestReport
    {
        private const string TestFrameworkName = "apidocs";
        private static AppVeyor.BuildWorkerApi BuildWorkerApi { get { return Program.BuildWorker; } }
        private static Dictionary<string, long> TestStartTimes = new Dictionary<string, long>();
        private static Dictionary<string, string> TestStartFilename = new Dictionary<string, string>();

        public static void StartTestAsync(string testName, string filename = null)
        {
            FancyConsole.WriteLine();
            FancyConsole.Write(FancyConsole.ConsoleHeaderColor, "Starting test: ");
            FancyConsole.Write(FancyConsole.ConsoleDefaultColor, testName);

            if (null != filename)
            {
                FancyConsole.Write(" [{0}]", filename);
                TestStartFilename[testName] = filename;
            }
            FancyConsole.WriteLine();

            TestStartTimes[testName] = DateTimeOffset.Now.Ticks;
        }


        public static async Task FinishTestAsync(string testName, AppVeyor.TestOutcome outcome, string message = null, string filename = null, string stdOut = null)
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

            FancyConsole.Write("Test {0} complete.", testName);

            switch (outcome)
            {
                case AppVeyor.TestOutcome.Failed:
                    FancyConsole.Write(ConsoleColor.Red, " Failed: {0}", message);
                    break;
                case AppVeyor.TestOutcome.Passed:
                    FancyConsole.Write(ConsoleColor.Green, " Passed: {0}", message);
                    break;
                default:
                    FancyConsole.Write(" {0}: {1}", outcome, message);
                    break;
            }

            FancyConsole.WriteLine(" duration: {0}", duration);

            await BuildWorkerApi.RecordTestAsync(testName, TestFrameworkName, outcome: outcome, durationInMilliseconds: (long)duration.TotalMilliseconds, errorMessage: message, filename: filename, stdOut: stdOut);
        }


        internal static async Task LogMessageAsync(string message, AppVeyor.MessageCategory category = AppVeyor.MessageCategory.Information, string details = null)
        {
            await BuildWorkerApi.AddMessageAsync(message, category, details);
        }
    }
}
