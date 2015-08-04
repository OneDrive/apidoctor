namespace ApiDocs.Validation
{
    using System;
    using ApiDocs.Validation.Error;

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
