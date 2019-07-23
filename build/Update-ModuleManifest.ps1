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
Write-Verbose "Version: $Version"
Write-Verbose "Branch: $Branch"
Write-Verbose "ManifestPath: $ManifestPath"
Write-Verbose "Prerelease: $Prerelease"
Write-Verbose "ReleaseBranch: $ReleaseBranch"

$ModuleManifest = Get-Content $ManifestPath -Raw
$Env:Rev = $null
$Env:Version = $null
if ($ModuleManifest -match "(ModuleVersion\s*=)\s*'(.*)'"){
	Write-Verbose "ModuleVersion matched [$($Matches[2])]"
	if ($Matches[2] -match "(?<Major>0|(?:[1-9]\d*))(?:\.(?<Minor>0|(?:[1-9]\d*))(?:\.(?<Patch>0|(?:[1-9]\d*)))?(?:\-(?<PreRelease>[0-9A-Z\.-]+))?(?:\+(?<Meta>[0-9A-Z\.-]+))?)?"){
		Write-Verbose "SymVer matched [$($Matches[0])]"
		$aVersion = $Version -split "\." | select -Last 2
		$Patch = $aVersion[0]
		$Env:Rev = $aVersion[1]
		if ($Matches.Patch -gt $Patch){
			Write-Verbose "Future release found [$($Matches.Patch)], setting as preview release."
			# If patch # in manifest is manually set to a future YYMM(greater than what is past in at build time)
			# Then, keep existing manifest patch #, and apply this as a preview release
			$Patch = $Matches.Patch
			$Prerelease = "preview$Env:Rev"
		}
		$Env:Version = "$($Matches.Major).$($Matches.Minor).$Patch"
		$env:PSModuleVersion = $Env:Version
	}
}
if ($Prerelease){
	$Env:Prerelease = $Prerelease
	$env:PSModuleVersion += "-$Env:Prerelease"
} elseif (!$Prerelease -and $Branch -inotlike "$ReleaseBranch*"){
	$Env:Prerelease = "preview$Env:Rev"
	$env:PSModuleVersion += "-$Env:Prerelease"
}
$date = Get-Date -Format "M/d/yyyy h:mm:ss tt"
$ModuleManifest = $ModuleManifest -replace "(ModuleVersion\s*=)\s*'(.*)'", "`$1 '$Env:Version'"
$ModuleManifest = $ModuleManifest -replace "(Generated on:)\s*(.*\s(?:AM|PM))", "`$1 $date"
if ($Env:Prerelease){
	Write-Host "Updating module manifest with pre-release version $Env:Version-$Env:Prerelease"
	$ModuleManifest = $ModuleManifest -replace "(?:#\s*)?(Prerelease\s*=)\s*'(.*)'", "`$1 '$Env:Prerelease'"
} else {
	Write-Host "Updating module manifest with release version $Env:Version"
	$ModuleManifest = $ModuleManifest -replace "(?:#\s*)?(Prerelease\s*=)\s*'(.*)'", "#`$1 '`$2'"
}
$ModuleManifest = $ModuleManifest -replace "[\s\t\r\n]*$", "" # Fix extra lines at EOF
$ModuleManifest | Out-File -LiteralPath $ManifestPath
#Write-Host "##vso[build.updatebuildnumber]$env:PSModuleVersion"
Write-Host "##vso[task.setvariable variable=Version]$env:Version"
Write-Host "##vso[task.setvariable variable=Rev]$env:Rev"
Write-Host "##vso[task.setvariable variable=Prerelease]$env:Prerelease"
Write-Host "##vso[task.setvariable variable=PSModuleVersion]$env:PSModuleVersion"
