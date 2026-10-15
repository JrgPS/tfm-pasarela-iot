# Definición inicial del proyecto IoT

> Documento histórico de la primera propuesta. Las respuestas posteriores y las decisiones consolidadas están en [Especificación del TFM](Especificacion-TFM-Pasarela-IoT.md), que es la referencia actual para implementar.

Estado: propuesta pendiente de concretar con el alumno. Fecha: 15/10/2026.

## Información confirmada

- Máster en Desarrollo con IA E.1, cursado de forma particular, sin FUNDAE.
- Objetivo temporal: entregar lo antes posible, sin fecha concreta aún.
- Perfil: ingeniero técnico informático; desarrolla una pasarela de pagos, trabaja con C++ y C#, ha empezado con .NET y utiliza Git.
- Proyecto: comunicación entre un dispositivo IoT y un servidor cloud, inspirada en su experiencia profesional y con un sistema actualizado.
- Tres componentes funcionales: servicio de conexión y validación de estructura/datos, servicio de validación del dispositivo contra un registro y base de datos separada.
- Los dos servicios de aplicación deben admitir réplicas.
- Contenedores Docker y capacidad de aumentar servicios según demanda.
- Presupuesto total máximo para completar el proyecto: 100 EUR. No se interpreta como presupuesto mensual.
- Suscripciones disponibles: ChatGPT y Gemini. Tiempo semanal variable.

## Propuesta de alcance, todavía no aceptada

Título provisional: Pasarela IoT escalable para recepción y validación de dispositivos.

Flujo inicial: dispositivo o simulador -> servicio de entrada -> servicio de registro -> base de datos. El resultado de la validación debe devolverse al emisor con un contrato de respuesta definido.

Se propone C#/.NET para los servicios por su relación con la experiencia del alumno. La versión concreta, el motor de base de datos y el protocolo quedan por elegir. No se ha decidido añadir un broker, un frontend ni lógica de pagos.

La solución necesitará distribuir tráfico entre réplicas. Dependiendo del protocolo y de la plataforma, esto puede requerir un balanceador u otro componente de infraestructura además de los tres componentes funcionales. El simulador y las herramientas de pruebas tampoco son servicios de negocio.

Docker Compose permite demostrar un despliegue reproducible y un número configurable de réplicas. No se debe presentar esa capacidad, por sí sola, como autoescalado por carga. Debe decidirse si el ajuste será manual o automático y cómo se comprobará.

Las réplicas en una sola máquina y una base de datos única no proporcionan alta disponibilidad completa frente a la caída del servidor o de la base de datos. La demostración debe describir con precisión los fallos que tolera.

## Evidencias propuestas para la entrega

- Instalación reproducible desde un repositorio con historial real.
- Casos de mensaje válido, estructura inválida y dispositivo no registrado.
- Autenticación del dispositivo con el mecanismo que se acuerde; consultar un identificador en una tabla no demuestra su identidad.
- Comportamiento definido ante indisponibilidad del servicio de registro o de la base de datos.
- Prueba reproducible de carga, midiendo peticiones o mensajes por segundo, latencia y errores, con entorno documentado.
- Comparación de una réplica y varias, sin prometer una mejora no medida.
- Prueba de caída de una réplica con tráfico activo, distinguiendo continuidad de nuevas peticiones y pérdida/reconexión de conexiones persistentes.
- README, diagrama, presentación y demostración de funcionamiento.

## Preguntas pendientes

1. Protocolo de dispositivo: HTTP/HTTPS, TCP persistente, MQTT u otro. Dispositivo real o simulador y posibilidad de modificar su cliente.
2. Ejemplo de mensaje y respuesta, formato y tamaño aproximado; una versión ficticia es suficiente.
3. Acción posterior a la validación: solo aceptar/rechazar o almacenar/procesar los mensajes.
4. Identidad y autenticación: identificador y secreto por dispositivo, certificado u otro mecanismo. Estados admitidos: registrado, activo, bloqueado, etc.
5. Carga objetivo: conexiones simultáneas, frecuencia de envío y latencia deseada.
6. Escalado manual reproducible o autoescalado, y qué fallo se desea tolerar.
7. Preferencia de motor de base de datos y necesidad real de alta disponibilidad de la base de datos en el MVP.
8. Entorno disponible: Docker, CPU/RAM del equipo y alojamiento existente.
9. Publicación del código y demo, usando una implementación propia y datos ficticios; restricciones profesionales concretas si las hay.

## Referencias técnicas consultadas

- [Docker Compose](https://docs.docker.com/compose/).
- [Ajuste de réplicas con Docker Compose](https://docs.docker.com/reference/cli/docker/compose/scale/).
- [Definición de servicios](https://docs.docker.com/reference/compose-file/services/).
- [Balanceo con YARP](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/load-balancing?view=aspnetcore-10.0), posibilidad por evaluar si se selecciona HTTP.

No hay un proveedor cloud elegido ni un gasto autorizado ejecutado. La estimación económica se hará al cerrar el protocolo y la forma de demostración.
