namespace ApiDocs.Validation
{
    using System;

    [Serializable]
    public class SchemaBuildException : Exception
    {
        public SchemaBuildException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
