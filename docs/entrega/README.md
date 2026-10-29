# Materiales de entrega del TFM

Proyecto: **Pasarela IoT TCP escalable**, Máster en Desarrollo con IA E.1.

## Contenido preparado

- [Manual completo del proyecto (PDF)](Manual-completo-Pasarela-IoT-TFM.pdf) y [fuente Markdown](Manual-proyecto.md): funcionamiento, instalación, protocolo, seguridad, pruebas y límites.
- [Guía de contenido de diapositivas (PDF)](Guia-contenido-diapositivas-TFM.pdf): propuesta de doce diapositivas para que el autor prepare personalmente la presentación definitiva.
- [Presentación final del autor](Presentacion-TFM.pptx): doce diapositivas; versión corregida y autorizada para publicación el 18 de octubre de 2026. Las diapositivas son imágenes, no texto editable.
- [Evidencia del archivo final](../evidence/presentacion-final.json): huella SHA-256 y comprobaciones de integridad.
- [Auditoría de requisitos](Auditoria-requisitos.md): revisión de evidencias y condiciones de entrega.
- [Guion de demostración](guion-demostracion.md): comandos, relato y preguntas para practicar la explicación del proyecto; no implica una defensa oral obligatoria.
- [README principal](../../README.md): instalación y uso.
- [Especificación](../../Doc/Especificacion-TFM-Pasarela-IoT.md): requisitos completos.
- [Validación Docker](../validacion-docker.md) y [fallos de dependencia y sobrecarga](../validacion-fallos.md): resultados con evidencia.
- [Uso de IA](../ai-development.md): explicación del proceso asistido.
- [Instalación limpia verificada](../evidence/clean-install.json): clon local sin secretos, base nueva y 50 operaciones correctas de diez dispositivos.

## Datos para el formulario

| Campo | Contenido |
|---|---|
| Nombre, apellidos y email | Los datos de matrícula del alumno. |
| URL GitHub | https://github.com/JrgPS/tfm-pasarela-iot |
| URL despliegue / instrucciones de acceso | https://github.com/JrgPS/tfm-pasarela-iot/blob/main/docs/acceso-evaluador.md — endpoint operativo TCP/TLS: iot.xjps.es:7443, verificado desde datos móviles. |
| URL Slides | https://github.com/JrgPS/tfm-pasarela-iot/blob/main/docs/entrega/Presentacion-TFM.pptx |
| URL Vídeo | El autor no tiene previsto grabarlo. La revisión del 15 de octubre observó un campo obligatorio; aclarar con BigSchool la discrepancia con las explicaciones que lo describían como opcional. |
| Credenciales de prueba | Identidad ficticia y certificado público en paquete privado facilitado por el autor. No hay login web ni que dar acceso administrativo al servidor. |

Texto sugerido para describir el despliegue: «Servicio backend TCP/TLS en iot.xjps.es:7443, verificado desde una conexión móvil externa. La guía enlazada explica cómo utilizar el simulador con el paquete privado de prueba facilitado al evaluador».

## Antes de enviar

1. Comprobar que el repositorio y la presentación son accesibles al evaluador.
2. Consultar la [ejecución correcta de GitHub Actions](https://github.com/JrgPS/tfm-pasarela-iot/actions/runs/38775260191).
3. Revisar personalmente la solución y practicar su explicación con la presentación final ya incorporada.
4. Aclarar con BigSchool cómo entregar sin vídeo si el campo URL Vídeo sigue siendo obligatorio, como en la revisión del 15 de octubre. No usar enlaces ficticios.
5. Revisar los campos y enviar personalmente el formulario del campus.

El formulario del campus no se ha enviado. No se han inventado enlaces, credenciales de evaluador ni una grabación inexistente.

El repositorio se mantiene público por decisión del autor. No se han añadido colaboradores. Antes de entregar debe comprobarse que el evaluador puede acceder al código y a los materiales. Los dos trabajos de [GitHub Actions](https://github.com/JrgPS/tfm-pasarela-iot/actions/runs/38775260191) terminaron correctamente: compilación y pruebas, y demo Docker con TCP/TLS real. La [evidencia de CI](../evidence/github-actions.json) identifica el commit comprobado. El trabajo Docker realiza su propio checkout y genera secretos nuevos en el runner. Cuando el repositorio era público se verificó un [clon sin credenciales](../evidence/public-clone.json). El clon limpio inicial con Docker conserva su informe por separado.

## Origen de la presentación final

La presentación final procede de la versión preparada por el autor y de la edición de sus imágenes en las diapositivas 1, 4, 5, 11 y 12. Las otras siete imágenes se conservaron idénticas. Se revisaron visualmente las doce diapositivas y se validaron el paquete y su importación. El original del autor se conserva localmente.

El script `scripts/build-presentation.mjs` genera únicamente el antiguo borrador de diez diapositivas; no reproduce la presentación final. Usa `@oai/artifact-tool` del entorno de documentos. Requiere `ARTIFACT_TOOL_MODULES` con el directorio de módulos, `PRESENTATIONS_SKILL` con la carpeta de la habilidad y `ARTIFACT_PYTHON` con el ejecutable Python. No son requisitos para ejecutar la pasarela. El borrador, los renders y los informes internos de revisión quedan en `tmp/presentation/`, excluido de Git.

## Versión de entrega

La etiqueta `v1.0.0` identifica la versión preparada. El [ZIP de GitHub](https://github.com/JrgPS/tfm-pasarela-iot/archive/refs/tags/v1.0.0.zip) permite descargar sus archivos. La [presentación de esa versión](https://github.com/JrgPS/tfm-pasarela-iot/blob/v1.0.0/docs/entrega/Presentacion-TFM.pptx) conserva el borrador de referencia, no la presentación definitiva del autor. El manual, la guía PDF y la presentación final están en main después de v1.0.0; la etiqueta original no se modifica. Para entregar las slides, usar el enlace de main indicado arriba, no el borrador de la etiqueta.

La ejecución de CI comprobó el commit `f6ef8b9`. Después se actualizaron únicamente enlaces y evidencia documental, sin cambiar código de aplicación ni configuración de CI.
