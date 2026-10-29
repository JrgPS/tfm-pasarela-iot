# Despliegue de evaluación en máquina propia

Estado a 29 de octubre de 2026: **servicio publicado en `iot.xjps.es:7443` (TCP/TLS) y [verificado desde datos móviles](validacion-proxmox.md)**. Reserva DHCP, DNS Solo DNS y regla TCP del router guardados. Cinco operaciones externas confirmadas, sin errores y con totales correctos. [Instrucciones para el evaluador](acceso-evaluador.md).

## Decisión de conectividad

La ruta prevista, si la conexión doméstica permite entrada TCP, es:

Simulador del evaluador → subdominio DNS → IP pública del router → puerto TCP 7443 de una VM Linux → HAProxy → gateways → API interna → PostgreSQL.

Cloudflare se utiliza como proveedor DNS. El registro de ese subdominio debe estar en modo **Solo DNS**, sin proxy web. El proxy HTTP/HTTPS habitual no transporta este protocolo TCP propio. Un Cloudflare Tunnel de TCP requiere software de conexión adicional en el cliente; no es equivalente a publicar un socket TCP directamente accesible por el simulador. No se ha contratado Spectrum ni otro servicio.

Si existe CG-NAT, no se configurará un registro hacia una IP privada ni se considerará resuelto con una simple apertura de puerto. Habrá que acordar una ruta alternativa antes de publicar.

