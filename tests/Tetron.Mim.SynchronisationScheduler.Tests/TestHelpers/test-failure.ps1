# Test PowerShell script that fails
Write-Host "PowerShell test script: Failure"
Write-Error "Error from test script"
throw "Script failed intentionally"
exit 1
