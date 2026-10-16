"""Generate the authoring brief, without creating or replacing the author's slides."""
from pathlib import Path
from html import escape
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, PageBreak, KeepTogether, Table, TableStyle

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT/'output/pdf/Guia-contenido-diapositivas-TFM.pdf'
DEST.parent.mkdir(parents=True,exist_ok=True)
for name,filename in [('Text','arial.ttf'),('Bold','arialbd.ttf')]:
    pdfmetrics.registerFont(TTFont(name,str(Path('C:/Windows/Fonts')/filename)))
NAVY=colors.HexColor('#11283E'); TEAL=colors.HexColor('#087F82'); INK=colors.HexColor('#253844')
styles={
 'title':ParagraphStyle('Title',fontName='Bold',fontSize=27,leading=33,textColor=NAVY,spaceAfter=20),
 'h':ParagraphStyle('Heading',fontName='Bold',fontSize=16.5,leading=21,textColor=NAVY,spaceAfter=9),
 'label':ParagraphStyle('Label',fontName='Bold',fontSize=10.5,leading=14,textColor=TEAL,spaceBefore=5,spaceAfter=3),
 'p':ParagraphStyle('Body',fontName='Text',fontSize=10.2,leading=14.5,textColor=INK,spaceAfter=6,allowWidows=False,allowOrphans=False),
 'small':ParagraphStyle('Small',fontName='Text',fontSize=8.5,leading=11.5,textColor=colors.HexColor('#607784'),spaceAfter=6),
}
def p(t,style='p'): return Paragraph(escape(t),styles[style])
slides=[
 dict(n=1,title='Pasarela IoT TCP escalable',purpose='Identificar el proyecto y situar al evaluador.',
 visible=['Pasarela IoT TCP escalable','TFM - Máster en Desarrollo con IA E.1 - BigSchool','Tu nombre completo, fecha y versión del proyecto.'],
 visual='Portada propia, sencilla. Puedes añadir un motivo visual de dispositivos conectados, sin logos de empresas de tu trabajo.',
 explain='Sistema de laboratorio con dispositivos ficticios. No procesa pagos reales.',source='README.md y Doc/Especificacion-TFM-Pasarela-IoT.md'),
 dict(n=2,title='Problema y objetivo',purpose='Explicar a quién ayuda y qué necesidad resuelve.',
 visible=['Cientos de dispositivos necesitan comunicarse con un servidor mediante conexiones persistentes.','El servidor debe reconocer cada dispositivo, validar sus mensajes y conservar trazabilidad.','Objetivo: separar recepción y operaciones para poder replicar ambos servicios.'],
 visual='Un dibujo simple de dispositivos y servidor, o un esquema del escenario actual y la solución propuesta.',
 explain='Motivación profesional propia, sin atribuir al proyecto código, datos o resultados de tu empresa. Usuarios: desarrolladores y operadores de una plataforma IoT.',source='README.md: Problema y alcance'),
 dict(n=3,title='Funciones y alcance',purpose='Dejar claro qué hace el MVP entregado.',
 visible=['001 - Inicializar y autenticar el dispositivo.','002 - Ejecutar una suma exacta y registrarla.','003 - Consultar el número de sumas y su acumulado.','Incluye simulador, registro de actividad y réplicas manuales.'],
 visual='Tres pasos o una tabla breve de acción, entrada y resultado. Menciona que no hay pagos reales.',
 explain='El número de comercio se devuelve cuando corresponde al dispositivo autenticado. Una consulta o una inicialización no incrementan los totales.',source='src/Iot.Protocol/WireProtocol.cs y src/Iot.Registry/RegistryStore.cs'),
 dict(n=4,title='Arquitectura y tecnologías',purpose='Mostrar responsabilidades y conexiones entre componentes.',
 visible=['Simulador - HAProxy - Gateway TCP/TLS - Registro y operaciones - PostgreSQL.','Gateway y registro admiten réplicas independientes.','C#/.NET 10, ASP.NET Core, Npgsql, PostgreSQL 18 y Docker Compose.'],
 visual='Diagrama con flechas. Diferencia TCP/TLS externo de HTTP interno. Señala dos réplicas de gateway y dos de registro, una base y un balanceador.',
 explain='El mismo HAProxy realiza balanceo TCP y HTTP. El gateway no recibe claves HMAC de dispositivos ni credenciales de la base. La API y PostgreSQL no publican puertos en el host.',source='compose.yaml y deploy/haproxy/haproxy.cfg'),
 dict(n=5,title='Protocolo e identificación',purpose='Explicar el contrato propio de los mensajes.',
 visible=['Identidad: fabricante de 2 dígitos, modelo de 2 y serie de 11.','Fecha UTC yyMMddHHmm, acción de 3 dígitos e ID de petición de 128 bits.','Tramas ASCII separadas por barra vertical, terminadas en LF y limitadas a 4096 bytes.','Respuesta: identidad, código, comercio de 9 dígitos y resultado.'],
 visual='Una trama esquemática coloreada por campos y una leyenda. Si usas marcadores, escribe "ejemplo esquemático"; no simules una firma real.',
 explain='TCP transporta bytes: una lectura puede contener parte de una trama o varias. Por eso existe FrameReader. Conserva ceros iniciales.',source='Doc/Especificacion-TFM-Pasarela-IoT.md y src/Iot.Protocol/FrameReader.cs'),
 dict(n=6,title='Seguridad y validación',purpose='Distinguir transporte, autenticación y protección de secretos.',
 visible=['TLS protege la comunicación con el gateway.','HMAC-SHA-256 autentica cada mensaje con una clave por dispositivo.','Registry comprueba registro, estado activo, fecha y contenido.','Las claves de dispositivo se guardan cifradas con AES-GCM.'],
 visual='Diagrama de dónde se valida cada elemento. Puedes resumir los rechazos 002 desconocido y 005 firma incorrecta.',
 explain='HMAC no cifra el contenido. El certificado es de laboratorio. No muestres devices.json, master-key, contraseñas ni claves en capturas.',source='src/Iot.Persistence/Infrastructure.cs y src/Iot.Simulator/DeviceClient.cs'),
 dict(n=7,title='Consistencia y reintentos',purpose='Explicar cómo se evita una suma duplicada.',
 visible=['Una transacción confirma operación, totales, respuesta y auditoría.','El servidor responde 000 después del commit.','Repetir una petición aceptada con el mismo ID y contenido devuelve su respuesta original.','Reutilizar un ID con otro contenido produce 012.'],
 visual='Secuencia breve: petición, commit, respuesta perdida, reconexión y reintento sin segundo efecto.',
 explain='El bloqueo por dispositivo y los registros compartidos en PostgreSQL mantienen la consistencia entre réplicas. Solo se guardan respuestas aceptadas como idempotentes; las denegaciones se reevalúan.',source='src/Iot.Registry/RegistryStore.cs y docs/adr/0001-implementacion-inicial.md'),
 dict(n=8,title='Simulador y demostración funcional',purpose='Permitir entender el uso aunque no haya vídeo.',
 visible=['Número de dispositivos, operaciones, rampa y pausa configurables.','Cada cliente inicializa, consulta totales, opera y comprueba el incremento final.','Verifica HMAC, correlación y resultado exacto.','Genera informes JSON/CSV y devuelve error si detecta fallos.'],
 visual='Captura real y legible de una ejecución corta: 1 dispositivo y 5 sumas, o 10 y 50. Recorta solo lo necesario y conserva el contexto del comando.',
 explain='Puedes mostrar el comando docker compose run --rm simulator --host proxy --devices 1 --operations 5. La suma de ejemplo devuelve SUM,+667. No fabriques una captura ni confundas una prueba anterior con una recién ejecutada.',source='src/Iot.Simulator/Program.cs y docs/evidence/docker-smoke.json'),
 dict(n=9,title='Resultados medidos',purpose='Presentar la evidencia de capacidad con sus condiciones.',
 visible=['500 dispositivos y 60.000 sumas confirmadas en 605,59 segundos.','Cero fallos finales y cero reintentos en ese ensayo sostenido.','p95 de la suma: 47,68 ms. Media: 99,08 operaciones/s.','Prueba local: cliente y servidores en el mismo Docker Desktop, 8 CPU virtuales y unos 7,6 GiB.'],
 visual='Destaca 60.000 y una tabla breve. Si añades estrés: 1000 dispositivos, 20.000 sumas, p95 1297,52 ms. Separa ambos escenarios.',
 explain='El ensayo sostenido usa una pausa de cinco segundos después de cada respuesta. No es una tasa de llegada fija ni un SLA. La ráfaga de mil no cumple p95 inferior a un segundo.',source='docs/evidence/acceptance-500-10min.json y verified-1000.json'),
 dict(n=10,title='Pruebas y recuperación',purpose='Demostrar que se verificó algo más que el caso correcto.',
 visible=['29 pruebas Docker correctas, sin fallos ni omisiones.','Caída de gateway y registro: 10.000 sumas por escenario y sin fallos finales.','PostgreSQL detenido: rechazo 009 y recuperación sin duplicar.','Sobrecarga controlada: rechazo 013 y totales verificados al recuperarse.'],
 visual='Tabla de escenario y resultado, con enlace o referencia a la evidencia. Añade CI correcto como comprobación independiente del entorno local.',
 explain='La sobrecarga utilizó límites reducidos temporalmente, con 32 dispositivos. No prueba la capacidad máxima con límites normales. Las reconexiones son parte de la recuperación, no una migración transparente.',source='docs/validacion-docker.md, docs/validacion-fallos.md y docs/evidence/github-actions.json'),
 dict(n=11,title='Desarrollo asistido por IA y decisiones',purpose='Mostrar aprendizaje y criterio propios.',
 visible=['Requisitos y caso de uso aportados por el autor.','IA utilizada para concretar el diseño, implementar, preparar pruebas y documentar.','Decisiones contrastadas con código y ejecución real.','Npgsql y SQL explícito para controlar las transacciones y la deduplicación.'],
 visual='Dos columnas breves: aportación del autor y asistencia de IA. Añade un ejemplo real de corrección, como limitar pools o reintentar 013.',
 explain='La aplicación no necesita un LLM en ejecución. Explica solo lo que hayas revisado y entendido; no atribuyas desarrollo manual a código asistido.',source='docs/ai-development.md y docs/adr/'),
 dict(n=12,title='Límites, conclusiones y acceso',purpose='Cerrar con una valoración honesta y una vía de evaluación.',
 visible=['MVP funcional con comunicación autenticada, persistencia y recuperación de réplicas.','PostgreSQL, HAProxy y el host siguen siendo puntos únicos de fallo.','Duplicar réplicas no mejoró el rendimiento en la comparación exploratoria.','Código y documentación en el repositorio; ejecución local con Docker. Repositorio público; comprobar enlaces definitivos antes de entregar.'],
 visual='Conclusión breve y enlaces clicables. El repositorio sigue público por decisión del autor; comprueba sus enlaces. No prometas una URL cloud inexistente.',
 explain='Mejoras futuras: mediciones con cliente separado, auditoría duradera, rotación de claves y disponibilidad de infraestructura. No hace falta incorporar estas mejoras antes de explicar el MVP.',source='docs/estado-proyecto.md y docs/entrega/README.md'),
]

