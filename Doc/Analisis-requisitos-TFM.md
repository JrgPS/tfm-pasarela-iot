# Requisitos del TFM: Máster en Desarrollo con IA E.1

Revisión realizada el 15 de octubre de 2026 a partir del PDF local y del campus autenticado. Este documento es una síntesis de trabajo, no una rúbrica oficial nueva.

## Fuentes consultadas

- PDF local: `Doc/Documentacion-TFM.pdf`, dos páginas, leído íntegramente.
- Temario del [Máster en Desarrollo con IA E.1](https://campus.thebigschool.com/cursos/master-en-desarrollo-con-ia/), revisado como índice para conocer los bloques y localizar materiales del TFM. No se han revisado todas las clases del máster.
- [Proyecto Final](https://campus.thebigschool.com/cursos/master-en-desarrollo-con-ia/modulos/proyecto-final/leccion/proyecto-final/): descripción, formulario, pestaña Materiales y transcripción autogenerada en español del vídeo de 18:47. Leída hasta su cierre.
- [Directo de la primera edición del 28/01/2026](https://campus.thebigschool.com/cursos/master-en-desarrollo-con-ia/modulos/directo-proyecto-final/leccion/directo-proyecto-final-brais-moure-28-01-2026/): transcripción autogenerada en español, leída hasta el cierre, aproximadamente 1:11:58 según el reproductor. El índice indica una duración distinta.
- [Directo de dudas finales de la primera edición del 17/02/2026](https://campus.thebigschool.com/cursos/master-en-desarrollo-con-ia/modulos/directo-proyecto-final/leccion/directo-proyecto-final-ii-brais-moure-17-02-2026/): transcripción autogenerada en español, leída hasta el cierre, aproximadamente 1:30:24. El título interno del vídeo usa 16/02; aquí se conserva la fecha de la lección.
- La pestaña Materiales de la lección principal enlaza a [Documentación TFM](https://campus.thebigschool.com/wp-content/uploads/2026/01/Documentacion-TFM.pdf). El contenido de esa copia remota no se ha descargado ni comparado con el PDF local.

Las transcripciones permiten revisar las explicaciones verbales; no equivalen a una revisión audiovisual de todas las imágenes mostradas. Sus posibles errores de transcripción no deben interpretarse como nuevas condiciones de entrega.

## Objetivo y evaluación

- Aplicación real, propia y útil, relacionada con los intereses personales o profesionales del alumno.
- Libertad de tecnología y tipo de software: web, móvil, escritorio, backend, terminal u otros.
- Debe demostrar aprendizaje, criterio y comprensión del producto y del código.
- La evaluación es aprobado o suspenso. No se establece nota numérica ni una rúbrica de puntos.
- No se fija un tamaño mínimo, una arquitectura obligatoria, un número de funcionalidades o un número de tests.
- Se espera calidad proporcionada: estructura, buenas prácticas, validación, seguridad, pruebas y usabilidad según las necesidades del proyecto.
- Se permite y fomenta el desarrollo asistido por IA. El alumno debe supervisarlo y entender las decisiones; generar código sin revisarlo ni comprenderlo no cumple el propósito del TFM.
- Las fuentes revisadas no exigen expresamente que el producto incorpore una función de IA en ejecución. El máster trata también de desarrollar software con ayuda de IA.

Referencias: vídeo principal, aproximadamente 04:11–08:43 y 11:02–12:38; dudas finales, 50:16–51:46 y 1:15:46–1:18:24.

## Entregables

### Código y control de versiones

- Facilitar acceso al código; preferiblemente mediante repositorio público de GitHub.
- Los directos insisten en usar Git y conservar la evolución del desarrollo. Un ZIP sin control de versiones no debe ser el procedimiento previsto.
- Se admiten repositorios privados con acceso del evaluador según las instrucciones del PDF. Debe haber una vía accesible que explique qué recursos son privados y cómo identificar la invitación.
- Si hay varios repositorios, entregar uno principal y enlazar los demás desde su README.
- Mantener una versión estable identificable para la evaluación, preferiblemente en `main`. Se puede fijar una versión de entrega mediante etiqueta.
- Los ficheros de reglas, contexto, especificaciones y decisiones pueden aportar evidencia del proceso; no son todos entregables obligatorios independientes.

Referencias: directo 28/01, 28:03–29:31, 31:13–32:58 y 58:38–1:00:39; dudas finales, 06:09–09:43, 31:17–32:25 y 1:12:14–1:13:09.

### Documentación

README.md en la raíz del proyecto, con:

1. Problema, usuarios y descripción general.
2. Stack tecnológico y explicación de las decisiones relevantes.
3. Requisitos previos, configuración, instalación y ejecución reproducibles.
4. Servicios externos e infraestructura que necesita el proyecto.
5. Estructura y arquitectura general.
6. Funcionalidades principales y cómo utilizarlas.
7. Enlaces a despliegue, presentación y otros repositorios, si los hay.
8. Limitaciones o condiciones especiales de acceso claramente explicadas.

El PDF establece los apartados básicos; los directos aclaran la importancia de explicar decisiones e infraestructura. No se exige una memoria académica separada de un número concreto de páginas. La documentación extensa puede distribuirse en documentos enlazados desde el README, como decisión de organización del proyecto.

Referencias: vídeo principal, 08:51–09:46; dudas finales, 16:46–22:05 y 49:07–49:58.

### Despliegue y demostración

- El PDF lo recomienda cuando sea posible. En los directos se recalca especialmente para proyectos web y servicios que se puedan publicar.
- Hay que facilitar que el evaluador vea y pruebe el producto funcionando; no basta con que el código parezca correcto.
- Para herramientas locales o casos justificados de infraestructura privada, se admite una demostración en vídeo y documentación de ejecución.
- El formulario incluye usuario y contraseña de prueba si el proyecto tiene login. Se preparará una cuenta de demostración independiente si procede.
- Si existen costes que impiden mantener un servidor encendido, explicar la situación y cómo solicitar su activación. El profesor admite esta coordinación; no se debe interpretar como garantía actual de una fecha de revisión.
- Un MVP es válido si sus funciones centrales están completas y son utilizables. Las ampliaciones futuras pueden quedar fuera; las funciones centrales a medias no constituyen una entrega suficiente.

Referencias: vídeo principal, 12:44–15:00; directo 28/01, 33:12–35:23, 38:00–45:59 y 50:01–51:01; dudas finales, 27:18–30:36, 40:28–44:57, 53:41–56:21 y 1:13:16–1:14:17.

### Presentación y vídeo

- Diapositivas que expliquen el problema, el valor de la solución, cómo funciona y cómo se ha construido.
- Formato libre: Google Slides, PowerPoint, Canva o PDF de presentación, entre otros.
- No se fija un mínimo o máximo de diapositivas.
- Incluirlas en el directorio del código o enlazarlas de forma accesible desde la documentación.
- El formulario añade un campo URL Vídeo. El profesor aclara expresamente que el vídeo es opcional; es útil para demostrar el producto y especialmente cuando no sea posible probar un despliegue.
- La defensa se realiza mediante documentación y diapositivas; las explicaciones revisadas no establecen una defensa oral en directo obligatoria.

Referencias: vídeo principal, 13:50–15:00; directo 28/01, 57:33–58:12; dudas finales, 45:58–46:07, 47:23–48:31 y 50:16–51:46.

## Formulario de entrega observado

La lección principal muestra los siguientes campos:

- Nombre.
- Apellidos.
- Email.
- URL Repositorio de GitHub.
- URL Despliegue o Publicación (si existe).
- URL Slides.
- URL Vídeo.
- Usuario de prueba (si tu proyecto tiene login).
- Contraseña del usuario de prueba (si tu proyecto tiene login).

La presencia de un campo no demuestra por sí sola que sea obligatorio. No se ha enviado ni modificado el formulario.

## Plazos y corrección

- El PDF indica 23/02 sin año. Los directos de enero y febrero de 2026 lo contextualizan como el 23 de febrero de 2026 para la primera edición.
- En esas explicaciones se admiten entregas posteriores y correcciones del proyecto. La corrección fuera de plazo puede tardar más.
- La promesa de revisión en menos de un mes se dio para las entregas dentro del plazo de la primera edición; no debe extrapolarse a una entrega en octubre de 2026 o posterior.
- Si el proyecto no se aprueba, el profesor indica que se comunican motivos y se pueden enviar mejoras.
- Si el alumno está acogido a FUNDAE, hay condiciones administrativas específicas. Se debe consultar su situación vigente con la escuela; no extrapolar fechas ni porcentajes históricos del directo.
- El directo final indica que la evaluación se comunica por correo, con información de titulación.

Referencias: directo 28/01, 11:03–13:38, 16:38–21:18 y 1:11:21–1:11:42; dudas finales, 02:36–05:12, 53:05–53:28 y 1:03:58–1:05:43.

## Relación con el temario

El índice del campus permite vincular el proyecto con análisis de requisitos, principios de diseño y arquitectura, fundamentos de IA, herramientas de desarrollo asistido, APIs, contexto y reglas de agentes, testing, seguridad, documentación, usabilidad, GitHub Actions, cloud y bases de datos. También contiene LangChain/RAG, LLMOps y agentes como posibilidades, sin que ello convierta todas esas tecnologías en obligatorias para el TFM.

## Decisiones pendientes del alumno

Actualización: las respuestas del alumno y las decisiones técnicas están recogidas en [Especificación del TFM](Especificacion-TFM-Pasarela-IoT.md). La siguiente lista conserva las cuestiones identificadas durante la revisión inicial.

1. Situación de matrícula: particular o FUNDAE, y fecha objetivo personal de entrega.
2. Problema que le interesa resolver, profesión o ámbito de uso, y usuarios previstos.
3. Idea existente o necesidad de explorar propuestas; posible trabajo previo aprovechable.
4. Experiencia real y qué partes quiere aprender a comprender y explicar durante el desarrollo.
5. Recursos, datos disponibles, restricciones de privacidad y presupuesto.
6. Preferencias de publicación del código y de la aplicación.

## Límites de esta revisión

Se han revisado las fuentes enumeradas, no todas las clases del máster. Se han localizado contenidos posteriores de otras ediciones, incluidos Primeros pasos del TFM y un directo de la segunda edición; no se han leído sus transcripciones en esta revisión. Si aparece una discrepancia o una indicación reciente aplicable a esta matrícula, se contrastará antes de cerrar la entrega.
