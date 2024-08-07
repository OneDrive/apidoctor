using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Kibali
{
    public class ProtectedResource
    {
        // Permission -> (Methods,Scheme) -> Path  (Darrel's format)
        // (Schemes -> Permissions) -> restriction -> target  (Kanchan's format)
        // target -> restrictions -> schemes -> Ordered Permissions (CSDL Format) 

        // path -> Method -> Schemes -> Permissions  (Inverted format) 

        // (Path, Method) -> Schemes -> Permissions (Docs)
        // (Path, Method) -> Scheme(delegated) -> Permissions (Graph Explorer Tab)
        // Permissions(delegated) (Graph Explorer Permissions List)
        // Schemas -> Permissions ( AAD Onboarding)
        public string Url { get; set; }
        public Dictionary<string, Dictionary<string, List<AcceptableClaim>>> SupportedMethods { get; set; } = new Dictionary<string, Dictionary<string, List<AcceptableClaim>>>();

        public Dictionary<(string, string), HashSet<string>> PermissionMethods {get; set;} = new();
        public ProtectedResource(string url)
        {
            Url = url;
        }

        public void AddRequiredClaims(string permission, PathSet pathSet, string[] leastPrivilegedPermissionSchemes, List<ProvisioningInfo> provisioningData, string[] alsoRequires)
        {
            Dictionary<string, ProvisioningInfo> schemeProvisioning = provisioningData?.ToDictionary(info => info.Scheme, info => info) ?? new();
            foreach (var supportedMethod in pathSet.Methods)
            {
                var supportedSchemes = new Dictionary<string, List<AcceptableClaim>>();
                foreach (var schemeKey in pathSet.SchemeKeys)
                {
                    if(!supportedSchemes.TryGetValue(schemeKey, out var acceptableClaims))
                    {
                        acceptableClaims = new List<AcceptableClaim>();
                        supportedSchemes.Add(schemeKey, acceptableClaims);
                    }

                    var permissionMethodKey = (permission, schemeKey);
                    if (!this.PermissionMethods.TryAdd(permissionMethodKey, new HashSet<string> { supportedMethod }))
                    {
                        this.PermissionMethods[permissionMethodKey].Add(supportedMethod);
                    }

                    var isLeastPrivilege = leastPrivilegedPermissionSchemes.Contains(schemeKey);
                    var claim = new AcceptableClaim(permission, isLeastPrivilege, alsoRequires);
                    if (schemeProvisioning.TryGetValue(schemeKey, out ProvisioningInfo provisioningInfo))
                    {
                        claim.IsHidden = provisioningInfo.IsHidden;
                        claim.SupportedEnvironments = provisioningInfo.Environment?.Split(";").ToList();
                        claim.IsEnabled = provisioningInfo.IsEnabled;
                    }
                    acceptableClaims.Add(claim);
                }

                if (!this.SupportedMethods.TryGetValue(supportedMethod, out var existingSupportedSchemes))
                {
                    this.SupportedMethods.Add(supportedMethod, supportedSchemes);
                }
                else
                {
                    Update(existingSupportedSchemes, supportedSchemes);
                }
            }
        }

        public IEnumerable<PermissionsError> ValidateLeastPrivilegePermissions()
        {
            var duplicateErrors = new HashSet<PermissionsError> ();
            var privs = this.FetchLeastPrivilege();
            foreach (var methodPrivs in privs)
            {
                var method = methodPrivs.Key;
                foreach (var schemePrivs in methodPrivs.Value)
                {
                    var scheme = schemePrivs.Key;
                    if (schemePrivs.Value.Count > 1)
                    {
                        duplicateErrors.Add(new PermissionsError
                        {
                            Path = this.Url,
                            ErrorCode = PermissionsErrorCode.DuplicateLeastPrivilegeScopes,
                            Message = string.Format(StringConstants.DuplicateLeastPrivilegeSchemeErrorMessage, string.Join(", ", schemePrivs.Value), scheme, method),
                        });
                    }
                }
            }
            
            return duplicateErrors;
        }

        
        public HashSet<PermissionsError> ValidateMismatchedSchemes(string permission, PathSet pathSet, IEnumerable<string> leastPrivilegePermissionSchemes)
        {
            var mismatchedPrivilegeSchemes = leastPrivilegePermissionSchemes.Except(pathSet.SchemeKeys);
            var errors = new HashSet<PermissionsError>();
            if (mismatchedPrivilegeSchemes.Any())
            {
                var invalidSchemes = string.Join(", ", mismatchedPrivilegeSchemes);
                var expectedSchemes = string.Join(", ", pathSet.SchemeKeys);
                errors.Add(new PermissionsError
                {
                    Path = this.Url,
                    ErrorCode = PermissionsErrorCode.InvalidLeastPrivilegeScheme,
                    Message = string.Format(StringConstants.UnexpectedLeastPrivilegeSchemeErrorMessage, invalidSchemes, permission, expectedSchemes),
                });
            }
            return errors;
        }

        private void Update(Dictionary<string, List<AcceptableClaim>> existingSchemes, Dictionary<string, List<AcceptableClaim>> newSchemes)
        {
            
            foreach(var newScheme in newSchemes)
            {
                if (existingSchemes.TryGetValue(newScheme.Key, out var existingScheme))
                {
                    existingScheme.AddRange(newScheme.Value);
                } 
                else
                {
                    existingSchemes[newScheme.Key] = newScheme.Value;
                }
            }
        }

        public void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("url");
            writer.WriteStringValue(Url);
            writer.WritePropertyName("methods");
            WriteSupportedMethod(writer, this.SupportedMethods);
            
            writer.WriteEndObject();
        }

        private void WriteSupportedMethod(Utf8JsonWriter writer, Dictionary<string, Dictionary<string, List<AcceptableClaim>>> supportedMethods)
        {
            writer.WriteStartObject();
            foreach (var item in supportedMethods)
            {
                writer.WritePropertyName(item.Key);
                WriteSupportedSchemes(writer, item.Value);
            }
            writer.WriteEndObject();
        }

        public void WriteSupportedSchemes(Utf8JsonWriter writer, Dictionary<string, List<AcceptableClaim>> methodClaims)
        {
            writer.WriteStartObject();
            foreach (var item in methodClaims)
            {
                writer.WritePropertyName(item.Key);
                WriteAcceptableClaims(writer, item.Value);
            }
            writer.WriteEndObject();
        }

        public void WriteAcceptableClaims(Utf8JsonWriter writer, List<AcceptableClaim> schemes)
        {
            writer.WriteStartArray();
            foreach (var item in schemes.OrderByDescending(c => c.Least))
            {
                item.Write(writer);
            }
            writer.WriteEndArray();
        }

        public string GeneratePermissionsTable(string method, Dictionary<string, List<AcceptableClaim>> methodClaims)
        {
            var scopesByScheme = FetchTableScopesByScheme(method, methodClaims);

            return TableGenerator.GeneratePermissionsTable(scopesByScheme);
        }

        public Dictionary<string, (string, string)> FetchTableScopesByScheme(string method, Dictionary<string, List<AcceptableClaim>> methodClaims)
        {
            var scopesByScheme = new Dictionary<string, (string, string)>();
            var leastPrivilege = this.FetchLeastPrivilege(method);
            var schemeKeys = new[] { "DelegatedWork", "Application", "DelegatedPersonal" };
            foreach (var scheme in schemeKeys)
            {
                scopesByScheme[scheme] = GetTableScopes(scheme, methodClaims, leastPrivilege[method]);
            }
            return scopesByScheme;
        }

        public Dictionary<string, Dictionary<string, HashSet<string>>> FetchLeastPrivilege(string method = null, string scheme = null)
        {
            var leastPrivilege = new Dictionary<string, Dictionary<string, HashSet<string>>>();
            if (method != null && scheme != null)
            {
                GetLeastPrivilegeForSchemeAndMethod(method, scheme, leastPrivilege);
            }
            if (method != null && scheme == null)
            {
                this.SupportedMethods.TryGetValue(method, out var supportedSchemes);
                if (supportedSchemes == null)
                {
                    return leastPrivilege;
                }
                GetLeastPrivilegeForAllSchemesMappedToMethod(method, leastPrivilege, supportedSchemes);
            }
            if (method == null && scheme != null)
            {
                GetLeastPrivilegeForAllMethodsMappedToScheme(scheme, leastPrivilege);
            }
            if (method == null && scheme == null)
            {
                GetLeastPrivilegeForAllMethodsAndSchemes(leastPrivilege);
            }
            return leastPrivilege;
        }

        private void GetLeastPrivilegeForAllMethodsAndSchemes(Dictionary<string, Dictionary<string, HashSet<string>>> leastPrivilege)
        {
            foreach (var supportedMethod in this.SupportedMethods.OrderBy(s => s.Key))
            {
                foreach (var supportedScheme in supportedMethod.Value.OrderBy(s => Enum.Parse(typeof(SchemeType), s.Key)))
                {
                    leastPrivilege.TryAdd(supportedMethod.Key, new Dictionary<string, HashSet<string>>());
                    var permissions = supportedScheme.Value.Where(p => p.Least).ToHashSet();
                    PopulateLeastPrivilege(leastPrivilege, supportedMethod.Key, supportedScheme.Key, permissions);
                }
            }
        }

        private void GetLeastPrivilegeForAllMethodsMappedToScheme(string scheme, Dictionary<string, Dictionary<string, HashSet<string>>> leastPrivilege)
        {
            foreach (var supportedMethod in this.SupportedMethods.OrderBy(s => s.Key))
            {
                supportedMethod.Value.TryGetValue(scheme, out var supportedSchemeClaims);
                if (supportedSchemeClaims == null)
                {
                    continue;
                }
                leastPrivilege.TryAdd(supportedMethod.Key, new Dictionary<string, HashSet<string>>());
                var permissions = supportedSchemeClaims.Where(p => p.Least).ToHashSet();
                PopulateLeastPrivilege(leastPrivilege, supportedMethod.Key, scheme, permissions);
            }
        }

        private void GetLeastPrivilegeForAllSchemesMappedToMethod(string method, Dictionary<string, Dictionary<string, HashSet<string>>> leastPrivilege, Dictionary<string, List<AcceptableClaim>> supportedSchemes)
        {
            foreach (var supportedScheme in supportedSchemes.OrderBy(s => Enum.Parse(typeof(SchemeType), s.Key)))
            {
                leastPrivilege.TryAdd(method, new Dictionary<string, HashSet<string>>());
                var permissions = supportedScheme.Value.Where(p => p.Least).ToHashSet();
                PopulateLeastPrivilege(leastPrivilege, method, supportedScheme.Key, permissions);
            }
        }

        private void GetLeastPrivilegeForSchemeAndMethod(string method, string scheme, Dictionary<string, Dictionary<string, HashSet<string>>> leastPrivilege)
        {
            leastPrivilege.TryAdd(method, new Dictionary<string, HashSet<string>>());
            var permissions = this.SupportedMethods[method][scheme].Where(p => p.Least).ToHashSet();
            PopulateLeastPrivilege(leastPrivilege, method, scheme, permissions);
        }

        public string WriteLeastPrivilegeTable(Dictionary<string, Dictionary<string, HashSet<string>>> leastPrivilege)
        {
            string output;
            var builder = new StringBuilder();
            foreach (var methodEntry in leastPrivilege)
            {
                builder.AppendLine();
                builder.AppendLine(methodEntry.Key);
                foreach (var schemeEntry in methodEntry.Value)
                {
                    builder.AppendLine($"|{schemeEntry.Key} |{string.Join(";", schemeEntry.Value)}|");
                    builder.AppendLine();
                }
                builder.AppendLine();
            }
            output = builder.ToString();
            return output;
        }
 
        private (string least, string higher) GetTableScopes(string scheme, Dictionary<string, List<AcceptableClaim>> methodClaims, Dictionary<string, HashSet<string>> leastPrivilegeScopesPerScheme)
        {
            IEnumerable<AcceptableClaim> orderedClaims = Enumerable.Empty<AcceptableClaim>();
            var schemeScopes = new List<string>();
            if (methodClaims.TryGetValue(scheme, out List<AcceptableClaim> claims))
            {
                orderedClaims = claims.OrderByDescending(c => c.Least);
                schemeScopes = orderedClaims.Select(c => c.Permission).ToList();
            }
            if (leastPrivilegeScopesPerScheme.TryGetValue(scheme, out HashSet<string> leastPrivilegeScopes))
            {
                (var least, var higherScopes) = ExtractScopes(schemeScopes, leastPrivilegeScopes);
                string higher = ProcessMultipleRequiredPermissions(orderedClaims, ref higherScopes);
                return (least, higher);
            }
            
            return (StringConstants.PermissionNotSupported, schemeScopes.Any() ? string.Join(", ", schemeScopes) : StringConstants.PermissionNotSupported);
        }

        private string ProcessMultipleRequiredPermissions(IEnumerable<AcceptableClaim> orderedClaims, ref IEnumerable<string> higherScopes)
        {
            var permissionPairs = new HashSet<(string, string)>(new OrderedPairEqualityComparer(StringComparer.OrdinalIgnoreCase));
            foreach (var claim in orderedClaims)
            {
                PairPermissions(claim, permissionPairs);
            }

            var leastPrivilegedPermissionPair = GetRequiredLeastPrivileged(orderedClaims);
            var invalidHigherPrivilegedEntries = new HashSet<(string, string)>();
            // The least privileged permission requires another permission
            if (leastPrivilegedPermissionPair != null && leastPrivilegedPermissionPair.Any())
            {
                // Since the least privileged permission requires another permission, we can't have either of those permissions or the least privileged pair as higher privileged permissions.
                invalidHigherPrivilegedEntries = new()
                    {
                        (leastPrivilegedPermissionPair.First(), leastPrivilegedPermissionPair.Last()),
                        (leastPrivilegedPermissionPair.Last(), leastPrivilegedPermissionPair.First()),
                        (leastPrivilegedPermissionPair.First(), string.Empty),
                        (leastPrivilegedPermissionPair.Last(), string.Empty),
                        (string.Empty, leastPrivilegedPermissionPair.First()),
                        (string.Empty, leastPrivilegedPermissionPair.Last()),
                    };

                // Filter out least privilege entries from the permission pairs to process
                permissionPairs = permissionPairs.Where(p => !invalidHigherPrivilegedEntries.Contains(p)).ToHashSet();

                // Filter out least privilege entries from the higher privileged permissions to process
                higherScopes = higherScopes.Where(p => !invalidHigherPrivilegedEntries.Contains((p, string.Empty)));
            }

            var higher = StringConstants.PermissionNotAvailable;
            if (permissionPairs.Any())
            {
                ProcessPermissionPairs(permissionPairs, leastPrivilegedPermissionPair, higherScopes, ref higher);
            }
            else
            {
                higher = higherScopes.Any() ? string.Join(", ", higherScopes) : leastPrivilegedPermissionPair.Any() ? StringConstants.PermissionNotAvailable : StringConstants.PermissionNotSupported;
            }

            return higher;
        }

        private IEnumerable<string> GetRequiredLeastPrivileged(IEnumerable<AcceptableClaim> claims)
        {
            var leastPrivilegedClaims = claims?.Where(c => c.Least && c.AlsoRequires.Length > 0);
            if (leastPrivilegedClaims?.Count() > 2)
            {
                throw new InvalidOperationException($"Too many Least Privilege Entries {string.Join(",", leastPrivilegedClaims.Select(c => c.Permission))}");
            }
            if (leastPrivilegedClaims?.Count() == 1)
            {
                var claim = leastPrivilegedClaims.First();
                if (claim.AlsoRequires.Length > 1)
                {
                    throw new InvalidOperationException($"The least privileged scope {claim.Permission} requires more than one other scope. Only one of {string.Join(", ", claim.AlsoRequires)} is allowed.");
                }
                return [claim.Permission, claim.AlsoRequires.First()];
            }

            return leastPrivilegedClaims?.Select(c => c.Permission);
        }

        private static void ProcessPermissionPairs(HashSet<(string, string)> permissionPairs, IEnumerable<string> leastRequired, IEnumerable<string> higherScopes, ref string higher)
        {
            var higherPrivilegedPairs = new List<string>();
            var found = new HashSet<(string, string)>(new OrderedPairEqualityComparer(StringComparer.OrdinalIgnoreCase));
            foreach (var pair in permissionPairs)
            {
                if (found.Contains(pair))
                {
                    continue;
                }
                // Check if any of the pair items is also a least privileged permission and display it as the first permission
                if (leastRequired.Contains(pair.Item1) || leastRequired.Contains(pair.Item2))
                {
                    var toAdd = leastRequired.Contains(pair.Item1) ? $"{pair.Item1} and {pair.Item2}" : $"{pair.Item2} and {pair.Item1}";
                    higherPrivilegedPairs.Add(toAdd);
                    found.Add(pair);
                    found.Add((pair.Item1, string.Empty));
                    found.Add((pair.Item2, string.Empty));
                }
                else if ((pair.Item1 == string.Empty && higherScopes.Contains(pair.Item2))|| (pair.Item2 == string.Empty && higherScopes.Contains(pair.Item1)))
                {
                    higherPrivilegedPairs.Add(string.Join(string.Empty, new[] { pair.Item1, pair.Item2 }));
                    found.Add(pair);
                }
                else if (higherScopes.Contains(pair.Item1) && higherScopes.Contains(pair.Item2))
                {
                    higherPrivilegedPairs.Add($"{pair.Item1} and {pair.Item2}");
                    found.Add(pair); 
                    found.Add((pair.Item1, string.Empty));
                    found.Add((pair.Item2, string.Empty));
                }
            }

            // Remaining higher privileged permissions that don't require any other permission e.g. Directory.ReadWrite.All
            var remainingHigher = higherScopes.Where(c => !found.Contains((c, string.Empty)));
            if (remainingHigher.Any())
            {
                higherPrivilegedPairs.AddRange(remainingHigher);
            }
            if (higherPrivilegedPairs.Count != 0)
            {
                higher = string.Join(", ", higherPrivilegedPairs);
            }
            else
            {
                higher = leastRequired != null ? StringConstants.PermissionNotAvailable : StringConstants.PermissionNotSupported;
            }
            
        }

        private void PairPermissions(AcceptableClaim claim, HashSet<(string, string)> permissionPairs)
        {
            if (claim.AlsoRequires.Any())
            {
                foreach (var alsoRequired in claim.AlsoRequires)
                {
                    permissionPairs.Add((claim.Permission, alsoRequired));
                }
            }
            else
            {
                permissionPairs.Add((claim.Permission, string.Empty));
            }
        }

        private void PopulateLeastPrivilege(Dictionary<string, Dictionary<string, HashSet<string>>> leastPrivilege, string method, string scheme, HashSet<AcceptableClaim> permissions)
        {
            if (permissions.Count == 0)
            {
                return;
            }
            leastPrivilege[method][scheme] = Disambiguate(method, scheme, permissions);
        }

        private HashSet<string> FetchLeastPrivilegeMultiplePermissions(HashSet<AcceptableClaim> claims)
        {
            var permissionPairs = new HashSet<(string, string)>(new OrderedPairEqualityComparer(StringComparer.OrdinalIgnoreCase));
            foreach (var claim in claims)
            {
                PairPermissions(claim, permissionPairs);
            }
            
            if (permissionPairs.Any())
            {
                return permissionPairs.Select(p => $"{p.Item1} and {p.Item2}").ToHashSet();
            }

            return claims.Select(p => p.Permission).ToHashSet();
        }
        private HashSet<string> Disambiguate(string method, string scheme, HashSet<AcceptableClaim> permissions)
        {
            // If more than one permission exists as the least privilege due to grouping of the methods
            if (permissions.Count > 1)
            {
                var exclusivePrivilegeCount = permissions.Count(perm => 
                    this.PermissionMethods.TryGetValue((perm.Permission, scheme), out HashSet<string> supportedMethods) &&
                    supportedMethods.Count == 1);

                if (exclusivePrivilegeCount > 1)
                {
                    return FetchLeastPrivilegeMultiplePermissions(permissions);
                }

                // Check for the permission supports the provided method only as the least privilege
                foreach (var perm in permissions)
                {
                    if (!(this.PermissionMethods.TryGetValue((perm.Permission, scheme), out HashSet<string> supportedMethods) && supportedMethods.Count == 1))
                    {
                        continue;
                    }
                    if (supportedMethods.First() == method)
                    {
                        return [FormatPermissionString(perm)];
                    }
                }

                var permissionMethodsCount = 100;
                var leastPrivilegePermission = permissions.First();
                // Check for the permission that supports the fewest number of methods as least privilege
                // TODO: Use permission risk levels once they get added to the model.
                foreach (var perm in permissions)
                {
                    if (!this.PermissionMethods.TryGetValue((perm.Permission, scheme), out HashSet<string> supportedMethods))
                    {
                        continue;
                    }
                    if (supportedMethods.Count < permissionMethodsCount && supportedMethods.Contains(method))
                    {
                        leastPrivilegePermission = perm;
                        permissionMethodsCount = supportedMethods.Count;
                    }
                }

                return [FormatPermissionString(leastPrivilegePermission)];
            }
            return permissions.Select(p => FormatPermissionString(p)).ToHashSet();
        }

        private string FormatPermissionString(AcceptableClaim claim)
        {
            if (claim.AlsoRequires.Length > 0)
            {
                return $"{claim.Permission} and {claim.AlsoRequires.First()}";
            }
            return claim.Permission;
        }
        
        private (string least, IEnumerable<string> higher) ExtractScopes(IEnumerable<string> orderedScopes, HashSet<string> leastPrivilege)
        {
            var least = leastPrivilege != null && leastPrivilege.Any() ? leastPrivilege.First() : StringConstants.PermissionNotSupported;
            var filteredScopes = orderedScopes.Where(s => s!= least);
            var higher = filteredScopes.Any() ? filteredScopes : leastPrivilege != null && leastPrivilege.Any() ? new[] { StringConstants.PermissionNotAvailable } : new[] { StringConstants.PermissionNotSupported };
            return (least, higher.ToHashSet());
        }
    }
}
