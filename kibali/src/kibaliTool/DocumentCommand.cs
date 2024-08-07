using Kibali;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KibaliTool;

internal class DocumentCommandParameters
{
    public string SourcePermissionsFile;
    public string SourcePermissionsFolder;
    public string Url;
    public string Method;
    public bool CombineMultiple;
}

internal class DocumentCommand
{
    public static async Task<int> Execute(DocumentCommandParameters documentCommandParameters)
    {
        PermissionsDocument doc;
        if (documentCommandParameters.SourcePermissionsFile != null)
        {
            using var stream = new FileStream(documentCommandParameters.SourcePermissionsFile, FileMode.Open);
            doc = PermissionsDocument.Load(stream);
        }
        else if (documentCommandParameters.SourcePermissionsFolder != null)
        {
            doc = PermissionsDocument.LoadFromFolder(documentCommandParameters.SourcePermissionsFolder);
        }
        else
        {
            throw new ArgumentException("Please provide a source permissions file or folder");
        }

        if (documentCommandParameters.Method == null)
        {
            throw new ArgumentException("Please provide a method");
        }

        var generator = new PermissionsStubGenerator(doc, documentCommandParameters.Url, documentCommandParameters.Method, lenientMatch: true) 
        { 
            MergeMultiplePaths = documentCommandParameters.CombineMultiple 
        };
        var table = generator.GenerateTable();
        Console.WriteLine(table);

        return 0;

    }

}
