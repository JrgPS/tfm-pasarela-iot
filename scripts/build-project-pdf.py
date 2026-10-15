"""Build the consolidated project manual using the bundled ReportLab runtime."""
from pathlib import Path
import re
from html import escape
from reportlab.pdfgen import canvas
from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate, PageTemplate, Frame, Paragraph, Spacer, PageBreak,
    Table, TableStyle, Preformatted, KeepTogether, Flowable, CondPageBreak,
)
from reportlab.platypus.tableofcontents import TableOfContents

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'docs/entrega/Manual-proyecto.md'
OUTPUT = ROOT / 'output/pdf/Manual-completo-Pasarela-IoT-TFM.pdf'
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
FONT_DIR = Path('C:/Windows/Fonts')
for name, filename in [('Body', 'arial.ttf'), ('Strong', 'arialbd.ttf'), ('Mono', 'consola.ttf')]:
    pdfmetrics.registerFont(TTFont(name, str(FONT_DIR / filename)))
pdfmetrics.registerFontFamily('Body', normal='Body', bold='Strong', italic='Body', boldItalic='Strong')
NAVY = colors.HexColor('#11283E')
TEAL = colors.HexColor('#087F82')
INK = colors.HexColor('#233744')
MUTED = colors.HexColor('#607784')
PALE = colors.HexColor('#EDF4F6')
WIDTH, HEIGHT = A4
LEFT, RIGHT, TOP, BOTTOM = 48, 48, 65, 48
CW = WIDTH - LEFT - RIGHT

styles = {
    'body': ParagraphStyle('Body', fontName='Body', fontSize=10.2, leading=14.7,
                           textColor=INK, spaceAfter=8.5, splitLongWords=True,
                           allowWidows=False, allowOrphans=False),
    'h1': ParagraphStyle('Chapter', fontName='Strong', fontSize=22, leading=27,
                        textColor=NAVY, spaceAfter=18, keepWithNext=True),
    'h2': ParagraphStyle('Section', fontName='Strong', fontSize=13, leading=17,
                        textColor=TEAL, spaceBefore=12, spaceAfter=8, keepWithNext=True),
    'table': ParagraphStyle('Cell', fontName='Body', fontSize=9, leading=12.3, textColor=INK),
    'th': ParagraphStyle('HeaderCell', fontName='Strong', fontSize=9, leading=12, textColor=colors.white),
    'code': ParagraphStyle('Code', fontName='Mono', fontSize=8.05, leading=11.6, textColor=NAVY),
    'bullet': ParagraphStyle('List', fontName='Body', fontSize=10.2, leading=14.7,
                            textColor=INK, leftIndent=16, firstLineIndent=0,
                            bulletIndent=0, bulletFontName='Body', bulletFontSize=10,
                            spaceAfter=6),
    'note': ParagraphStyle('Note', fontName='Body', fontSize=8.4, leading=11.5,
                          textColor=MUTED, spaceBefore=6, spaceAfter=11),
}

def inline(text):
    text = escape(text)
    text = re.sub(r'`([^`]+)`', r'<font name="Mono">\1</font>', text)
    text = re.sub(r'\*\*([^*]+)\*\*', r'<b>\1</b>', text)
    text = re.sub(r'https://[^\s<>]+', lambda m: '<link href="'+m[0]+'" color="#087F82">'+m[0]+'</link>', text)
    return text

def paragraph(text, kind='body'):
    return Paragraph(inline(text), styles[kind])

class Architecture(Flowable):
    def __init__(self):
        super().__init__()
        self.width, self.height = CW, 247

    def draw(self):
        c = self.canv
        def box(x, y, w, title, subtitle):
            c.setFillColor(PALE); c.setStrokeColor(colors.HexColor('#B8D0D7'))
            c.roundRect(x, y, w, 55, 5, stroke=1, fill=1)
            c.setFillColor(NAVY); c.setFont('Strong', 10.3)
            c.drawCentredString(x+w/2, y+33, title)
            c.setFillColor(MUTED); c.setFont('Body', 8.7)
            c.drawCentredString(x+w/2, y+16, subtitle)
        def arrow(points):
            c.setStrokeColor(TEAL); c.setFillColor(TEAL); c.setLineWidth(1.5)
            p = c.beginPath(); p.moveTo(*points[0])
            for xy in points[1:]: p.lineTo(*xy)
            c.drawPath(p)
            x,y = points[-1]; px,py=points[-2]
            import math
            a=math.atan2(y-py,x-px)
            p=c.beginPath(); p.moveTo(x,y)
            p.lineTo(x-7*math.cos(a-.45),y-7*math.sin(a-.45))
            p.lineTo(x-7*math.cos(a+.45),y-7*math.sin(a+.45)); p.close()
            c.drawPath(p,fill=1,stroke=0)
        box(0,164,100,'Simulador','N dispositivos')
        box(130,164,100,'HAProxy','TCP :7443')
        box(265,164,110,'gateway','2 réplicas, TLS')
        box(130,45,100,'HAProxy','HTTP interno')
        box(265,45,110,'registry','2 réplicas')
        box(403,45,96,'PostgreSQL','Volumen pgdata')
        arrow([(100,191),(130,191)])
        arrow([(230,191),(265,191)])
        arrow([(320,164),(320,130),(180,130),(180,100)])
        arrow([(230,72),(265,72)])
        arrow([(375,72),(403,72)])
        c.setFont('Body',8.5);c.setFillColor(MUTED)
        c.drawString(0,230,'Red de aplicación y balanceo')
        c.drawString(335,115,'Llamadas a API')
        c.drawString(400,27,'Red de datos')
        c.setFont('Body',8.2)
        c.drawString(0,7,'Solo la entrada TCP se publica en localhost. Las dos funciones de HAProxy comparten instancia.')

