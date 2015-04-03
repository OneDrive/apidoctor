msbuild ../MarkdownScanner.sln /p:Configuration=Debug
nuget pack ../apidocs.nuspec
nuget push *.nupkg -ApiKey h6e37a0g5y1a052yo3mnuauq -Source https://ci.appveyor.com/nuget/rgregg-dju7gcli4m8k/api/v2/package
del *.nupkg