param([ValidateRange(1,100000)][int]$Devices=1000,
      [ValidateRange(1,8)][int]$Gateways=1,
      [ValidateRange(1,8)][int]$Registries=1)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$dockerCommand=Get-Command docker -ErrorAction SilentlyContinue
$dockerExe=if ($dockerCommand) { $dockerCommand.Source } else {
    @("$env:LOCALAPPDATA/Programs/DockerDesktop/resources/bin/docker.exe",
      "$env:ProgramFiles/Docker/Docker/resources/bin/docker.exe") |
        Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (!$dockerExe) { throw 'Docker CLI not found. Install Docker Desktop and start its Linux engine.' }
$env:PATH=(Split-Path $dockerExe -Parent)+';'+$env:PATH
function Run-Docker([string[]]$DockerArguments) {
    & $dockerExe @DockerArguments
    if ($LASTEXITCODE -ne 0) { throw "Docker failed: $($DockerArguments -join ' ')" }
}
New-Item -ItemType Directory -Force -Path secrets,results | Out-Null
Run-Docker -DockerArguments @('compose','run','--rm','--no-deps','--build','provision','prepare','--count',"$Devices")
Run-Docker -DockerArguments @('compose','up','-d','--wait','db')
Run-Docker -DockerArguments @('compose','run','--rm','--no-deps','provision','seed')
Run-Docker -DockerArguments @('compose','up','-d','--build','--scale',"gateway=$Gateways",'--scale',"registry=$Registries",'gateway','registry','proxy')
Write-Host 'Services launched. Verify readiness in logs, then run the simulator. Public listener: localhost:7443.'
