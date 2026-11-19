$ErrorActionPreference='Stop'
$checks=New-Object System.Collections.Generic.List[object]
$reports=@('GiftReliability','PowerUps','ClassesAndAI','RoundCycle','ViewerProfiles','PlayMode','AvatarDownloadCache','LiveTransport')
foreach($name in $reports){
    $path="Tools/TestResults/$name.txt"
    $content=Get-Content -LiteralPath $path -Raw
    if(-not $content.StartsWith('PASS')){throw "Acceptance check failed: $name"}
    $checks.Add([pscustomobject]@{Name=$name;Result='PASS';Report=$path;Updated=(Get-Item -LiteralPath $path).LastWriteTime.ToString('s')})
}
$buildPath='Tools/TestResults/WindowsBuild.txt'
$build=Get-Content -LiteralPath $buildPath -Raw
if($build -notmatch 'Succeeded; errors=0; warnings=(\d+)'){throw 'Windows build failed'}
$warnings=[int]$Matches[1]
$buildTime=(Get-Item -LiteralPath $buildPath).LastWriteTime
foreach($name in @('10_1920x1080','20_1920x1080','50_1920x1080','20_540x960')){
    $path="Builds/TestResults/Standalone_$name.txt"
    $content=Get-Content -LiteralPath $path -Raw
    if(-not $content.StartsWith('PASS') -or (Get-Item -LiteralPath $path).LastWriteTime -lt $buildTime){throw "Final player smoke check missing or stale: $name"}
    $checks.Add([pscustomobject]@{Name="Player_$name";Result='PASS';Report=$path;Updated=(Get-Item -LiteralPath $path).LastWriteTime.ToString('s')})
}
$soak=Get-Content -LiteralPath 'Builds/TestResults/SoakTest.txt' -Raw
if(-not $soak.StartsWith('PASS') -or $soak -notmatch 'ElapsedSeconds=(1800[,.]0)'){throw 'Full 1800-second soak missing'}
$longHash=(Get-Content -LiteralPath 'Tools/TestResults/TestedPlayerSHA256.txt' -Raw).Trim()
$finalHash=(Get-FileHash -LiteralPath 'Builds/Windows/ARENA_CHAOS_Data/Managed/Assembly-CSharp.dll' -Algorithm SHA256).Hash
$latestRuntimeEdit=(Get-ChildItem -LiteralPath Assets -Filter '*.cs' -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1).LastWriteTime
if($buildTime -lt $latestRuntimeEdit){throw 'Player build predates a runtime source edit'}
$data=[ordered]@{
    Status='Local checks passed; actual TikTok LIVE acceptance pending'
    Generated=(Get-Date).ToString('s')
    BuildResult=$build.Trim()
    BuildWarnings=$warnings
    Checks=$checks
    SoakReport='Builds/TestResults/SoakTest.txt'
    SoakPlayerAssemblySHA256=$longHash
    FinalPlayerAssemblySHA256=$finalHash
    SamePlayerBinaryForSoakAndFinal=($longHash -eq $finalHash)
    ValidationScope='The 30-minute run preceded the final sender-death meteor fix, own-viewer score guard and LIVE configuration draft isolation. Meteor and scoring received dedicated Editor checks; the settings draft change was compiled and reviewed. The rebuilt final player received 10/20/50 landscape and 20 portrait smoke tests. The final binary was not rerun for another 30 minutes.'
    ExternalAcceptanceRequired=@('Broadcast username and connection provider','Actual gift IDs and account gift/combo/avatar delivery','LIVE Studio or OBS capture, audio and simultaneous load','1080x1920 portrait on a compatible display/output; 540x960 was validated here')
}
$data | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath 'Tools/TestResults/FinalAcceptance.json' -Encoding UTF8
Write-Output ('ACCEPTANCE READY: '+$checks.Count+' checks; build errors=0; warnings='+$warnings)
