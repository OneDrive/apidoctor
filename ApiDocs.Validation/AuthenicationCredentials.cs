namespace ApiDocs.Validation
{
    using System;

    /// <summary>
    /// Authenication Credentials for interacting with the service
    /// </summary>
    public abstract class AuthenicationCredentials
    {
        public abstract string AuthenicationToken { get; internal set; }
        public string FirstPartyApplicationHeaderValue { get; protected set; }

        public static AuthenicationCredentials CreateAutoCredentials(string authenicationToken)
        {
            if (String.IsNullOrEmpty(authenicationToken)) { return CreateNoCredentials(); }
            if (authenicationToken.StartsWith("t="))
            {
                return CreateFirstPartyCredentials(authenicationToken);
            }

            return CreateBearerCredentials(authenicationToken);
        }

        public static AuthenicationCredentials CreateBearerCredentials(string authenicationToken)
        {
            if (String.IsNullOrEmpty(authenicationToken)) { return CreateNoCredentials(); }
            return new BearerCredentials { AuthenicationToken = "Bearer " + authenicationToken };
        }

        public static AuthenicationCredentials CreateFirstPartyCredentials(string authenicationToken)
        {
            if (String.IsNullOrEmpty(authenicationToken)) { return CreateNoCredentials(); }
            return new FirstPartyCredentials { AuthenicationToken = "WLID1.1 " + authenicationToken };
        }

        public static AuthenicationCredentials CreateNoCredentials()
        {
            return new NoCredentials();
        }
    }

    public class BearerCredentials : AuthenicationCredentials
    {
        internal BearerCredentials() { }

        public override string AuthenicationToken { get; internal set; }
    }

    public class FirstPartyCredentials : AuthenicationCredentials
    {
        internal FirstPartyCredentials()
        {
            this.FirstPartyApplicationHeaderValue = "SaveToOneDriveWidget";
        }

        public override string AuthenicationToken { get; internal set; }
    }

    public class NoCredentials : AuthenicationCredentials
    {
        internal NoCredentials() { }

        public override string AuthenicationToken { get { return null; } internal set { } }
    }
}
