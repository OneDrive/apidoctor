using System.Collections.Generic;

namespace ApiDoctor.ConsoleApp
{
    public static class Constants
    {
        public static class PermissionConstants
        {
            public const string DefaultBoilerPlateText = "Choose the permission or permissions marked as least privileged for this API." +
                    " Use a higher privileged permission or permissions [only if your app requires it](/graph/permissions-overview#best-practices-for-using-microsoft-graph-permissions)." +
                    " For details about delegated and application permissions, see [Permission types](/graph/permissions-overview#permission-types). To learn more about these permissions, see the [permissions reference](/graph/permissions-reference).";
            public const string MultipleTableBoilerPlateText = "The following tables show the least privileged permission or permissions required to call this API on each supported resource type." +
                    " Follow [best practices](/graph/permissions-overview#best-practices-for-using-microsoft-graph-permissions) to request least privileged permissions." +
                    " For details about delegated and application permissions, see [Permission types](/graph/permissions-overview#permission-types). To learn more about these permissions, see the [permissions reference](/graph/permissions-reference).";
        }

    }
}
