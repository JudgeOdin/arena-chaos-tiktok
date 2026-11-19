param([int]$Width=540,[int]$Height=960)
$ErrorActionPreference='Stop'
$gamePath=Join-Path $PSScriptRoot 'ARENA_CHAOS.exe'
if (-not (Test-Path -LiteralPath $gamePath)) { $gamePath=Join-Path $PSScriptRoot '../Builds/Windows/ARENA_CHAOS.exe' }
$gamePath=(Resolve-Path -LiteralPath $gamePath).Path
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ArenaPortraitWindow {
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int left,top,right,bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
'@
$game=Start-Process -FilePath $gamePath -WorkingDirectory (Split-Path $gamePath) -ArgumentList @('-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height) -WindowStyle Normal -PassThru
$deadline=(Get-Date).AddSeconds(30)
do {
    Start-Sleep -Milliseconds 200
    $game.Refresh()
    if($game.HasExited) { throw 'Game exited during startup' }
    if((Get-Date) -gt $deadline) { throw 'Game window not ready' }
} until ($game.MainWindowHandle -ne [IntPtr]::Zero)
Start-Sleep -Seconds 2
$outer=New-Object ArenaPortraitWindow+Rect
$client=New-Object ArenaPortraitWindow+Rect
[ArenaPortraitWindow]::GetWindowRect($game.MainWindowHandle,[ref]$outer) | Out-Null
[ArenaPortraitWindow]::GetClientRect($game.MainWindowHandle,[ref]$client) | Out-Null
$outerWidth=$Width+($outer.right-$outer.left)-($client.right-$client.left)
$outerHeight=$Height+($outer.bottom-$outer.top)-($client.bottom-$client.top)
if(-not [ArenaPortraitWindow]::SetWindowPos($game.MainWindowHandle,[IntPtr]::Zero,0,0,$outerWidth,$outerHeight,0x14)) { throw 'Window resize failed' }
