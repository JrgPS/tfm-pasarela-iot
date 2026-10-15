$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$docker=(Get-Command docker -ErrorAction SilentlyContinue).Source
if (!$docker) { $docker="$env:LOCALAPPDATA/Programs/DockerDesktop/resources/bin/docker.exe" }
$hostSummary=& $docker info --format '{{json .NCPU}}|{{json .MemTotal}}|{{json .OperatingSystem}}|{{json .Architecture}}'
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect Docker environment.' }
$parts=$hostSummary.Split('|')
$images=[ordered]@{}
foreach ($image in @('iot-tfm-gateway','iot-tfm-registry','iot-tfm-simulator','postgres:18','haproxy:3.0-alpine')) {
    $images[$image]=& $docker image inspect --format '{{.Id}}' $image
    if ($LASTEXITCODE -ne 0) { throw "Cannot inspect image $image" }
}
$sourceHashes=[ordered]@{}
Get-ChildItem src -Recurse -File | Where-Object {
    $_.Extension -in @('.cs','.csproj','.sql') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
} | Sort-Object FullName | ForEach-Object {
    $relative=[IO.Path]::GetRelativePath((Get-Location).Path,$_.FullName).Replace('\','/')
    $sourceHashes[$relative]=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
}
$metadata=[ordered]@{
    capturedUtc=[DateTime]::UtcNow.ToString('o')
    dockerCpus=[int]$parts[0]
    dockerMemoryBytes=[long]$parts[1]
    dockerOs=($parts[2] | ConvertFrom-Json)
    architecture=($parts[3] | ConvertFrom-Json)
    gatewayReplicas=2
    registryReplicas=2
    databaseInstances=1
    topology='All containers, including the simulator, share one local Docker Desktop engine.'
    images=$images
    applicationSourceSha256=$sourceHashes
}
New-Item -ItemType Directory -Force -Path 'results' | Out-Null
$metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath 'results/test-environment.json' -Encoding utf8
Write-Output 'Environment metadata captured without secrets.'
