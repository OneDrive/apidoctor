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

namespace ApiDocs.Validation
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public class BackoffHelper
    {
        public int MaximumTimeMilliseconds { get; set; }

        public int BaseTimeMilliseconds { get; set; }

        private readonly Random random = new Random();

        public BackoffHelper()
        {
            this.MaximumTimeMilliseconds = 30000;
            this.BaseTimeMilliseconds = 1000;
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

            Debug.WriteLine("Waiting for: {0} milliseconds", sleepDuration);
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

