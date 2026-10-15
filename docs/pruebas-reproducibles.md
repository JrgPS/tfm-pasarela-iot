# Pruebas reproducibles en Docker

Estos comandos se ejecutan en la raíz del repositorio, con Docker Desktop o Docker Engine en modo Linux. Usan exclusivamente dispositivos y datos ficticios. El equipo inicial tiene la CLI en `%LOCALAPPDATA%\Programs\DockerDesktop\resources\bin`; los scripts PowerShell la detectan aunque VS Code no haya actualizado su PATH.

## Preparación

```powershell
.\scripts\Start-Demo.ps1 -Devices 1000 -Gateways 2 -Registries 2
```

Genera secretos privados si no existen, conserva los ya creados, aprovisiona los dispositivos y levanta los servicios. Repetir el comando no borra operaciones ni totales. El certificado es de laboratorio y la entrada de red se limita a `localhost:7443`.

## Batería automática

```powershell
.\scripts\Test-Docker.ps1 -Quick
.\scripts\Test-Docker.ps1
```

La primera variante ejecuta pruebas y un perfil corto de 100 dispositivos. La segunda añade perfiles de 500 y 1000, apagado ordenado y caídas abruptas de una réplica. Devuelve un error si falla una comprobación; los resultados no se interpretan únicamente a partir de que el contenedor esté arrancado.

La batería xUnit usa una base `iot_tests` separada para persistencia y consistencia. Sus pruebas de red son optativas mediante `IOT_NETWORK_TESTS=true`: crean identidades ficticias con prefijo 97 en la base de la demo para verificar gateway y registro reales. Se habilitan en `Test-Docker.ps1`. No deben apuntarse a una plataforma de trabajo.

Si se ejecuta `dotnet test` fuera de estos scripts, las pruebas que no dispongan de su entorno aparecerán como omitidas; no se consideran aprobadas. El informe TRX se conserva en `results/tests/docker-tests.trx` cuando se usa el contenedor de pruebas.

## Prueba sostenida

```powershell
.\scripts\Test-Docker.ps1 -IncludeAcceptance
```

El perfil de aceptación conecta 500 dispositivos, espera a que todos hayan consultado sus totales iniciales y ejecuta 120 sumas por dispositivo con una pausa de cinco segundos. Cada dispositivo comprueba su total al terminar. Se esperan 60.000 sumas únicas confirmadas.

La duración es de unos diez minutos, más conexión, consultas y tiempos de procesamiento. Es un modelo de carga cerrado: la pausa empieza después de recibir la respuesta, por lo que la tasa efectiva puede ser inferior a las 100 operaciones por segundo nominales. No se presenta como una prueba de tasa fija ni de capacidad máxima.

`--synchronize-start true` hace que los dispositivos mantengan sus conexiones abiertas hasta alcanzar el punto de inicio. Esto evita confundir una secuencia de clientes breves con cientos de sesiones simultáneas. Una única petición está pendiente en cada dispositivo.

El simulador genera JSON y CSV. La latencia mide la operación `002`, incluye los reintentos cuando los hay y excluye la conexión inicial y las consultas de totales. El throughput del informe divide operaciones únicas confirmadas por la duración total, incluidas esas fases.

## Fallos de réplica

```powershell
.\scripts\Test-Resilience.ps1 -Service gateway -Mode abrupt
.\scripts\Test-Resilience.ps1 -Service gateway -Mode graceful
.\scripts\Test-Resilience.ps1 -Service registry -Mode abrupt
```

Cada prueba requiere dos réplicas del servicio afectado. Arranca 100 dispositivos, comprueba sus conexiones en la base de datos e interrumpe exactamente una réplica del proyecto. Después la arranca y exige 10.000 sumas confirmadas sin discrepancias de totales.

La variante abrupta usa SIGKILL; la ordenada usa SIGTERM y el plazo de drenaje. Los dispositivos conectados al gateway afectado deben reconectar; no se promete migración transparente de una sesión TCP.

Los scripts restauran la réplica interrumpida y eliminan únicamente su contenedor auxiliar. No borran el volumen PostgreSQL. Se debe dejar terminar una prueba antes de lanzar otra sobre los mismos dispositivos, porque cada cliente contrasta el incremento de sus propios totales.

## Copia y restauración

```powershell
.\scripts\Test-Backup.ps1
```

Crea una copia lógica, la restaura en una base nueva de nombre único y comprueba recuentos y coherencia entre operaciones y totales. Elimina esa base temporal al terminar y conserva un informe JSON, sin alterar la base original.

Para una recuperación real, el archivo `master-key` debe respaldarse de forma privada junto con las instrucciones: contiene la clave necesaria para descifrar los secretos de los dispositivos. Una copia de las tablas no reemplaza ese archivo. Las copias reales deben tener acceso restringido y una política de retención.

## Comparación de configuraciones

```powershell
.\scripts\Compare-Scaling.ps1
```

Ejecuta el mismo perfil de 500 dispositivos con una réplica de cada servicio y después con dos. Aplica un calentamiento previo y deja dos réplicas al terminar. Es una comparación exploratoria con una sola ejecución por configuración: debe publicarse aunque no muestre mejora. No ejecutar a la vez que otras pruebas del simulador.

## Alcance y limitaciones

- Cliente, servicios y base de datos comparten el mismo motor Docker Desktop en estas mediciones; compiten por CPU y memoria.
- Dos réplicas pueden mejorar capacidad y recuperación de aplicaciones, pero no eliminan los puntos únicos de fallo del balanceador, PostgreSQL o el equipo físico.
- Una respuesta perdida no demuestra que la operación no se ejecutó. Se conserva el mismo request ID durante el reintento.
- Los resultados de carga se comparan indicando configuración, número de dispositivos, pausas y réplicas. No se extrapolan directamente a cloud o a otro equipo.
- Los datos de ejecución en `results/` están ignorados por Git. La evidencia seleccionada y revisada para la entrega se conserva bajo `docs/evidence/`.
