"""Build original vector-derived phone UI assets; no game or third-party artwork.

Requires Pillow. Runtime loads the baked textures, rather than generating them per frame.
"""
from pathlib import Path
import math
from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parents[1] / 'assets/ui'
S = 3
COLORS = {'guess': (242, 180, 110), 'ice': (120, 191, 249), 'gohan': (110, 220, 185)}

def canvas(w, h):
    return Image.new('RGBA', (w*S, h*S)), (w, h)

def save(im, name, size):
    OUT.mkdir(parents=True, exist_ok=True)
    im.resize(size, Image.Resampling.LANCZOS).save(OUT / ('phone-' + name + '.png'))

def rounded(name, size, radius, fill, outline=None):
    im, dims = canvas(*size)
    ImageDraw.Draw(im).rounded_rectangle((1, 1, size[0]*S-2, size[1]*S-2), radius=radius*S,
                                        fill=fill, outline=outline, width=S if outline else 1)
    save(im, name, dims)

def wallpaper(hero, accent):
    w, h = 296, 554
    im, dims = canvas(w, h)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((0, 0, w*S-1, h*S-1), 35*S, fill=(12, 15, 22), outline=(88, 94, 106), width=S)
    screen = Image.new('RGBA', im.size)
    sd = ImageDraw.Draw(screen)
    for y in range(h*S):
        t = y/(h*S)
        sd.line((0, y, w*S, y), fill=(int(22-12*t), int(30-15*t), int(43-21*t), 255))
    # Broad, softly lit ribbons give the phone a wallpaper without competing with text.
    glow = Image.new('RGBA', im.size)
    gd = ImageDraw.Draw(glow)
    gd.ellipse((-120*S, 70*S, 240*S, 445*S), fill=accent+(55,))
    gd.ellipse((180*S, 230*S, 460*S, 580*S), fill=accent+(24,))
    screen.alpha_composite(glow.filter(ImageFilter.GaussianBlur(58*S)))
    ribbon = Image.new('RGBA', im.size)
    rd = ImageDraw.Draw(ribbon)
    for k in range(10):
        points=[]
        for y in range(-30, 580, 3):
            x=172+math.sin(y/155+0.8)*118+k*10
            points.append((int(x*S), y*S))
        rd.line(points, fill=accent+(max(5,30-k*2),), width=2*S)
    screen.alpha_composite(ribbon)
    mask = Image.new('L', im.size)
    ImageDraw.Draw(mask).rounded_rectangle((6*S, 6*S, (w-6)*S, (h-6)*S), 29*S, fill=255)
    im.paste(screen, (0, 0), mask)
    d=ImageDraw.Draw(im)
    d.rounded_rectangle((122*S, 13*S, 174*S, 22*S), 5*S, fill=(5, 8, 12))
    # Status symbols and home indicator are texture details, not claims about hardware.
    for j in range(3):
        d.rounded_rectangle(((225+j*5)*S,(35-j*3)*S,(228+j*5)*S,42*S),S,fill=(220,230,240))
    d.rounded_rectangle((248*S,30*S,268*S,41*S),3*S,outline=(211,223,234),width=S)
    d.rectangle((251*S,33*S,264*S,38*S),fill=accent)
    d.rectangle((269*S,34*S,271*S,37*S),fill=(211,223,234))
    d.rounded_rectangle((113*S,539*S,183*S,542*S),2*S,fill=(194,206,219))
    save(im, hero, dims)

