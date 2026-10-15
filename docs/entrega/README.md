# Materiales de entrega del TFM

Proyecto: **Pasarela IoT TCP escalable**, Máster en Desarrollo con IA E.1.

## Contenido preparado

- [Manual completo del proyecto (PDF)](Manual-completo-Pasarela-IoT-TFM.pdf) y [fuente Markdown](Manual-proyecto.md): funcionamiento, instalación, protocolo, seguridad, pruebas y límites.
- [Guía de contenido de diapositivas (PDF)](Guia-contenido-diapositivas-TFM.pdf): propuesta de doce diapositivas para que el autor prepare personalmente la presentación definitiva.
- [Presentación editable anterior](Presentacion-TFM.pptx): borrador de referencia de diez diapositivas; pendiente de sustituir por la versión del autor.
- [Auditoría de requisitos](Auditoria-requisitos.md): revisión de evidencias y condiciones de entrega.
- [Guion de demostración](guion-demostracion.md): comandos, relato y preguntas para preparar la defensa.
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
| URL despliegue | No hay despliegue público. Servicio TCP/TLS ejecutable localmente con Docker. |
| URL Slides | Pendiente de la presentación definitiva del autor y de comprobar el acceso del evaluador. El PPTX actual es un borrador. |
| URL Vídeo | El autor no tiene previsto grabarlo. El formulario actual lo exige; aclarar con BigSchool la discrepancia con las explicaciones que lo describían como opcional. |
| Usuario y contraseña de prueba | No aplica: no hay login web. El aprovisionamiento genera credenciales de dispositivos ficticios en cada instalación. |

Texto sugerido para describir la publicación local: «Servicio backend TCP/TLS sin interfaz web. Incluye Docker Compose, simulador, instrucciones reproducibles y evidencias de pruebas. Cada instalación genera sus propios secretos y dispositivos de demostración».

## Antes de enviar

1. Comprobar que el repositorio y la presentación son accesibles al evaluador.
2. Consultar la [ejecución correcta de GitHub Actions](https://github.com/JrgPS/tfm-pasarela-iot/actions/runs/38775260191).
3. Incorporar la presentación definitiva del autor y revisar la explicación del proyecto.
4. Aclarar con BigSchool cómo entregar sin vídeo: el campo actual URL Vídeo es obligatorio. No usar enlaces ficticios.
5. Revisar los campos y enviar personalmente el formulario del campus.

El formulario del campus no se ha enviado. No se han inventado enlaces, credenciales de evaluador ni una grabación inexistente.

El autor ha solicitado repositorio privado; los permisos del evaluador se añadirán más adelante. Antes de entregar deberá disponer de acceso al código y a los materiales. Los dos trabajos de [GitHub Actions](https://github.com/JrgPS/tfm-pasarela-iot/actions/runs/38775260191) terminaron correctamente: compilación y pruebas, y demo Docker con TCP/TLS real. La [evidencia de CI](../evidence/github-actions.json) identifica el commit comprobado. El trabajo Docker realiza su propio checkout y genera secretos nuevos en el runner. Cuando el repositorio era público se verificó un [clon sin credenciales](../evidence/public-clone.json). El clon limpio inicial con Docker conserva su informe por separado.

## Fuente editable de la presentación

`scripts/build-presentation.mjs` usa `@oai/artifact-tool` del entorno de documentos. Requiere `ARTIFACT_TOOL_MODULES` con el directorio de módulos, `PRESENTATIONS_SKILL` con la carpeta de la habilidad y `ARTIFACT_PYTHON` con el ejecutable Python. No son requisitos para ejecutar la pasarela. El borrador, los renders y los informes internos de revisión quedan en `tmp/presentation/`, excluido de Git.

## Versión de entrega

La etiqueta `v1.0.0` identifica la versión preparada. El [ZIP de GitHub](https://github.com/JrgPS/tfm-pasarela-iot/archive/refs/tags/v1.0.0.zip) permite descargar sus archivos. La [presentación de esa versión](https://github.com/JrgPS/tfm-pasarela-iot/blob/v1.0.0/docs/entrega/Presentacion-TFM.pptx) conserva el borrador de referencia, no la presentación definitiva del autor. El manual y la guía PDF se añaden en main después de v1.0.0; la etiqueta no se modifica.

La ejecución de CI comprobó el commit `f6ef8b9`. Después se actualizaron únicamente enlaces y evidencia documental, sin cambiar código de aplicación ni configuración de CI.
