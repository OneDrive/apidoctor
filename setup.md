## Setup of API Doctor

API Doctor is available on Nuget https://www.nuget.org/packages/ApiDoctor/. Once installed you can run commands against it as outlined in the [README.md](/readme.md) file.

### Using API Doctor with Azure Pipelines

[Azure Pipelines](https://azure.microsoft.com/en-us/services/devops/pipelines/) offers cloud-hosted pipelines for Linux, macOS, and Windows with 10 free parallel jobs and unlimited minutes for open source projects.

A GitHub app exists which can be installed from the GitHub MarketPlace. Integrate a GitHub project with an Azure DevOps pipeline and track pull requests through the pipeline. For more information, please go to https://www.azuredevopslabs.com/labs/azuredevops/github-integration/

#### Azure Build Pipelines
The build pipeline is defined as YAML, a markup syntax well-suited to defining processes like this because it allows you to manage the configuration of the pipeline like any other file in the repo.

A sample YAML file would look like:
```
# .NET Desktop
# Build and run tests for .NET Desktop or Windows classic desktop solutions.
# Add steps that publish symbols, save build artifacts, and more:
# https://docs.microsoft.com/azure/devops/pipelines/apps/windows/dot-net

trigger:
- master

pool:
  vmImage: 'VS2017-Win2016'
  
steps:
- task: Powershell@2
  inputs:
    targetType: filePath
    filePath: .\Test-Docs.ps1

```
The file `.\Test-Docs.ps1` in the above snippet will be in your root project folder with commands to run API Doctor against your documentation. Below is a code sample of the file.

```
Param(
    [switch]$cleanUp,
    [string]$file
)
$repoPath = (Get-Location).Path
$downloadedApiDoctor = $false
$downloadedNuGet = $false

Write-Host "Repository location: ", $repoPath

# Check for ApiDoctor in path
$apidoc = $null
if (Get-Command "apidoc.exe" -ErrorAction SilentlyContinue) {
    $apidoc = (Get-Command "apidoc.exe").Source
} else {
    $nugetPath = $null
    if (Get-Command "nuget.exe" -ErrorAction SilentlyContinue) {
        # Use the existing nuget.exe from the path
        $nugetPath = (Get-Command "nuget.exe").Source
    }
    else
    {
        # Download nuget.exe from the nuget server if required
        $nugetPath = Join-Path $repoPath -ChildPath "nuget.exe"
        $nugetExists = Test-Path $nugetPath
        if ($nugetExists -eq $false) {
            Write-Host "nuget.exe not found. Downloading from dist.nuget.org"
            Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nugetPath
        }
        $downloadedNuGet = $true
    }

    $packagesPath = Join-Path $repoPath -ChildPath "apidoctor"
    $result = New-Item -ItemType Directory -Force -Path $packagesPath

    # install apidoctor from nuget
    Write-Host "Running nuget.exe from ", $nugetPath
    $nugetParams = "install", "ApiDoctor", "-OutputDirectory", $packagesPath, "-NonInteractive", "-DisableParallelProcessing"
    & $nugetPath $nugetParams

    if ($LastExitCode -ne 0) { 
        # nuget error, so we can't proceed
        Write-Host "Error installing Api Doctor from NuGet. Aborting."
        Remove-Item $nugetPath
        exit $LastExitCode
    }

    # get the path to the Api Doctor exe
    $pkgfolder = Get-ChildItem -LiteralPath $packagesPath -Directory | Where-Object {$_.name -match "ApiDoctor"}
    $apidoc = [System.IO.Path]::Combine($packagesPath, $pkgfolder.Name, "tools\apidoc.exe")
    $downloadedApiDoctor = $true
}

$lastResultCode = 0

# run validation at the root of the repository
$appVeyorUrl = $env:APPVEYOR_API_URL
$gitHubToken = $env: GITHUB_TOKEN
$pullRequstNumber =  $env: SYSTEM_PULLREQUEST_PULLREQUESTNUMBER
$gitPath = "./git.exe"

$parms = "check-all", "--path", $repoPath, "--pull", $pullRequestNumber, "--github-token", $gitHubtoken, "--git-path", $gitPath
if ($appVeyorUrl -ne $null)
{
    $parms = $parms += "--appveyor-url", $appVeyorUrl
}

& $apidoc $parms

if ($LastExitCode -ne 0) { 
    $lastResultCode = $LastExitCode
}

# Clean up the stuff we downloaded
if ($cleanUp -eq $true) {
    if ($downloadedNuGet -eq $true) {
        Remove-Item $nugetPath 
    }
    if ($downloadedApiDoctor -eq $true) {
        Remove-Item $packagesPath -Recurse
    }
}

if ($lastResultCode -ne 0) {
    Write-Host "Errors were detected. This build failed."
    exit $lastResultCode
}

```
