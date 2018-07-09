#
# Module manifest for module 'SS.CliMenu'
#
# Generated by: John W Carew
#
# Generated on: 06/18/2018
#
@{
	RootModule = 'SS.CliMenu.dll'
	ModuleVersion = '1.0.1807.18'
	GUID = '10439487-2aa5-49b0-bd83-99ee7e4208dd'
	Author = 'John W Carew'
	CompanyName = 'Simpler Software'
	Copyright = '(c) 2018 Simpler Software. All rights reserved.'
	Description = 'CLI menu infrastructure for PowerShell. 
	This is a major modified version of https://github.com/torgro/cliMenu with some advanced features.'
	PowerShellVersion = '5.0'
	DotNetFrameworkVersion = '4.0'
	CLRVersion = '4.0'
	FileList = @(
		'SS.CliMenu.dll',
		'SS.CliMenu.dll-Help.xml',
		'SS.CliMenu.psd1',
		'LICENSE.txt'
	)
	CmdletsToExport = @('New-CliMenu','New-CliMenuItem','Set-CliMenuOption','Show-CliMenu','Get-CliMenuOption','Write-CliMenuLine')
	FunctionsToExport = @()
	AliasesToExport = @()

	PrivateData = @{
		PSData = @{
			Tags = @('Menu','Cli','Console')
			#Prerelease = 'preview'
			ReleaseNotes = ''
			LicenseUri = 'https://github.com/Simpler-Software/SS.CliMenu/blob/master/LICENSE.md'
			ProjectUri = 'https://github.com/Simpler-Software/SS.CliMenu'
			# IconUri = ''
		} 
	}
}

