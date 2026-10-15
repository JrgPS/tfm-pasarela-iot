# ADR 0002: pruebas de red y recuperación en Docker

Estado: adoptada el 15 de octubre de 2026.

Docker Desktop permite comprobar la solución Linux sin cambiar las políticas de Control de aplicaciones de Windows. El simulador se ejecuta en un contenedor y atraviesa el balanceador TCP, el gateway TLS, la API interna balanceada y PostgreSQL.

Se añade una batería optativa de red que verifica fragmentación y agrupación TCP, identidad desconocida, HMAC incorrecto, exceso de tamaño y pérdida de una respuesta después del commit. En este último caso se cierra el cliente sin consumir la respuesta y se reintenta la misma trama desde otra conexión: no puede aparecer un segundo efecto.

El servidor de pruebas se comparte con la demo, por lo que los pools deben acotarse. Se detectó un fallo por agotamiento al usar el pool predeterminado de cien conexiones en una prueba concurrente; el pool de la prueba se limita ahora a veinte, como el del servicio. No se aumentó a ciegas el límite de PostgreSQL para ocultar el problema.

El gateway limita a 128 los procesos de mensaje activos y acota la espera adicional a dos segundos. El exceso genera rechazo explícito; no se declara aceptada una operación que no ha llegado a ejecutarse. Este límite es por réplica y no equivale a un control global de tasa.

Para apagado planificado, la réplica deja de estar preparada y cierra la escucha antes de cancelar las sesiones. Permite terminar trabajo en curso durante un máximo de diez segundos y después cierra las sesiones restantes. La recuperación sigue siendo responsabilidad del cliente TCP. Una caída abrupta se prueba de forma independiente con SIGKILL.

La auditoría actualiza la última actividad en la transacción de cada intento. Un proceso periódico marca conexiones sin actividad durante más de tres minutos como pérdidas inferidas; no inventa una fecha exacta de desconexión observada.

El simulador incorpora inicio sincronizado y salida CSV además de JSON. Los perfiles sincronizados producen ráfagas y conservan las conexiones; la prueba sostenida utiliza un modelo cerrado de petición-respuesta con pausa entre operaciones. La metodología y la topología se publican junto con los resultados para evitar extrapolaciones indebidas.

La imagen de ejecución incluye la biblioteca GSSAPI que Npgsql intenta cargar en Linux. Se conserva la autenticación configurada de PostgreSQL; no se cambian políticas de seguridad de Windows ni se desactiva la validación TLS para resolver fallos de ejecución.
