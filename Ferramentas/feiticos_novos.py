import math, os
from PIL import Image, ImageDraw
from icones_feiticos import canvas, P, fin, glow_ball, OL, SS
def gelo():
    im=canvas(); d=ImageDraw.Draw(im)
    glow_ball(d,128,128,112,[(1,(120,200,255,90)),(0.75,(160,220,255,120))])
    for k in range(6):
        a=k*math.pi/3
        tip=(128+math.cos(a)*105,128+math.sin(a)*105)
        l=(128+math.cos(a+0.22)*40,128+math.sin(a+0.22)*40); r=(128+math.cos(a-0.22)*40,128+math.sin(a-0.22)*40)
        d.polygon(P(l,tip,r,(128,128)),fill=(200,240,255),outline=OL)
        for s in (-1,1):
            b=(128+math.cos(a)*68,128+math.sin(a)*68)
            e=(b[0]+math.cos(a+s*0.9)*28,b[1]+math.sin(a+s*0.9)*28)
            d.line(P(b,e),fill=(230,250,255),width=6*SS)
    d.ellipse(P((100,100),(156,156)),fill=(245,252,255),outline=OL,width=4*SS)
    return fin(im)
def cura():
    im=canvas(); d=ImageDraw.Draw(im)
    glow_ball(d,128,128,115,[(1,(80,220,120,90)),(0.7,(140,255,160,130)),(0.45,(210,255,210,150))])
    d.rounded_rectangle(P((98,40),(158,216)),radius=14*SS,fill=(90,230,110),outline=OL,width=5*SS)
    d.rounded_rectangle(P((40,98),(216,158)),radius=14*SS,fill=(90,230,110),outline=OL,width=5*SS)
    d.rectangle(P((103,103),(153,153)),fill=(90,230,110))
    d.rounded_rectangle(P((108,52),(126,120)),radius=6*SS,fill=(200,255,200))
    for x,y,r in [(50,50,9),(205,60,7),(200,200,10),(60,205,6)]:
        d.ellipse(P((x-r,y-r),(x+r,y+r)),fill=(255,255,220))
    return fin(im)
def tornado():
    im=canvas(); d=ImageDraw.Draw(im)
    for i in range(9):
        t=i/8; y=40+t*180; w=110-t*85; x=128+math.sin(t*5)*18
        col=(int(150+80*t),int(170+60*t),int(230-30*t))
        d.ellipse(P((x-w,y-16),(x+w,y+16)),fill=col,outline=OL,width=4*SS)
        d.arc(P((x-w*0.7,y-9),(x+w*0.7,y+9)),200,340,fill=(245,250,255),width=4*SS)
    for x,y in [(40,90),(210,120),(60,170)]:
        d.ellipse(P((x-8,y-5),(x+8,y+5)),fill=(120,100,80))
    return fin(im)
if __name__=="__main__":
    os.makedirs("out7/Cartas",exist_ok=True)
    for k,f in [("gel",gelo),("cur",cura),("tor",tornado)]: f().save(f"out7/Cartas/{k}.png")
    c=Image.new("RGBA",(780,260),(60,90,140,255))
    for i,k in enumerate(["gel","cur","tor"]):
        im=Image.open(f"out7/Cartas/{k}.png"); im.thumbnail((240,240)); c.alpha_composite(im,(10+i*260,10))
    c.save("/tmp/feit.png")
