using Kibali;
using oauthpermissions;
using System.IO;
using System.Threading.Tasks;

namespace KibaliTool
{
    internal class ExportCommand
    {
        public async Task<int> Execute(string sourcePermissionsFile, string outFile)
        {
            using var sourceFileStream = new FileStream(sourcePermissionsFile, FileMode.Open);
            var doc = PermissionsDocument.Load(sourceFileStream);
            CsdlExporter.Export(outFile, doc);
            return 0;
        }

    }
}
