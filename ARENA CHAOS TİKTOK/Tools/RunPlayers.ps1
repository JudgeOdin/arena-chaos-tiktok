param([switch]$PortraitOnly,[switch]$FullSizePortrait)
$ErrorActionPreference = 'Stop'
$gamePath = Join-Path (Get-Location) 'Builds/Windows/ARENA_CHAOS.exe'
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ArenaPlayerWindow {
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int left,top,right,bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
'@
$portrait=if($FullSizePortrait){@(20,1080,1920)}else{@(20,540,960)}
$runs = @(@(10,1920,1080),@(20,1920,1080),@(50,1920,1080),$portrait)
if($PortraitOnly){$runs=,$portrait}
foreach ($run in $runs) {
    $name = "$($run[0])_$($run[1])x$($run[2])"
    $arguments = @('-arenaSmokeTest','-arenaFighters',$run[0],'-screen-fullscreen','0','-screen-width',$run[1],'-screen-height',$run[2],'-logFile',"Builds/TestResults/Player_$name.log")
    Write-Output "START player $name"
    $game = Start-Process -FilePath $gamePath -WorkingDirectory (Get-Location).Path -ArgumentList $arguments -WindowStyle Normal -PassThru
    if ($run[2] -gt $run[1]) {
        Start-Sleep -Seconds 2
        $game.Refresh()
        $outer=New-Object ArenaPlayerWindow+Rect
        $client=New-Object ArenaPlayerWindow+Rect
        [ArenaPlayerWindow]::GetWindowRect($game.MainWindowHandle,[ref]$outer) | Out-Null
        [ArenaPlayerWindow]::GetClientRect($game.MainWindowHandle,[ref]$client) | Out-Null
        $width=$run[1]+($outer.right-$outer.left)-($client.right-$client.left)
        $height=$run[2]+($outer.bottom-$outer.top)-($client.bottom-$client.top)
        [ArenaPlayerWindow]::SetWindowPos($game.MainWindowHandle,[IntPtr]::Zero,0,0,$width,$height,0x14) | Out-Null
    }
    $deadline = (Get-Date).AddSeconds(90)
    while (-not $game.HasExited) {
        Start-Sleep -Seconds 1
        if ((Get-Date) -gt $deadline) { Stop-Process -Id $game.Id; throw "Player timed out: $name" }
        $game.Refresh()
    }
    $game.WaitForExit()
    if ($game.ExitCode -ne 0) { throw "Player failed: $name / $($game.ExitCode)" }
    $report = "Builds/TestResults/Standalone_$($run[0]).txt"
    $text = Get-Content -LiteralPath $report -Raw
    Write-Output $text
    if (-not $text.StartsWith('PASS')) { throw "Player report failed: $name" }
    if ($text -notmatch "Resolution=$($run[1])x$($run[2])") { throw "Requested render resolution was not achieved: $name" }
    Copy-Item -LiteralPath $report -Destination "Builds/TestResults/Standalone_$name.txt"
    Copy-Item -LiteralPath "Builds/TestResults/Standalone_$($run[0]).png" -Destination "Builds/TestResults/Standalone_$name.png"
}
Write-Output 'PLAYER SUITE COMPLETE'
