"""Gera as plataformas do Caminho de Troféus (4 temas) e os ícones de recompensa."""
import math
import random
import sys
from PIL import Image, ImageDraw, ImageFilter

SS = 3
THEMES = [
    dict(top=(98, 172, 74), top2=(86, 156, 64), path=(214, 182, 128), side=(130, 90, 56), rock=(96, 70, 50), deco="bush"),
    dict(top=(150, 158, 104), top2=(138, 146, 94), path=(196, 180, 150), side=(120, 116, 110), rock=(84, 80, 78), deco="stone"),
    dict(top=(226, 238, 246), top2=(208, 226, 240), path=(176, 206, 230), side=(110, 154, 200), rock=(70, 110, 160), deco="ice"),
    dict(top=(96, 70, 62), top2=(84, 60, 54), path=(140, 108, 90), side=(66, 46, 42), rock=(40, 28, 28), deco="lava"),
]


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3])


def outline_glow(im, px=5, alpha=110):
    a = im.split()[3]
    g = Image.new("RGBA", im.size, (10, 20, 35, 0))
    g.putalpha(a.filter(ImageFilter.MaxFilter(px)).point(lambda v: alpha if v > 10 else 0))
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    out.alpha_composite(g)
    out.alpha_composite(im)
    return out


def platform(th, seed):
    rng = random.Random(seed)
    W, H = 420, 190
    img = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    s = SS
    # tampo em perspectiva (trapézio)
    x0, x1, x2, x3 = 30, 390, 370, 50
    ytop, ybot = 30, 112
    top = [(x3 * s, ytop * s), (x2 * s, ytop * s), (x1 * s, ybot * s), (x0 * s, ybot * s)]
    thick = 30
    # parte de baixo com rochas
    under = [(x0 * s, (ybot + thick) * s)]
    for i in range(1, 12):
        f = i / 12
        x = x0 + (x1 - x0) * f
        y = ybot + thick + math.sin(f * math.pi) * 34 + rng.uniform(-6, 6)
        under.append((x * s, y * s))
    under.append((x1 * s, (ybot + thick) * s))
    d.polygon(under, fill=th["rock"])
    for _ in range(6):
        x = rng.uniform(x0 + 30, x1 - 30)
        y = ybot + thick + math.sin((x - x0) / (x1 - x0) * math.pi) * 30
        d.polygon([((x - 10) * s, y * s), ((x + 10) * s, y * s), ((x + rng.uniform(-3, 3)) * s, (y + rng.uniform(14, 30)) * s)],
                  fill=shade(th["rock"], rng.uniform(0.8, 1.15)))
    # frente de terra
    d.polygon([(x0 * s, ybot * s), (x1 * s, ybot * s), (x1 * s, (ybot + thick) * s), (x0 * s, (ybot + thick) * s)],
              fill=th["side"])
    d.line([(x0 * s, (ybot + 14) * s), (x1 * s, (ybot + 14) * s)], fill=shade(th["side"], 0.8), width=2 * s)
    d.polygon([(x0 * s, ybot * s), (x1 * s, ybot * s), (x1 * s, (ybot + 6) * s), (x0 * s, (ybot + 6) * s)],
              fill=shade(th["top"], 0.75))
    # tampo quadriculado
    d.polygon(top, fill=th["top"])
    cols, rows = 10, 3
    for r in range(rows):
        for c in range(cols):
            if (r + c) % 2:
                continue
            fy0, fy1 = r / rows, (r + 1) / rows
            def edge(fy):
                y = ytop + (ybot - ytop) * fy
                xl = x3 + (x0 - x3) * fy
                xr = x2 + (x1 - x2) * fy
                return y, xl, xr
            y0, l0, r0 = edge(fy0)
            y1, l1, r1 = edge(fy1)
            fx0, fx1 = c / cols, (c + 1) / cols
            pts = [(l0 + (r0 - l0) * fx0, y0), (l0 + (r0 - l0) * fx1, y0), (l1 + (r1 - l1) * fx1, y1), (l1 + (r1 - l1) * fx0, y1)]
            d.polygon([(px * s, py * s) for px, py in pts], fill=th["top2"])
    # caminho no meio
    cx = (x0 + x1) / 2
    d.polygon([((cx - 18) * s, ytop * s), ((cx + 18) * s, ytop * s), ((cx + 24) * s, ybot * s), ((cx - 24) * s, ybot * s)],
              fill=th["path"])
    d.rectangle([(cx - 24) * s, ybot * s, (cx + 24) * s, (ybot + thick) * s], fill=shade(th["path"], 0.75))
    # decoração do tema
    for side in (-1, 1):
        bx = cx + side * rng.uniform(95, 140)
        by = rng.uniform(55, 90)
        if th["deco"] == "bush":
            for k, (dx, dy, r) in enumerate([(0, -8, 14), (-12, 0, 10), (12, 0, 10)]):
                col = shade((60, 140, 64), 0.85 + 0.1 * k)
                d.ellipse([(bx + dx - r) * s, (by + dy - r) * s, (bx + dx + r) * s, (by + dy + r) * s], fill=col)
        elif th["deco"] == "stone":
            for dx, r in [(0, 13), (16, 8)]:
                d.ellipse([(bx + dx - r * 1.3) * s, (by - r) * s, (bx + dx + r * 1.3) * s, (by + r * 0.6) * s],
                          fill=(150, 144, 136), outline=(100, 96, 92), width=s)
        elif th["deco"] == "ice":
            for dx, hgt in [(-6, 22), (0, 32), (7, 18)]:
                d.polygon([((bx + dx - 5) * s, by * s), ((bx + dx + 5) * s, by * s), ((bx + dx) * s, (by - hgt) * s)],
                          fill=(170, 225, 250), outline=(90, 160, 210))
        else:
            d.ellipse([(bx - 20) * s, (by - 6) * s, (bx + 20) * s, (by + 8) * s], fill=(255, 130, 30))
            d.ellipse([(bx - 12) * s, (by - 3) * s, (bx + 12) * s, (by + 4) * s], fill=(255, 220, 110))
    out = img.resize((W, H), Image.LANCZOS)
    out = outline_glow(out)
    return out.crop(out.getbbox())


