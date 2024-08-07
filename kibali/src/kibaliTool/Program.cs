using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Binding;

namespace KibaliTool
{

    class Program
    {
        // CommandLine
        // vibali import -s {oldPermissionsFile} -o {outFolder} [--single-file] // backfill
        // vibali query -u {url} -m {method} [-s {scheme}]  // GE + Docs
        // vibali export -s {permissionFileSpec} -o {csdlFile}   

        static async Task Main(string[] args)
        {

            Command importCommand = new Command("import") { 
                ImportCommandBinder.PermissionDescriptionOption,
                ImportCommandBinder.PermissionFileOption,
                ImportCommandBinder.OutFolderOption,
                ImportCommandBinder.SingleFileOption
            };
            importCommand.SetHandler(ImportCommand.Execute, new ImportCommandBinder());

            Command queryCommand = new Command("query") {
                QueryCommandBinder.PermissionFileOption,
                QueryCommandBinder.DeploymentsFileOption,
                QueryCommandBinder.UrlOption,
                QueryCommandBinder.MethodOption,
                QueryCommandBinder.SchemeOption,
                QueryCommandBinder.LeastPrivilegeOption,
                QueryCommandBinder.LenientMatchOption,
            };
            
            queryCommand.SetHandler(QueryCommand.Execute, new QueryCommandBinder());
            
            Command exportCommand = new Command("export");
	       
            Command validateCommand = new Command("validate") {
                ValidateCommandBinder.PermissionFileOption,
                ValidateCommandBinder.PermissionFolderOption,
                ValidateCommandBinder.LenientMatchOption,
            };

            validateCommand.SetHandler(ValidateCommand.Execute, new ValidateCommandBinder());

            Command documentCommand = new Command("document") {
                DocumentCommandBinder.PermissionFileOption,
                DocumentCommandBinder.PermissionFolderOption,
                DocumentCommandBinder.UrlOption,
                DocumentCommandBinder.MethodOption,
                DocumentCommandBinder.CombineMultipleOption,
            };

            documentCommand.SetHandler(DocumentCommand.Execute, new DocumentCommandBinder());

            var rootCommand = new RootCommand()
            {
                importCommand,
                queryCommand,
                exportCommand,
                validateCommand,
                documentCommand
            };
            

            await rootCommand.InvokeAsync(args);
            
            //var doc = new PermissionsDocument();
            ////   ParseFromMerillCSV(doc, "../../../../permissions.csv");
            //await ImportCommand.ParseFromGEPermissions(doc, "https://raw.githubusercontent.com/microsoftgraph/microsoft-graph-devx-content/dev/permissions/permissions-beta.json", "https://raw.githubusercontent.com/microsoftgraph/microsoft-graph-devx-content/dev/permissions/permissions-descriptions.json");
            ////   await WriteDocuments(doc, "./output");
            //await ImportCommand.WriteSingleDocument(doc, "./output");
        }
    }

    internal class ImportCommandBinder : BinderBase<ImportCommandParameters>
    {
        public static readonly Option<string> PermissionDescriptionOption = new (new[] { "--permissionDescription", "--pd" }, "GE Permission Description File");
        public static readonly Option<string> PermissionFileOption = new (new[] { "--permissionFile", "--pf" }, "GE Permissions File");
        public static readonly Option<string> OutFolderOption = new (new[] { "--outFolder", "--of" }, "Output folder");
        public static readonly Option<bool> SingleFileOption = new (new[] { "--singleFile", "--sf" }, "Single file");

        public ImportCommandBinder()
        {
            PermissionDescriptionOption.SetDefaultValue(@"https://raw.githubusercontent.com/microsoftgraph/microsoft-graph-devx-content/dev/permissions/permissions-descriptions.json");
            PermissionFileOption.SetDefaultValue(@"https://raw.githubusercontent.com/microsoftgraph/microsoft-graph-devx-content/dev/permissions/permissions-beta.json");
            OutFolderOption.SetDefaultValue(@"./output");
            SingleFileOption.SetDefaultValue(true);
        }
        protected override ImportCommandParameters GetBoundValue(BindingContext bindingContext)
        {
            return new ImportCommandParameters()
            {
                SourceDescriptionsFile = bindingContext.ParseResult.GetValueForOption(PermissionDescriptionOption),
                SourcePermissionsFile = bindingContext.ParseResult.GetValueForOption(PermissionFileOption),
                OutFolder = bindingContext.ParseResult.GetValueForOption(OutFolderOption),
                SingleFile = bindingContext.ParseResult.GetValueForOption(SingleFileOption)
            };
        }
    }


