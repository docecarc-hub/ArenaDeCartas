import math, os
from personagens3d import *
from render3d import Scene, render
from PIL import Image, ImageDraw, ImageFilter
M = dict(BASE_MATS); M.update({"blue": (0.2, 0.45, 0.95, 0.35), "red": (0.9, 0.25, 0.3, 0.35), "steel": (0.85, 0.88, 0.95, 0.95)})
def emblema():
    P = []
    # escudo: metade azul, metade vermelha, borda dourada
    P.append(Prim("box", "gold", c=V(0, 1.0, 0.05), h=V(0.62, 0.72, 0.08), r=0.12))
    P.append(Prim("box", "blue", c=V(-0.27, 1.02, -0.02), h=V(0.27, 0.62, 0.06), r=0.08))
    P.append(Prim("box", "red", c=V(0.27, 1.02, -0.02), h=V(0.27, 0.62, 0.06), r=0.08))
    P.append(Prim("cone", "gold", c=V(0, 0.32, 0.05), r=0.6, h=0.4, R=rot_x(math.pi)))
    # duas espadas cruzadas atrás
    for s in (-1, 1):
        R = rot_z(s * 0.75)
        a = V(0, 0.2, 0.2) + R @ V(0, -0.9, 0); b = V(0, 1.0, 0.2) + R @ V(0, 1.45, 0)
        P.append(Prim("capsule", "steel", a=V(0, 1.0, 0.2), b=b, r=0.07))
        P.append(Prim("capsule", "gold", a=V(0, 1.0, 0.2) + R @ V(-0.25, -0.2, 0), b=V(0, 1.0, 0.2) + R @ V(0.25, -0.2, 0), r=0.05))
        P.append(Prim("capsule", "leather", a=V(0, 1.0, 0.2) + R @ V(0, -0.2, 0), b=V(0, 1.0, 0.2) + R @ V(0, -0.55, 0), r=0.05))
    # coroa no topo
    P.append(Prim("cyl", "gold", c=V(0, 1.85, 0), r=0.32, h=0.1, rr=0.04))
    for i in range(5):
        a = i / 5 * math.tau
        P.append(Prim("cone", "gold", c=V(math.sin(a) * 0.28, 2.0, math.cos(a) * 0.28), r=0.09, h=0.25))
    P.append(Prim("sphere", "red", c=V(0, 1.86, -0.33), r=0.07))
    # carta no centro
    P.append(Prim("box", "white", c=V(0, 1.05, -0.12), h=V(0.2, 0.28, 0.02), r=0.04, R=rot_z(-0.15)))
    P.append(Prim("sphere", "gold", c=V(0.0, 1.05, -0.16), r=0.08))
    return P
if __name__ == "__main__":
    os.makedirs("out13", exist_ok=True)
    im = render(Scene(emblema(), M, yaw=math.radians(-12)), size=512, extent=1.55, center=(0, 1.1, 0), pitch=12)
    im = im.crop(im.getbbox())
    im.save("out13/emblema.png")
    # ícone quadrado com fundo
    S = 512
    ic = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(ic)
    d.rounded_rectangle([8, 8, S - 8, S - 8], radius=96, fill=(28, 44, 92, 255), outline=(255, 205, 70, 255), width=14)
    glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse([S/2 - 170, S/2 - 190, S/2 + 170, S/2 + 150], fill=(110, 160, 255, 150))
    glow = glow.filter(ImageFilter.GaussianBlur(40))
    ic.alpha_composite(glow)
    e = im.copy(); e.thumbnail((400, 400), Image.LANCZOS)
    ic.alpha_composite(e, ((S - e.width) // 2, (S - e.height) // 2))
    ic.save("out13/icone.png")
    print("ok", im.size)
