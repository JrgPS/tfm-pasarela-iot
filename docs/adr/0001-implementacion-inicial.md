# ADR 0001: implementación inicial del recorrido completo

Estado: adoptada durante la primera implementación.

Se mantiene el diseño C#/.NET 10, TCP/TLS, HMAC y PostgreSQL del documento de especificación.

Para reducir dependencias y hacer explícitas las transacciones de deduplicación, la primera versión usa **Npgsql y SQL parametrizado**, sin Entity Framework Core. La biblioteca `Iot.Persistence` comparte cifrado de claves, configuración y esquema con el aprovisionador. El gateway y el simulador solo dependen del contrato de protocolo, y el gateway no recibe claves de dispositivos ni credenciales de base de datos.

La consistencia se asegura bloqueando la fila del dispositivo durante el procesamiento autenticado. Así se serializan operaciones del mismo dispositivo entre réplicas, mientras dispositivos distintos pueden avanzar en paralelo. Es una decisión inicial sencilla: el rendimiento de una única identidad muy activa puede quedar limitado, y se medirá antes de optimizarla.

Se conservan permanentemente las respuestas de acciones aceptadas. Las denegaciones se auditan, pero todavía no se almacenan como respuestas idempotentes en `requests`: se vuelven a evaluar, porque no tienen efecto matemático. Esta precisión actualiza la formulación general de la especificación.

La migración inicial se aplica mediante `provision seed`, bajo un bloqueo de transacción. No se ejecuta al iniciar cada réplica. El esquema inicial es idempotente; las modificaciones futuras requerirán migraciones incrementales versionadas.

No hay simulación de persistencia en memoria en el servicio productivo. Las pruebas de integración requieren PostgreSQL real; sin él se marcan como omitidas de forma visible.

Pendientes del alcance completo: drenaje planificado, reconciliación de conexiones perdidas, perfiles avanzados del simulador y verificación efectiva del despliegue Docker/balanceador. No se debe describir esta primera versión como el TFM acabado.