class ManualDoc(BaseDocTemplate):
    def afterFlowable(self, flowable):
        if isinstance(flowable, Paragraph) and flowable.style.name == 'Chapter':
            text=flowable.getPlainText()
            number=re.match(r'(\d+)\.', text)
            if number:
                key='chapter-'+number[1]
                self.canv.bookmarkPage(key)
                self.canv.addOutlineEntry(text,key,0,False)
                self.notify('TOCEntry',(0,text,self.page,key))

def page(canvas, doc):
    canvas.saveState()
    if doc.page == 1:
        canvas.setFillColor(NAVY); canvas.rect(0,0,WIDTH,HEIGHT,fill=1,stroke=0)
        canvas.setFillColor(colors.HexColor('#42D7BA'))
        canvas.setFont('Strong',11); canvas.drawString(LEFT,HEIGHT-78,'TFM  /  BIGSCHOOL')
        canvas.setFillColor(colors.white); canvas.setFont('Strong',34)
        canvas.drawString(LEFT,HEIGHT-190,'Pasarela IoT')
        canvas.drawString(LEFT,HEIGHT-233,'TCP escalable')
        canvas.setFont('Body',18)
        canvas.drawString(LEFT,HEIGHT-291,'Manual completo del proyecto')
        canvas.setStrokeColor(colors.HexColor('#42D7BA'));canvas.setLineWidth(2)
        canvas.line(LEFT,HEIGHT-327,WIDTH-RIGHT,HEIGHT-327)
        canvas.setFont('Body',12);canvas.setFillColor(colors.HexColor('#C4D6E1'))
        for i,line in enumerate(['Funcionamiento, arquitectura y protocolo',
                                 'Instalación, operación y pruebas verificadas',
                                 'Seguridad, decisiones y preparación de la entrega']):
            canvas.drawString(LEFT,HEIGHT-371-i*25,line)
        canvas.setFillColor(colors.white);canvas.setFont('Strong',11)
        canvas.drawString(LEFT,166,'Máster en Desarrollo con IA E.1')
        canvas.setFont('Body',10.5)
        canvas.drawString(LEFT,143,'Versión de referencia v1.0.0  -  15 de octubre de 2026')
        canvas.setFillColor(colors.HexColor('#C4D6E1'));canvas.setFont('Body',9)
        canvas.drawString(LEFT,86,'Proyecto individual con dispositivos y operaciones ficticios.')
        canvas.drawString(LEFT,70,'Documento consolidado a partir del código y la evidencia del repositorio.')
    else:
        canvas.setFillColor(NAVY);canvas.setFont('Strong',8.5)
        canvas.drawString(LEFT,HEIGHT-35,'PASARELA IoT TCP  /  MANUAL DEL PROYECTO')
        canvas.setFillColor(MUTED);canvas.setFont('Body',8)
        canvas.drawRightString(WIDTH-RIGHT,HEIGHT-35,'v1.0.0  ·  15/10/2026')
        canvas.setStrokeColor(colors.HexColor('#CCDADF'));canvas.setLineWidth(.5)
        canvas.line(LEFT,HEIGHT-45,WIDTH-RIGHT,HEIGHT-45)
        canvas.line(LEFT,35,WIDTH-RIGHT,35)
        canvas.setFont('Body',8);canvas.setFillColor(MUTED)
        canvas.drawString(LEFT,23,'TFM BigSchool  ·  Datos ficticios  ·  Sin pagos reales')
        canvas.drawRightString(WIDTH-RIGHT,23,f'Página {doc.page}')
    canvas.restoreState()

