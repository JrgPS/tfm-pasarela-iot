# Materiales de entrega del TFM

Proyecto: **Pasarela IoT TCP escalable**, Máster en Desarrollo con IA E.1.

## Contenido preparado

- [Presentación editable](Presentacion-TFM.pptx): diez diapositivas con notas para explicar arquitectura, protocolo, consistencia, pruebas, uso de IA y límites.
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
| URL GitHub | Pendiente de publicación y verificación de acceso público. |
| URL despliegue | No hay despliegue público. Servicio TCP/TLS ejecutable localmente con Docker. |
| URL Slides | Enlace al archivo `docs/entrega/Presentacion-TFM.pptx` en GitHub cuando se publique. |
| URL Vídeo | Pendiente de grabar y publicar, si se incluye. |
| Usuario y contraseña de prueba | No aplica: no hay login web. El aprovisionamiento genera credenciales de dispositivos ficticios en cada instalación. |

Texto sugerido para describir la publicación local: «Servicio backend TCP/TLS sin interfaz web. Incluye Docker Compose, simulador, instrucciones reproducibles y evidencias de pruebas. Cada instalación genera sus propios secretos y dispositivos de demostración».

## Antes de enviar

1. Comprobar que el repositorio y la presentación son accesibles al evaluador.
2. Confirmar el resultado de GitHub Actions cuando se publique.
3. Revisar la presentación y practicar el guion con el autor.
4. Incluir la URL del vídeo si se graba y verificar su acceso.
5. Revisar los campos y enviar personalmente el formulario del campus.

El formulario del campus no se ha enviado. No se han inventado enlaces, credenciales de evaluador ni una grabación inexistente.

GitHub Actions está preparado con un trabajo de compilación y pruebas y otro de demo Docker con TCP/TLS real. Todavía no se ha ejecutado en GitHub. La validación local no equivale a un resultado de Actions. El clon limpio comprobado procede del repositorio Git local, no de una descarga pública de GitHub.

## Fuente editable de la presentación

`scripts/build-presentation.mjs` usa `@oai/artifact-tool` del entorno de documentos. Requiere `ARTIFACT_TOOL_MODULES` con el directorio de módulos, `PRESENTATIONS_SKILL` con la carpeta de la habilidad y `ARTIFACT_PYTHON` con el ejecutable Python. No son requisitos para ejecutar la pasarela. El borrador, los renders y los informes internos de revisión quedan en `tmp/presentation/`, excluido de Git.
