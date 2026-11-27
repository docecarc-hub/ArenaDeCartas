"""Gera ilustrações isométricas originais das arenas (ilha flutuante) para o lobby."""
import math
import random
import sys
from PIL import Image, ImageDraw, ImageFilter

SS = 3                      # supersampling para bordas suaves
W, H = 640, 560             # tamanho final
TW, TH = 44, 22             # tamanho de um "tile" isométrico (2:1)
NU, NV = 8, 12              # largura x comprimento da arena em tiles
THICK = 46                  # espessura do bloco de terra

THEMES = [
    dict(name="campo_verde", grass=(98, 172, 74), grass2=(88, 160, 66), lip=(70, 128, 52),
         path=(214, 182, 128), side_l=(120, 82, 52), side_r=(150, 104, 66), rock=(96, 70, 50),
         water=(74, 150, 222), water_hi=(150, 205, 245), bridge=(132, 90, 52), fence=(150, 104, 60),
         deco="trees"),
    dict(name="vale_das_pedras", grass=(150, 158, 104), grass2=(140, 148, 96), lip=(110, 116, 76),
         path=(196, 180, 150), side_l=(104, 100, 96), side_r=(132, 128, 122), rock=(84, 80, 78),
         water=(80, 140, 190), water_hi=(150, 195, 225), bridge=(120, 110, 100), fence=(120, 112, 104),
         deco="rocks"),
    dict(name="fortaleza_de_gelo", grass=(226, 238, 246), grass2=(212, 228, 240), lip=(170, 196, 220),
         path=(176, 206, 230), side_l=(96, 140, 186), side_r=(128, 172, 214), rock=(70, 110, 160),
         water=(120, 200, 235), water_hi=(220, 245, 255), bridge=(150, 120, 96), fence=(160, 190, 216),
         deco="ice"),
    dict(name="vulcao_rugidor", grass=(96, 70, 62), grass2=(86, 62, 56), lip=(64, 44, 40),
         path=(140, 108, 90), side_l=(58, 40, 38), side_r=(78, 54, 50), rock=(40, 28, 28),
         water=(250, 120, 30), water_hi=(255, 210, 90), bridge=(70, 52, 46), fence=(90, 64, 56),
         deco="volcano"),
]

RED = dict(top=(226, 78, 72), l=(160, 46, 44), r=(196, 60, 56), roof=(250, 196, 70))
BLUE = dict(top=(76, 136, 232), l=(40, 80, 160), r=(56, 108, 200), roof=(250, 196, 70))


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3])


class Iso:
    def __init__(self, ox, oy):
        self.ox, self.oy = ox, oy

    def p(self, u, v, z=0.0):
        x = self.ox + (u - v) * TW / 2
        y = self.oy + (u + v) * TH / 2 - z
        return (x * SS, y * SS)


def poly(d, pts, fill, outline=None, width=1):
    d.polygon(pts, fill=fill)
    if outline:
        d.line(pts + [pts[0]], fill=outline, width=width * SS, joint="curve")


def box(d, iso, u, v, su, sv, h, top, left, right, z=0.0):
    """Caixa isométrica com base centrada em (u,v)."""
    u0, u1, v0, v1 = u - su / 2, u + su / 2, v - sv / 2, v + sv / 2
    a, b, c, e = iso.p(u0, v1, z), iso.p(u1, v1, z), iso.p(u1, v0, z), iso.p(u0, v0, z)
    at, bt, ct, et = iso.p(u0, v1, z + h), iso.p(u1, v1, z + h), iso.p(u1, v0, z + h), iso.p(u0, v0, z + h)
    ol = shade(left, 0.6)
    poly(d, [a, b, bt, at], left, ol)          # face da frente-esquerda (v = v1)
    poly(d, [b, c, ct, bt], right, ol)         # face da direita (u = u1)
    poly(d, [at, bt, ct, et], top, ol)         # tampa
    return at, bt, ct, et