def table(rows):
    n=len(rows[0]); header=rows[0]
    if n==6:
        widths=[131,76,62,81,77,CW-427]
    elif n==3:
        if header[0]=='Código': widths=[56,200,CW-256]
        elif header[0]=='Tabla': widths=[105,205,CW-310]
        elif header[0]=='Interrupción': widths=[185,105,CW-290]
        else: widths=[107,169,CW-276]
    else:
        first=150
        if header[0] in ('Ruta','Archivo en secrets/','Variable'): first=204
        if header[0]=='Componente': first=88
        if header[0]=='Opción': first=139
        widths=[first,CW-first]
    data=[[Paragraph(inline(cell),styles['th' if r==0 else 'table']) for cell in row] for r,row in enumerate(rows)]
    t=Table(data,colWidths=widths,repeatRows=1,hAlign='LEFT',
            rowSplitRange=(2,-2) if len(rows)>3 else None)
    t.setStyle(TableStyle([
        ('BACKGROUND',(0,0),(-1,0),NAVY),('VALIGN',(0,0),(-1,-1),'TOP'),
        ('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.white,PALE]),
        ('LINEBELOW',(0,0),(-1,0),.5,NAVY),
        ('LINEBELOW',(0,1),(-1,-1),.25,colors.HexColor('#D5E1E5')),
        ('LEFTPADDING',(0,0),(-1,-1),8),('RIGHTPADDING',(0,0),(-1,-1),8),
        ('TOPPADDING',(0,0),(-1,-1),7),('BOTTOMPADDING',(0,0),(-1,-1),7),
    ]))
    if header[0]=='Perfil': t=KeepTogether([t])
    return [t,Spacer(1,12)]

story=[Spacer(1,10),PageBreak(),paragraph('Índice','h1')]
story.append(paragraph('Guía técnica y de uso de la versión publicada. Las entradas del índice y los enlaces del documento son navegables. El manual distingue requisitos, comportamiento implementado y resultados históricos.'))
toc=TableOfContents()
toc.levelStyles=[ParagraphStyle('TOC0',fontName='Body',fontSize=10.3,leading=17.8,
                               textColor=NAVY,leftIndent=0,firstLineIndent=0,spaceBefore=0)]
story.append(toc)
story.append(Spacer(1,15))
story.append(paragraph('No se incluyen claves operativas, contraseñas ni datos financieros reales. Las cifras de las pruebas están vinculadas a sus condiciones y no constituyen un compromiso de servicio.','note'))

lines=SOURCE.read_text(encoding='utf-8').replace('\u2011','-').replace('\u2013','-').replace('\u2014','-').splitlines()
i=0
chapter_count=0
while i<len(lines):
    line=lines[i].strip()
    if not line: i+=1;continue
    if line.startswith('# '):
        if chapter_count == 0:
            story.append(PageBreak())
        else:
            story.extend([CondPageBreak(270),Spacer(1,20)])
        chapter_count+=1
        story.append(paragraph(line[2:],'h1'));i+=1;continue
    if line.startswith('## '):
        story.append(paragraph(line[3:],'h2'));i+=1;continue
    if line=='<architecture/>':
        story.append(Architecture());story.append(Spacer(1,9));i+=1;continue
    if line.startswith('```'):
        i+=1; code=[]
        while i<len(lines) and not lines[i].startswith('```'):
            code.append(lines[i]);i+=1
        i+=1
        pre=Preformatted('\n'.join(code),styles['code'],maxLineLength=108)
        block=Table([[pre]],colWidths=[CW])
        block.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,-1),PALE),
                                  ('LEFTPADDING',(0,0),(-1,-1),10),('RIGHTPADDING',(0,0),(-1,-1),10),
                                  ('TOPPADDING',(0,0),(-1,-1),10),('BOTTOMPADDING',(0,0),(-1,-1),10)]))
        story.extend([Spacer(1,3),block,Spacer(1,11)]);continue
    if line.startswith('|'):
        rows=[]
        while i<len(lines) and lines[i].strip().startswith('|'):
            cells=[x.strip() for x in lines[i].strip().strip('|').split('|')]
            if not all(re.fullmatch(r':?-+:?',cell) for cell in cells): rows.append(cells)
            i+=1
        story.extend(table(rows));continue
    match=re.match(r'^(\d+)\. (.*)',line)
    if line.startswith('- ') or match:
        label='-' if not match else match[1]+'.'
        body=line[2:] if not match else match[2]
        story.append(Paragraph(inline(body),styles['bullet'],bulletText=label));i+=1;continue
    para=[line];i+=1
    while i<len(lines) and lines[i].strip() and not lines[i].startswith(('#','|','```','- ')):
        para.append(lines[i].strip());i+=1
    story.append(paragraph(' '.join(para)))

doc=ManualDoc(str(OUTPUT),pagesize=A4,leftMargin=LEFT,rightMargin=RIGHT,
              topMargin=TOP,bottomMargin=BOTTOM,title='Pasarela IoT TCP escalable - Manual completo',
              author='Proyecto TFM BigSchool',subject='Arquitectura, protocolo, uso y validación de v1.0.0')
frame=Frame(LEFT,BOTTOM,CW,HEIGHT-TOP-BOTTOM,leftPadding=0,rightPadding=0,topPadding=0,bottomPadding=0)
doc.addPageTemplates(PageTemplate(id='Manual',frames=[frame],onPage=page))
doc.multiBuild(story)
print(OUTPUT)
