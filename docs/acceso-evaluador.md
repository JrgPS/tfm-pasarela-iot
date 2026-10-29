# Probar la Pasarela IoT publicada

**Servicio:** `iot.xjps.es`, puerto **7443**, protocolo **TCP/TLS**.

Acceso comprobado el 29 de octubre de 2026 desde datos móviles, con una IP de salida distinta de la red del servidor: cinco operaciones confirmadas, cero fallos, cero reintentos y totales correctos. [Informe original](evidence/proxmox-external.json) y [comprobación de red externa](evidence/proxmox-external-network.json).

El servicio se utiliza con el simulador; al abrirlo en un navegador no se obtiene una página web. El backend se ejecuta en una VM dedicada en Proxmox, con HAProxy, dos Gateway, dos Registry y PostgreSQL. [Instalación y pruebas](validacion-proxmox.md).

## Requisitos del cliente

- Git y Docker con contenedores Linux y Docker Compose.
- Conexión a Internet que permita salir por TCP 7443.
- Paquete privado facilitado por el autor: una identidad ficticia y el certificado público de la instalación.

El paquete contiene una carpeta `evaluation-client/` con `devices.json` y `server.cer`. Se entrega por un canal privado; queda excluido de Git. Las claves del servidor no forman parte de ese paquete. Si no se ha recibido, solicitarlo al autor por el canal de evaluación acordado.

## Preparar y ejecutar

```sh
git clone https://github.com/JrgPS/tfm-pasarela-iot.git
cd tfm-pasarela-iot
```

Descomprimir el paquete privado en esa carpeta. La estructura debe quedar:

```text
tfm-pasarela-iot/
  compose.client.yaml
  evaluation-client/
    devices.json
    server.cer
```

PowerShell:

```powershell
$env:IOT_REMOTE_HOST = 'iot.xjps.es'
$env:IOT_REMOTE_PORT = '7443'
docker compose -f compose.client.yaml run --rm --build simulator
```

Linux o macOS:

```sh
export IOT_REMOTE_HOST=iot.xjps.es
export IOT_REMOTE_PORT=7443
docker compose -f compose.client.yaml run --rm --build simulator
```

Este perfil inicia únicamente el cliente. La primera ejecución descarga las imágenes y compila el simulador; las siguientes reutilizan las capas disponibles. Se conecta, inicializa un dispositivo, consulta sus totales, ejecuta cinco sumas y vuelve a comprobar los incrementos persistidos.

## Resultado esperado

La salida JSON debe mostrar:

```json
{
  "devices": 1,
  "confirmedOperations": 5,
  "failedDevices": 0,
  "errors": []
}
```

Además se generan informes JSON/CSV en `results/`. Los totales pueden contener operaciones anteriores; el simulador comprueba el incremento de esta ejecución. Ejecutar las pruebas de una misma identidad de una en una para no superponer sus comprobaciones de totales.

Para veinte sumas con una pequeña pausa:

```sh
docker compose -f compose.client.yaml run --rm simulator --host iot.xjps.es --port 7443 --devices 1 --operations 20 --interval-ms 200
```

Una carga con más dispositivos requiere que el autor facilite suficientes identidades. El paquete de evaluación contiene una sola.

## Diagnóstico y disponibilidad

- Error de nombre o certificado: comprobar el hostname y utilizar el certificado del paquete vigente. Mantener la validación TLS activada.
- Código 002 o 005: comprobar que se utiliza el manifiesto privado de esta instalación.
- Error de conexión: revisar DNS y salida TCP 7443 de la red del cliente; contactar con el autor si el servicio no está disponible.

La instalación está en una conexión doméstica y no tiene SLA. La IP pública puede cambiar; el autor debe actualizar el registro DNS si sucede. La prueba externa acredita su funcionamiento en la fecha indicada.

## Enlaces para la entrega

- Código: https://github.com/JrgPS/tfm-pasarela-iot
- Guía de acceso al servicio: https://github.com/JrgPS/tfm-pasarela-iot/blob/main/docs/acceso-evaluador.md
- Presentación final: https://github.com/JrgPS/tfm-pasarela-iot/blob/main/docs/entrega/Presentacion-TFM.pptx

Cuando el formulario requiera una URL web para el despliegue, la URL de esta guía proporciona las instrucciones y el endpoint TCP/TLS operativo. El paquete de credenciales se facilita por separado y de forma privada.