def shadow(img, iso, u, v, r):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x, y = iso.p(u, v)
    d.ellipse([x - r * SS, y - r * 0.5 * SS, x + r * SS, y + r * 0.5 * SS], fill=(0, 0, 0, 70))
    img.alpha_composite(layer)


def tower(img, iso, u, v, team, king):
    d = ImageDraw.Draw(img)
    size = 1.5 if king else 1.1
    h = 34 if king else 26
    shadow(img, iso, u + 0.2, v + 0.2, 26 if king else 20)
    d = ImageDraw.Draw(img)
    stone_top, stone_l, stone_r = (178, 172, 164), (120, 114, 108), (150, 144, 136)
    # base de pedra
    box(d, iso, u, v, size, size, h, stone_top, stone_l, stone_r)
    # faixa colorida do time
    box(d, iso, u, v, size * 1.02, size * 1.02, 8, team["top"], team["l"], team["r"], z=h - 12)
    # ameias (dentes) no topo
    s = size / 4
    for i in range(4):
        for j in range(4):
            if 0 < i < 3 and 0 < j < 3:
                continue
            cu = u - size / 2 + s / 2 + i * s
            cv = v - size / 2 + s / 2 + j * s
            box(d, iso, cu, cv, s * 0.8, s * 0.8, 6, stone_top, stone_l, stone_r, z=h)
    if king:
        # telhado cônico
        cx, cy = iso.p(u, v, h + 4)
        rw = size * TW * 0.42 * SS
        apex = (cx, cy - 34 * SS)
        d.polygon([(cx - rw, cy), (cx + rw, cy), apex], fill=team["top"], outline=shade(team["top"], 0.6))
        d.polygon([(cx, cy + 6 * SS), (cx + rw, cy), apex], fill=team["r"])
        d.polygon([(cx - rw, cy), (cx, cy + 6 * SS), apex], fill=team["l"])
        # bandeira
        d.line([apex, (apex[0], apex[1] - 14 * SS)], fill=(60, 50, 40), width=2 * SS)
        d.polygon([(apex[0], apex[1] - 14 * SS), (apex[0] + 12 * SS, apex[1] - 10 * SS),
                   (apex[0], apex[1] - 6 * SS)], fill=team["roof"])
    else:
        # bandeirinha
        cx, cy = iso.p(u, v, h + 6)
        d.line([(cx, cy), (cx, cy - 16 * SS)], fill=(60, 50, 40), width=2 * SS)
        d.polygon([(cx, cy - 16 * SS), (cx + 10 * SS, cy - 12 * SS), (cx, cy - 8 * SS)], fill=team["top"])


def tree(img, iso, u, v, rng, snowy=False):
    shadow(img, iso, u + 0.15, v + 0.15, 14)
    d = ImageDraw.Draw(img)
    x, y = iso.p(u, v)
    d.rectangle([x - 2 * SS, y - 14 * SS, x + 2 * SS, y], fill=(96, 64, 40))
    base = (52, 130, 60) if not snowy else (70, 120, 90)
    for k, (dx, dy, r) in enumerate([(0, -22, 12), (-7, -16, 9), (7, -16, 9), (0, -30, 8)]):
        col = shade(base, 0.85 + 0.12 * k)
        d.ellipse([x + (dx - r) * SS, y + (dy - r) * SS, x + (dx + r) * SS, y + (dy + r) * SS], fill=col)
        if snowy:
            d.ellipse([x + (dx - r * 0.7) * SS, y + (dy - r) * SS, x + (dx + r * 0.7) * SS, y + (dy - r * 0.3) * SS],
                      fill=(240, 248, 255))