Fuentes: [puertos del proxy de Cloudflare](https://developers.cloudflare.com/fundamentals/reference/network-ports/) y [TCP con Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/access-controls/applications/non-http/cloudflared-authentication/arbitrary-tcp/).

## Datos de la instalación realizada

- VM Ubuntu 24.04.5 en el CHUWI, con SSH por clave y reserva DHCP. Docker y QEMU Guest Agent activos.
- Dominio `iot.xjps.es`, puerto TCP 7443. DNS público y acceso mediante datos móviles comprobados.
- Redirección del router activada con confirmación del autor, exclusivamente hacia HAProxy en la VM.
- El autor debe facilitar al evaluador el paquete privado con una identidad ficticia y certificado público. No se ha enviado a terceros desde este trabajo.

No se necesitan contraseñas, claves privadas ni tokens pegados en el chat o en Git.

## Instalación nueva en Linux

Usar una VM dedicada con Docker Engine y Docker Compose disponibles, Git y Python 3. Determinar sus recursos al elegir el servidor; las cifras de carga anteriores proceden del PC con Docker Desktop, no de esta VM.

Para una VM KVM nueva llamada `tfm-iot`, con Ubuntu 24.04 y usuario `tfm`, se incluye [setup-ubuntu-host.sh](../scripts/setup-ubuntu-host.sh). El administrador puede revisar y ejecutar `sudo bash scripts/setup-ubuntu-host.sh`: instala Docker desde el repositorio oficial, Compose, Git, Python y QEMU Guest Agent, y comprueba Docker con `hello-world`. No elimina paquetes o datos existentes; si encuentra paquetes incompatibles, se detiene para revisarlos. El script no cambia el firewall ni publica puertos del proyecto.

Por defecto se conserva la necesidad de `sudo` para administrar Docker. La opción explícita `--grant-docker-access` añade `tfm` al grupo `docker`, con acceso equivalente a root dentro de la VM; debe decidirlo su administrador. El procedimiento de este documento presupone acceso administrativo para los comandos Docker. Fuente: [instalación oficial de Docker en Ubuntu](https://docs.docker.com/engine/install/ubuntu/).

```sh
git clone https://github.com/JrgPS/tfm-pasarela-iot.git
cd tfm-pasarela-iot
cp .env.evaluation.example .env.evaluation
```

Editar `.env.evaluation`: `TLS_DNS_NAME` debe ser el hostname real del servicio, sin esquema ni puerto. Para publicar, `IOT_BIND_ADDRESS` debe ser la IP LAN de la VM que recibirá el tráfico; el valor del ejemplo es `127.0.0.1`, accesible solo desde el propio host. El puerto externo se configura con `IOT_PUBLIC_PORT`. Mantener el proyecto bajo una cuenta con acceso administrativo restringido.

En una instalación nueva, generar secretos propios de evaluación; no reutilizar los secretos del PC de desarrollo:

```sh
umask 077
mkdir -p secrets results
docker compose --env-file .env.evaluation run --rm --no-deps --build \
  --user "$(id -u):$(id -g)" provision prepare --count 100
docker compose --env-file .env.evaluation up -d --wait db
docker compose --env-file .env.evaluation run --rm --no-deps provision seed
docker compose --env-file .env.evaluation up -d --build \
  --scale gateway=2 --scale registry=2 gateway registry proxy
docker compose --env-file .env.evaluation ps
```

`prepare` conserva certificados existentes. Definir el hostname antes del primer `prepare`; cambiar después `TLS_DNS_NAME` no reemite el certificado. No borrar secretos para corregirlo sobre una base ya aprovisionada: se debe comprobar y renovar únicamente el certificado manteniendo las claves de dispositivos y la clave maestra. Comprobar SAN y caducidad antes de abrir acceso externo.

El certificado de laboratorio usa confianza explícita: el simulador verifica tanto su huella como el nombre del servidor. No desactivar esa validación ni utilizar un hostname distinto del certificado. La clave maestra y los demás secretos del servidor permanecen en este equipo. Mantener una copia protegida de la base y de los secretos necesarios para restaurarla.

## DNS y entrada de red

Después de comprobar la red doméstica, crear en Cloudflare el registro A del subdominio hacia la IP pública real, en modo Solo DNS. Crear AAAA solo si hay IPv6 funcional y probado. Si la IP cambia, prever su actualización DNS.

En el router, redirigir únicamente el puerto TCP acordado hacia la IP LAN fija y el puerto publicado de la VM. Verificar las reglas de Proxmox, del host y de Docker. Docker publica puertos mediante sus propias reglas: no dar por suficiente una configuración de UFW sin comprobar el tráfico real.

La API HTTP, los controles de salud y PostgreSQL no publican puertos de host en Compose. No abrirlos en el router ni habilitar administración remota de Proxmox para el evaluador. Mantener límites de conexiones, trabajo y logs del proyecto.

## Cliente del evaluador

En el servidor, exportar por defecto una sola identidad ficticia y el certificado público:

```sh
python3 scripts/export-evaluation-client.py --count 1
```

La carpeta `evaluation-client/` queda ignorada por Git. Contiene solo `devices.json` con las claves HMAC de las identidades seleccionadas y `server.cer`. Facilitarla por un canal privado al evaluador. No adjuntar `db-password`, `api-key`, `master-key`, `cert-password` ni `server.pfx`. No publicar este paquete en el repositorio. Las claves de cliente son credenciales de laboratorio: restringirlas a la evaluación y retirarlas cuando termine.

En otro equipo, clonar el código y colocar la carpeta recibida como `evaluation-client/` en su raíz. La prueba debe hacerse desde una red externa, por ejemplo otra conexión a Internet; una prueba desde la misma LAN no acredita acceso externo.

Linux:

```sh
mkdir -p results
export IOT_REMOTE_HOST=iot.example.com
export IOT_REMOTE_PORT=7443
docker compose -f compose.client.yaml run --rm --build simulator
```

PowerShell:

```powershell
New-Item -ItemType Directory -Force results | Out-Null
$env:IOT_REMOTE_HOST = 'iot.example.com'
$env:IOT_REMOTE_PORT = '7443'
docker compose -f compose.client.yaml run --rm --build simulator
```

Sustituir el hostname de ejemplo por el real. Este Compose inicia solo el simulador, sin levantar otra base ni servicios locales. Por defecto ejecuta cinco sumas de un dispositivo. El informe debe mostrar cinco confirmadas, cero dispositivos fallidos y `errors` vacío, con salida 0. Comprueba incrementos de totales: no requiere totales iniciales cero.

Para una carga mayor, exportar antes suficientes identidades y acordar la prueba. No asumir que una VM o conexión doméstica reproduce la capacidad del PC de desarrollo.

## Cierre del despliegue

Hostname, puerto y evidencia del 29 de octubre están registrados en [validación Proxmox](validacion-proxmox.md). El endpoint es TCP/TLS; la [guía de acceso](acceso-evaluador.md) proporciona una URL web con instrucciones. Mantener la instalación encendida durante la evaluación y actualizar DNS si cambia la IP pública. Los apartados anteriores documentan cómo reproducir la instalación; los comandos con sudo requieren una sesión administrativa en la VM.
