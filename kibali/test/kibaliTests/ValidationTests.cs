using Kibali;
using Xunit.Abstractions;

namespace KibaliTests;

public class ValidationTests
{
    private readonly ITestOutputHelper _output;
    public ValidationTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public void ValidateSinglePermissionsFileIsValid()
    {
        // Arrange
        using var stream = new FileStream("ValidUser.json", FileMode.Open);
        var doc = PermissionsDocument.Load(stream);

        // Act
        var authZChecker = new AuthZChecker();
        var errors = authZChecker.Validate(doc);
        
        // Assert
        Assert.False(errors.Any());
    }
    [Fact]
    public void ValidateSinglePermissionFileIsNotvalid()
    {
        // Arrange
        using var stream = new FileStream("InvalidUser.json", FileMode.Open);
        var doc = PermissionsDocument.Load(stream);
        
        // Act
        var authZChecker = new AuthZChecker();
        var errors = authZChecker.Validate(doc);

        // Assert
        Assert.True(errors.Any());
        Assert.Contains(PermissionsErrorCode.DuplicateLeastPrivilegeScopes, errors.Select(e => e.ErrorCode));
        Assert.Contains(PermissionsErrorCode.InvalidLeastPrivilegeScheme, errors.Select(e => e.ErrorCode));
        Assert.Contains("/me", errors.Select(e => e.Path));
        Assert.Contains("/me/createdobjects", errors.Select(e => e.Path)); 
    }
    [Fact]
    public void ValidateFolderIsValid()
    {
        // Arrange
        var doc = PermissionsDocument.LoadFromFolder("ValidPermissions");

        // Act
        var authZChecker = new AuthZChecker();
        var errors = authZChecker.Validate(doc);

        // Assert
        Assert.False(errors.Any());
    }
    [Fact]
    public void ValidateFolderIsNotValid()
    {
        // Arrange
        var doc = PermissionsDocument.LoadFromFolder("InvalidPermissions");

        // Act
        var authZChecker = new AuthZChecker();
        var errors = authZChecker.Validate(doc);

        // Assert
        Assert.True(errors.Any());
        Assert.Contains(PermissionsErrorCode.DuplicateLeastPrivilegeScopes, errors.Select(e => e.ErrorCode));
        Assert.Contains(PermissionsErrorCode.InvalidLeastPrivilegeScheme, errors.Select(e => e.ErrorCode));
        Assert.Contains("/me", errors.Select(e => e.Path));
        Assert.Contains("/users/{id}/licensedetails", errors.Select(e => e.Path));
    }