    internal class QueryCommandBinder : BinderBase<QueryCommandParameters>
    {
        public static readonly Option<string> PermissionFileOption = new (new[] { "--sourcePermissionFile", "--pf" }, "Permission File");
        public static readonly Option<string> DeploymentsFileOption = new(new[] { "--deploymentsFile", "--df" }, "Deployments File");
        public static readonly Option<string> UrlOption = new (new[] { "--url", "-u" }, "Test Url");
        public static readonly Option<string> MethodOption = new (new[] { "--method", "-m" }, "Method");
        public static readonly Option<string> SchemeOption = new( new[] { "--scheme", "-s" }, "Scheme");
        public static readonly Option<bool> LeastPrivilegeOption = new(new[] { "--least", "-l" }, "LeastPrivilege");
        public static readonly Option<bool> LenientMatchOption = new(new[] { "--lenient", "--lm" }, "LenientMatch");

        protected override QueryCommandParameters GetBoundValue(BindingContext bindingContext)
        {
            return new QueryCommandParameters()
            {
                SourcePermissionsFile = bindingContext.ParseResult.GetValueForOption(PermissionFileOption),
                DeploymentsFile = bindingContext.ParseResult.GetValueForOption(DeploymentsFileOption),
                Url = bindingContext.ParseResult.GetValueForOption(UrlOption),
                Method = bindingContext.ParseResult.GetValueForOption(MethodOption),
                Scheme = bindingContext.ParseResult.GetValueForOption(SchemeOption),
                LeastPrivilege = bindingContext.ParseResult.GetValueForOption(LeastPrivilegeOption),
                LenientMatch = bindingContext.ParseResult.GetValueForOption(LenientMatchOption),
            };
        }
    }


    internal class ValidateCommandBinder : BinderBase<ValidateCommandParameters>
    {
        public static readonly Option<string> PermissionFileOption = new(new[] { "--sourcePermissionFile", "--pf" }, "Permission File");
        public static readonly Option<string> PermissionFolderOption = new(new[] { "--sourcePermissionsFolder", "--fo" }, "Permission Folder");
        public static readonly Option<bool> LenientMatchOption = new(new[] { "--lenient", "--lm" }, "Lenient Match");

        protected override ValidateCommandParameters GetBoundValue(BindingContext bindingContext)
        {
            return new ValidateCommandParameters()
            {
                SourcePermissionsFile = bindingContext.ParseResult.GetValueForOption(PermissionFileOption),
                SourcePermissionsFolder = bindingContext.ParseResult.GetValueForOption(PermissionFolderOption),
                LenientMatch = bindingContext.ParseResult.GetValueForOption(LenientMatchOption),
            };
        }
    }

    internal class DocumentCommandBinder : BinderBase<DocumentCommandParameters>
    {
        public static readonly Option<string> PermissionFileOption = new(new[] { "--sourcePermissionFile", "--pf" }, "Permission File");
        public static readonly Option<string> PermissionFolderOption = new(new[] { "--sourcePermissionsFolder", "--fo" }, "Permission Folder");
        public static readonly Option<string> UrlOption = new(new[] { "--url", "-u" }, "Test Url");
        public static readonly Option<string> MethodOption = new(new[] { "--method", "-m" }, "Method");
        public static readonly Option<bool> CombineMultipleOption = new(new[] { "--combine", "-c" }, "Combine Multiple Paths");
        protected override DocumentCommandParameters GetBoundValue(BindingContext bindingContext)
        {
            return new DocumentCommandParameters()
            {
                SourcePermissionsFile = bindingContext.ParseResult.GetValueForOption(PermissionFileOption),
                SourcePermissionsFolder = bindingContext.ParseResult.GetValueForOption(PermissionFolderOption),
                Url = bindingContext.ParseResult.GetValueForOption(UrlOption),
                Method = bindingContext.ParseResult.GetValueForOption(MethodOption),
                CombineMultiple = bindingContext.ParseResult.GetValueForOption(CombineMultipleOption)
            };
        }
    }
}
