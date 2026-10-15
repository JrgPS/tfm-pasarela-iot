# Estado del proyecto

Actualizado el 15 de octubre de 2026 tras la validación local y en GitHub Actions. El MVP funciona en contenedores Linux y tiene evidencia de pruebas funcionales, carga y recuperación. El autor ha solicitado repositorio privado y preparará sus diapositivas con la guía PDF. Los permisos del evaluador quedan para más adelante. No prevé grabar vídeo; la discrepancia con el campo obligatorio del formulario debe aclararse antes de enviar. Falta la presentación definitiva, la revisión final y el envío al campus.

## Implementado

- Solución C#/.NET 10: protocolo, persistencia, registro, gateway, aprovisionamiento, simulador y pruebas.
- TCP persistente con TLS, HMAC por dispositivo, fechas UTC, campos de longitud fija y mensajes ASCII/LF acotados.
- Inicialización, suma exacta y consulta de totales; PostgreSQL, transacciones e idempotencia entre réplicas.
- Claves de dispositivo cifradas y secretos privados montados en los servicios que los necesitan.
- Dos gateways y dos registros con HAProxy; límite de conexiones y trabajo activo por gateway.
- Actualización transaccional de actividad, reconciliación de conexiones perdidas y drenaje al apagar un gateway.
- Simulador con inicio sincronizado, reconexión y reintentos, comprobación de totales, métricas JSON/CSV.
- Scripts de arranque, validación, fallos de réplica, comparación de configuraciones y copia/restauración.
- GitHub Actions ejecutado correctamente: trabajo de compilación/pruebas y trabajo de demo Docker con TCP/TLS real.

## Verificado

1. Construcción de las imágenes y despliegue en Docker Desktop con motor Linux, 8 CPU virtuales y aproximadamente 7,6 GiB de RAM asignada.
2. Aprovisionamiento de 1000 dispositivos ficticios para el simulador y datos adicionales aislados de las pruebas de red.
3. **29 pruebas correctas, cero fallos y cero omisiones** en Docker: 15 de protocolo, 9 de PostgreSQL y 5 de TCP/TLS real.
4. Recorrido completo: dispositivo, balanceador, gateway, registro y PostgreSQL. Autenticación, suma, respuesta y totales comprobados.
5. Perfiles cortos sincronizados de 100, 500 y 1000 dispositivos, sin fallos finales ni diferencias de totales.
6. Prueba sostenida: **500 dispositivos y 60.000 operaciones en 605,59 segundos**, cero fallos y cero reintentos; media de 99,08 operaciones/s y p95 de la suma de 47,68 ms.
7. Caída abrupta y apagado ordenado de un gateway, y caída abrupta de un registro: 10.000 sumas confirmadas en cada caso, reconexiones observadas y totales correctos.
8. Pérdida deliberada de una respuesta tras commit y reintento por otra conexión sin repetir el efecto.
9. Restauración de una copia lógica en una base nueva: mismos dispositivos y operaciones del punto de copia, cero incoherencias de totales. La base temporal se eliminó al terminar.
10. Comparación exploratoria 1+1 frente a 2+2 réplicas y metadatos de imágenes y recursos capturados.

El [informe con evidencia](validacion-docker.md) contiene las métricas exactas. [Pruebas reproducibles](pruebas-reproducibles.md) detalla los comandos y las condiciones. Los resultados TRX locales están en `results/tests/docker-tests.trx`.

## Interpretación de los resultados

- La prueba de diez minutos cumple el objetivo inicial de 500 dispositivos, confirmación superior al 99 % y p95 inferior a un segundo.
- El perfil corto de 1000 dispositivos es una prueba de estrés con ráfagas; su p95 supera un segundo. No se declara que ese perfil cumpla el objetivo de latencia de la carga de 500.
- La comparación exploratoria no mostró mejora de rendimiento al duplicar réplicas. No se atribuye a una causa concreta sin más análisis; la tolerancia a fallos sí quedó demostrada.
- Cliente y servidores comparten el mismo equipo. No son cifras de producción ni un compromiso de capacidad máxima.
- HAProxy, PostgreSQL y el host son puntos únicos de fallo. La prueba no demuestra alta disponibilidad integral.

## Incidencias resueltas o contextualizadas

- El bloqueo anterior de Windows por Control de aplicaciones no se ha desactivado: la ejecución se ha validado dentro de Docker Linux.
- Un pool predeterminado de cien conexiones en una prueba saturó PostgreSQL al compartir servidor con la demo. Se limitó a veinte y la batería pasó.
- La imagen inicial emitía un aviso por biblioteca GSSAPI ausente. La dependencia está incluida en la imagen de ejecución actual.

## Pendientes para la entrega

1. Revisar los [enlaces de entrega](entrega/README.md). CI terminó correctamente y el clon sin credenciales fue comprobado cuando el repositorio era público; esa evidencia es histórica.
2. Preparar la presentación final siguiendo la guía de [materiales de entrega](entrega/README.md). Aclarar con la escuela cómo entregar sin vídeo, por la discrepancia del formulario.
3. Facilitar al evaluador repositorio y presentación accesibles, y enviar el formulario del campus tras la revisión del autor. El perfil actual ofrece ejecución local reproducible.

Si se decide añadir un despliegue remoto, revisar antes privilegios de base de datos, límites de autenticación y certificados. Las etiquetas de imágenes son mutables y conviene fijar digests para conservación a largo plazo. Las imágenes usadas en los ensayos y en el cierre están identificadas en los metadatos, sin afirmar que una descarga futura producirá exactamente los mismos binarios.

Mejoras posteriores al mínimo: mezclas configurables de escenarios inválidos en el simulador, métricas de todas las acciones, escalado por calendario y un journal duradero para auditoría durante averías. En rechazos de transporte la auditoría conserva la causa; no se garantiza todavía una muestra de los bytes rechazados ni reenvío de toda auditoría tras caída de PostgreSQL.

## Entorno y confidencialidad

Los servicios quedan disponibles únicamente en `localhost:7443`. La API interna y PostgreSQL no publican sus puertos en el host. Los contenedores auxiliares de las pruebas se retiran y no se elimina el volumen de datos.

`.tools/`, `secrets/`, compilaciones, logs y resultados de ejecución están ignorados por Git. Solo se selecciona evidencia sin claves bajo `docs/evidence/`. El PDF original del campus se conserva localmente. No se han contratado servicios, consumido APIs de IA desde la aplicación ni accedido al NAS o Proxmox.

## Cierre técnico adicional

La [caída de PostgreSQL y la sobrecarga controlada](validacion-fallos.md) están verificadas con pruebas coordinadas, recuperación e idempotencia. Los rechazos observados fueron 009 y 013. La configuración habitual se restauró al terminar. El simulador reconoce 013 como rechazo temporal. Presentación y guion disponibles en la carpeta de entrega.

La [instalación limpia](evidence/clean-install.json) clonó el commit `cadd88b`, generó secretos nuevos y una base independiente, y confirmó 50 operaciones de diez dispositivos sin fallos. Se retiraron únicamente los contenedores y el volumen de ese proyecto temporal. El código de aplicación no ha cambiado desde ese commit. El [resumen de regresión](evidence/regression-tests.json) registra 29 pruebas aprobadas sin omisiones. Los [metadatos del cierre](evidence/environment-release.json) conservan las huellas del código actual.
