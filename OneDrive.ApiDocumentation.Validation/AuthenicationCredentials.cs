using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation
{
    /// <summary>
    /// Authenication Credentials for interacting with the service
    /// </summary>
    public abstract class AuthenicationCredentials
    {
        public abstract string AuthenicationToken { get; internal set; }

        public static BearerCredentials CreateBearerCredentials(string authenicationToken)
        {
            return new BearerCredentials { AuthenicationToken = "Bearer " + authenicationToken };
        }

        public static WLIDCredentials CreateWLIDCredentials(string authenicationToken) {
            return new WLIDCredentials { AuthenicationToken = "WLID 1.0 " + authenicationToken };
        }

        public static NoCredentials CreateNoCredentials()
        {
            return new NoCredentials();
        }
    }

    public class BearerCredentials : AuthenicationCredentials
    {
        internal BearerCredentials() { }

        public override string AuthenicationToken { get; internal set; }
    }

    public class WLIDCredentials : AuthenicationCredentials
    {
        internal WLIDCredentials() { }

        public override string AuthenicationToken { get; internal set; }
    }

    public class NoCredentials : AuthenicationCredentials
    {
        internal NoCredentials() { }

        public override string AuthenicationToken { get { return null; } internal set { } }
    }
}
