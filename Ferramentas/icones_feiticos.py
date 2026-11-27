import math, os, random
from PIL import Image, ImageDraw, ImageFilter
SS=3; OL=(38,28,30)
def canvas(): return Image.new("RGBA",(256*SS,256*SS),(0,0,0,0))
def P(*pts): return [(x*SS,y*SS) for x,y in pts]
def fin(img, size=256):
    out=img.resize((size,size),Image.LANCZOS)
    a=out.split()[3]; g=Image.new("RGBA",out.size,(30,22,26,0)); g.putalpha(a.filter(ImageFilter.MaxFilter(5)))
    g.alpha_composite(out); return g.crop(g.getbbox())
def glow_ball(d,cx,cy,r,cols):
    for i,(k,col) in enumerate(cols):
        rr=r*k; d.ellipse([(cx-rr)*SS,(cy-rr)*SS,(cx+rr)*SS,(cy+rr)*SS],fill=col)
def fireball():
    im=canvas(); d=ImageDraw.Draw(im)
    for k in range(6):
        x=70-k*12; y=190+k*10; r=26-k*4
        d.ellipse([(x-r)*SS,(y-r)*SS,(x+r)*SS,(y+r)*SS],fill=(255,120,30,220))
    glow_ball(d,140,120,95,[(1,(230,70,20)),(0.8,(255,130,30)),(0.58,(255,200,70)),(0.32,(255,250,200))])
    return fin(im)
def arrows():
    im=canvas(); d=ImageDraw.Draw(im)
    for i,(x,y) in enumerate([(60,40),(110,20),(160,50),(85,90),(140,95),(190,80)]):
        a=(x,y); b=(x-40,y+150)
        d.line(P(a,b),fill=(150,100,50),width=7*SS)
        d.polygon(P((b[0]-12,b[1]-10),(b[0]+8,b[1]-4),(b[0]-6,b[1]+22)),fill=(225,230,240),outline=OL)
        d.polygon(P((a[0]-10,a[1]-6),(a[0]+10,a[1]-2),(a[0]+2,a[1]+16)),fill=(245,245,245))
    return fin(im)
def lightning():
    im=canvas(); d=ImageDraw.Draw(im)
    glow_ball(d,128,128,110,[(1,(80,90,170,120)),(0.7,(120,130,220,140))])
    pts=[(150,10),(80,130),(130,130),(95,250),(190,105),(140,105),(185,10)]
    d.polygon(P(*pts),fill=(255,235,80),outline=OL)
    d.line(P(*(pts+[pts[0]])),fill=OL,width=5*SS)
    d.polygon(P((150,25),(110,115),(135,115)),fill=(255,255,210))
    return fin(im)
def meteor():
    im=canvas(); d=ImageDraw.Draw(im)
    for k in range(7):
        x=200-k*0; y=40
    d.polygon(P((250,0),(200,0),(120,130),(170,170)),fill=(255,140,40,200))
    d.polygon(P((256,30),(230,0),(140,150),(160,165)),fill=(255,210,90,200))
    rng=random.Random(3)
    pts=[]
    for i in range(10):
        a=i/10*math.tau; r=70*rng.uniform(0.85,1.1); pts.append((110+math.cos(a)*r,160+math.sin(a)*r))
    d.polygon(P(*pts),fill=(110,80,70),outline=OL)
    d.line(P(*(pts+[pts[0]])),fill=OL,width=5*SS)
    for cx,cy,r in [(90,140,14),(130,185,10),(120,130,8)]:
        d.ellipse([(cx-r)*SS,(cy-r)*SS,(cx+r)*SS,(cy+r)*SS],fill=(255,150,40))
    d.ellipse([60*SS,110*SS,110*SS,150*SS],fill=(150,115,100))
    return fin(im)
def orb(cols,size=64):
    im=canvas(); d=ImageDraw.Draw(im); glow_ball(d,128,128,110,cols); return im.resize((size,size),Image.LANCZOS)
def bomb():
    im=canvas(); d=ImageDraw.Draw(im)
    d.ellipse(P((48,70),(208,230)),fill=(50,50,60),outline=OL,width=6*SS)
    d.ellipse(P((80,95),(120,130)),fill=(120,120,140))
    d.rectangle(P((115,52),(141,78)),fill=(90,90,100),outline=OL,width=4*SS)
    d.line(P((128,52),(150,20)),fill=(150,110,60),width=6*SS)
    glow_ball(d,152,18,18,[(1,(255,140,30)),(0.5,(255,240,150))])
    return im.resize((64,64),Image.LANCZOS)
if __name__=="__main__":
    os.makedirs("out4/Cartas",exist_ok=True); os.makedirs("out4/Batalha",exist_ok=True)
    fireball().save("out4/Cartas/fog.png"); arrows().save("out4/Cartas/fle.png")
    lightning().save("out4/Cartas/rai.png"); meteor().save("out4/Cartas/met.png")
    orb([(1,(80,160,255,90)),(0.7,(150,210,255)),(0.45,(220,245,255)),(0.2,(255,255,255))]).save("out4/Batalha/gelo.png")
    orb([(1,(255,90,20,110)),(0.7,(255,150,40)),(0.45,(255,210,90)),(0.2,(255,250,220))]).save("out4/Batalha/fogo.png")
    orb([(1,(150,90,255,90)),(0.7,(190,140,255)),(0.45,(230,210,255)),(0.2,(255,255,255))]).save("out4/Batalha/magia.png")
    bomb().save("out4/Batalha/bomba.png")
    print("ok")
