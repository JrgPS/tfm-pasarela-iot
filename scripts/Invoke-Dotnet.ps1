param([Parameter(ValueFromRemainingArguments=$true)][string[]]$DotnetArguments)
$ErrorActionPreference='Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location $projectRoot
$sdk = Join-Path $projectRoot '.tools/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $sdk)) { $sdk = (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.tools/cli'
$env:NUGET_PACKAGES = Join-Path $projectRoot '.tools/nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
& $sdk @DotnetArguments
exit $LASTEXITCODE
