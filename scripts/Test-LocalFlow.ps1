param([ValidateRange(1,1000)][int]$Devices=1,[ValidateRange(1,10000)][int]$Operations=5,
      [int]$DatabasePort=55432,[int]$TcpPort=17443)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Set-Location $projectRoot
$sdk=Join-Path $projectRoot '.tools/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $sdk)) { $sdk=(Get-Command dotnet -ErrorAction Stop).Source }
$registryDll=Join-Path $projectRoot 'src/Iot.Registry/bin/Debug/net10.0/Iot.Registry.dll'
$gatewayDll=Join-Path $projectRoot 'src/Iot.Gateway/bin/Debug/net10.0/Iot.Gateway.dll'
$simulatorDll=Join-Path $projectRoot 'src/Iot.Simulator/bin/Debug/net10.0/Iot.Simulator.dll'
$registry=$null
$gateway=$null
New-Item -ItemType Directory -Force -Path '.tools','results' | Out-Null
function Wait-Ready([string]$Url,[System.Diagnostics.Process]$Process) {
    for ($i=0;$i -lt 100;$i++) {
        if ($Process.HasExited) { throw 'Service exited before readiness; inspect .tools/*.log.' }
        try {
            Invoke-RestMethod -Uri $Url -TimeoutSec 1 | Out-Null
            return
        } catch { Start-Sleep -Milliseconds 200 }
    }
    throw "Service not ready: $Url"
}
try {
    $env:DB_HOST='127.0.0.1'
    $env:DB_PORT="$DatabasePort"
    $env:IOT_SECRETS=Join-Path $projectRoot 'secrets'
    $env:Logging__LogLevel__Microsoft='Warning'
    $registry=Start-Process -FilePath $sdk -ArgumentList @("`"$registryDll`"",'--urls','http://127.0.0.1:18080') -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput '.tools/registry.out.log' -RedirectStandardError '.tools/registry.err.log'
    Wait-Ready 'http://127.0.0.1:18080/health/ready' $registry
    $env:TCP_PORT="$TcpPort"
    $env:REGISTRY_URL='http://127.0.0.1:18080'
    $gateway=Start-Process -FilePath $sdk -ArgumentList @("`"$gatewayDll`"",'--urls','http://127.0.0.1:18081') -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput '.tools/gateway.out.log' -RedirectStandardError '.tools/gateway.err.log'
    Wait-Ready 'http://127.0.0.1:18081/health/ready' $gateway
    & $sdk $simulatorDll --host localhost --port "$TcpPort" --devices "$Devices" --operations "$Operations" --ramp-ms 1 --interval-ms 100
    if ($LASTEXITCODE -ne 0) { throw 'Simulator reported an error.' }
}
finally {
    # Only stop the two exact process handles created by this invocation.
    foreach ($process in @($gateway,$registry)) {
        if ($null -ne $process -and !$process.HasExited) { Stop-Process -Id $process.Id }
    }
}
