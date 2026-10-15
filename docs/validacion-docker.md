# Validación de Docker y resultados medidos

Ejecuciones locales del 15 de octubre de 2026. Cliente y servidores comparten Docker Desktop: 8 CPU virtuales y 8.163.889.152 bytes de RAM asignada al motor. Dos gateways, dos registros, un PostgreSQL y un HAProxy. Los resultados no representan un SLA ni capacidad de otra máquina.

## Perfiles sin fallos inducidos

| Caso | Dispositivos listos | Operaciones confirmadas | Dispositivos con fallo | Duración (s) | p95 suma (ms) | Evidencia |
|---|---:|---:|---:|---:|---:|---|
| Recorrido inicial | 1 | 5 | 0 | 0.48 | 18.58 | [JSON](evidence/docker-smoke.json) |
| 100 dispositivos | 100 | 2000 | 0 | 2.86 | 90.00 | [JSON](evidence/verified-100.json) |
| 500 dispositivos | 500 | 10000 | 0 | 12.56 | 541.64 | [JSON](evidence/verified-500.json) |
| 1000 dispositivos | 1000 | 20000 | 0 | 28.10 | 1297.52 | [JSON](evidence/verified-1000.json) |
| Carga sostenida de 500 | 500 | 60000 | 0 | 605.59 | 47.68 | [JSON](evidence/acceptance-500-10min.json) |

Los perfiles sincronizados mantienen las conexiones hasta que todos los clientes están preparados. La latencia incluye reintentos de la suma, si los hay; el tiempo total incluye las fases de conexión y consulta de totales. La tasa media no equivale a la tasa máxima del servidor.

## Comparación exploratoria de réplicas

| Gateways + registros | Operaciones | Fallos finales | Duración (s) | p95 (ms) | Evidencia |
|---|---:|---:|---:|---:|---|
| 1 + 1 | 10000 | 0 | 13.18 | 533.85 | [JSON](evidence/scaling-1.json) |
| 2 + 2 | 10000 | 0 | 16.78 | 976.25 | [JSON](evidence/scaling-2.json) |

Una ejecución por configuración, en orden 1+1 y después 2+2, con calentamiento de 100 dispositivos y dos operaciones antes de medir 500 dispositivos y veinte operaciones. En esta comparación duplicar réplicas no mejoró el rendimiento. No se ha aislado la causa ni controlado toda la carga del host; no permite atribuir el resultado a un cuello de botella concreto. Las réplicas sí demostraron recuperación ante fallos. No se afirma escalado lineal ni aumento de capacidad a partir de estas dos ejecuciones.

## Recuperación de réplicas

| Caso | Operaciones confirmadas | Dispositivos con fallo final | Reconexiones | Evidencia |
|---|---:|---:|---:|---|
| graceful loss and restart of one gateway replica | 10000 | 0 | 50 | [JSON](evidence/fault-gateway-graceful.json) |
| abrupt loss and restart of one gateway replica | 10000 | 0 | 50 | [JSON](evidence/fault-gateway-abrupt.json) |
| abrupt loss and restart of one registry replica | 10000 | 0 | 21 | [JSON](evidence/fault-registry-abrupt.json) |

Cada caso usa 100 dispositivos y 100 sumas por dispositivo. Se observa el reparto de conexiones antes del fallo, se interrumpe una sola réplica y se comprueban los totales al terminar. La recuperación del cliente puede añadir latencia; no es continuidad transparente de TCP.

## Pruebas automatizadas

Última ejecución Docker: 29 pruebas, 29 aprobadas, 0 fallidas y 0 omitidas. Incluye protocolo, persistencia PostgreSQL y casos de red con TLS.

## Interpretación y repetición

- Objetivo sostenido: 500 dispositivos, 120 sumas por dispositivo y cinco segundos de pausa después de cada respuesta. Es una carga cerrada, de aproximadamente diez minutos; las consultas inicial y final no son sumas.
- Cada cliente exige el incremento exacto de sus totales. También se comprueba directamente la coherencia entre la tabla de operaciones y los acumulados.
- Los metadatos contienen imágenes y huellas SHA-256 del código de aplicación. Las muestras de recursos son periódicas, no una captura continua de máximos de CPU o memoria.
- PostgreSQL, HAProxy y el host son puntos únicos de fallo. Estos resultados prueban recuperación frente a réplicas de aplicación, no alta disponibilidad integral.
- La prueba de copia y restauración conserva su informe, si se ha ejecutado, sin publicar datos ni secretos del volcado.
- [Metadatos de entorno e imágenes](evidence/test-environment.json), [muestras de recursos](evidence/acceptance-resource-samples.ndjson), [resumen de muestras](evidence/acceptance-resources-summary.json) y [restauración](evidence/backup-verification.json).
- Véase [Pruebas reproducibles](pruebas-reproducibles.md) para los comandos y las condiciones de ejecución.