def rock(img, iso, u, v, rng, col, big=1.0):
    d = ImageDraw.Draw(img)
    x, y = iso.p(u, v)
    r = rng.uniform(5, 9) * big
    pts = []
    for i in range(7):
        a = math.pi * 2 * i / 7
        rr = r * rng.uniform(0.75, 1.15)
        pts.append((x + math.cos(a) * rr * 1.3 * SS, y + (math.sin(a) * rr * 0.8 - r * 0.6) * SS))
    d.polygon(pts, fill=col, outline=shade(col, 0.6))
    d.ellipse([x - r * 0.5 * SS, y - r * 1.1 * SS, x + r * 0.2 * SS, y - r * 0.6 * SS], fill=shade(col, 1.3))


def crystal(img, iso, u, v, rng):
    d = ImageDraw.Draw(img)
    x, y = iso.p(u, v)
    for dx, hgt in [(-5, 18), (0, 26), (6, 15)]:
        bx = x + dx * SS
        d.polygon([(bx - 4 * SS, y), (bx + 4 * SS, y), (bx, y - hgt * SS)], fill=(170, 225, 250), outline=(90, 160, 210))
        d.polygon([(bx, y), (bx + 4 * SS, y), (bx, y - hgt * SS)], fill=(120, 195, 235))


def lava_vent(img, iso, u, v, rng):
    d = ImageDraw.Draw(img)
    x, y = iso.p(u, v)
    d.polygon([(x - 16 * SS, y), (x + 16 * SS, y), (x + 6 * SS, y - 20 * SS), (x - 6 * SS, y - 20 * SS)],
              fill=(70, 46, 40), outline=(40, 26, 24))
    d.ellipse([x - 6 * SS, y - 23 * SS, x + 6 * SS, y - 17 * SS], fill=(255, 150, 40))
    for k in range(3):
        sx = x + rng.uniform(-4, 4) * SS
        sy = y - (32 + k * 10) * SS
        r = (6 + k * 3) * SS
        d.ellipse([sx - r, sy - r, sx + r, sy + r], fill=(110, 100, 100, 150))


