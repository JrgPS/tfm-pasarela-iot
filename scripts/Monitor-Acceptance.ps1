param([string]$Container='iot-tfm-acceptance-500')
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$docker=(Get-Command docker -ErrorAction SilentlyContinue).Source
if (!$docker) { $docker="$env:LOCALAPPDATA/Programs/DockerDesktop/resources/bin/docker.exe" }
$ids=@(& $docker compose ps -q db gateway registry proxy)
$ids+=(& $docker inspect --format '{{.Id}}' $Container)
$path='results/acceptance-resource-samples.ndjson'
$watch=[Diagnostics.Stopwatch]::StartNew()
$samples=0
while ($watch.Elapsed.TotalSeconds -lt 700) {
    $running=& $docker inspect --format '{{.State.Running}}' $Container
    if ($LASTEXITCODE -ne 0 -or $running -ne 'true') { break }
    $stats=& $docker stats --no-stream --format '{{json .}}' @ids
    if ($LASTEXITCODE -ne 0) { throw 'Cannot sample container resources.' }
    foreach ($line in $stats) {
        $entry=[ordered]@{ sampledUtc=[DateTime]::UtcNow.ToString('o'); container=($line | ConvertFrom-Json) }
        $entry | ConvertTo-Json -Depth 4 -Compress | Add-Content -LiteralPath $path -Encoding utf8
    }
    $samples++
    Start-Sleep -Seconds 8
}
Write-Output "Resource snapshots captured: $samples"
