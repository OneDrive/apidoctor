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

namespace ApiDoctor.Validation
{
    using System;
    using ApiDoctor.Validation.Error;

    /// <summary>
    /// Provides an accessible Logger for use throughout the library and callers to the library
    /// </summary>
    public static class Logging
    {
        private static ILogHelper StaticLogHelper { get; set; }

        public static void ProviderLogger(ILogHelper helper)
        {
            StaticLogHelper = helper;
        }

        private static ILogHelper GetLogger()
        {
            if (null == StaticLogHelper)
            {
                StaticLogHelper = new ConsoleLogHelper();
            }

            // Make sure we always return something, evne if StaticLogHelper ended up being null due to a race condition.
            return StaticLogHelper ?? new ConsoleLogHelper();
        }

        public static void LogMessage(ValidationError error)
        {
            var helper = GetLogger();
            helper.RecordError(error);
        }
    }

    public interface ILogHelper
    {
        void RecordError(ValidationError error);

    }

    internal class ConsoleLogHelper : ILogHelper
    {
        public void RecordError(ValidationError error)
        {
            if (null != error)
                System.Diagnostics.Debug.WriteLine(error.ErrorText);
        }
    }
}
