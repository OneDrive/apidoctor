using Kibali;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace KibaliTool
{
    internal class ImportCommandParameters
    {
        public string SourcePermissionsFile;
        public string SourceDescriptionsFile;
        public string OutFolder;
        public bool SingleFile;
    }

    internal class ImportCommand
    {
        public static async Task<int> Execute(ImportCommandParameters commandOptions)
        {
            var doc = new PermissionsDocument();
            
            await ParseFromGEPermissions(doc, commandOptions.SourcePermissionsFile, commandOptions.SourceDescriptionsFile);

            if (commandOptions.SingleFile)
            {
                await WriteSingleDocument(doc, commandOptions.OutFolder);
            } else
            {
                await WriteDocuments(doc, commandOptions.OutFolder);
            }

            return 0;
        } 

        public static async Task ParseFromGEPermissions(PermissionsDocument doc, string inputFile, string permissionsFile)
        {
            JsonElement rootObject;
            JsonElement permissionsObject;

            using (var client = new HttpClient())
            {
                rootObject = await FetchAndParseJsonAsync(inputFile, client);
                permissionsObject = await FetchAndParseJsonAsync(permissionsFile, client);
            }

            var apiPermissions = rootObject.GetProperty("ApiPermissions");

            CreatePermissions(doc, permissionsObject);

            var entries = CreatePermissionsEntries(apiPermissions);

            var permissionsInfoList = entries.GroupBy(pe => pe.Permission)
                        .Select(gr => new { Permission = gr.Key, Paths = gr.GroupBy(p => p.Path) })
                        .OrderBy(gr => gr.Permission);

            foreach (var permissionInfo in permissionsInfoList)
            {
                if (!doc.Permissions.ContainsKey(permissionInfo.Permission))
                {
                    doc.Permissions.Add(permissionInfo.Permission, new Permission());
                }

                var perm = doc.Permissions[permissionInfo.Permission];
                foreach (var pathDetails in permissionInfo.Paths)
                {
                    SortedSet<string> methods = new SortedSet<string>();
                    SortedSet<string> schemes = new SortedSet<string>();
                    foreach (var entry in pathDetails)
                    {
                        methods.Add(entry.Method);
                        schemes.Add(entry.Scheme);
                    }
                    var pathSet = GetOrCreatePathSet(perm, methods, schemes);
                    pathSet.Paths.Add(pathDetails.Key, string.Empty);
                }
            }
        }

        private static void CreatePermissions(PermissionsDocument doc, JsonElement permissionsObject)
        {
            var delegatedElement = permissionsObject.GetProperty("delegatedScopesList");
            foreach (var entry in delegatedElement.EnumerateArray())
            {
                var name = entry.GetProperty("value").GetString();
                if (!doc.Permissions.TryGetValue(name, out var permission))
                {
                    permission = new Permission();
                }

                var scheme = new Scheme
                {
                    RequiresAdminConsent = entry.GetProperty("isAdmin").GetBoolean(),
                    AdminDescription = entry.GetProperty("adminConsentDescription").GetString(),
                    AdminDisplayName = entry.GetProperty("adminConsentDisplayName").GetString(),
                    UserDescription = entry.GetProperty("consentDescription").GetString(),
                    UserDisplayName = entry.GetProperty("consentDisplayName").GetString()
                };

                permission.Schemes["DelegatedWork"] = scheme;

                doc.Permissions.Add(name, permission);
            }


            var applicationElement = permissionsObject.GetProperty("applicationScopesList");
            foreach (var entry in applicationElement.EnumerateArray())
            {
                var name = entry.GetProperty("value").GetString();
                if (!doc.Permissions.TryGetValue(name, out var permission))
                {
                    permission = new Permission();
                    doc.Permissions.Add(name, permission);
                }

                var scheme = new Scheme
                {
                    RequiresAdminConsent = entry.GetProperty("isAdmin").GetBoolean(),
                    AdminDescription = entry.GetProperty("consentDescription").GetString(),
                    AdminDisplayName = entry.GetProperty("consentDisplayName").GetString(),
                };

                permission.Schemes["Application"] = scheme;
            }

        }

        private static PathSet GetOrCreatePathSet(Permission perm, SortedSet<string> methods, SortedSet<string> schemes)
        {
            var pathSet = perm.PathSets.FirstOrDefault(x => x.SchemeKeys.SetEquals(schemes) && x.Methods.SetEquals(methods));
            if (pathSet != null)
                return pathSet;

            var newPathSet = new PathSet()
            {
                Methods = methods,
                SchemeKeys = schemes
            };
            perm.PathSets.Add(newPathSet);
            return newPathSet;
        }

        public static async Task WriteSingleDocument(PermissionsDocument doc, string outputPath)
        {
            doc.Permissions = new SortedDictionary<string, Permission>(doc.Permissions.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value));
            Directory.CreateDirectory(outputPath);
            var filename = "GraphPermissions";
            using (var outStream = new FileStream($"{outputPath}/{filename}.json", FileMode.Create))
            {
                await doc.WriteAsync(outStream);
            }
        }

        public static async Task WriteDocuments(PermissionsDocument doc, string outputPath)
        {
            PermissionsDocument tempDoc = new PermissionsDocument();
            string currentResource = string.Empty;
            Directory.CreateDirectory(outputPath);
            foreach (var permPair in doc.Permissions.OrderBy(p => p.Key))
            {
                var resource = permPair.Key.Split('.').Take(1).FirstOrDefault();
                if (string.IsNullOrEmpty(currentResource))
                {
                    currentResource = resource;
                }
                if (resource != currentResource)
                {
                    if (tempDoc != null)
                    {
                        Console.WriteLine("Outputing " + currentResource);
                        var filename = currentResource.Replace("/", "-");
                        using (var outStream = new FileStream($"{outputPath}/{filename}.json", FileMode.Create))
                        {
                            await tempDoc.WriteAsync(outStream);
                        }
                    }
                    tempDoc = new PermissionsDocument();
                    currentResource = resource;
                }
                tempDoc.Permissions.Add(permPair.Key, permPair.Value);
            }
        }

        private static List<PermissionEntry> CreatePermissionsEntries(JsonElement apiPermissions)
        {
            List<PermissionEntry> entries = new();

            foreach (var path in apiPermissions.EnumerateObject())
            {
                foreach (var method in path.Value.EnumerateObject())
                {
                    foreach (var scheme in method.Value.EnumerateObject())
                    {
                        foreach (var permission in scheme.Value.EnumerateArray())
                        {
                            entries.Add(new PermissionEntry()
                            {
                                Permission = permission.GetString(),
                                Path = path.Name,
                                Method = method.Name,
                                Scheme = scheme.Name
                            });
                        }
                    }
                }
            }
            return entries;
        }

        private static async Task<JsonElement> FetchAndParseJsonAsync(string path, HttpClient client)
        {
            Stream inputStream = path.StartsWith("http") 
                ? await client.GetStreamAsync(path) 
                : new FileStream(path, FileMode.Open);

            using (inputStream)
            {
                var jsonDoc = await JsonDocument.ParseAsync(inputStream);
                return jsonDoc.RootElement;
            }
        }

        //private static void ProcessPermissionsSchemes(string schemeType, JsonElement schemes, PermissionsDocument doc)
        //{
        //    foreach (var scheme in schemes.EnumerateArray())
        //    {
        //        Permission perm;
        //        var name = scheme.GetProperty("Name").GetString();
        //        // Use name to see if permission exists, if not create it
        //        if (!doc.Permissions.TryGetValue(name, out perm))
        //        {
        //            perm = new Permission();
        //            doc.Permissions.Add(name, perm);
        //        }

        //        // add scheme of schemeType, set descriptions
        //        var newScheme = new Scheme
        //        {
        //            RequiresAdminConsent = scheme.GetProperty("Grant").GetString() == "admin",
        //            UserDescription = scheme.GetProperty("Description").GetString(),
        //            AdminDescription = scheme.GetProperty("ConsentDescription").GetString()
        //        };
        //        if (!perm.Schemes.ContainsKey(schemeType))
        //        {
        //            perm.Schemes.Add(schemeType, newScheme);
        //        }
        //        else
        //        {
        //            Console.WriteLine($"Duplicate entry for {name} in scheme {schemeType}");
        //        }
        //    }
        //}

        //private static string GetSchemeType(bool isApplication, bool isDelegatedWork)
        //{
        //    if (isApplication)
        //    {
        //        // Add ApplicationPermission
        //        return "Application";
        //    }
        //    if (isDelegatedWork)
        //    {
        //        // Add DelegatePermission
        //        return "DelegatedWork";

        //    }
        //    return "DelegatedPersonal";
        //}

        //private static void ParseFromMerillCSV(PermissionsDocument doc, string inputfile)
        //{
        //    var reader = new StreamReader(inputfile);
        //    reader.ReadLine(); // Skip titles.
        //    string line;
        //    string[] row;
        //    while ((line = reader.ReadLine()) != null)
        //    {
        //        row = line.Split(',');

        //        var name = row[0];

        //        CreatePath(doc, name, path: row[5], method: row[4].TrimEnd(), GetSchemeType(bool.Parse(row[1]), bool.Parse(row[2])));
        //    }
        //}

        //private static void CreatePath(PermissionsDocument doc, string name, string path, string method, string type)
        //{
        //    if (doc.Permissions.TryGetValue(name, out Permission perm))
        //    {

        //        if (!perm.Schemes.ContainsKey(type))
        //        {
        //            perm.Schemes.Add(type, new Scheme());
        //        }
        //        PathSet pathSet;
        //        if (perm.PathSets.Count == 0)
        //        {
        //            pathSet = new PathSet();
        //            perm.PathSets.Add(pathSet);
        //        }
        //        else
        //        {
        //            pathSet = perm.PathSets[0];
        //        }

        //        pathSet.Methods.Add(method);
        //        pathSet.Paths.TryAdd(path, new ApiPermissions.PathConstraints());
        //    }
        //    else
        //    {
        //        var newPermission = new Permission();
        //        doc.Permissions.Add(name, newPermission);
        //    }
        //}


        //private static void CreatePermissions(PermissionsDocument doc, JsonElement rootObject)
        //{
        //    var permissionSchemes = rootObject.GetProperty("PermissionSchemes");

        //    ProcessPermissionsSchemes("DelegatedPersonal",
        //                                permissionSchemes.GetProperty("DelegatedPersonal"), doc);
        //    ProcessPermissionsSchemes("DelegatedWork",
        //                                permissionSchemes.GetProperty("DelegatedWork"), doc);
        //    ProcessPermissionsSchemes("Application",
        //                                permissionSchemes.GetProperty("Application"), doc);
        //}


    }

    public class PermissionEntry
    {
        public string Path { get; set; }
        public string Method { get; set; }
        public string Scheme { get; set; }
        public string Permission { get; set; }
    }
}
