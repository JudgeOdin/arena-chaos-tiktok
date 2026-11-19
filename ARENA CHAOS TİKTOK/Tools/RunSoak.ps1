param([int]$Seconds=1800)
$ErrorActionPreference = 'Stop'
Add-Type @'
using System.Runtime.InteropServices;
public static class ArenaSoakPower {
    [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags);
}
'@
[ArenaSoakPower]::SetThreadExecutionState([uint32]2147483651) | Out-Null
try {
$gamePath = Join-Path (Get-Location) 'Builds/Windows/ARENA_CHAOS.exe'
$arguments = @('-arenaSoakTest','-arenaSoakSeconds',$Seconds,'-arenaFighters','50','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile','Builds/TestResults/Player_Soak.log')
$game = Start-Process -FilePath $gamePath -WorkingDirectory (Get-Location).Path -ArgumentList $arguments -WindowStyle Normal -PassThru
$began=Get-Date
$deadline=$began.AddSeconds($Seconds+120)
$nextSample=Get-Date
$samples=New-Object System.Collections.Generic.List[object]
Write-Output "SOAK START PID=$($game.Id), seconds=$Seconds"
while (-not $game.HasExited) {
    if ((Get-Date) -gt $deadline) { Stop-Process -Id $game.Id; throw 'Soak player timed out' }
    if ((Get-Date) -ge $nextSample) {
        $nextSample=(Get-Date).AddSeconds(30)
        $game.Refresh()
        $gpu='unavailable'
        if (Get-Command nvidia-smi -ErrorAction SilentlyContinue) {
            $gpu=(& nvidia-smi --query-gpu=memory.used,utilization.gpu,temperature.gpu --format=csv,noheader,nounits) -join ';'
        }
        $row=[pscustomobject]@{ElapsedSeconds=[math]::Round(((Get-Date)-$began).TotalSeconds,1);PrivateMB=[math]::Round($game.PrivateMemorySize64/1MB,1);WorkingSetMB=[math]::Round($game.WorkingSet64/1MB,1);WholeGpuUsedMiB_UtilizationPercent_TemperatureC=$gpu}
        $samples.Add($row)
        $samples | Export-Csv -LiteralPath 'Builds/TestResults/SoakTelemetry.csv' -NoTypeInformation -Encoding UTF8
        Write-Output ($row | ConvertTo-Json -Compress)
    }
    Start-Sleep -Seconds 1
    $game.Refresh()
}
$game.WaitForExit()
Get-Content -LiteralPath 'Builds/TestResults/SoakTest.txt'
if ($game.ExitCode -ne 0) { throw "Soak failed: exit $($game.ExitCode)" }
Write-Output 'SOAK COMPLETE. GPU samples cover the whole adapter, including other programs; not per-game VRAM.'
} finally {
    [ArenaSoakPower]::SetThreadExecutionState([uint32]2147483648) | Out-Null
}
