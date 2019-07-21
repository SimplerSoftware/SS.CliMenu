[cmdletbinding()]
param(
	[Parameter(Mandatory)]
	[string] $Version,
	[Parameter(Mandatory)]
	[string] $Branch,
	[Parameter(Mandatory)]
	[string] $ManifestPath,
	[string] $Prerelease = $null,
	[string] $ReleaseBranch = 'refs/heads/release'
)
$ModuleManifest = Get-Content $ManifestPath -Raw
$rev = $null
$sVersion
if ($ModuleManifest -match "(ModuleVersion\s*=)\s*'(.*)'"){
	Write-Verbose "ModuleVersion matched [$($Matches[2])]"
	if ($Matches[2] -match "(?<Major>0|(?:[1-9]\d*))(?:\.(?<Minor>0|(?:[1-9]\d*))(?:\.(?<Patch>0|(?:[1-9]\d*)))?(?:\-(?<PreRelease>[0-9A-Z\.-]+))?(?:\+(?<Meta>[0-9A-Z\.-]+))?)?"){
		Write-Verbose "SymVer matched [$($Matches[0])]"
		$aVersion = $Version -split "\." | select -Last 2
		$rev = $aVersion[1]
		$sVersion = "$($Matches.Major).$($Matches.Minor).$($aVersion[0])"
		$PSModuleVersion = $sVersion
	}
}
if (!$Prerelease -and $Branch -inotlike "$ReleaseBranch*"){
	$Prerelease = "preview$rev"
	$PSModuleVersion += "-$Prerelease"
}
$date = Get-Date -Format "M/d/yyyy h:mm:ss tt"
$ModuleManifest = $ModuleManifest -replace "(ModuleVersion\s*=)\s*'(.*)'", "`$1 '$sVersion'"
$ModuleManifest = $ModuleManifest -replace "(Generated on:)\s*(.*\s(?:AM|PM))", "`$1 $date"
if ($Prerelease){
	Write-Host "Updating module manifest with pre-release version $sVersion-$Prerelease"
	$ModuleManifest = $ModuleManifest -replace "(?:#\s*)?(Prerelease\s*=)\s*'(.*)'", "`$1 '$Prerelease'"
} else {
	Write-Host "Updating module manifest with release version $sVersion"
	$ModuleManifest = $ModuleManifest -replace "(?:#\s*)?(Prerelease\s*=)\s*'(.*)'", "#`$1 '`$2'"
}
$ModuleManifest = $ModuleManifest -replace "[\s\t\r\n]*$", "" # Fix extra lines at EOF
$ModuleManifest | Out-File -LiteralPath $ManifestPath
#Write-Host "##vso[build.updatebuildnumber]$PSModuleVersion"
Write-Host "##vso[task.setvariable variable=PSModuleVersion]$PSModuleVersion"