def coins():
    S = 128
    img = Image.new("RGBA", (S * SS, S * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    s = SS
    stacks = [(40, 96, 5), (70, 102, 7), (92, 92, 4)]
    for x, y, n in stacks:
        for i in range(n):
            yy = y - i * 9
            d.ellipse([(x - 22) * s, (yy - 9) * s, (x + 22) * s, (yy + 9) * s], fill=(200, 140, 20), outline=(130, 85, 10), width=2 * s)
            d.ellipse([(x - 22) * s, (yy - 13) * s, (x + 22) * s, (yy + 5) * s], fill=(255, 205, 60), outline=(150, 100, 15), width=2 * s)
            d.ellipse([(x - 12) * s, (yy - 8) * s, (x + 12) * s, (yy) * s], outline=(220, 160, 30), width=2 * s)
    return outline_glow(img.resize((S, S), Image.LANCZOS), 5)


def gem():
    S = 128
    img = Image.new("RGBA", (S * SS, S * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    s = SS
    c = (64, 64)
    top = [(28, 46), (48, 24), (80, 24), (100, 46)]
    d.polygon([(x * s, y * s) for x, y in top + [(64, 112)]], fill=(40, 170, 90))
    d.polygon([(x * s, y * s) for x, y in [(28, 46), (100, 46), (64, 112)]], fill=(60, 205, 110))
    d.polygon([(x * s, y * s) for x, y in [(48, 24), (80, 24), (72, 46), (56, 46)]], fill=(170, 250, 190))
    d.polygon([(x * s, y * s) for x, y in [(28, 46), (48, 24), (56, 46)]], fill=(110, 230, 150))
    d.polygon([(x * s, y * s) for x, y in [(80, 24), (100, 46), (72, 46)]], fill=(80, 200, 120))
    d.polygon([(x * s, y * s) for x, y in [(56, 46), (72, 46), (64, 112)]], fill=(120, 235, 160))
    d.line([(x * s, y * s) for x, y in top + [(64, 112), (28, 46)]], fill=(20, 100, 50), width=3 * s, joint="curve")
    return outline_glow(img.resize((S, S), Image.LANCZOS), 5)


def chest(body, band):
    S = 128
    img = Image.new("RGBA", (S * SS, S * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    s = SS
    dark = shade(body, 0.6)
    # corpo
    d.rounded_rectangle([18 * s, 56 * s, 110 * s, 112 * s], radius=6 * s, fill=body, outline=dark, width=3 * s)
    # tampa
    d.rounded_rectangle([14 * s, 26 * s, 114 * s, 62 * s], radius=16 * s, fill=shade(body, 1.15), outline=dark, width=3 * s)
    # faixas
    for x in (30, 90):
        d.rectangle([(x - 6) * s, 26 * s, (x + 6) * s, 112 * s], fill=band, outline=shade(band, 0.6), width=2 * s)
    d.rectangle([14 * s, 56 * s, 114 * s, 64 * s], fill=band, outline=shade(band, 0.6), width=2 * s)
    # fechadura
    d.rounded_rectangle([54 * s, 52 * s, 74 * s, 76 * s], radius=4 * s, fill=(255, 215, 80), outline=(140, 100, 20), width=2 * s)
    d.ellipse([60 * s, 58 * s, 68 * s, 66 * s], fill=(80, 50, 20))
    return outline_glow(img.resize((S, S), Image.LANCZOS), 5)


if __name__ == "__main__":
    out = sys.argv[1]
    for i, th in enumerate(THEMES):
        p = platform(th, 50 + i)
        p.save(f"{out}/trilha{i}.png")
    coins().save(f"{out}/moedas.png")
    gem().save(f"{out}/gemas.png")
    chest((150, 100, 60), (110, 110, 120)).save(f"{out}/bau0.png")
    chest((170, 180, 195), (90, 110, 140)).save(f"{out}/bau1.png")
    chest((240, 190, 60), (170, 60, 60)).save(f"{out}/bau2.png")
    print("ok")
