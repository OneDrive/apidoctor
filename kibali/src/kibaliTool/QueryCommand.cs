using Kibali;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace KibaliTool
{
    internal class QueryCommandParameters {
        public string SourcePermissionsFile;
        public string DeploymentsFile;
        public string Url;
        public string Method;
        public string Scheme;
        public bool LeastPrivilege;
        public bool LenientMatch;
    }

    internal class QueryCommand
    {
        
        public static async Task<int> Execute(QueryCommandParameters queryCommandParameters)
        {
            using var fileStream = new FileStream(queryCommandParameters.SourcePermissionsFile, FileMode.Open);
            var doc = PermissionsDocument.Load(fileStream);

            var authZChecker = new AuthZChecker() { LenientMatch = queryCommandParameters.LenientMatch };
            authZChecker.Load(doc);

            var resource = authZChecker.FindResource(queryCommandParameters.Url);

            if(resource == null)
            {
                Console.WriteLine($"Resource {queryCommandParameters.Url} not found in the input file.");
                return 0;
            }

            var writer = new Utf8JsonWriter(Console.OpenStandardOutput(), new JsonWriterOptions() { Indented= true });

            if (!string.IsNullOrEmpty(queryCommandParameters.Scheme))
            {
                if (string.IsNullOrEmpty(queryCommandParameters.Method))
                {
                    throw new ArgumentException("Missing method");
                }
                if (resource.SupportedMethods.TryGetValue(queryCommandParameters.Method, out var supportedSchemes))
                {
                    resource.WriteAcceptableClaims(writer, supportedSchemes[queryCommandParameters.Scheme]);
                } else
                {
                    throw new ArgumentException("Unknown scheme");
                }
            }
            else if (!string.IsNullOrEmpty(queryCommandParameters.Method))
            {
                if (resource.SupportedMethods.TryGetValue(queryCommandParameters.Method, out var supportedSchemes))
                {
                    resource.WriteSupportedSchemes(writer, supportedSchemes);
                } 
                else
                {
                    throw new ArgumentException("Unknown method");
                }
            }
            else
            {
                resource.Write(writer);
            }

            await writer.FlushAsync();

            if (queryCommandParameters.LeastPrivilege)
            {
                Console.WriteLine();
                var leastPrivilege = resource.FetchLeastPrivilege(queryCommandParameters.Method, queryCommandParameters.Scheme);
                Console.WriteLine(resource.WriteLeastPrivilegeTable(leastPrivilege));
            }

            return 0;
        }
    }
}
