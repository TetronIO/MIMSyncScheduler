$ErrorActionPreference = 'Stop'
$module_name = 'LithnetRMA'
$module_name = 'C:\WINDOWS\system32\WindowsPowerShell\v1.0\Modules\LithnetRMA\LithnetRMA.psd1'
$module_name = 'TLS'
$module_name = 'AppX'
$module_name = 'LAPS'
$module_name = 'DISM'
Import-Module $module_name
Write-Host "Hello World. This is test-1.ps1! $module_name module imported."