def footer(c,d):
    w,h=A4;c.saveState();c.setFont('Text',8);c.setFillColor(colors.HexColor('#607784'))
    c.drawString(46,h-29,'TFM BIGSCHOOL / GUÍA PARA CREAR TU PRESENTACIÓN')
    c.drawRightString(w-46,h-29,'15/10/2026')
    c.setStrokeColor(colors.HexColor('#CCDADF'));c.line(46,36,w-46,36)
    c.drawString(46,23,'Contenido recomendado. Diseño y presentación final a cargo del autor.')
    c.drawRightString(w-46,23,str(d.page));c.restoreState()

story=[p('Guía de contenido para tus diapositivas','title')]
story += [p('Pasarela IoT TCP escalable','h'),p('Máster en Desarrollo con IA E.1 - BigSchool')]
story += [p('Qué debe conseguir tu presentación','label'),p('El evaluador debe entender el problema, el producto que funciona, cómo lo construiste, qué has probado y qué límites tiene. Puedes elegir libremente herramienta, estilo y composición.')]
story += [p('Estructura propuesta: 12 diapositivas','label'),p('Es una recomendación para este proyecto, no un número exigido por la escuela. Si prefieres diez, combina las diapositivas 5 y 6 y las 9 y 10. Incluye siempre problema, solución, funcionamiento, construcción y resultados.')]
story += [p('Cómo utilizar esta guía','label'),p('Cada ficha indica el objetivo, texto que puedes adaptar, una propuesta visual, una explicación para tus notas y las fuentes del repositorio. El texto visible contiene lo esencial. Las notas amplían, pero no deben ocultar limitaciones importantes.')]
story += [p('Diseño a tu criterio','label'),p('Como orientación, usa formato 16:9, una idea principal por diapositiva y texto legible. Sustituye párrafos largos por diagramas, tablas cortas y capturas reales. No copies páginas enteras del manual. Añade enlaces a las evidencias en notas o al pie.')]
story += [p('Entrega sin vídeo y acceso al repositorio','label'),p('Has decidido preparar personalmente las diapositivas y no grabar vídeo. Haz la presentación autocontenida, con al menos una captura funcional y resultados legibles. La auditoría observó que el formulario actual marca URL Vídeo como requerida: la discrepancia sigue pendiente de aclaración y no se resuelve con este PDF. El autor ha decidido mantener el repositorio público por ahora. No se han añadido colaboradores; comprueba los enlaces definitivos antes del envío.')]
story += [p('No incluir','label'),p('Claves o contraseñas, datos del trabajo, pagos reales, resultados inventados ni afirmaciones de alta disponibilidad integral o escalado lineal. La presentación anterior es solo un borrador de referencia y será sustituida por la tuya.')]
for start in range(0,len(slides),2):
    story.append(PageBreak())
    for s in slides[start:start+2]:
        block=[p(str(s['n']).zfill(2)+'  '+s['title'],'h'),p(s['purpose'],'small'),p('Texto visible sugerido','label')]
        for t in s['visible']:
            block.append(p('- '+t))
        block.extend([p('Visual y explicación','label'),p(s['visual']),p(s['explain'],'small'),p('Fuente: '+s['source'],'small')])
        story.append(KeepTogether(block));story.append(Spacer(1,19))
