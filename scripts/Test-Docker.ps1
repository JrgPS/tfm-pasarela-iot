param([switch]$Quick,[switch]$IncludeAcceptance)
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
& "$PSScriptRoot/Start-Demo.ps1" -Devices 1000 -Gateways 2 -Registries 2
$docker=(Get-Command docker -ErrorAction SilentlyContinue).Source
if (!$docker) { $docker="$env:LOCALAPPDATA/Programs/DockerDesktop/resources/bin/docker.exe" }
function Run-Docker([string[]]$Arguments) {
    & $docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker verification failed: $($Arguments[0])" }
}
$exists=& $docker compose exec -T db psql -U iot -d iot -tAc "SELECT 1 FROM pg_database WHERE datname='iot_tests'"
if ($exists -ne '1') { Run-Docker -Arguments @('compose','exec','-T','db','createdb','-U','iot','iot_tests') }
$previousNetwork=$env:IOT_NETWORK_TESTS
try {
    $env:IOT_NETWORK_TESTS='true'
    Run-Docker -Arguments @('compose','run','--rm','--build','tests')
} finally { $env:IOT_NETWORK_TESTS=$previousNetwork }
Run-Docker -Arguments @('compose','build','simulator')
$counts=if ($Quick) { @(100) } else { @(100,500,1000) }
foreach ($count in $counts) {
    Run-Docker -Arguments @('compose','run','--rm','simulator','--host','proxy','--devices',"$count",
        '--operations','20','--ramp-ms','2','--interval-ms','50','--synchronize-start','true',
        '--output',"results/verified-$count.json")
}
if (!$Quick) {
    & "$PSScriptRoot/Test-Resilience.ps1" -Service gateway -Mode graceful
    & "$PSScriptRoot/Test-Resilience.ps1" -Service gateway -Mode abrupt
    & "$PSScriptRoot/Test-Resilience.ps1" -Service registry -Mode abrupt
}
if ($IncludeAcceptance) {
    Run-Docker -Arguments @('compose','run','--rm','simulator','--host','proxy','--devices','500',
        '--operations','120','--ramp-ms','2','--interval-ms','5000','--synchronize-start','true',
        '--output','results/acceptance-500-10min.json')
}
Run-Docker -Arguments @('compose','exec','-T','proxy','haproxy','-c','-f','/usr/local/etc/haproxy/haproxy.cfg')
Write-Output 'Docker verification finished. Reports are in results/. Services remain available on localhost:7443.'
