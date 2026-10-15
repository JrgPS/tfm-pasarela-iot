$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$destination=Join-Path (Get-Location) 'docs/evidence'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
$cases=@(
    @{Name='Recorrido inicial';File='docker-smoke.json'},
    @{Name='100 dispositivos';File='verified-100.json'},
    @{Name='500 dispositivos';File='verified-500.json'},
    @{Name='1000 dispositivos';File='verified-1000.json'},
    @{Name='Carga sostenida de 500';File='acceptance-500-10min.json'}
)
$lines=[Collections.Generic.List[string]]::new()
$lines.Add('# Validación de Docker y resultados medidos')
$lines.Add('')
$lines.Add('Ejecuciones locales del 15 de octubre de 2026. Cliente y servidores comparten Docker Desktop: 8 CPU virtuales y 8.163.889.152 bytes de RAM asignada al motor. Dos gateways, dos registros, un PostgreSQL y un HAProxy. Los resultados no representan un SLA ni capacidad de otra máquina.')
$lines.Add('')
$lines.Add('## Perfiles sin fallos inducidos')
$lines.Add('')
$lines.Add('| Caso | Dispositivos listos | Operaciones confirmadas | Dispositivos con fallo | Duración (s) | p95 suma (ms) | Evidencia |')
$lines.Add('|---|---:|---:|---:|---:|---:|---|')
foreach ($case in $cases) {
    $source=Join-Path 'results' $case.File
    if (!(Test-Path -LiteralPath $source)) { continue }
    Copy-Item -LiteralPath $source -Destination (Join-Path $destination $case.File) -Force
    $r=Get-Content -LiteralPath $source -Raw | ConvertFrom-Json
    $ready=if ($null -ne $r.devicesReady) { $r.devicesReady } else { $r.devices }
    $duration=([double]$r.durationSeconds).ToString('F2',[Globalization.CultureInfo]::InvariantCulture)
    $p95=([double]$r.operationLatencyMs.p95).ToString('F2',[Globalization.CultureInfo]::InvariantCulture)
    $lines.Add("| $($case.Name) | $ready | $($r.confirmedOperations) | $($r.failedDevices) | $duration | $p95 | [JSON](evidence/$($case.File)) |")
}
$lines.Add('')
$lines.Add('Los perfiles sincronizados mantienen las conexiones hasta que todos los clientes están preparados. La latencia incluye reintentos de la suma, si los hay; el tiempo total incluye las fases de conexión y consulta de totales. La tasa media no equivale a la tasa máxima del servidor.')
$lines.Add('')
$lines.Add('## Comparación exploratoria de réplicas')
$lines.Add('')
$lines.Add('| Gateways + registros | Operaciones | Fallos finales | Duración (s) | p95 (ms) | Evidencia |')
$lines.Add('|---|---:|---:|---:|---:|---|')
foreach ($replicas in @(1,2)) {
    $file="scaling-$replicas.json"
    $source=Join-Path 'results' $file
    if (!(Test-Path -LiteralPath $source)) { continue }
    Copy-Item -LiteralPath $source -Destination (Join-Path $destination $file) -Force
    $r=Get-Content -LiteralPath $source -Raw | ConvertFrom-Json
    $duration=([double]$r.durationSeconds).ToString('F2',[Globalization.CultureInfo]::InvariantCulture)
    $p95=([double]$r.operationLatencyMs.p95).ToString('F2',[Globalization.CultureInfo]::InvariantCulture)
    $lines.Add("| $replicas + $replicas | $($r.confirmedOperations) | $($r.failedDevices) | $duration | $p95 | [JSON](evidence/$file) |")
}
$lines.Add('')
$lines.Add('Una ejecución por configuración, en orden 1+1 y después 2+2, con calentamiento de 100 dispositivos y dos operaciones antes de medir 500 dispositivos y veinte operaciones. En esta comparación duplicar réplicas no mejoró el rendimiento. No se ha aislado la causa ni controlado toda la carga del host; no permite atribuir el resultado a un cuello de botella concreto. Las réplicas sí demostraron recuperación ante fallos. No se afirma escalado lineal ni aumento de capacidad a partir de estas dos ejecuciones.')
$lines.Add('')
$lines.Add('## Recuperación de réplicas')
$lines.Add('')
$lines.Add('| Caso | Operaciones confirmadas | Dispositivos con fallo final | Reconexiones | Evidencia |')
$lines.Add('|---|---:|---:|---:|---|')
foreach ($file in @('fault-gateway-graceful.json','fault-gateway-abrupt.json','fault-registry-abrupt.json')) {
    $source=Join-Path 'results' $file
    if (!(Test-Path -LiteralPath $source)) { continue }
    Copy-Item -LiteralPath $source -Destination (Join-Path $destination $file) -Force
    $r=Get-Content -LiteralPath $source -Raw | ConvertFrom-Json
    $lines.Add("| $($r.scenario) | $($r.result.confirmedOperations) | $($r.result.failedDevices) | $($r.result.reconnections) | [JSON](evidence/$file) |")
}
$lines.Add('')
$lines.Add('Cada caso usa 100 dispositivos y 100 sumas por dispositivo. Se observa el reparto de conexiones antes del fallo, se interrumpe una sola réplica y se comprueban los totales al terminar. La recuperación del cliente puede añadir latencia; no es continuidad transparente de TCP.')
$trxPath='results/tests/docker-tests.trx'
if (Test-Path -LiteralPath $trxPath) {
    [xml]$trx=Get-Content -LiteralPath $trxPath -Raw
    $counter=$trx.TestRun.ResultSummary.Counters
    $lines.Add('')
    $lines.Add('## Pruebas automatizadas')
    $lines.Add('')
    $lines.Add("Última ejecución Docker: $($counter.total) pruebas, $($counter.passed) aprobadas, $($counter.failed) fallidas y $([int]$counter.total-[int]$counter.executed) omitidas. Incluye protocolo, persistencia PostgreSQL y casos de red con TLS.")
}
foreach ($file in @('test-environment.json','backup-verification.json','acceptance-resource-samples.ndjson','acceptance-resources-summary.json')) {
    $source=Join-Path 'results' $file
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $destination $file) -Force }
}
$lines.Add('')
$lines.Add('## Interpretación y repetición')
$lines.Add('')
$lines.Add('- Objetivo sostenido: 500 dispositivos, 120 sumas por dispositivo y cinco segundos de pausa después de cada respuesta. Es una carga cerrada, de aproximadamente diez minutos; las consultas inicial y final no son sumas.')
$lines.Add('- Cada cliente exige el incremento exacto de sus totales. También se comprueba directamente la coherencia entre la tabla de operaciones y los acumulados.')
$lines.Add('- Los metadatos contienen imágenes y huellas SHA-256 del código de aplicación. Las muestras de recursos son periódicas, no una captura continua de máximos de CPU o memoria.')
$lines.Add('- PostgreSQL, HAProxy y el host son puntos únicos de fallo. Estos resultados prueban recuperación frente a réplicas de aplicación, no alta disponibilidad integral.')
$lines.Add('- La prueba de copia y restauración conserva su informe, si se ha ejecutado, sin publicar datos ni secretos del volcado.')
$lines.Add('- [Metadatos de entorno e imágenes](evidence/test-environment.json), [muestras de recursos](evidence/acceptance-resource-samples.ndjson), [resumen de muestras](evidence/acceptance-resources-summary.json) y [restauración](evidence/backup-verification.json).')
$lines.Add('- Véase [Pruebas reproducibles](pruebas-reproducibles.md) para los comandos y las condiciones de ejecución.')
$lines | Set-Content -LiteralPath 'docs/validacion-docker.md' -Encoding utf8
Write-Output 'Selected evidence exported. Secret files and database dumps were not copied.'
