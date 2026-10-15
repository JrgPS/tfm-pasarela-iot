$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$docker=(Get-Command docker -ErrorAction SilentlyContinue).Source
if (!$docker) { $docker="$env:LOCALAPPDATA/Programs/DockerDesktop/resources/bin/docker.exe" }
function Run-Docker([string[]]$Arguments) {
    $value=& $docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Backup test failed: $($Arguments[0])" }
    return $value
}
$suffix=[Guid]::NewGuid().ToString('N').Substring(0,12)
$database="iot_restore_${suffix}_tests"
$archive="/tmp/iot-backup-$suffix.dump"
$created=$false
$summarySql="SELECT json_build_object('devices',(SELECT count(*) FROM devices),'operations',(SELECT count(*) FROM operations),'inconsistentTotals',(SELECT count(*) FROM device_totals t LEFT JOIN (SELECT device_id,count(*) n,sum(result) s FROM operations GROUP BY device_id) o ON o.device_id=t.device_id WHERE t.operation_count<>COALESCE(o.n,0) OR t.result_sum<>COALESCE(o.s,0)))"
try {
    $original=(Run-Docker -Arguments @('compose','exec','-T','db','psql','-U','iot','-d','iot','-tAc',$summarySql)) | ConvertFrom-Json
    Run-Docker -Arguments @('compose','exec','-T','db','pg_dump','-U','iot','-d','iot','--format=custom',"--file=$archive") | Out-Null
    Run-Docker -Arguments @('compose','exec','-T','db','createdb','-U','iot',$database) | Out-Null
    $created=$true
    Run-Docker -Arguments @('compose','exec','-T','db','pg_restore','-U','iot','-d',$database,
        '--no-owner','--exit-on-error',$archive) | Out-Null
    $summary=Run-Docker -Arguments @('compose','exec','-T','db','psql','-U','iot','-d',$database,'-tAc',
        $summarySql)
    $result=$summary | ConvertFrom-Json
    if ($result.inconsistentTotals -ne 0 -or $result.devices -lt 1000) { throw 'Restored data is inconsistent.' }
    if ($result.devices -ne $original.devices -or $result.operations -ne $original.operations) {
        throw 'Source changed during backup or restored counts differ. Run this check without concurrent workload.'
    }
    [ordered]@{
        verifiedUtc=[DateTime]::UtcNow.ToString('o')
        method='pg_dump custom archive followed by pg_restore into a fresh isolated database'
        sourceBeforeDump=$original
        restored=$result
        masterKeyRequired='Encrypted device keys require the original master-key file, backed up separately.'
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath 'results/backup-verification.json' -Encoding utf8
    Write-Output 'Backup restored successfully into an isolated database; totals are consistent.'
}
finally {
    # Exact disposable database created above; never drop the source iot or an existing database.
    if ($created) { & $docker compose exec -T db dropdb -U iot $database | Out-Null }
    & $docker compose exec -T db rm -- $archive | Out-Null
}
