using Kibali;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace KibaliTool
{
    internal class TestCommand
    {


        // private static async Task TestGraphAPI(PermissionsDocument permissionDocument)
        // {
        //     var stream = new MemoryStream();
        //     var result = await new OpenApiStreamReader().ReadAsync(stream);
        //     var openApiDoc = result.OpenApiDocument;
        //     var authZChecker = new AuthZChecker();
        //     authZChecker.Load(permissionDocument);

        //     //Create Application

        //     //Create credentials to use for testing delegated permissions
        //     TokenCredential delegatedCredential = null;
        //     //Create credentials to use for testing application-only permissions
        //     TokenCredential appOnlyCredential = null;

        //     foreach (var (name, permission) in permissionDocument.Permissions)
        //     {
        //         // Consent app to the permission

        //         TestPaths(openApiDoc, (string url) => AccessRequestResult.Success == authZChecker.CanAccess(url, "GET", "DelegatedWork", new string[] { name }), delegatedCredential);
        //         TestPaths(openApiDoc, (string url) => AccessRequestResult.Success == authZChecker.CanAccess(url, "GET", "Application", new string[] { name }), appOnlyCredential);

        //         // Remove consent from the app
        //     }

        //     // Delete the application
        // }

        // private static void TestPaths(OpenApiDocument openApiDoc, Func<string, bool> canAccess, TokenCredential credential)
        // {
        //     foreach (var (url, pathItem) in openApiDoc.Paths)
        //     {
        //         foreach (var (method, operation) in pathItem.Operations)
        //         {
        //             List<string> allowedUrls = null;
        //             HttpStatusCode status = HttpStatusCode.OK;
        //             switch (method)
        //             {
        //                 case Microsoft.OpenApi.Models.OperationType.Get:
        //                     status = TestGetOperation(operation);
        //                     break;
        //                 case Microsoft.OpenApi.Models.OperationType.Put:
        //                     break;
        //                 case Microsoft.OpenApi.Models.OperationType.Post:
        //                     break;
        //                 case Microsoft.OpenApi.Models.OperationType.Delete:
        //                     break;
        //                 case Microsoft.OpenApi.Models.OperationType.Patch:
        //                     break;
        //                 default:
        //                     break;
        //             }

        //             if (canAccess(url))
        //             {
        //                 if (status == HttpStatusCode.Forbidden)
        //                 {
        //                     // This is a problem
        //                 }
        //             }
        //             else
        //             {
        //                 if (status != HttpStatusCode.Forbidden)
        //                 {
        //                     // This is a problem
        //                 }
        //             }

        //             if (!allowedUrls.Contains(url) && status != HttpStatusCode.Forbidden)
        //             {
        //                 // Danger will
        //             }

        //         }
        //     }
        // }

        private static HttpStatusCode TestGetOperation(OpenApiOperation operation)
        {
            throw new NotImplementedException();
        }

    }
}
