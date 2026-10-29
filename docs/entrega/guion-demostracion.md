# Guion para explicar y demostrar el proyecto

Duración orientativa: 6–8 minutos. Es una práctica opcional para el autor; las fuentes revisadas no establecen una defensa oral en directo obligatoria. El autor no tiene previsto grabar vídeo. El servicio publicado se prueba siguiendo [acceso del evaluador](../acceso-evaluador.md); los comandos siguientes describen la demo local. No se ha realizado una grabación con este guion.

## Preparación

Abrir el repositorio en VS Code, Docker Desktop y la presentación. Cerrar documentos personales. Mantener cerrada la carpeta `secrets/` y no mostrar variables con contraseñas. Los comandos siguientes se ejecutan desde la raíz. No hace falta mostrar la instalación completa durante el vídeo.

```powershell
.\scripts\Start-Demo.ps1 -Devices 1000 -Gateways 2 -Registries 2
docker compose ps
```

Esperar a que los servicios estén preparados antes de iniciar la simulación. La entrada publicada es TCP/TLS en localhost:7443. No hay interfaz web ni cuenta de usuario web.

## 0:00–1:00. Problema y objetivo

«Este proyecto conecta dispositivos IoT mediante TCP persistente. Separa la recepción de conexiones del registro y las operaciones para poder replicar ambos servicios. Usa dispositivos ficticios y operaciones de suma. El objetivo es demostrar comunicación autenticada, persistencia y recuperación ante fallos sin duplicar operaciones».

Mostrar las diapositivas de alcance y arquitectura. Explicar las responsabilidades del gateway, registro, PostgreSQL y HAProxy.

## 1:00–2:30. Recorrido funcional

```powershell
docker compose run --rm --build simulator --host proxy --devices 1 --operations 5
```

Mostrar el informe: un dispositivo preparado, cinco sumas confirmadas, cero fallos y totales comprobados por el cliente. Explicar la inicialización `001`, la suma `002` y la consulta `003`. El simulador realiza estas acciones automáticamente. La suma de ejemplo devuelve 667 unidades enteras de escala 100.

## 2:30–3:30. Concurrencia

```powershell
docker compose run --rm simulator --host proxy --devices 100 --operations 20 --ramp-ms 2 --interval-ms 50 --synchronize-start true
```

Mostrar los dispositivos preparados, las 2000 operaciones y las métricas. Explicar que los totales se contrastan por dispositivo y que las latencias dependen del equipo. Mostrar el informe conservado de 500 dispositivos durante diez minutos, diferenciándolo claramente de la ejecución actual.

## 3:30–5:00. Recuperación

```powershell
.\scripts\Test-Resilience.ps1 -Service gateway -Mode abrupt
```

Explicar que la prueba detiene una réplica, mantiene la otra y recupera la detenida. Mostrar las reconexiones, 10.000 operaciones confirmadas y cero fallos finales. El mismo identificador de petición permite reintentar sin duplicar el efecto. Como alternativa breve, enseñar los resultados ya guardados en `docs/evidence/` y dejar claro que son pruebas anteriores.

## 5:00–6:30. Calidad, IA y límites

Mostrar `docs/validacion-docker.md`, `docs/validacion-fallos.md` y `docs/ai-development.md`. Explicar el uso de IA para requisitos, código, pruebas y documentación, y la necesidad de revisar sus propuestas con ejecución real.

Mencionar los límites: base de datos y balanceador únicos, certificado de laboratorio, ejecución local y auditoría incompleta si PostgreSQL cae. Duplicar réplicas no mejoró la velocidad en el ensayo exploratorio. El resultado de tolerancia a fallos es independiente de esa comparación de rendimiento.

## Preguntas de defensa

| Pregunta | Explicación que debe dominar el autor |
|---|---|
| ¿Por qué TCP persistente? | Requisito del escenario IoT, conexión duradera y control de tramas sobre un flujo de bytes. |
| ¿Por qué TLS y HMAC? | TLS protege el transporte. HMAC autentica el contenido con una clave de cada dispositivo. |
| ¿Qué ocurre si se pierde la respuesta? | El commit puede existir. Repetir la trama con el mismo ID recupera la respuesta sin repetir la suma. |
| ¿Por qué no usar float para sumar? | Los enteros con escala fija evitan errores de representación decimal binaria. |
| ¿Qué significa escalar una conexión existente? | Permanece en su gateway. Nuevas conexiones se distribuyen y las perdidas reconectan. |
| ¿Tiene alta disponibilidad completa? | No. Las réplicas de aplicación toleran las interrupciones ensayadas, pero base, proxy y host siguen siendo únicos. |
| ¿Por qué no incluir un LLM en la aplicación? | El problema es comunicación y persistencia determinista. La IA se aplica al desarrollo sin añadir un coste operativo innecesario. |
| ¿Qué demuestra la sobrecarga? | La ruta de rechazo con límites temporalmente reducidos y recuperación consistente. No determina la capacidad máxima. |

## Grabación y revisión final

El autor debe revisar que puede explicar el código y los resultados antes de grabar. Comprobar que voz y texto son legibles, que no se muestran secretos y que el enlace permite reproducir el vídeo sin solicitar acceso. La grabación es opcional según las explicaciones de E.1 revisadas, pero ayuda a evaluar un servicio TCP sin despliegue público.
