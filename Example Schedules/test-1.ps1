$ErrorActionPreference = 'Stop'
$module_name = 'LithnetMiisAutomation'

Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force
Write-Host 'Installed NuGet package provider'

Install-Module -Name $module_name -AllowClobber -Scope CurrentUser -Force
Write-Host "Installed module $module_name"

Import-Module $module_name
Write-Host "Hello World. This is test-1.ps1! $module_name module imported."