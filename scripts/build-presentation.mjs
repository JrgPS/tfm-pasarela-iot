// Requires the bundled @oai/artifact-tool runtime. See docs/entrega/README.md.
import fs from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
const modules = process.env.ARTIFACT_TOOL_MODULES;
const skill = process.env.PRESENTATIONS_SKILL;
if (!modules || !skill) throw new Error('Set ARTIFACT_TOOL_MODULES and PRESENTATIONS_SKILL.');
process.env.RUNTIME_NODE_MODULES=modules;
const { Presentation, PresentationFile } = await import(pathToFileURL(path.join(modules,'@oai/artifact-tool/dist/artifact_tool.mjs')));
const { finalizePresentation } = await import(pathToFileURL(path.join(skill,'container_tools/artifact_tool_utils.mjs')));
const root = process.cwd();
const build = path.join(root,'tmp/presentation');
const output = path.join(root,'docs/entrega');
await fs.mkdir(build,{recursive:true}); await fs.mkdir(output,{recursive:true});
const deck = Presentation.create({slideSize:{width:1280,height:720}});
const navy='#10243A', white='#F4F7FA', accent='#39D6B3', muted='#A9BDC9';
function text(slide,value,x,y,w,h,size,color=white,bold=false) {
  const box=slide.shapes.add({geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
  box.text=value; box.text.style={typeface:'Arial',fontSize:size,color,bold};
}
function slide(title,body,note) {
  const s=deck.slides.add(); s.background.fill=navy;
  const n=deck.slides.items.length;
  text(s,'TFM BIGSCHOOL  /  DESARROLLO CON IA',72,40,1100,34,18,accent,true);
  text(s,title,72,112,1136,135,48,white,true);
  body(s);
  text(s,'Pasarela IoT TCP · Datos ficticios · Octubre de 2026',72,658,1030,28,16,muted);
  text(s,String(n).padStart(2,'0'),1150,658,60,28,16,accent);
  s.speakerNotes.text=note;
  return s;
}
slide('Pasarela IoT TCP escalable',s=>{
  text(s,'Conexiones persistentes, operaciones verificables\ny recuperación ante fallos de servicio',72,290,1090,130,34);
  text(s,'Máster en Desarrollo con IA E.1',72,490,1100,50,25,muted);
},'Presentar el proyecto individual y su motivación profesional. Toda la implementación y las pruebas utilizan dispositivos y operaciones ficticios. No procesa pagos.');
slide('Problema y alcance',s=>{
  text(s,'Cientos de dispositivos mantienen una conexión\ny necesitan una respuesta consistente.',72,270,1100,110,34);
  text(s,'El MVP inicializa dispositivos, ejecuta sumas exactas\ny consulta sus totales acumulados.',72,430,1100,110,30,muted);
},'El fabricante tiene 2 dígitos, el modelo 2 y la serie 11. PostgreSQL asocia cada dispositivo a un comercio de 9 dígitos. Alcance acordado en Doc/Especificacion-TFM-Pasarela-IoT.md.');
slide('Arquitectura y despliegue',s=>{
  text(s,'Simulador   /   HAProxy   /   Gateway TCP y TLS',72,270,1120,65,32);
  text(s,'API de registro y operaciones   /   PostgreSQL',72,365,1120,65,32);
  text(s,'Docker Compose permite replicar gateway y registro.\nEl balanceador distribuye las nuevas conexiones.',72,480,1120,95,28,muted);
},'Seguir el recorrido completo de una petición. Gateway recibe y delimita tramas. Registro autentica y accede a la base. El gateway no recibe las credenciales de base ni las claves de los dispositivos. Fuentes: compose.yaml y README.md.');
slide('Protocolo y autenticación',s=>{
  text(s,'001  Inicialización\n002  Suma con enteros de escala 100\n003  Consulta de totales',72,263,1100,190,32);
  text(s,'ASCII, campos delimitados, LF y máximo de 4096 bytes.\nTLS protege el canal. HMAC autentica cada trama.',72,500,1100,90,27,muted);
},'Explicar por qué TCP exige tratar fragmentación y agrupación de mensajes. Fechas UTC con yyMMddHHmm. HMAC-SHA256 con clave por dispositivo. El cliente verifica certificado por huella y nombre. Fuente: src/Iot.Protocol y especificación.');
slide('Una operación, un único efecto',s=>{
  text(s,'Cada petición tiene un identificador de 128 bits.',72,270,1110,70,34);
  text(s,'Una transacción confirma operación, totales y auditoría.\nEl servidor responde después del commit.',72,365,1110,100,30);
  text(s,'Repetir la misma petición devuelve la respuesta guardada.\nCambiar el contenido con el mismo ID devuelve 012.',72,510,1110,90,26,muted);
},'La pérdida de respuesta no equivale a ausencia de commit. La idempotencia evita duplicados entre conexiones y réplicas. Se comprueba además la identidad y el estado activo al repetir una solicitud. Fuentes: RegistryStore.cs y tests/Iot.Tests.');
slide('500 dispositivos durante diez minutos',s=>{
  text(s,'60.000',72,263,1120,145,110,accent,true);
  text(s,'operaciones confirmadas, sin fallos ni reintentos',72,422,1120,65,31);
  text(s,'p95 de la suma: 47,68 ms   ·   Duración: 605,59 s\nTotales de cada dispositivo verificados',72,525,1120,85,27,muted);
},'Fuente: docs/evidence/acceptance-500-10min.json. 500 dispositivos con 120 operaciones y pausa de 5 segundos después de cada respuesta. Ensayo de lazo cerrado, no tasa constante de llegada. Cliente y servidores comparten Docker Desktop, 8 CPU virtuales y unos 7,6 GiB. No son cifras de producción.');
slide('Pruebas y recuperación',s=>{
  text(s,'29 pruebas automatizadas correctas\nProtocolo, PostgreSQL y TCP con TLS real',72,263,1120,110,33);
  text(s,'Caída de gateway y registro: recuperación sin duplicados.\nPostgreSQL detenido: respuesta 009 y recuperación.\nSobrecarga controlada: rechazo 013 y totales correctos.',72,435,1120,150,28,muted);
},'Fuentes: docs/validacion-docker.md y docs/validacion-fallos.md. Fallos de réplica: 10000 operaciones por escenario, cero fallos finales. Base de datos: 1 operación y reenvío idéntico. Sobrecarga: 32 dispositivos, límite reducido a 1 por gateway, 30 respuestas 013 y 2 respuestas 009. No implica que PostgreSQL tenga alta disponibilidad.');
slide('Límites comprobados',s=>{
  text(s,'Duplicar réplicas no mejoró el rendimiento\nen la comparación exploratoria realizada.',72,263,1110,115,34);
  text(s,'PostgreSQL, HAProxy y el host son puntos únicos de fallo.\nEl perfil de 1000 dispositivos superó 1 s en p95.\nLa auditoría puede quedar incompleta si la base cae.',72,435,1110,155,28,muted);
},'Comparación de una ejecución por configuración, orden fijo, mismo host: 1+1 obtuvo 758,67 ops/s y 2+2 obtuvo 596,10. No afirmar escalado lineal ni atribuir una causa sin medirla. Fuente: docs/validacion-docker.md. Alta disponibilidad integral y journal durable quedan fuera del MVP.');
slide('Desarrollo asistido por IA',s=>{
  text(s,'La IA ayudó con requisitos, implementación,\ndocumentación y preparación de pruebas.',72,263,1110,115,34);
  text(s,'Las decisiones se contrastan con el código y la ejecución.\nLos informes conservan resultados medidos y limitaciones.\nEl autor debe comprender y defender la solución.',72,435,1110,155,28,muted);
},'Fuente: docs/ai-development.md. Separar requisitos humanos y asistencia de IA. No afirmar trabajo manual que no se haya realizado ni ocultar resultados desfavorables. La aplicación no necesita un LLM en ejecución.');
slide('Demostración reproducible',s=>{
  text(s,'Arranque con secretos nuevos\nSimulación de dispositivos y comprobación de totales\nRéplicas y recuperación ante una interrupción',72,265,1110,190,32);
  text(s,'Código, comandos y evidencias acompañan la entrega.\nEjecución local con Docker, sin pagos ni datos reales.',72,510,1110,85,27,muted);
},'Seguir docs/entrega/guion-demostracion.md. El evaluador puede ejecutar la demo local. La publicación en GitHub y la ejecución de Actions se documentan con su estado real en el índice de entrega. No inventar URLs ni indicar un vídeo grabado si aún no existe.');
await (await PresentationFile.exportPptx(deck)).save(path.join(build,'candidate.pptx'));
for (const [i,s] of deck.slides.items.entries()) {
  const blob=await deck.export({slide:s,format:'png',scale:1});
  await fs.writeFile(path.join(build,`slide-${i+1}.png`),new Uint8Array(await blob.arrayBuffer()));
}
await finalizePresentation({workspaceDir:root,candidatePath:path.join(build,'candidate.pptx'),finalPath:path.join(output,'Presentacion-TFM.pptx'),
  pythonExecutable:process.env.ARTIFACT_PYTHON,integrityValidatorPath:path.join(skill,'container_tools/inspect_presentation_package_integrity.py'),
  layoutValidatorPath:path.join(skill,'container_tools/inspect_presentation_layout_geometry.py'),
  layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-bullet-geometry','--validate-heading-fit'],
  fontPolicy:{basis:'design',families:['Arial']},verifyArtifactToolImport:true,
  receiptPath:path.join(build,'validation.json')});
console.log('Presentation exported and package validated.');

