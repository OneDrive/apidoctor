Param(
    [switch]$cleanUp,
    [Parameter(Mandatory)]
	[string]$apiDoctorPath,
    [Parameter(Mandatory)]
	[string]$docSubPath,
    [Parameter(Mandatory)]
	[string]$graphDocsRepo,
    [Parameter(Mandatory)]
	[string]$graphDocsBranch,
    [Parameter(Mandatory)]
	[string]$graphDocsPath
)
$repoPath = (Get-Location).Path
$fullDocsPath = Join-Path $repoPath $graphDocsPath
$fullDocsSubPath =  Join-Path $fullDocsPath $docSubPath
$params ="check-all", "--path", "$fullDocsSubPath", "--ignore-warnings"
$apiDoctor = Join-Path $repoPath $apiDoctorPath

Write-Host $repoPath
Write-Host $docSubPath
Write-Host $fullDocsPath
Write-Host $fullDocsSubPath
Write-Host $params
Write-Host $apiDoctor

#Clone Docs Repo
New-Item -Path $fullDocsPath -ItemType Directory
Write-Host "Cloning Microsoft Graph Docs from Github"
Write-Host "`tRemote URL: $graphDocsRepo"
Write-Host "`tBranch: $graphDocsBranch"
Invoke-Expression "git clone -b $graphDocsBranch $graphDocsRepo --recurse-submodules $fullDocsPath"

& $apiDoctor $params

# Clean up the stuff we downloaded
if ($cleanUp -eq $true) {
   Remove-Item $graphDocsPath -Recurse -Force
}


if ($LastExitCode -ne 0) {
    Write-Host "Errors were detected. This build failed."
    Write-Host $LastExitCode
    exit $LastExitCode
}