# Builds the plugin and copies it (plus the key images) into the game. Run while the game is NOT running.
$ErrorActionPreference = "Stop"
if (Get-Process | Where-Object { $_.ProcessName -like "Harvest Moon*" }) {
    Write-Host "Close the game first (the plugin DLL is locked while it runs)." -ForegroundColor Yellow
    exit 1
}
Set-Location "$PSScriptRoot\..\src\WoaKbm"
dotnet build -c Release --nologo -v minimal
