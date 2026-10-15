param([ValidateRange(1,100000)][int]$Devices=1000,
      [ValidateRange(1,8)][int]$Gateways=1,
      [ValidateRange(1,8)][int]$Registries=1)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
Get-Command docker -ErrorAction Stop | Out-Null
function Run-Docker([string[]]$DockerArguments) {
    & docker @DockerArguments
    if ($LASTEXITCODE -ne 0) { throw "Docker failed: $($DockerArguments -join ' ')" }
}
New-Item -ItemType Directory -Force -Path secrets,results | Out-Null
Run-Docker -DockerArguments @('compose','run','--rm','--no-deps','--build','provision','prepare','--count',"$Devices")
Run-Docker -DockerArguments @('compose','up','-d','--wait','db')
Run-Docker -DockerArguments @('compose','run','--rm','--no-deps','provision','seed')
Run-Docker -DockerArguments @('compose','up','-d','--build','--scale',"gateway=$Gateways",'--scale',"registry=$Registries",'gateway','registry','proxy')
Write-Host 'Services launched. Verify readiness in logs, then run the simulator. Public listener: localhost:7443.'
