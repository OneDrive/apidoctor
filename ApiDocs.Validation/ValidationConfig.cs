namespace ApiDocs.Validation
{
    public static class ValidationConfig
    {
        static ValidationConfig()
        {
            ExpectedResponseAsRequiredProperties = true;
            AdditionalHttpHeaders = new string[0];
            RetryAttemptsOnServiceUnavailableResponse = 4; // Try 1 + 4 retries = 5 total attempts
            MaximumBackoffMilliseconds = 5000;
            BaseBackoffMilliseconds = 100;
        }

        /// <summary>
        /// Validatation requires that properties shown in the documentation's expected response are
        /// found when testing the service or simulatedResponse.
        /// </summary>
        public static bool ExpectedResponseAsRequiredProperties { get; set; }

        /// <summary>
        /// Instead of using the default OData metadata settings, force the odata metadata parameters to none.
        /// </summary>
        public static string ODataMetadataLevel { get; set; }

        /// <summary>
        /// An array of additional HTTP headers that are added to outgoing requests to the service.
        /// </summary>
        public static string[] AdditionalHttpHeaders { get; set; }

        public static int RetryAttemptsOnServiceUnavailableResponse { get; set; }

        public static int MaximumBackoffMilliseconds { get; set; }

        public static int BaseBackoffMilliseconds { get; set; }

    }
}
