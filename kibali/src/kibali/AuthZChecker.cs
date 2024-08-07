using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Writers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kibali
{
    public class AuthZChecker
    {
        private readonly Dictionary<string, ProtectedResource> resources = new();
        private OpenApiUrlTreeNode urlTree;

        public Dictionary<string, ProtectedResource> Resources { get
            {
                return resources;
            }
        }

        public bool LenientMatch { get; set; }

        public PermissionsDeployment PermissionsDeployment { get; set; }

        public void Load(PermissionsDocument permissionsDocument, PermissionsDeployment deployment)
        {
            this.PermissionsDeployment = deployment;
            InvertPermissionsDocument(permissionsDocument);
        }

        public void Load(PermissionsDocument permissionsDocument)
        {
            InvertPermissionsDocument(permissionsDocument);
        }

        public ProtectedResource Lookup(string url)
        {
            var parsedUrl = new Uri(new Uri("https://example.org/"), url, true);
            var segments = parsedUrl.AbsolutePath.Split("/").Skip(1);
            return Find(UrlTree, segments);
        }

        public ProtectedResource FindResource(string url)
        {
            if (LenientMatch)
            {
                url = CleanRequestUrl(url);
                return Lookup(url);
            }
            else
            {
                return Lookup(url);
            }
        }

        public IEnumerable<AcceptableClaim> GetRequiredPermissions(string url, string method, string scheme)
        {
            var resource = FindResource(url);
            if (resource == null)
            {
                return new List<AcceptableClaim>();
            }
            if (!resource.SupportedMethods.TryGetValue(method, out var supportedSchemes))
            {
                return new List<AcceptableClaim>();
            }

            if (!supportedSchemes.TryGetValue(scheme, out var acceptableClaims))
            {
                return new List<AcceptableClaim>();
            }

            return acceptableClaims;
        }

        public AccessRequestResult CanAccess(string url, string method, string scheme, string[] providedPermissions)
        {
            var resource = FindResource(url);
            if (resource == null)
            {
                return AccessRequestResult.MissingResource;
            }
            if (!resource.SupportedMethods.TryGetValue(method, out var supportedSchemes)) {
                return AccessRequestResult.UnsupportedMethod;
            }

            if (!supportedSchemes.TryGetValue(scheme, out var acceptableClaims)) {
                return AccessRequestResult.UnsupportedScheme;
            }

            if (acceptableClaims.Any(claim => claim.IsAuthorized(providedPermissions)))
            {
                return AccessRequestResult.Success;
            }

            return AccessRequestResult.InsufficientPermissions;
        }

        public HashSet<PermissionsError> Validate(PermissionsDocument permissionsDocument)
        {
            return InvertPermissionsDocument(permissionsDocument, validate: true);
        }

        private HashSet<PermissionsError> InvertPermissionsDocument(PermissionsDocument permissionsDocument, bool validate = false)
        {
            // Walk permissions, find each pathSet and add path to dictionary
            var errors = new HashSet<PermissionsError>();
            foreach (var permission in permissionsDocument.Permissions)
            {
                var provisioningData = new List<ProvisioningInfo>();
                this.PermissionsDeployment?.Deployments?.TryGetValue(permission.Key, out provisioningData);
                var permissionSchemes = permission.Value.Schemes.Keys;
                foreach (var pathSet in permission.Value.PathSets)
                {
                    var pathsetSchemes = pathSet.SchemeKeys;
                    var unsupportedSchemes = pathsetSchemes.Except(permissionSchemes);
                    if (unsupportedSchemes.Any() && validate)
                    {
                        errors.Add(new PermissionsError { ErrorCode = PermissionsErrorCode.InvalidPathsetScheme, Message = $"Pathset contains schemes that the permission does not support - {string.Join(',', unsupportedSchemes)}", Path = permission.Key });
                    }
                    foreach (var path in pathSet.Paths)
                    {
                        var pathKey = this.LenientMatch ? CleanRequestUrl(path.Key) : path.Key;
                        if (!resources.TryGetValue(pathKey, out var resource))
                        {
                            resource = new ProtectedResource(pathKey);
                            resources.Add(pathKey, resource);
                        }
                        var leastPrivilegedPermissionSchemes = ParseLeastPrivilegeSchemes(path.Value);
                        var alsoRequires = ParseAlsoRequiresPermissions(path.Value);
                        resource.AddRequiredClaims(permission.Key, pathSet, leastPrivilegedPermissionSchemes, provisioningData, alsoRequires);
                        if (validate)
                        {
                            foreach (var requiredPermission in alsoRequires)
                            {
                                if (!permissionsDocument.Permissions.TryGetValue(requiredPermission, out var value))
                                {
                                    errors.Add(new PermissionsError { ErrorCode = PermissionsErrorCode.InvalidAlsoRequiresPermission, Message = $"{pathKey} which has permission {permission.Key} also requires permissions that don't exist - {requiredPermission}", Path = pathKey });
                                }
                                else
                                {
                                    var permissionPathSetSchemes = value.Schemes.Keys;
                                    var invalidAlsoRequiresSchemes = pathsetSchemes.Where(c => !permissionPathSetSchemes.Contains(c));
                                    if (invalidAlsoRequiresSchemes.Any())
                                    {
                                        errors.Add(new PermissionsError { ErrorCode = PermissionsErrorCode.InvalidAlsoRequiresPermission, Message = $"{pathKey} which has permission {permission.Key} also requires permission {requiredPermission} that does not support schemes {string.Join(",", invalidAlsoRequiresSchemes)}", Path = pathKey });
                                    }
                                }
                            }
                            errors.UnionWith(resource.ValidateMismatchedSchemes(permission.Key, pathSet, leastPrivilegedPermissionSchemes));
                        }
                    }
                }
            }
            if (validate)
            {
                ValidateAllResources(errors);
            }
            return errors;
        }

        private void ValidateAllResources(HashSet<PermissionsError> errors)
        {
            foreach (var resource in this.Resources)
            {
                errors.UnionWith(resource.Value.ValidateLeastPrivilegePermissions());
                var url = resource.Key;
                foreach (var methodEntry in resource.Value.SupportedMethods)
                {
                    var method = methodEntry.Key;
                    foreach (var schemeEntry in methodEntry.Value)
                    {
                        var scheme = schemeEntry.Key;
                        var least = schemeEntry.Value.Where(s => s.Least);
                        var perms = schemeEntry.Value.Select(e => e.Permission).Distinct();
                        var supportedPermissions = string.Join(",", perms);

                        if (!least.Any())
                        {
                            errors.Add(new PermissionsError { ErrorCode = PermissionsErrorCode.MissingLeastPrivilegePermission, Message = $"Missing least privilege permission entry for url {url} method {method} scheme {scheme}. Supported permissions are {supportedPermissions}", Path = url });
                        }
                    }
                }
            }
        }

        private ProtectedResource Find(OpenApiUrlTreeNode openApiUrlTree, IEnumerable<string> segments)
        {
            
            var segment = segments.FirstOrDefault();
            if (string.IsNullOrEmpty(segment))
            {
                return openApiUrlTree.PathItems.Any() ? (openApiUrlTree.PathItems.First().Value.Extensions["x-permissions"] as OpenApiProtectedResource).Resource : null;  // Can the root have a permission?
            }

            if (openApiUrlTree.Children.TryGetValue(segment, out var urlTreeNode))
            {
                return Find(urlTreeNode, segments.Skip(1));
            }
            else
            {
                var parameterSegment = openApiUrlTree.Children.Where(k => k.Key.StartsWith("{")).FirstOrDefault();
                if (parameterSegment.Key == null) return null;
                return Find(parameterSegment.Value, segments: segments.Skip(1));
            }
        }

        private OpenApiUrlTreeNode UrlTree
        {
            get
            {
                urlTree ??= CreateUrlTree(this.resources);
                return urlTree;
            }
        }

        private static OpenApiUrlTreeNode CreateUrlTree(Dictionary<string, ProtectedResource> resourcesMap)
        {
            var tree = OpenApiUrlTreeNode.Create();

            foreach (var resource in resourcesMap)
            {
                var pathItem = new OpenApiPathItem();
                var openApiResource = new OpenApiProtectedResource(resource.Value);
                pathItem.AddExtension("x-permissions", openApiResource);
                tree.Attach(resource.Key, pathItem, "!");
            }

            return tree;
        }
        private static string[] ParseLeastPrivilegeSchemes(string pathValue)
        {
            var defaultLeastPrivilege = Array.Empty<string>();
            if (string.IsNullOrEmpty(pathValue))
            {
                return defaultLeastPrivilege;
            }
            var parsedPathValue = ParsingHelpers.ParseProperties(pathValue);
            parsedPathValue.TryGetValue("least", out var privilegeString);
            var leastPrivilegedPermissionSchemes = privilegeString != null ? privilegeString.Split(",") : defaultLeastPrivilege;
            return leastPrivilegedPermissionSchemes;
        }

        private static string[] ParseAlsoRequiresPermissions(string pathValue)
        {
            var alsoRequired = Array.Empty<string>();
            if (string.IsNullOrEmpty(pathValue))
            {
                return alsoRequired;
            }
            var parsedPathValue = ParsingHelpers.ParseProperties(pathValue);
            parsedPathValue.TryGetValue("AlsoRequires", out var permissions);
            var additionalPermissions = permissions != null ? permissions.Split(",") : alsoRequired;
            return additionalPermissions;
        }

        private static string CleanRequestUrl(string requestUrl)
        {
            if (string.IsNullOrEmpty(requestUrl))
            {
                return requestUrl;
            }

            var parensRemoved = Regex.Replace(requestUrl.ToLowerInvariant(), @"\/\(.*?\)", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(5)).Replace(@"//", "/");
            var braceValuesReplaced = Regex.Replace(parensRemoved, @"\{.*?\}", "{id}", RegexOptions.None, TimeSpan.FromSeconds(5));
            return braceValuesReplaced;
        }
    }

    public class OpenApiProtectedResource : IOpenApiExtension, IOpenApiAny
    {
        public OpenApiProtectedResource(ProtectedResource resource)
        {
            Resource = resource;
        }

        public ProtectedResource Resource { get; }

        public AnyType AnyType => AnyType.Object;

        public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
        {
        }
    }                                           

    public enum AccessRequestResult
    {
        Success,
        MissingResource,
        UnsupportedMethod,
        UnsupportedScheme,
        InsufficientPermissions
    }

    public enum PermissionsErrorCode
    {
        DuplicateLeastPrivilegeScopes,
        InvalidLeastPrivilegeScheme,
        InvalidPathsetScheme,
        MissingLeastPrivilegePermission,
        InvalidAlsoRequiresPermission,
    }
}
