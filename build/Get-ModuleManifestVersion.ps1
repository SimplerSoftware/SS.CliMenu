[cmdletbinding()]
param(
	[Parameter(Mandatory)]
	$ManifestPath,
	[Switch] $IncrementRev,
	[Switch] $AsPrerelease,
	[switch] $PatchAsYYMM
)
$symVer2Regex = "(?<Major>0|(?:[1-9]\d*))(?:\.(?<Minor>0|(?:[1-9]\d*))(?:\.(?<Patch>0|(?:[1-9]\d*)))?(?:\-(?<PreRelease>[0-9A-Z\.-]+))?(?:\+(?<Meta>[0-9A-Z\.-]+))?)?"
$PSModuleVersionOrig = $null
$PSModuleVersion = $null
$Prerelease = $null
$Meta = $null
$rev = $null
$ModuleManifest = Get-Content $ManifestPath -Raw
if ($ModuleManifest -match "(ModuleVersion\s*=)\s*'(.*)'"){
	Write-Verbose "ModuleVersion matched: [$($Matches[0])]"
	$PSModuleVersionOrig = $Matches[2]
	if ($Matches[2] -match $symVer2Regex){
		Write-Verbose "SymVer matched: [$($Matches[0])]"
		$Version = $Matches.Major
		if ($Matches.Minor){$Version += "{0:\.0;\.#;\.0}" -f (Invoke-Expression $Matches.Minor)}else{$Version += ".0"}
		if($PatchAsYYMM){$Version += ".$(Get-Date -Format yyMM)"}elseif($Matches.Patch){$Version += "{0:\.0;\.#;\.0}" -f (Invoke-Expression $Matches.Patch)}else{$Version += ".0"}
		$Prerelease = $Matches.PreRelease
		$Meta = $Matches.Meta
		if (!$Prerelease -and $ModuleManifest -match "(?<Mark>#?)\s?(Prerelease\s*=)\s*'(?<PreRelease>.*)'"){
			if (!$Matches.Mark -or ($Matches.Mark -and $AsPrerelease)){
				Write-Verbose "Prerelease matched: [$($Matches.PreRelease)]"
				$Prerelease = $Matches.PreRelease
				$PrereleaseOrig = "-$Prerelease"
			}
		}
		if (!$Meta -and $Prerelease -and "$Version-$Prerelease" -match $symVer2Regex -and $Matches.Meta){
			Write-Verbose "Meta matched: [$($Matches.Meta)]"
			$Prerelease = $Matches.PreRelease
			$Meta = $Matches.Meta
		}
		if ($IncrementRev -and $Prerelease -match "(?<Name>[0-9A-Z\.-]+)(?<Rev>\d+)$"){
			Write-Verbose "Revision matched: [$($Matches[0])]"
			$Name = $Matches.Name
			$rev = Invoke-Expression $Matches.Rev
			$Prerelease = "$Name$((++$rev))"
		}
		if ($AsPrerelease -and !$rev){
			$Prerelease += "1"
		}
		$PSModuleVersion = $Version
		$env:Version = $Version
	}
}
if ($Prerelease){
	$PSModuleVersion += "-$Prerelease"
	$env:Prerelease = "$Prerelease"
}
if ($Meta){
	$PSModuleVersion += "+$Meta"
	$env:Prerelease = "+$Meta"
}
$env:PSModuleVersion = $PSModuleVersion
Write-Verbose "PSModuleVersion: $PSModuleVersionOrig$PrereleaseOrig -> $PSModuleVersion"
