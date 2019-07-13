[cmdletbinding()]
param(
	[Parameter(Mandatory)]
	$Version,
	[Parameter(Mandatory)]
	$Branch,
	[Parameter(Mandatory)]
	$ManifestPath,
	$Prerelease = $null
)
$ModuleManifest = Get-Content $ManifestPath -Raw
$rev = $null
if ($ModuleManifest -match "(ModuleVersion\s*=)\s*'(.*)'"){
	Write-Verbose "ModuleVersion matched"
	if ($Matches[2] -match "(?<Major>0|(?:[1-9]\d*))(?:\.(?<Minor>0|(?:[1-9]\d*))(?:\.(?<Patch>0|(?:[1-9]\d*)))?(?:\-(?<PreRelease>[0-9A-Z\.-]+))?(?:\+(?<Meta>[0-9A-Z\.-]+))?)?"){
		Write-Verbose "SymVer matched"
		$aVersion = $Version -split "\."
		$rev = $aVersion[1]
		$Version = "$($Matches.Major).$($Matches.Minor).$($aVersion[0])"
		$PSModuleVersion = $Version
	}
}
if (!$Prerelease -and $Branch -inotlike 'refs/heads/release*'){
	$Prerelease = "preview$rev"
	$PSModuleVersion += "-$Prerelease"
}
Write-Host "Updating module manifest with version $Version $Prerelease"
$date = Get-Date -Format "M/d/yyyy h:mm:ss tt"
$ModuleManifest = $ModuleManifest -replace "(ModuleVersion\s*=)\s*'(.*)'", "`$1 '$Version'"
$ModuleManifest = $ModuleManifest -replace "(Generated on:)\s*(.*\s(?:AM|PM))", "`$1 $date"
if ($Prerelease){
	$ModuleManifest = $ModuleManifest -replace "(?:#\s*)?(Prerelease\s*=)\s*'(.*)'", "`$1 '$Prerelease'"
}
$ModuleManifest = $ModuleManifest -replace "[\s\t\r\n]*$", "" # Fix extra lines at EOF
$ModuleManifest | Out-File -LiteralPath $ManifestPath