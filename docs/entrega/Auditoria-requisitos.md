# Auditoría de requisitos y preparación de entrega

Revisión: 15 de octubre de 2026. Proyecto: Pasarela IoT TCP escalable. Referencia publicada: v1.0.0, commit 245b834. Este informe es una revisión del proyecto y de su entrega, no una calificación emitida por BigSchool.

## Decisiones posteriores del autor

Actualización del 29 de octubre de 2026: la [presentación final de doce diapositivas](Presentacion-TFM.pptx), el manual actualizado y el [servicio publicado, probado desde datos móviles](../validacion-proxmox.md) están preparados. La [guía de acceso](../acceso-evaluador.md) contiene el endpoint operativo. La matriz que sigue conserva la auditoría histórica del 15 de octubre y no sustituye el [estado vigente](../estado-proyecto.md).

Este informe conserva el estado observado durante la auditoría, antes de los cambios documentales siguientes. El autor preparará sus diapositivas personalmente con la nueva guía PDF y no tiene previsto grabar vídeo. Tras revisar el aviso de GitHub, ha decidido mantener el repositorio público por ahora; no se han añadido colaboradores. Se incorporan el manual PDF, su fuente y la guía a main después de v1.0.0. Las menciones a repositorio público, archivos locales o presentación terminada en la matriz describen el estado auditado, no el cierre actual. Para el estado vigente, consultar [materiales de entrega](README.md). La entrega sin vídeo debe aclararse con BigSchool por el campo obligatorio observado; no se ha contactado al centro ni enviado el formulario.

## Dictamen

Los entregables técnicos principales exigidos en el PDF oficial están preparados: README, código con historial Git, información de ejecución y presentación. Las funciones centrales acordadas tienen implementación y evidencia de pruebas. **La entrega todavía no está cerrada**: el formulario actual exige una URL de vídeo que aún no existe, el autor debe revisar y comprender la solución y el formulario no se ha enviado.

No es correcto afirmar que se ha cumplido literalmente cada ampliación de la especificación inicial del proyecto. Varias se han aplazado o acotado y deben presentarse como tales, sin confundirlas con requisitos obligatorios de la escuela.

## Fuentes y comprobaciones

