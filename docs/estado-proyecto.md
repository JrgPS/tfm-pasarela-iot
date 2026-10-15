# Estado del proyecto

Primera implementación: 15 de octubre de 2026. Este registro distingue lo implementado de lo comprobado. El TFM todavía no está preparado para enviar.

## Implementado

- Solución .NET con siete proyectos: protocolo, persistencia, registro, gateway, aprovisionamiento, simulador y pruebas.
- Protocolo ASCII/LF acotado, fechas UTC, firmas HMAC, identidad con ceros iniciales y aritmética exacta.
- Gateway TCP/TLS asíncrono y API interna autenticada.
- Persistencia PostgreSQL, auditoría, respuestas aceptadas idempotentes y transacciones con bloqueo por dispositivo.
- Generación de secretos y certificado de laboratorio, cifrado de claves de dispositivos y alta ficticia.
- Simulador concurrente básico con comprobación de resultados y totales y exportación JSON.
- Dockerfile, Compose, HAProxy, scripts de arranque y tareas para VS Code.
- Flujo de GitHub Actions preparado; todavía no ejecutado en GitHub.

## Verificado en este equipo

1. SDK oficial local .NET **10.0.402**, descargado a `.tools/dotnet`.
2. Compilación de los siete proyectos: cero errores y cero advertencias.
3. PostgreSQL 18, binarios oficiales EDB, iniciado temporalmente en `127.0.0.1:55432`, con almacenamiento dentro de `.tools/pgdata`. No se instaló un servicio Windows.
4. Aprovisionamiento de **1000 dispositivos ficticios**, con secretos generados localmente y excluidos de Git.
5. **23 pruebas correctas, cero fallos y cero omisiones**: 15 de protocolo y 8 de integración con PostgreSQL real.
6. Arranque del registro y gateway, con comprobaciones HTTP de readiness durante el script de prueba local. El gateway anunció escucha TLS en `17443`.

Las pruebas de integración incluyen persistencia entre instancias del servicio, 50 reintentos concurrentes de una misma operación, 100 operaciones distintas concurrentes, reutilización de ID con contenido diferente, firma inválida, dispositivo desconocido, bloqueo, inicialización y errores funcionales. Son pruebas de consistencia; no constituyen la prueba de carga del simulador ni una prueba completa de red.

El resultado TRX de esta ejecución está en `tests/Iot.Tests/TestResults/initial-tests.trx`, ignorado por Git. Las pruebas pueden repetirse con `IOT_TEST_CONNECTION` apuntando a una base de datos de pruebas; en su ausencia se omiten explícitamente las ocho de PostgreSQL.

## Bloqueo observado

Al ejecutar el simulador a través del script de prueba local, Windows rechazó cargar su ensamblado con el error **Control de aplicaciones, 0x800711C7**. El simulador no llegó a generar tráfico. Por tanto:

- No se afirma que el recorrido completo TCP/TLS haya pasado.
- No hay métricas de conexiones, operaciones por segundo ni latencia del simulador verificadas.
- No se han desactivado protecciones ni alterado políticas de Windows.
- El script detuvo las dos instancias de servicios que había creado tras el fallo.

Docker no estaba disponible en las rutas y comandos inspeccionados. La configuración de contenedores y HAProxy queda pendiente de prueba, preferentemente en una VM Linux sobre Proxmox o en un equipo con Docker operativo. El bloqueo de Windows no demuestra un defecto del protocolo, pero tampoco permite dar por verificado el programa.

## Trabajo pendiente, por prioridad

1. Ejecutar el conjunto Docker en Linux y corregir cualquier problema real de imágenes, DNS, salud o configuración.
2. Verificar con el simulador un dispositivo y después perfiles concurrentes de 100, 500 y 1000; guardar resultados medidos.
3. Pruebas de caída de réplica y pérdida de respuesta por red, límites de trabajo activo y agotamiento de capacidad.
4. Actualización de actividad y reconciliación de conexiones perdidas, drenaje planificado y modo avanzado de errores del simulador.
5. Revisar tratamiento de denegaciones, límites de autenticación, secretos y privilegios de base de datos antes de exposición remota.
6. Fijar versiones de imágenes y dependencias para la entrega, verificar copias y restauración e instalación desde un clon limpio.
7. Publicar repositorio y ejecutar CI; preparar presentación, vídeo, acceso del evaluador y formulario de entrega.

## Entorno local y confidencialidad

`.tools/`, `secrets/`, salidas de compilación, logs y resultados locales están excluidos del repositorio. El PDF original del campus se mantiene localmente y no se añade al código público. No se han contratado servicios ni consumido API de IA desde la aplicación.

Los datos persistidos son únicamente ficticios. No se ha accedido al NAS ni a Proxmox, ni se ha publicado el servicio en Internet.
