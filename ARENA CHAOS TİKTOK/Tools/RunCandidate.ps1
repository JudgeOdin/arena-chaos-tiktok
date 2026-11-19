$ErrorActionPreference='Stop'
$queued=[datetime]::Parse((Get-Content -LiteralPath 'Tools/TestResults/EditorJob.txt' -First 1))
$deadline=(Get-Date).AddMinutes(8)
while((Get-Item -LiteralPath 'Tools/TestResults/WindowsBuild.txt').LastWriteTime -lt $queued){
    if((Get-Date) -gt $deadline){throw 'Final Windows build timed out'}
    Start-Sleep -Seconds 2
}
$build=Get-Content -LiteralPath 'Tools/TestResults/WindowsBuild.txt' -Raw
Write-Output $build
if($build -notmatch 'Succeeded; errors=0;'){throw 'Final Windows build failed'}
& "$PSScriptRoot/RunPlayers.ps1" -PortraitOnly
& "$PSScriptRoot/RunSoak.ps1" -Seconds 1800