1. Se han releído las dos páginas completas de `Doc/Documentacion-TFM.pdf`.
2. Se ha descargado de nuevo el [PDF oficial](https://campus.thebigschool.com/wp-content/uploads/2026/01/Documentacion-TFM.pdf) y comparado su SHA-256 con la copia local: son idénticos.
3. Se ha abierto la [lección Proyecto Final de E.1](https://campus.thebigschool.com/cursos/master-en-desarrollo-con-ia/modulos/proyecto-final/leccion/proyecto-final/) con la sesión del autor. Se han leído los atributos `required` de los controles del formulario, sin modificar valores ni intentar enviarlo.
4. Se ha contrastado el análisis previo de las transcripciones de la lección y de los dos directos de E.1. No se han vuelto a reproducir todas esas horas de vídeo en esta auditoría.
5. Se han revisado README, estado, especificación, decisiones, guía de entrega, guion, estructura de código, contratos del protocolo y evidencias seleccionadas.
6. Se han leído las diez diapositivas del PowerPoint publicado y verificado que cubren problema, arquitectura, funcionamiento, pruebas, uso de IA y límites.
7. La API pública de GitHub confirma repositorio público, rama main, etiqueta v1.0.0 y ejecución 38775260191 completada con conclusión success en el commit f6ef8b9. El último commit de la etiqueta solo cambió documentación y evidencia.
8. No hay enlaces locales rotos entre los Markdown versionados ni carpetas secrets, .tools o tmp entre sus archivos rastreados. Esta comprobación de rutas no equivale a una auditoría exhaustiva de seguridad del contenido.
9. Se ha confirmado que el manual y PDF creados después de v1.0.0 no están en esa etiqueta: las rutas públicas consultadas devuelven 404 y los archivos locales están sin versionar.

No se ha vuelto a ejecutar toda la batería de carga o fallos durante esta revisión documental. Se utilizan los resultados fechados, el código y la ejecución de CI conservada. No se han alterado servicios, permisos ni el formulario del campus.

## 1. Requisitos oficiales del PDF

| Requisito | Estado | Evidencia y precisión |
|---|---|---|
| Aplicación real, original y de valor profesional | Cubierto como producto | Backend independiente inspirado en la comunicación IoT, protocolo propio y datos ficticios. La valoración de originalidad y aprendizaje corresponde al evaluador. |
| README: descripción general | Cumple | Explica objetivo, escenario, flujo y exclusión de pagos reales. |
| README: stack | Cumple | C#/.NET, PostgreSQL, Docker, HAProxy, Npgsql y pruebas. |
| README: instalación y ejecución | Cumple | Comandos de clonación, aprovisionamiento, arranque, simulación y parada. |
| README: estructura | Cumple | Relaciona proyectos, despliegue, pruebas y documentación. |
| README: funcionalidades | Cumple | Inicialización, suma, totales, autenticación y replicación. |
| Acceso al código con control de versiones | Cumple | Repositorio público JrgPS/tfm-pasarela-iot con historial y etiqueta estable. |
| Despliegue/publicación funcionando | Recomendación parcialmente cubierta | Docker local verificado y CI con demo real. No hay servicio público remoto para usar directamente. El PDF lo recomienda siempre que sea posible, no lo formula como obligación universal. |
| Slides accesibles | Cumple | PowerPoint de diez diapositivas dentro del repositorio, con URL estable por etiqueta. |
| Documentación, información de despliegue y slides en el directorio del código | Cumple | README, Compose, scripts y docs/entrega/Presentacion-TFM.pptx. |
| Envío por el formulario de Proyecto Final | Pendiente | No hay envío realizado en este trabajo. |
| Datos de matrícula y enlaces correctos | Revisión final pendiente | El autor debe verificar su email de inscripción y los enlaces antes del envío. No se publican sus datos personales en este informe. |

El PDF oficial no fija número de tests, páginas de memoria, diapositivas, stack obligatorio ni exige un LLM dentro del producto. No hace falta añadir Kubernetes, un chatbot o pagos reales para cumplir esos recursos de entrega.

## 2. Hallazgo prioritario: vídeo requerido por el formulario

El análisis previo recogía que el profesor describía el vídeo como opcional. El PDF oficial tampoco lo enumera expresamente entre los recursos obligatorios. Sin embargo, **el control actual URL Vídeo es de tipo URL y tiene `required=true`**. Este dato no se había comprobado en la revisión anterior y corrige la afirmación de que el vídeo podía omitirse sin más al presentar la entrega.

| Campo del formulario actual | Tipo | Obligatorio en HTML |
|---|---|---|
| Nombre | text | Sí |
| Apellidos | text | Sí |
| Email | email | Sí |
| URL Repositorio de GitHub | url | Sí |
| URL Despliegue o Publicación (si existe) | url | No |
| URL Slides | url | Sí |
| URL Vídeo | url | Sí |
| Usuario de prueba (si tu proyecto tiene login) | text | No |
| Contraseña de prueba (si tu proyecto tiene login) | text | No |

La observación es sobre el formulario servido en esta sesión. No se ha probado su validación de backend ni se ha enviado una entrega de prueba. No debe inventarse un enlace ni alterar los atributos del formulario para omitir la condición.

Para entregar por la vía normal tal como está, se necesita grabar una demostración, publicarla con acceso para el evaluador y usar su URL. Si se pretende entregar sin vídeo, antes habría que aclarar con la escuela la discrepancia entre las explicaciones y el formulario. No se ha enviado ningún mensaje al centro.

El guion ya disponible permite una grabación breve mostrando arquitectura, una simulación, resultados, recuperación y límites. Por tratarse de un backend TCP sin despliegue público, el vídeo también aporta evidencia útil de funcionamiento.

## 3. Requisitos funcionales acordados con el autor

| Requisito central | Resultado de revisión |
|---|---|
| Conexión TCP persistente | Implementada con TLS, sockets asíncronos y delimitación de tramas. |
| Fabricante 2, modelo 2, serie 11 | Implementados y preservando ceros iniciales. |
| Fecha de diez dígitos | Implementada como yyMMddHHmm UTC, con regla de siglo y validación. |
| Acciones 001, 002 y 003 | Implementadas como inicialización, suma y totales. |
| Identidad, resultado de tres dígitos y comercio de nueve | Implementados. Comercio oculto cuando no procede autenticar/divulgarlo; el código 004 también lo oculta en la versión actual. |
| Comprobar registro en base independiente | registry consulta PostgreSQL, verifica estado y HMAC. |
| Registrar conexión y datos enviados | Implementado para el recorrido funcional con base disponible. La auditoría durante averías o bytes rechazados tiene límites documentados. |
| Integridad del paquete | HMAC-SHA-256 por dispositivo; además TLS y cifrado de claves. |
| Dos servicios replicables y base separada | Implementados con Compose y HAProxy. |
| Escalado manual | Implementado y ensayado con 1+1 y 2+2. No se promete mejora lineal ni HA de todo el conjunto. |
| Simulador de cientos/miles configurables | Configurable por manifiesto. Ensayos de 100, 500 y 1000; no se garantiza capacidad de cualquier número de dispositivos. |
| Cientos de operaciones concurrentes y consistencia | Evidencia funcional y de carga, incluido el ensayo sostenido de 500 y las pruebas de concurrencia. |
| Datos ficticios y presupuesto | No hay pagos reales ni servicios contratados para ejecutar el proyecto. |
| Repositorio GitHub | Publicado y accesible sin credenciales. |

Las 29 pruebas locales y el CI correcto respaldan funciones y condiciones concretas. No prueban ausencia universal de errores ni sustituyen la comprensión del autor.

## 4. Diferencias respecto a la especificación ampliada inicial

La especificación inicial se conserva como contrato de diseño histórico. Debe evitarse que el evaluador confunda todo lo que propone con lo que se entregó:

- Se utiliza Npgsql con SQL parametrizado; no se implementó Entity Framework Core.
- Solo se conservan respuestas aceptadas en requests. Las denegaciones se auditan y reevalúan.
- El simulador no ofrece todos los porcentajes configurables de errores, distribuciones de operandos, semillas y métricas por acción descritos inicialmente. Existen pruebas específicas de errores, pero no equivalen a esos modos del simulador.
- No hay un heartbeat autónomo periódico ni calendario de escalado implementado.
- La versión verificada se ejecuta en Docker Desktop; no se desplegó en Proxmox, Synology ni cloud.
- Los ejemplos de tramas en la especificación son esquemáticos y contienen marcadores. Existe un vector HMAC independiente y pruebas reales, pero falta un ejemplo completo de protocolo con valores sintéticos, firma calculada y resultado esperado publicado como tal.
- El flujo de recuperación y la restauración están ensayados; no se ha localizado una evidencia separada que documente todos los casos ampliados, por ejemplo llegada lenta controlada o reinicio completo del conjunto comparando recuentos antes/después. No se presentan como pruebas ejecutadas en esta auditoría.

Estas diferencias no añaden automáticamente obligaciones al PDF del curso. Deben declararse como decisiones, alcance acotado o mejoras, y no afirmarse como funciones ya completas.

## 5. Correcciones documentales antes del cierre

1. **Actualizar el estado del vídeo.** La guía de entrega, el manual PDF y otras menciones lo presentan como opcional sin la precisión del formulario actual. Debe registrarse la discrepancia y el enlace real cuando exista.
2. **Aclarar documentos históricos.** La especificación inicial habla de EF Core y despliegue en Proxmox; ADR 0001 conserva pendientes de drenaje/reconciliación ya resueltos después. Conviene añadir un estado actual visible y enlazar el resultado final. No es necesario borrar el historial.
3. **Publicar el manual si se quiere incluir.** El PDF de 26 páginas y su Markdown solo están locales. El evaluador de v1.0.0 no los recibe por usar el enlace del repositorio. El README obligatorio sí está publicado y este PDF adicional no es una exigencia oficial independiente.
4. **Consolidar enlaces estables.** Para slides puede usarse la ruta con v1.0.0; para una revisión con vídeo o manual nuevos, crear un nuevo cierre documental sin mover la etiqueta ya publicada.

La matriz anterior distingue cumplimiento objetivo de recursos, mejoras de presentación y condiciones de evaluación que requieren intervención del alumno. No declara suspendido el proyecto por carecer de un despliegue público o de funciones fuera del MVP.

## 6. Secuencia mínima para poder entregar

1. El autor realiza una revisión guiada: explica inicialización, TLS/HMAC, delimitación TCP, transacción, idempotencia y límites de las réplicas. Esta comprensión no se puede certificar por generar archivos.
2. Según la decisión posterior del autor, no se grabará vídeo. Aclara con BigSchool la vía de entrega sin vídeo antes de enviar el formulario.
3. Actualiza la documentación sobre vídeo, pendientes históricos y estado del alcance. Publica el manual si se incorpora a los materiales.
4. Comprueba los enlaces definitivos de código, slides y vídeo. Deja despliegue vacío si no existe URL real y credenciales vacías al no haber login web; el formulario actual no exige esos campos.
5. Revisa nombre y email de matrícula y envía el formulario desde el campus. La publicación en GitHub no constituye por sí sola una entrega a BigSchool.

No es necesario desarrollar una nueva función principal antes de estos pasos. La grabación, coherencia documental y envío son ahora más importantes que añadir alcance técnico.

## Enlaces para revisión

- [Código público](https://github.com/JrgPS/tfm-pasarela-iot).
- [Versión estable](https://github.com/JrgPS/tfm-pasarela-iot/tree/v1.0.0).
- [Slides](https://github.com/JrgPS/tfm-pasarela-iot/blob/v1.0.0/docs/entrega/Presentacion-TFM.pptx).
- [CI comprobado](https://github.com/JrgPS/tfm-pasarela-iot/actions/runs/38775260191).
- [Requisitos oficiales](https://campus.thebigschool.com/wp-content/uploads/2026/01/Documentacion-TFM.pdf).
- [Formulario del campus](https://campus.thebigschool.com/cursos/master-en-desarrollo-con-ia/modulos/proyecto-final/leccion/proyecto-final/).

Informe creado tras v1.0.0 e incorporado después a main junto al manual y la guía. No se modifica la etiqueta ni el producto, y el formulario sigue sin enviarse.
