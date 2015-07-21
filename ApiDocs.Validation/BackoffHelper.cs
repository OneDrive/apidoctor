using System;
using System.Threading.Tasks;

namespace ApiDocs.Validation
{
    public class BackoffHelper
    {
        public int MaximumTimeMilliseconds { get; set; }

        public int BaseTimeMilliseconds { get; set; }

        private readonly Random random = new Random();

        public BackoffHelper()
        {
            this.MaximumTimeMilliseconds = 5000;
            this.BaseTimeMilliseconds = 100;
        }

        /// <summary>
        /// Implements the full jitter backoff algorithm as defined here: http://www.awsarchitectureblog.com/2015/03/backoff.html
        /// </summary>
        /// <returns>A task that completes after the duration of the delay.</returns>
        /// <param name="errorCount">The number of times an error has occured for this particular command / session.</param>
        public async Task FullJitterBackoffDelayAsync(int errorCount)
        {
            double expectedBackoffTime = Math.Min(this.MaximumTimeMilliseconds, this.BaseTimeMilliseconds * Math.Pow(2 , errorCount));

            var sleepDuration = Between(this.random, 0, (int)expectedBackoffTime);

            System.Diagnostics.Debug.WriteLine("Waiting for: {0} milliseconds", sleepDuration);
            await Task.Delay(sleepDuration);
        }

        private static int Between(Random rnd, int lowNumber, int highNumber)
        {
            var dbl = rnd.NextDouble();
            int range = highNumber - lowNumber;
            double selectedValue = (dbl * range);
            return (int)(lowNumber + selectedValue);
        }


        private static BackoffHelper defaultInstance;
        public static BackoffHelper Default
        {
            get { return defaultInstance ?? (defaultInstance = new BackoffHelper()); }
        }
    }
}

