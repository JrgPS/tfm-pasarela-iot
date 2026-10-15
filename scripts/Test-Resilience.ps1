param([ValidateSet('gateway','registry')][string]$Service='gateway',
      [ValidateSet('abrupt','graceful')][string]$Mode='abrupt')
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$docker=(Get-Command docker -ErrorAction SilentlyContinue).Source
if (!$docker) { $docker="$env:LOCALAPPDATA/Programs/DockerDesktop/resources/bin/docker.exe" }
if (!(Test-Path -LiteralPath $docker)) { throw 'Docker CLI not found.' }
function Run-Docker([string[]]$Arguments) {
    $value=& $docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker command failed: $($Arguments[0])" }
    return $value
}
$replicas=@(Run-Docker -Arguments @('compose','ps','-q',$Service))
if ($replicas.Count -lt 2) { throw 'This test requires two replicas of the target service.' }
$target=$replicas[0]
$name='iot-tfm-test-'+$Service+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$relativeReport='results/resilience-'+$Service+'-'+$Mode+'.json'
$startUtc=[DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss.ffffffZ')
$interrupted=$false
$simulator=$null
try {
    $simulator=Run-Docker -Arguments @('compose','run','-d','--no-deps','--name',$name,'simulator',
        '--host','proxy','--devices','100','--operations','100','--ramp-ms','2','--interval-ms','100',
        '--synchronize-start','true','--output',$relativeReport)
    $watch=[Diagnostics.Stopwatch]::StartNew()
    do {
        $active=Run-Docker -Arguments @('compose','exec','-T','db','psql','-U','iot','-d','iot','-tAc',
            "SELECT count(*) FROM connections WHERE opened_at >= '$startUtc' AND closed_at IS NULL")
        if ([int]$active -ge 100) { break }
        if ($watch.Elapsed.TotalSeconds -gt 30) { throw 'Clients did not become ready in time.' }
        Start-Sleep -Milliseconds 200
    } while ($true)
    Start-Sleep -Seconds 1
    $distribution=Run-Docker -Arguments @('compose','exec','-T','db','psql','-U','iot','-d','iot','-tAc',
        "SELECT gateway || ':' || count(*) FROM connections WHERE opened_at >= '$startUtc' GROUP BY gateway ORDER BY gateway")
    if ($Mode -eq 'graceful') {
        Run-Docker -Arguments @('stop','--time','25',$target) | Out-Null
    } else {
        Run-Docker -Arguments @('kill','--signal','SIGKILL',$target) | Out-Null
    }
    $interrupted=$true
    Start-Sleep -Seconds 2
    Run-Docker -Arguments @('start',$target) | Out-Null
    $interrupted=$false
    do {
        $running=Run-Docker -Arguments @('inspect','--format','{{.State.Running}}',$name)
        if ($running -eq 'false') { break }
        if ($watch.Elapsed.TotalSeconds -gt 120) { throw 'Simulator did not finish.' }
        Start-Sleep -Milliseconds 500
    } while ($true)
    $exitCode=Run-Docker -Arguments @('inspect','--format','{{.State.ExitCode}}',$name)
    $result=Get-Content -LiteralPath $relativeReport -Raw | ConvertFrom-Json
    $evidence=[ordered]@{
        scenario="$Mode loss and restart of one $Service replica"
        startUtc=$startUtc
        targetContainer=$target
        connectionsBeforeFault=[int]$active
        gatewayDistributionBeforeFault=@($distribution)
        simulatorExitCode=[int]$exitCode
        result=$result
    }
    $evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath "results/fault-$Service-$Mode.json" -Encoding utf8
    if ([int]$exitCode -ne 0 -or $result.failedDevices -ne 0 -or $result.confirmedOperations -ne 10000) {
        throw 'Resilience verification failed. Inspect the saved report.'
    }
    $evidence | ConvertTo-Json -Depth 8
}
finally {
    if ($interrupted) { & $docker start $target | Out-Null }
    if ($simulator) { & $docker stop --time 1 $name 2>$null | Out-Null; & $docker rm $name | Out-Null }
}
