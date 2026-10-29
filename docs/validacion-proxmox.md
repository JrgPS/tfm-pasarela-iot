# Instalación y validación en Proxmox

Comprobación: 29 de octubre de 2026. **El servicio `iot.xjps.es:7443` está publicado y comprobado desde una conexión móvil externa.** Se confirmó que el PC salía por una IPv4 distinta de la conexión doméstica y el simulador completó cinco operaciones con TLS, autenticación y totales correctos. La reserva DHCP, el registro A en modo Solo DNS y la redirección TCP del router están guardados. [Guía para el evaluador](acceso-evaluador.md).

## Entorno instalado

- VM KVM dedicada `tfm-iot`, 2 vCPU, 4 GiB de RAM y 40 GiB de disco, en el CHUWI con Proxmox.
- Ubuntu Server 24.04.5 LTS, kernel `6.8.0-147-generic`.
- Docker Engine `29.9.2`, Compose `5.6.1` y QEMU Guest Agent activo.
- Usuario `tfm`; administración Docker con `sudo`. No se ha añadido el usuario al grupo `docker`.
- Acceso SSH por una clave exclusiva; huella del servidor contrastada desde Proxmox. Autenticación por contraseña, autenticación interactiva y acceso directo de root desactivados y comprobados con `sshd -T`.
- Seis contenedores: HAProxy, dos Gateway, dos Registry y PostgreSQL. PostgreSQL dispone de volumen persistente. Solo HAProxy publica el puerto TCP 7443 en la dirección LAN de la VM.
- Certificado de laboratorio con SAN `iot.xjps.es`; la comprobación TLS ha validado su confianza y el nombre. La aplicación no exige un certificado de una CA pública: el simulador utiliza confianza explícita en el certificado recibido.
- Secretos nuevos de esta instalación y 100 dispositivos ficticios aprovisionados. Para el cliente se exportó únicamente una identidad ficticia y el certificado público, conservados fuera de Git.

Se clonó `195c48db1be621a70606ffe6b341c5f943f36d61` y se aplicaron los archivos de preparación del despliegue presentes en el workspace: Compose parametrizado, exclusiones de Git/Docker, cliente independiente y scripts de instalación/exportación. El código C# no cambió. Estos ensayos no equivalen a un despliegue del tag `v1.0.0` sin modificaciones.

## Pruebas realizadas

| Prueba | Dispositivos | Sumas confirmadas | Fallos | Reintentos | p95 de suma |
|---|---:|---:|---:|---:|---:|
| Funcional dentro de la VM | 10 | 50 | 0 | 0 | 92,96 ms |
| Ráfaga sincronizada dentro de la VM | 100 | 1.000 | 0 | 0 | 809,50 ms |
| Simulador en Docker Desktop del PC → VM por LAN | 1 | 5 | 0 | 0 | 20,57 ms |
| Dominio público desde la LAN (retorno NAT) | 1 | 5 | 0 | 0 | 17,51 ms |
| Dominio público desde datos móviles, fuera de la LAN | 1 | 5 | 0 | 0 | 138,16 ms |

El simulador comprueba también los incrementos de totales persistidos; los cinco informes tienen `errors` vacío. La prueba sincronizada duró 9,42 s y confirmó 106,15 operaciones/s de media. Son ensayos cortos en un host compartido; no acreditan capacidad máxima ni sustituyen los ensayos sostenidos realizados previamente en el PC.

Evidencias originales sin credenciales: [funcional](evidence/proxmox-smoke.json), [100 dispositivos](evidence/proxmox-100.json), [cliente por LAN](evidence/proxmox-lan.json), [dominio público desde LAN](evidence/proxmox-public-from-lan.json), [cliente externo](evidence/proxmox-external.json) y [red externa](evidence/proxmox-external-network.json). La dirección IP del móvil se omitió del informe; se conserva la comprobación de que era distinta de la doméstica.

La prueba desde el PC usó `compose.client.yaml` con un override local `extra_hosts` para resolver `iot.xjps.es` hacia la VM, sin cambiar el DNS público ni desactivar la validación del certificado. El cliente y el backend se ejecutaron en máquinas distintas de la misma LAN. Esto **no acredita acceso externo**.

La prueba externa utilizó el Compose independiente del cliente, sin `extra_hosts` ni otro override de resolución local. El DNS público se comprobó también con el resolver 1.1.1.1. La regla del Huawei EG8147X6 está habilitada para TCP 7443 hacia la VM; la activación fue confirmada por el autor. Se comprobó la reserva DHCP por la MAC de la VM. No se modificaron las reglas anteriores.

## Operación y entrega

Mantener la VM y la conexión doméstica disponibles durante la evaluación. Si cambia la IP pública, actualizar el registro DNS; no se ha instalado actualización automática. Facilitar privadamente el paquete de una identidad ficticia y certificado público. SSH, Proxmox, PostgreSQL y la API interna no forman parte de la redirección del TFM. Esta prueba verifica el acceso externo en la fecha indicada, sin garantizar disponibilidad futura. Véase la [guía de despliegue](despliegue-evaluacion.md).