    [Fact]
    public void ValdiateInvalidScheme()
    {
        // Arrange
        var doc = new PermissionsDocument();
        var fooRead = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "DelegatedWork", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application" },
                Methods = { "GET" },
                Paths = { { "/foo",  "least=Application" } }
            }
            }
        };
        doc.Permissions.Add("Foo.Read", fooRead);
        // Act
        var authZChecker = new AuthZChecker();
        var errors = authZChecker.Validate(doc);

        // Assert
        Assert.True(errors.Any());
        Assert.Contains(PermissionsErrorCode.InvalidPathsetScheme, errors.Select(e => e.ErrorCode));

    }

    [Fact]
    public void ValidateNoLeastPrivilege()
    {
        // Arrange
        var doc = new PermissionsDocument();
        var fooRead = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application" },
                Methods = { "GET" },
                Paths = { { "/foo",  "" } }
            }
            }
        };
        doc.Permissions.Add("Foo.Read", fooRead);
        // Act
        var authZChecker = new AuthZChecker();
        var errors = authZChecker.Validate(doc);

        // Assert
        Assert.True(errors.Any());
        Assert.Contains(PermissionsErrorCode.MissingLeastPrivilegePermission, errors.Select(e => e.ErrorCode));

    }

    [Fact]
    public void ValidateAlsoRequiresFailsIfMoreThanOnePairMarkedAsLeast()
    {
        // Arrange
        var doc = new PermissionsDocument();
        var fooRead = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application" },
                Methods = { "GET" },
                Paths = { { "/foo",  "least=Application;AlsoRequires=Bar.Read" } }
            }
            }
        };

        var barRead = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() },
                { "DelegatedWork", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application", "DelegatedWork" },
                Methods = { "GET" },
                Paths = { { "/foo",  "least=Application,DelegatedWork;AlsoRequires=Foo.Read" } }
            }
            }
        };

        var bazRead = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() },
                { "DelegatedWork", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application", "DelegatedWork" },
                Methods = { "GET" },
                Paths = { { "/foo",  "least=Application,DelegatedWork;AlsoRequires=Foo.Read" } }
            }
            }
        };


        doc.Permissions.Add("Foo.Read", fooRead);
        doc.Permissions.Add("Bar.Read", barRead);
        doc.Permissions.Add("Baz.Read", bazRead);
        // Act
        var authZChecker = new AuthZChecker();
        var errors = authZChecker.Validate(doc);

        // Assert
        Assert.True(errors.Any());
        Assert.Contains(PermissionsErrorCode.DuplicateLeastPrivilegeScopes, errors.Select(e => e.ErrorCode));

    }

    [Fact]
    public void ValidateAlsoRequiresFailsIfScopeDoesNotExist()
    {
        // Arrange
        var doc = new PermissionsDocument();
        var fooRead = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application" },
                Methods = { "GET" },
                Paths = { { "/foo",  "least=Application;AlsoRequires=Baz.Read" } }
            }
            }
        };


        doc.Permissions.Add("Foo.Read", fooRead);
        // Act
        var authZChecker = new AuthZChecker();
        var errors = authZChecker.Validate(doc);

        // Assert
        Assert.True(errors.Any());
        Assert.Contains(PermissionsErrorCode.InvalidAlsoRequiresPermission, errors.Select(e => e.ErrorCode));

    }

    [Fact]
    public void ValidateAlsoRequiresFailsIfSchemeDoesNotExist()
    {
        // Arrange
        var doc = new PermissionsDocument();
        var fooRead = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application" },
                Methods = { "GET" },
                Paths = { { "/foo",  "least=Application;AlsoRequires=Bar.Read" } }
            }
            }
        };

        var barRead = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() },
                { "DelegatedWork", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application", "DelegatedWork" },
                Methods = { "GET" },
                Paths = { { "/foo",  "least=Application,DelegatedWork;AlsoRequires=Foo.Read" } }
            }
            }
        };


        doc.Permissions.Add("Foo.Read", fooRead);
        doc.Permissions.Add("Bar.Read", barRead);
        // Act
        var authZChecker = new AuthZChecker();
        var errors = authZChecker.Validate(doc);

        // Assert
        Assert.True(errors.Any());
        Assert.Contains(PermissionsErrorCode.InvalidAlsoRequiresPermission, errors.Select(e => e.ErrorCode));

    }

    [Fact]
    public void ValidateAlsoRequiresSucceedsWithReciprocity()
    {
        // Arrange
        var doc = new PermissionsDocument();
        var fooRead = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application" },
                Methods = { "GET" },
                Paths = { { "/foo",  "least=Application;AlsoRequires=Bar.Read" } }
            }
            }
        };

        var barRead = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() },
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application" },
                Methods = { "GET" },
                Paths = { { "/foo",  "least=Application;AlsoRequires=Foo.Read" } }
            }
            }
        };
        doc.Permissions.Add("Foo.Read", fooRead);
        doc.Permissions.Add("Bar.Read", barRead);
        // Act
        var authZChecker = new AuthZChecker();
        var errors = authZChecker.Validate(doc);

        // Assert
        Assert.False(errors.Any());
    }

    [Fact]
    public void ValidateAlsoRequiresSucceedsWithReciprocityMultipleRequired()
    {
        // Arrange
        var doc = new PermissionsDocument();
        var fooRead = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() },
                { "DelegatedWork", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application" },
                Methods = { "GET" },
                Paths = { { "/foo",  "least=Application;AlsoRequires=Bar.Read" } }
            }
            }
        };

        var barRead = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() },
                { "DelegatedWork", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application", "DelegatedWork" },
                Methods = { "GET" },
                Paths = { { "/foo",  "least=Application,DelegatedWork;AlsoRequires=Foo.Read" } }
            }
            }
        };
        var fooWrite = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() },
                { "DelegatedWork", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application", "DelegatedWork" },
                Methods = { "GET" },
                Paths = { { "/foo",  "AlsoRequires=Foo.Read,Bar.Read,Bar.Write" } }
            }
            }
        };
        var barWrite = new Permission
        {
            Schemes = new SortedDictionary<string, Scheme>()
            {
                { "Application", new Scheme() },
                { "DelegatedWork", new Scheme() }
            },
            PathSets = {
            new PathSet() {
                SchemeKeys = { "Application", "DelegatedWork" },
                Methods = { "GET" },
                Paths = { { "/foo",  "AlsoRequires=Foo.Write" } }
            }
            }
        };
        doc.Permissions.Add("Foo.Read", fooRead);
        doc.Permissions.Add("Bar.Read", barRead);
        doc.Permissions.Add("Foo.Write", fooWrite);
        doc.Permissions.Add("Bar.Write", barWrite);
        // Act
        var authZChecker = new AuthZChecker();
        var errors = authZChecker.Validate(doc);

        // Assert
        Assert.False(errors.Any());
    }
}
