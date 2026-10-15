$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$docker=(Get-Command docker -ErrorAction SilentlyContinue).Source
if (!$docker) { $docker="$env:LOCALAPPDATA/Programs/DockerDesktop/resources/bin/docker.exe" }
function Run-Docker([string[]]$Arguments) {
    & $docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw 'Scaling comparison command failed.' }
}
try {
    foreach ($replicas in @(1,2)) {
        Run-Docker -Arguments @('compose','up','-d','--scale',"gateway=$replicas",'--scale',"registry=$replicas",'gateway','registry','proxy')
        Start-Sleep -Seconds 3
        Run-Docker -Arguments @('compose','run','--rm','simulator','--host','proxy','--devices','100','--operations','2',
            '--ramp-ms','2','--interval-ms','0','--synchronize-start','true','--output',"results/warmup-$replicas.json")
        Run-Docker -Arguments @('compose','run','--rm','simulator','--host','proxy','--devices','500','--operations','20',
            '--ramp-ms','2','--interval-ms','50','--synchronize-start','true','--output',"results/scaling-$replicas.json")
    }
}
finally {
    & $docker compose up -d --scale gateway=2 --scale registry=2 gateway registry proxy
}
