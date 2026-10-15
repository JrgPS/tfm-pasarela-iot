# Caída de PostgreSQL y control de sobrecarga

Pruebas ejecutadas el 15 de octubre de 2026 sobre la demo Docker local. No intervienen datos reales. Se conservan los resultados originales en [base de datos](evidence/fault-database.json) y [sobrecarga](evidence/fault-overload.json).

## Base de datos detenida

El cliente abre una conexión TLS, inicializa el dispositivo y consulta sus totales. El coordinador detiene PostgreSQL y solo entonces permite enviar una suma. La respuesta observada fue `009`, dependencia no disponible, sin comercio ni firma de éxito. El coordinador arranca PostgreSQL y espera la recuperación de los controles de salud.

El cliente reconecta, inicializa y envía dos veces exactamente la misma trama de suma, conservando su identificador. Ambas respuestas son correctas y autenticadas. El total aumenta en una operación y tres unidades enteras. No hubo aceptación durante la parada ni efecto duplicado durante la recuperación.

Es un escenario controlado con un dispositivo ya conectado. No demuestra recuperación automática de PostgreSQL ni resistencia a corrupción de disco. Las caídas antes de abrir una sesión pueden terminar la conexión sin respuesta de aplicación.

## Saturación del trabajo activo

Se reduce temporalmente `MAX_ACTIVE_REQUESTS` de 128 a 1 por gateway y se mantienen dos réplicas. Se conectan e inicializan 32 dispositivos. Una sesión SQL exclusiva de la prueba bloquea las filas de dispositivos dentro de una transacción. Tras confirmar que el bloqueo está activo, los clientes envían una suma simultáneamente.

Resultados iniciales: 30 respuestas `013` por capacidad y dos `009` por tiempo de espera de la dependencia. No hubo éxitos iniciales. Se cancela la sesión de bloqueo y se deja recuperar el servicio. Cada dispositivo reconecta y reenvía dos veces su trama original. Se confirman las 32 operaciones y los 32 totales aumentan exactamente una vez.

El límite normal de 128 se restaura en `finally`, incluso si la prueba falla. El simulador general reconoce ahora `013` como rechazo temporal y aplica los mismos reintentos acotados que a `009`.

Esta prueba verifica la ruta de rechazo con límites reducidos e inyección de bloqueo. No determina la capacidad máxima con la configuración normal ni constituye un ensayo prolongado de consumo de memoria. Las conexiones y la longitud de trama también tienen límites explícitos.

## Reproducción

Con la demo iniciada, sin otras simulaciones en curso:

```powershell
.\scripts\Test-DependencyFaults.ps1 -Scenario database
.\scripts\Test-DependencyFaults.ps1 -Scenario overload
```

Estos comandos interrumpen deliberadamente la demo local. El primer escenario detiene temporalmente PostgreSQL. El segundo recrea los gateways y bloquea filas de dispositivos ficticios. El script no borra el volumen de datos. Los archivos de coordinación y los resultados nuevos quedan en `results/`.

## Regresión posterior

Tras cambiar el tratamiento de `013`, `Test-Docker.ps1 -Quick` pasó las 29 pruebas, sin omisiones, y confirmó 2000 operaciones de 100 dispositivos sin fallos ni reintentos. Se conserva este ensayo en [regresión de 100 dispositivos](evidence/regression-100.json). Su p95 fue 1032,67 ms, mayor que el del ensayo corto anterior. Se ejecutó durante la preparación de materiales en el mismo host y no se midió el efecto de las tareas concurrentes. No sustituye ni reproduce el ensayo sostenido de 500 dispositivos. Por ello, no se afirma una latencia garantizada para cada ejecución.

El [resumen extraído del TRX](evidence/regression-tests.json) conserva el número de pruebas y la fecha de finalización. Los [metadatos del cierre](evidence/environment-release.json) identifican el código de aplicación y las imágenes locales.

## Instalación desde cero

`Test-CleanInstall.ps1` clonó el commit `cadd88b` del repositorio local en una carpeta nueva. Antes del arranque no existía `secrets/`. El procedimiento documentado generó credenciales y certificado nuevos, una clave maestra distinta a la de la demo original y un volumen PostgreSQL independiente. Diez dispositivos completaron 50 sumas, sin fallos ni reintentos, con totales verificados. [Informe](evidence/clean-install.json).

La prueba empleó un nombre de proyecto Docker único y el puerto local 17443 para evitar interferir con la demo habitual. El script retiró solo sus contenedores, redes y volumen desechable. La carpeta del clon queda bajo `tmp/`, ignorada por Git. Este ensayo valida instalación sin secretos preexistentes en el clon; se ejecuta en el mismo Docker Desktop con caché de imágenes disponible y no demuestra instalación offline ni desde un repositorio remoto aún no publicado.

```powershell
.\scripts\Test-CleanInstall.ps1
```

Si Git no está en PATH, pasar `-GitExecutable` con la ruta de `git.exe`. Requiere la demo inicial preparada para comparar que la clave maestra del clon es independiente.