story += [PageBreak(),p('Revisión antes de entregar','title')]
for title,body in [
 ('Contenido','El problema y el usuario están claros. Se entiende qué hace 001, 002 y 003, dónde está la base y cómo se recupera una operación. La presentación tiene sentido sin escuchar una narración.'),
 ('Exactitud técnica','Usas Npgsql, no EF Core. Distingues TLS de HMAC. Explicas que se replican gateway y registry; la base, el proxy y el host son únicos. No confundes conexión persistente con sesión trasladable entre réplicas.'),
 ('Resultados','Cada cifra conserva escenario y unidades. Los datos de 500 dispositivos con pausa no se mezclan con ráfagas de mil. La prueba de sobrecarga indica límites temporales. El resultado desfavorable de la comparación de réplicas sigue visible.'),
 ('Autoría y comprensión','Puedes explicar personalmente las decisiones, la transacción, el mismo ID durante el reintento y el uso de IA. Las capturas son reales y no contienen secretos.'),
 ('Archivo final','Exporta tu presentación a PDF y conserva su fuente editable. Revisa a pantalla completa que no haya texto cortado y que los enlaces funcionen. No hay un número de diapositivas obligatorio en los requisitos revisados.'),
 ('Acceso del evaluador','El repositorio se mantiene público por decisión del autor. Comprueba que el evaluador puede abrir el código, los PDF y las diapositivas definitivas; no se han añadido colaboradores.'),
 ('Formulario','Nombre y email deben coincidir con matrícula. Repositorio y slides necesitan enlaces válidos. No inventes una URL de vídeo para superar el campo; la entrega sin vídeo requiere aclarar la condición actual del formulario.'),
]:
    story += [p(title,'label'),p(body)]
story += [p('Fuentes para preparar tu versión','label'),p('README.md; docs/entrega/Manual-proyecto.md; docs/validacion-docker.md; docs/validacion-fallos.md; docs/ai-development.md; docs/adr/; docs/evidence/; código de la etiqueta v1.0.0. Esta guía no es la presentación final ni una nueva rúbrica del curso.','small')]
doc=SimpleDocTemplate(str(DEST),pagesize=A4,leftMargin=46,rightMargin=46,topMargin=53,bottomMargin=49,
                       title='Guía de contenido para diapositivas - TFM Pasarela IoT',author='Proyecto TFM BigSchool')
doc.build(story,onFirstPage=footer,onLaterPages=footer)
print(DEST)