def icon(name):
    im, dims=canvas(48,48);d=ImageDraw.Draw(im);ink=(239,247,255,255)
    def line(points): d.line([(x*S,y*S) for x,y in points],fill=ink,width=2*S,joint='curve')
    def rect(bounds,r=4): d.rounded_rectangle(tuple(v*S for v in bounds),r*S,outline=ink,width=2*S)
    def ellipse(bounds): d.ellipse(tuple(v*S for v in bounds),outline=ink,width=2*S)
    if name=='messages':
        rect((7,8,41,33),7);line([(15,33),(12,40),(25,33)]);line([(14,17),(33,17)]);line([(14,24),(27,24)])
    elif name=='crew':
        ellipse((18,7,30,19));ellipse((5,14,14,23));ellipse((34,14,43,23))
        d.arc((12*S,23*S,36*S,46*S),180,360,fill=ink,width=2*S);line([(12,34),(12,39),(36,39),(36,34)])
        d.arc((1*S,26*S,18*S,42*S),180,270,fill=ink,width=2*S);d.arc((30*S,26*S,47*S,42*S),270,360,fill=ink,width=2*S)
    elif name=='job':
        d.arc((11*S,5*S,37*S,31*S),180,360,fill=ink,width=2*S)
        line([(11,18),(12,26),(24,42),(36,26),(37,18)]);ellipse((19,13,29,23))
    elif name=='wallet':
        rect((6,12,41,37),5);line([(9,12),(31,6),(37,12)]);rect((28,21,43,31),3);ellipse((33,25,35,27))
    elif name=='news':
        rect((8,7,39,40),3);rect((13,12,23,22),1);line([(28,13),(34,13)]);line([(28,19),(34,19)])
        line([(13,28),(34,28)]);line([(13,34),(34,34)]);line([(39,16),(43,16),(43,37),(39,40)])
    elif name=='garage':
        line([(5,20),(24,6),(43,20)]);rect((9,20,39,41),2)
        rect((14,29,34,37),2);line([(17,29),(19,24),(29,24),(31,29)]);line([(17,37),(17,40)]);line([(31,37),(31,40)])
    elif name=='orders':
        ellipse((6,6,42,42));line([(17,31),(22,16),(32,12),(27,27),(17,31)]);line([(22,16),(27,27)])
    elif name=='journal':
        rect((10,6,39,42),4);line([(17,7),(17,41)]);line([(23,16),(33,16)]);line([(23,23),(33,23)]);line([(23,30),(29,30)])
    elif name=='progression':
        line([(7,8),(7,40),(42,40)]);rect((13,28,19,36),1);rect((23,20,29,36),1);rect((33,10,39,36),1)
    elif name=='alerts':
        d.arc((12*S,9*S,36*S,34*S),180,360,fill=ink,width=2*S)
        line([(12,21),(12,31),(8,35),(40,35),(36,31),(36,21)])
        d.arc((19*S,33*S,29*S,43*S),0,180,fill=ink,width=2*S);line([(24,6),(24,9)])
    elif name=='vehicles':
        line([(7,25),(12,14),(35,14),(41,25)]);rect((6,24,42,36),4)
        line([(12,30),(17,30)]);line([(31,30),(36,30)]);rect((10,36,15,41),1);rect((33,36,38,41),1)
    elif name=='properties':
        line([(4,23),(24,6),(44,23)]);line([(10,21),(10,41),(38,41),(38,21)])
        rect((20,28,28,41),1);rect((14,22,18,26),1);rect((30,22,34,26),1)
    elif name=='settings':
        for x,y in ((12,17),(24,31),(36,21)):
            line([(x,7),(x,y-4)]);line([(x,y+4),(x,41)]);ellipse((x-4,y-4,x+4,y+4))
    elif name=='planning':
        rect((5,8,43,35),3);line([(24,35),(24,42)]);line([(15,42),(33,42)])
        rect((11,14,22,21),1);rect((28,24,37,29),1);line([(22,18),(32,18),(32,24)])
    else:
        ellipse((6,6,42,42));ellipse((22,14,26,18));line([(24,24),(24,34)])
    save(im,'icon-'+name,dims)

def main():
    for hero,accent in COLORS.items():
        wallpaper(hero,accent)
        rounded('tile-'+hero,(118,88),15,accent+(42,),accent+(240,))
        rounded('row-'+hero,(256,50),12,accent+(30,),accent+(185,))
        rounded('button-'+hero,(250,36),11,accent+(255,))
    rounded('tile',(118,88),15,(35,47,63,230),(137,162,185,40))
    rounded('row',(256,50),12,(15,23,35,236),(126,151,177,32))
    rounded('reading',(256,304),16,(10,17,27,238),(132,151,174,30))
    rounded('balance',(256,48),13,(10,17,27,225),(132,151,174,35))
    rounded('avatar',(30,30),9,(164,191,217,35))
    for name in ('messages','crew','job','wallet','news','help','garage','orders','journal','progression','alerts','planning','vehicles','properties','settings'):icon(name)
    print('Generated 32 original phone UI textures.')

if __name__=='__main__':main()
