param([ValidateSet('power-test','reliability-test')][string]$FocusedTest='power-test')
$ErrorActionPreference='Stop'
function Invoke-EditorJob([string]$Command,[string]$Report,[int]$Timeout=480) {
    $began=Get-Date
    Set-Content -LiteralPath 'Tools/ArenaJob.txt' -Value $Command -Encoding Ascii
    Write-Output ('START '+$Command)
    $deadline=$began.AddSeconds($Timeout)
    do {
        Start-Sleep -Seconds 2
        if((Get-Date) -gt $deadline){throw ('Editor job timed out: '+$Command)}
    } until ((Test-Path -LiteralPath $Report) -and (Get-Item -LiteralPath $Report).LastWriteTime -ge $began)
    $text=Get-Content -LiteralPath $Report -Raw
    Write-Output $text
    if($text -match 'FAILED|errors=[1-9]'){throw ('Editor job failed: '+$Command)}
    Start-Sleep -Seconds 3
}
$deadline=(Get-Date).AddMinutes(6)
do {
    $editor=Get-Process -Id 24292
    $busy=$editor.MainWindowTitle -match 'Compiling|Reload|Hold on|Importing'
    if((Get-Date) -gt $deadline){throw 'Editor did not finish compilation'}
    Start-Sleep -Seconds 2
} while($busy)
Start-Sleep -Seconds 5
Invoke-EditorJob 'connect-mcp' 'Tools/TestResults/EditorJob.txt'
$focusedReport=if($FocusedTest -eq 'reliability-test'){'Tools/TestResults/GiftReliability.txt'}else{'Tools/TestResults/PowerUps.txt'}
Invoke-EditorJob $FocusedTest $focusedReport
Write-Output 'Waiting for the independent 30-minute soak player to finish before rebuilding its files'
while(Get-Process -Id 30048 -ErrorAction SilentlyContinue){Start-Sleep -Seconds 2}
$soak=Get-Content -LiteralPath 'Builds/TestResults/SoakTest.txt' -Raw
Write-Output $soak
if(-not $soak.StartsWith('PASS')){throw 'Long soak test did not pass'}
Invoke-EditorJob 'build-windows' 'Tools/TestResults/WindowsBuild.txt'
(Get-FileHash -LiteralPath 'Builds/Windows/ARENA_CHAOS_Data/Managed/Assembly-CSharp.dll' -Algorithm SHA256).Hash | Set-Content -LiteralPath 'Tools/TestResults/FinalPlayerSHA256.txt' -Encoding Ascii
& "$PSScriptRoot/RunPlayers.ps1"
Copy-Item -LiteralPath 'Tools/StartPortrait.ps1' -Destination 'Builds/Windows/StartPortrait.ps1'
Write-Output 'FINAL CYCLE COMPLETE'
