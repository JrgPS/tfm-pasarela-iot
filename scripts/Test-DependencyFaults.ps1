param([ValidateSet('database','overload')][string]$Scenario='database')
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$docker="$env:LOCALAPPDATA/Programs/DockerDesktop/resources/bin/docker.exe"
if (Get-Command docker -ErrorAction SilentlyContinue) { $docker=(Get-Command docker).Source }
$env:PATH=(Split-Path $docker)+[IO.Path]::PathSeparator+$env:PATH
function Docker([string[]]$Arguments) {
    $result=& $docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker failed: $($Arguments[0])" }
    return $result
}
$id=[Guid]::NewGuid().ToString('N').Substring(0,8)
$name="iot-tfm-probe-$id"
$prefix="results/probe-$Scenario-$id"
$override="tmp/fault-$id.yaml"
$dbStopped=$false
$probeStarted=$false
function Wait-File([string]$Suffix) {
    $watch=[Diagnostics.Stopwatch]::StartNew()
    while (!(Test-Path -LiteralPath ($prefix+$Suffix))) {
        if ((Docker @('inspect','--format','{{.State.Running}}',$name)) -eq 'false') {
            Docker @('logs',$name) | Write-Output
            throw 'Probe exited before reaching the coordination point.'
        }
        if ($watch.Elapsed.TotalSeconds -gt 60) { throw "Timed out waiting for $Suffix" }
        Start-Sleep -Milliseconds 200
    }
}
try {
    Docker @('compose','build','simulator') | Out-Host
    if ($Scenario -eq 'overload') {
        New-Item -ItemType Directory -Force tmp | Out-Null
        "services:`n  gateway:`n    environment:`n      MAX_ACTIVE_REQUESTS: '1'" | Set-Content -LiteralPath $override -Encoding utf8
        Docker @('compose','-f','compose.yaml','-f',$override,'up','-d','--no-deps','--scale','gateway=2','gateway') | Out-Host
        Start-Sleep -Seconds 5
    }
    Docker @('compose','run','-d','--no-deps','--name',$name,'simulator','--fault-probe',$Scenario,'--coordination',$prefix) | Out-Null
    $probeStarted=$true
    Wait-File '.ready'
    if ($Scenario -eq 'database') {
        Docker @('compose','stop','--timeout','5','db') | Out-Host
        $dbStopped=$true
    } else {
        $db=Docker @('compose','ps','-q','db')
        Docker @('exec','-d',$db,'psql','-U','iot','-d','iot','-v','ON_ERROR_STOP=1','-c',
            "SET application_name='iot_fault_lock'; BEGIN; SELECT id FROM devices ORDER BY id FOR UPDATE; SELECT pg_sleep(60); ROLLBACK;") | Out-Null
        $locked=$false
        for ($i=0;$i -lt 50;$i++) {
            $waiting=Docker @('compose','exec','-T','db','psql','-U','iot','-d','iot','-tAc',
                "SELECT count(*) FROM pg_stat_activity WHERE application_name='iot_fault_lock' AND wait_event='PgSleep'")
            if ([int]$waiting -eq 1) { $locked=$true; break }
            Start-Sleep -Milliseconds 100
        }
        if (!$locked) { throw 'Could not confirm database lock injection.' }
    }
    'go' | Set-Content -LiteralPath ($prefix+'.go')
    Wait-File '.observed'
    if ($dbStopped) {
        Docker @('compose','start','db') | Out-Host
        $dbStopped=$false
    } else {
        Docker @('compose','exec','-T','db','psql','-U','iot','-d','iot','-tAc',
            "SELECT pg_cancel_backend(pid) FROM pg_stat_activity WHERE application_name='iot_fault_lock'") | Out-Null
    }
    # Allow health checks, HTTP failover and any uncertain transaction to settle.
    Start-Sleep -Seconds 12
    'recover' | Set-Content -LiteralPath ($prefix+'.recover')
    $watch=[Diagnostics.Stopwatch]::StartNew()
    while ((Docker @('inspect','--format','{{.State.Running}}',$name)) -eq 'true') {
        if ($watch.Elapsed.TotalSeconds -gt 60) { throw 'Probe recovery timed out.' }
        Start-Sleep -Milliseconds 200
    }
    Docker @('logs',$name) | Out-Host
    if ([int](Docker @('inspect','--format','{{.State.ExitCode}}',$name)) -ne 0) { throw 'Fault probe failed.' }
    Copy-Item -LiteralPath ($prefix+'.json') -Destination "results/fault-$Scenario.json"
    Get-Content -LiteralPath "results/fault-$Scenario.json"
} finally {
    if ($dbStopped) { & $docker compose start db | Out-Null }
    if ($Scenario -eq 'overload') {
        & $docker compose exec -T db psql -U iot -d iot -tAc "SELECT pg_cancel_backend(pid) FROM pg_stat_activity WHERE application_name='iot_fault_lock'" | Out-Null
        & $docker compose up -d --no-deps --scale gateway=2 gateway | Out-Null
    }
    if ($probeStarted) { & $docker rm -f $name | Out-Null }
    if (Test-Path -LiteralPath $override) { Remove-Item -LiteralPath $override }
}
