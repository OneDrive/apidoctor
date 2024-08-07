using System;
using System.Collections.Generic;

namespace Kibali;

public static class TableGenerator
{
    public static string GeneratePermissionsTable(Dictionary<string, (string, string)> scopesByScheme)
    {
        var allRowsAreValid = true;
        var markdownBuilder = new MarkDownBuilder();
        markdownBuilder.StartTable("Permission type", "Least privileged permissions", "Higher privileged permissions");

        (var least, var higher) = scopesByScheme["DelegatedWork"];
        markdownBuilder.AddTableRow("Delegated (work or school account)", least, higher);
        allRowsAreValid &= TableRowIsValid(least, higher);

        (least, higher) = scopesByScheme["DelegatedPersonal"];
        markdownBuilder.AddTableRow("Delegated (personal Microsoft account)", least, higher);
        allRowsAreValid &= TableRowIsValid(least, higher);

        (least, higher) = scopesByScheme["Application"];
        markdownBuilder.AddTableRow("Application", least, higher);
        allRowsAreValid &= TableRowIsValid(least, higher);

        markdownBuilder.EndTable();
        return allRowsAreValid ? markdownBuilder.ToString() : string.Empty;
    }

    private static bool TableRowIsValid(string least, string higher)
    {
        // Row is invalid if we don't have least privilege permissions but have higher privileged permissions.
        if (least.Equals(StringConstants.PermissionNotSupported, StringComparison.OrdinalIgnoreCase) && (!higher.Equals(StringConstants.PermissionNotSupported, StringComparison.OrdinalIgnoreCase) && !higher.Equals(StringConstants.PermissionNotAvailable, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }
        return true;
    }
}