def island(theme, seed=1):
    rng = random.Random(seed)
    img = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))
    # centraliza o losango da arena
    top_y = (H - (NU + NV) * TH / 2 - THICK) / 2 - 40
    iso = Iso(W / 2 + (NV - NU) * TW / 4, top_y + 20)
    d = ImageDraw.Draw(img)

    # ---------- parte de baixo da ilha (rochas penduradas)
    L, B, R = iso.p(0, NV), iso.p(NU, NV), iso.p(NU, 0)
    t = THICK * SS
    def jag(p0, p1, n, depth, rng):
        pts = []
        for i in range(n + 1):
            f = i / n
            x = p0[0] + (p1[0] - p0[0]) * f
            y = p0[1] + (p1[1] - p0[1]) * f
            bulge = math.sin(f * math.pi) * depth
            pts.append((x, y + t + bulge * SS + rng.uniform(-6, 6) * SS))
        return pts
    tip = (B[0] + rng.uniform(-10, 10) * SS, B[1] + t + 120 * SS)
    left_under = [(L[0], L[1] + t)] + jag(L, B, 7, 60, rng)[1:-1] + [tip]
    right_under = [tip] + jag(B, R, 7, 60, rng)[1:-1] + [(R[0], R[1] + t)]
    poly(d, [(L[0], L[1] + t)] + left_under[1:] + [(B[0], B[1] + t)], shade(theme["rock"], 0.9))
    poly(d, [(B[0], B[1] + t)] + right_under + [(R[0], R[1] + t)], theme["rock"])
    # rochas penduradas
    for _ in range(9):
        f = rng.uniform(0.1, 0.9)
        side = rng.choice([0, 1])
        p0, p1 = (L, B) if side == 0 else (B, R)
        x = p0[0] + (p1[0] - p0[0]) * f
        y = p0[1] + (p1[1] - p0[1]) * f + t + math.sin(f * math.pi) * 40 * SS
        hh = rng.uniform(20, 50) * SS
        ww = rng.uniform(8, 16) * SS
        col = shade(theme["rock"], rng.uniform(0.8, 1.1))
        d.polygon([(x - ww, y - 4 * SS), (x + ww, y - 4 * SS), (x + rng.uniform(-4, 4) * SS, y + hh)], fill=col)
    if theme["deco"] == "ice":
        for _ in range(12):
            f = rng.uniform(0.05, 0.95)
            p0, p1 = (L, B) if rng.random() < 0.5 else (B, R)
            x = p0[0] + (p1[0] - p0[0]) * f
            y = p0[1] + (p1[1] - p0[1]) * f + t
            hh = rng.uniform(12, 34) * SS
            d.polygon([(x - 4 * SS, y), (x + 4 * SS, y), (x, y + hh)], fill=(200, 235, 250))
    if theme["deco"] == "volcano":
        for _ in range(6):
            f = rng.uniform(0.1, 0.9)
            p0, p1 = (L, B) if rng.random() < 0.5 else (B, R)
            x = p0[0] + (p1[0] - p0[0]) * f
            y = p0[1] + (p1[1] - p0[1]) * f + t * 0.5
            d.line([(x, y), (x + rng.uniform(-10, 10) * SS, y + rng.uniform(30, 70) * SS)],
                   fill=(255, 120, 30), width=3 * SS)

    # ---------- laterais do bloco (camadas de terra)
    poly(d, [L, B, (B[0], B[1] + t), (L[0], L[1] + t)], theme["side_l"], shade(theme["side_l"], 0.6))
    poly(d, [B, R, (R[0], R[1] + t), (B[0], B[1] + t)], theme["side_r"], shade(theme["side_r"], 0.6))
    for k in (0.45, 0.75):
        y = t * k
        d.line([(L[0], L[1] + y), (B[0], B[1] + y)], fill=shade(theme["side_l"], 0.8), width=2 * SS)
        d.line([(B[0], B[1] + y), (R[0], R[1] + y)], fill=shade(theme["side_r"], 0.85), width=2 * SS)
    # borda de grama caindo pela lateral
    lip = 7 * SS
    poly(d, [L, B, (B[0], B[1] + lip), (L[0], L[1] + lip)], theme["lip"])
    poly(d, [B, R, (R[0], R[1] + lip), (B[0], B[1] + lip)], shade(theme["lip"], 1.1))

    # ---------- piso quadriculado
    for u in range(NU):
        for v in range(NV):
            col = theme["grass"] if (u + v) % 2 == 0 else theme["grass2"]
            poly(d, [iso.p(u, v), iso.p(u + 1, v), iso.p(u + 1, v + 1), iso.p(u, v + 1)], col)

    def flat(u0, u1, v0, v1, col, z=0.0, outline=None):
        poly(d, [iso.p(u0, v0, z), iso.p(u1, v0, z), iso.p(u1, v1, z), iso.p(u0, v1, z)], col, outline)

    # caminhos (pistas + ligação entre torres)
    path = theme["path"]
    for lu in (2.0, 6.0):
        flat(lu - 0.45, lu + 0.45, 1.6, NV - 1.6, path)
    flat(2.0, 6.0, 1.6, 2.4, path)
    flat(2.0, 6.0, NV - 2.4, NV - 1.6, path)
    # rio
    flat(0, NU, NV / 2 - 0.45, NV / 2 + 0.45, theme["water"])
    flat(0, NU, NV / 2 - 0.45, NV / 2 - 0.3, theme["water_hi"])
    if theme["deco"] == "volcano":
        for i in range(10):
            u = rng.uniform(0.2, NU - 0.2)
            x, y = iso.p(u, NV / 2 + rng.uniform(-0.3, 0.3))
            d.ellipse([x - 3 * SS, y - 2 * SS, x + 3 * SS, y + 2 * SS], fill=(255, 230, 120))
    # pontes
    for lu in (2.0, 6.0):
        box(d, iso, lu, NV / 2, 1.0, 1.4, 3, theme["bridge"], shade(theme["bridge"], 0.7), shade(theme["bridge"], 0.85))
        for k in range(4):
            vv = NV / 2 - 0.55 + k * 0.37
            a, b = iso.p(lu - 0.5, vv, 3.2), iso.p(lu + 0.5, vv, 3.2)
            d.line([a, b], fill=shade(theme["bridge"], 0.65), width=1 * SS)

    # ---------- objetos em pé, do fundo para a frente
    items = []
    items.append((1.0 + 4, lambda: tower(img, iso, 4, 1.0, RED, True)))
    items.append((2.5 + 2, lambda: tower(img, iso, 2, 2.6, RED, False)))
    items.append((2.5 + 6, lambda: tower(img, iso, 6, 2.6, RED, False)))
    items.append((NV - 2.6 + 2, lambda: tower(img, iso, 2, NV - 2.6, BLUE, False)))
    items.append((NV - 2.6 + 6, lambda: tower(img, iso, 6, NV - 2.6, BLUE, False)))
    items.append((NV - 1.0 + 4, lambda: tower(img, iso, 4, NV - 1.0, BLUE, True)))

    deco_spots = [(0.6, 4.2), (7.4, 4.0), (0.6, 8.0), (7.4, 8.2), (0.7, 0.8), (7.3, 11.2), (7.3, 0.8), (0.7, 11.2)]
    for (u, v) in deco_spots:
        kind = theme["deco"]
        if kind == "trees":
            fn = (lambda u=u, v=v: tree(img, iso, u, v, rng))
        elif kind == "rocks":
            fn = (lambda u=u, v=v: rock(img, iso, u, v, rng, (150, 144, 136), 1.7))
        elif kind == "ice":
            fn = (lambda u=u, v=v: (tree(img, iso, u, v, rng, snowy=True) if rng.random() < 0.5 else crystal(img, iso, u, v, rng)))
        else:
            fn = (lambda u=u, v=v: (lava_vent(img, iso, u, v, rng) if (u, v) in [(0.6, 4.2), (7.4, 8.2)] else rock(img, iso, u, v, rng, (70, 50, 46))))
        items.append((u + v, fn))

    # cerca nas bordas da frente
    def post(u, v):
        dd = ImageDraw.Draw(img)
        box(dd, iso, u, v, 0.14, 0.14, 9, theme["fence"], shade(theme["fence"], 0.7), shade(theme["fence"], 0.85))
    for v in range(0, NV + 1):
        items.append((NU + v - 0.01, lambda v=v: post(NU - 0.08, v)))
    for u in range(0, NU + 1):
        items.append((u + NV - 0.01, lambda u=u: post(u, NV - 0.08)))

    items.sort(key=lambda it: it[0])
    for _, fn in items:
        fn()

    # trilhos da cerca
    d = ImageDraw.Draw(img)
    for z in (4, 8):
        d.line([iso.p(NU - 0.08, 0, z), iso.p(NU - 0.08, NV - 0.08, z)], fill=shade(theme["fence"], 0.75), width=2 * SS)
        d.line([iso.p(0, NV - 0.08, z), iso.p(NU - 0.08, NV - 0.08, z)], fill=shade(theme["fence"], 0.75), width=2 * SS)

    out = img.resize((W, H), Image.LANCZOS)
    # contorno escuro suave em volta da ilha (deixa com cara de "cartoon")
    alpha = out.split()[3]
    glow = Image.new("RGBA", out.size, (10, 20, 35, 0))
    glow.putalpha(alpha.filter(ImageFilter.MaxFilter(5)).point(lambda a: 120 if a > 10 else 0))
    final = Image.new("RGBA", out.size, (0, 0, 0, 0))
    final.alpha_composite(glow)
    final.alpha_composite(out)
    bbox = final.getbbox()
    return final.crop(bbox)


if __name__ == "__main__":
    outdir = sys.argv[1]
    for i, th in enumerate(THEMES):
        im = island(th, seed=10 + i)
        im.save(f"{outdir}/arena{i}.png")
        print(i, th["name"], im.size)
