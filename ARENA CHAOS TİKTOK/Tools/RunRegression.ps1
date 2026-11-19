$ErrorActionPreference = 'Stop'
$suite = @(
    @('reliability-test','GiftReliability.txt'),
    @('power-test','PowerUps.txt'),
    @('classes-test','ClassesAndAI.txt'),
    @('round-test','RoundCycle.txt'),
    @('profile-test','ViewerProfiles.txt'),
    @('play-test','PlayMode.txt'),
    @('validate','ContentValidation.txt'),
    @('build-windows','WindowsBuild.txt')
)
$deadline = (Get-Date).AddMinutes(6)
while (Test-Path -LiteralPath 'Tools/ArenaJob.txt') {
    if ((Get-Date) -gt $deadline) { throw 'Unity did not consume the previous editor job' }
    Start-Sleep -Seconds 1
}
Start-Sleep -Seconds 3
foreach ($job in $suite) {
    $began = Get-Date
    Set-Content -LiteralPath 'Tools/ArenaJob.txt' -Value $job[0] -Encoding Ascii
    $report = Join-Path 'Tools/TestResults' $job[1]
    Write-Output ('START ' + $job[0])
    $deadline = $began.AddMinutes(8)
    do {
        Start-Sleep -Seconds 2
        if ((Get-Date) -gt $deadline) { throw ('Timed out: ' + $job[0]) }
        $ready = (Test-Path -LiteralPath $report) -and ((Get-Item -LiteralPath $report).LastWriteTime -ge $began)
    } until ($ready)
    $result = Get-Content -LiteralPath $report -Raw
    Write-Output $result
    if ($result -match 'FAILED|errors=[1-9]|errors: [1-9]') { throw ('Regression failed: ' + $job[0]) }
    Start-Sleep -Seconds 3
}
Write-Output 'REGRESSION SUITE COMPLETE'
