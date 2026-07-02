param(
	[string]$Version = "",
	[switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"
$project = "Bitmute/Bitmute.csproj"
$tfm = "net10.0-windows10.0.19041.0"
$rid = "win-x64"

if ($Version -eq "") {
	[xml]$projectXml = Get-Content $project
	foreach ($group in $projectXml.Project.PropertyGroup) {
		if ($group.ApplicationDisplayVersion) {
			$Version = $group.ApplicationDisplayVersion
		}
	}
	if ($Version -eq "") {
		$Version = "0.0"
	}
}

$publishArgs = @("publish", $project, "-f", $tfm, "-c", "Release", "-p:RuntimeIdentifier=$rid")
if (-not $FrameworkDependent) {
	$publishArgs += @("-p:SelfContained=true", "-p:WindowsAppSDKSelfContained=true")
}

Write-Host "Publishing Bitmute $Version ($rid, self-contained=$(-not $FrameworkDependent))..."
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
	throw "dotnet publish failed"
}

$publishDir = "Bitmute/bin/Release/$tfm/$rid/publish"
$distDir = "dist"
New-Item -ItemType Directory -Force -Path $distDir | Out-Null
$zipPath = Join-Path $distDir "Bitmute-$Version-$rid.zip"
if (Test-Path $zipPath) {
	Remove-Item $zipPath -Force
}

Write-Host "Zipping to $zipPath..."
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath
Write-Host "Done: $zipPath"
