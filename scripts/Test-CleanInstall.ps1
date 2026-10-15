param([string]$GitExecutable='git')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Set-Location $root
$id=[Guid]::NewGuid().ToString('N').Substring(0,8)
$project="iot-clean-$id"
$checkout=Join-Path $root "tmp/clean-$id"
$docker="$env:LOCALAPPDATA/Programs/DockerDesktop/resources/bin/docker.exe"
if (Get-Command docker -ErrorAction SilentlyContinue) { $docker=(Get-Command docker).Source }
$env:PATH=(Split-Path $docker)+[IO.Path]::PathSeparator+$env:PATH
$oldProject=$env:COMPOSE_PROJECT_NAME
$oldFiles=$env:COMPOSE_FILE
$started=$false
function Run-CleanDocker([string[]]$Arguments) {
    & $docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker failed: $($Arguments[0])" }
}
try {
    & $GitExecutable clone --no-hardlinks -- $root $checkout
    if ($LASTEXITCODE -ne 0) { throw 'Clone failed.' }
    Set-Location $checkout
    $commit=& $GitExecutable rev-parse HEAD
    if (Test-Path secrets) { throw 'A clean clone must not contain secrets.' }
    # Compose 2.24.4+ supports !override. Isolate the test listener and project volume.
    "services:`n  proxy:`n    ports: !override`n      - '127.0.0.1:17443:7443'" | Set-Content clean.override.yaml -Encoding utf8
    $env:COMPOSE_PROJECT_NAME=$project
    $env:COMPOSE_FILE='compose.yaml'+[IO.Path]::PathSeparator+'clean.override.yaml'
    $started=$true
    & ./scripts/Start-Demo.ps1 -Devices 10 -Gateways 1 -Registries 1
    Start-Sleep -Seconds 6
    Run-CleanDocker @('compose','run','--rm','--build','simulator','--host','proxy','--devices','10','--operations','5','--output','results/clean-install.json') | Out-Host
    $simulation=Get-Content results/clean-install.json -Raw | ConvertFrom-Json
    $fresh=(Get-FileHash secrets/master-key).Hash -ne (Get-FileHash (Join-Path $root 'secrets/master-key')).Hash
    if (!$fresh -or $simulation.failedDevices -ne 0 -or $simulation.confirmedOperations -ne 50) { throw 'Clean installation verification failed.' }
    [ordered]@{utc=[DateTime]::UtcNow.ToString('o');commit=$commit;source='local Git clone';
        composeProject=$project;secretsInCloneBeforeSetup=$false;independentMasterKey=$fresh;
        newPostgresVolume=$true;simulation=$simulation} |
        ConvertTo-Json -Depth 8 | Set-Content (Join-Path $root 'results/clean-install.json') -Encoding utf8
    Get-Content (Join-Path $root 'results/clean-install.json')
} finally {
    if ($started) {
        # Only the unique, disposable project created above may be removed with its test volume.
        if ($project -notmatch '^iot-clean-[a-f0-9]{8}$' -or $env:COMPOSE_PROJECT_NAME -ne $project) { throw 'Unexpected cleanup project.' }
        & $docker compose down --volumes | Out-Host
    }
    $env:COMPOSE_PROJECT_NAME=$oldProject
    $env:COMPOSE_FILE=$oldFiles
    Set-Location $root
}
