using Kibali;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace KibaliTests
{
    public class GraphPermissionTests
    {

        private readonly ITestOutputHelper _output;
        private PermissionsDocument graphPermissions;
        private PermissionsDocument userAuthPermissions;
        public GraphPermissionTests(ITestOutputHelper output)
        {
            _output = output;
            using var graphStream = new FileStream("GraphPermissions.json", FileMode.Open);
            graphPermissions = PermissionsDocument.Load(graphStream);
            using var authStream = new FileStream("UserAuthenticationMethod.json", FileMode.Open);
            userAuthPermissions = PermissionsDocument.Load(authStream);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        public void GraphFindMailReadAll(string httpMethod)
        {

            var authZChecker = new AuthZChecker();
            authZChecker.Load(graphPermissions);

            var resource = authZChecker.FindResource("/me/messages");

            Assert.True(resource.SupportedMethods.TryGetValue(httpMethod, out var methodPermissions));
            var expectedPermission = httpMethod == "GET" ? "Mail.Read" : "Mail.ReadWrite";
            Assert.Contains(methodPermissions["DelegatedWork"], ac => ac.Permission == expectedPermission);    
        }

        [Fact]
        public void GraphFindUSerAuthenticationMethods()
        {
            var authZChecker = new AuthZChecker();
            authZChecker.Load(userAuthPermissions);

            var resource = authZChecker.FindResource("/users/sdfsdfsdfsdf/authentication/microsoftauthenticatormethods");

            Assert.True(resource.SupportedMethods.TryGetValue("GET", out var methodPermissions));
            Assert.Contains(methodPermissions["DelegatedWork"], ac => ac.Permission == "UserAuthenticationMethod.ReadWrite");
        }

        [Fact]
        public void GraphFindUSerAuthenticationMethods2()
        {
            var authZChecker = new AuthZChecker();
            authZChecker.Load(graphPermissions);

            var resource = authZChecker.FindResource("/users/sdfsdfsdfsdf/authentication/microsoftauthenticatormethods");

            Assert.True(resource.SupportedMethods.TryGetValue("GET", out var methodPermissions));
            Assert.Contains(methodPermissions["DelegatedWork"], ac => ac.Permission == "UserAuthenticationMethod.ReadWrite");
        }

        [Fact]
        public void GraphFindPerfTest()
        {
            var authZChecker = new AuthZChecker();
            authZChecker.Load(graphPermissions);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < 1000; i++)
            {
                var resource = authZChecker.FindResource("/me/messages");
                Assert.NotNull(resource);
                resource = authZChecker.FindResource("/users/sdfsdfsdfsdf/authentication/microsoftauthenticatormethods");
                Assert.NotNull(resource);
                resource = authZChecker.FindResource("/admin/windows/updates/resourceconnections/microsoft.graph.windowsupdates.operationalinsightsconnection");
                Assert.NotNull(resource);
            }
            _output.WriteLine(stopwatch.ElapsedMilliseconds.ToString());


        }

        private PermissionsDocument CreatePermissionsDocument()
        {
            var permissionsDocument = new PermissionsDocument();

            var fooRead = new Permission
            {
                PathSets = {
                        new PathSet() {
                            Methods = {
                                "GET"
                            },
                            Paths = {
                                { "/foo",  null },
                                { "/bar",  null },
                                { "/bar/{id}",  null },
                                { "/bar/{id}/schmo",  null }
                            }
                        }
                    }
            };
            permissionsDocument.Permissions.Add("Foo.Read", fooRead);
            return permissionsDocument;
        }
    }
}
