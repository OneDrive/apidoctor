using System;
using System.Collections.Generic;
using System.Linq;

namespace Kibali;

public class PermissionsStubGenerator
{
    private readonly PermissionsDocument document;
    private readonly string path;
    private readonly string method;
    private readonly bool generateDefault;
    private readonly bool lenientMatch;
    public PermissionsStubGenerator(PermissionsDocument document, string path, string method, bool generateDefault = false, bool lenientMatch = false)
    {
        this.document = document;
        this.path = path;
        this.method = method;
        this.generateDefault = generateDefault;
        this.lenientMatch = lenientMatch;
    }
    
    public PermissionsDocument Document { get; set; }

    public string Path { get; set; }

    public string Method { get; set; }

    public bool LenientMatch { get; set; }
    
    public bool MergeMultiplePaths { get; set; }

    public string GenerateTable()
    {
        if (this.MergeMultiplePaths)
        {
            return GenerateMultiplePathsTable();
        }
        return GenerateSinglePathTable();
        
    }

    private string GenerateSinglePathTable()
    {
        var authZChecker = new AuthZChecker() { LenientMatch = this.lenientMatch };
        authZChecker.Load(this.document);

        var resource = authZChecker.FindResource(this.path);
        var table = this.generateDefault ? this.UnsupportedPermissionsStub() : string.Empty;
        if (resource == null)
        {
            return table;
        }

        if (!string.IsNullOrEmpty(this.method) && resource.SupportedMethods.TryGetValue(this.method, out var supportedSchemes))
        {
            table = resource.GeneratePermissionsTable(this.method, supportedSchemes);
        }
        return table;
    }

    public string GenerateMultiplePathsTable()
    {
        var resources = new Dictionary<string, ProtectedResource>();
        var authZChecker = new AuthZChecker() { LenientMatch = this.lenientMatch };
        authZChecker.Load(this.document);
        foreach (var path in this.path.Split(';'))
        {
            var resource = authZChecker.FindResource(path);
            if (resource == null)
            {
                continue;
            }
            resources[path] = resource;
        } 
        
        var table = this.generateDefault ? this.UnsupportedPermissionsStub() : string.Empty;
        if (resources.Count == 0)
        {
            return table;
        }

        var mergedTableScopes = new Dictionary<string, Dictionary<string,SortedSet<string>>>();
       
        foreach (var resourceEntry in resources)
        {
            var resource = resourceEntry.Value;
            if (!string.IsNullOrEmpty(this.method) && resource.SupportedMethods.TryGetValue(this.method, out var supportedSchemes))
            {
                var scopesByScheme = resource.FetchTableScopesByScheme(this.method, supportedSchemes);
                MergeTableScopes(scopesByScheme, mergedTableScopes);
            }
        }
        if (mergedTableScopes.Count == 0)
        {
            return table;
        }

        var groupedScopesByScheme = GroupAndCleanScopes(mergedTableScopes);
        table = TableGenerator.GeneratePermissionsTable(groupedScopesByScheme);
        return table;
    }

    void MergeTableScopes(Dictionary<string, (string, string)> scopesByName, Dictionary<string, Dictionary<string, SortedSet<string>>> mergedTableScopes)
    {
        foreach (var scheme in scopesByName)
        {
            var least = scheme.Value.Item1;
            var higher = scheme.Value.Item2.Split(',').Select(k => k.Trim());
            if (mergedTableScopes.TryGetValue(scheme.Key, out var mergedScopes))
            {
                mergedScopes["least"].Add(least);
                mergedScopes["higher"].UnionWith(higher);
            }
            else
            {
                mergedTableScopes[scheme.Key] = new() { { "least", new() { least } }, { "higher", new SortedSet<string>(higher) } };
            }
        }
    }

    Dictionary<string, (string, string)> GroupAndCleanScopes(Dictionary<string, Dictionary<string, SortedSet<string>>> mergedTableScopes)
    {
        var notPresent = new[] { StringConstants.PermissionNotSupported, StringConstants.PermissionNotAvailable };
        var groupedSchemePermissions = new Dictionary<string, (string, string)>();
        foreach (var (scheme, value) in mergedTableScopes)
        {
            var least = value["least"];
            string mergedLeast;
            if (least.Count == 1)
            {
                mergedLeast = least.First();
            }
            else
            {
                var filtered = least.Except(notPresent);
                if (filtered.Any())
                {
                    if (filtered.Count() > 1)
                    {
                        throw new ArgumentException($"Differing least privilege permissions {string.Join(",", filtered)} for the scheme {scheme}.");
                    }
                    else
                    {
                        mergedLeast = filtered.First();
                    }
                }
                else
                {
                    mergedLeast = StringConstants.PermissionNotSupported;
                }
            }

            var higher = value["higher"];
            string mergedHigher;
            if (higher.Count == 1)
            {
                var higherPerm = higher.First();
                mergedHigher = (higherPerm == mergedLeast && higherPerm != StringConstants.PermissionNotSupported) ? StringConstants.PermissionNotAvailable : higherPerm;
            }
            else
            {
                least.Remove(mergedLeast); // Remove least privileged permission and add the rest to the higher privileged permissions.
                var toSkip = notPresent.Append(mergedLeast);
                var filtered = higher.Concat(least).Except(toSkip);
                if (filtered.Any())
                {
                    mergedHigher = string.Join(", ", filtered);
                }
                else
                {
                    mergedHigher = mergedLeast != StringConstants.PermissionNotSupported ? StringConstants.PermissionNotAvailable : StringConstants.PermissionNotSupported;
                }
            }

            groupedSchemePermissions[scheme] = (mergedLeast, mergedHigher);
        }
        return groupedSchemePermissions;
    }



    private string UnsupportedPermissionsStub(){
        var permissionsStub = StringConstants.PermissionNotSupported;
        var markdownBuilder = new MarkDownBuilder();
        markdownBuilder.StartTable("Permission type", "Least privileged permissions", "Higher privileged permissions");

        markdownBuilder.AddTableRow("Delegated (work or school account)", permissionsStub, permissionsStub);

        markdownBuilder.AddTableRow("Delegated (personal Microsoft account)", permissionsStub, permissionsStub);

        markdownBuilder.AddTableRow("Application", permissionsStub, permissionsStub);
        markdownBuilder.EndTable();
        return markdownBuilder.ToString();
    }
}
