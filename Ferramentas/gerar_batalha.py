"""Arte original da batalha: fundos de arena, torres, personagens, projéteis."""
import math
import random
import sys
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1] if len(sys.argv) > 1 else "out3"
SS = 3
OL = (38, 28, 30)          # cor do contorno
BLUE = dict(main=(62, 124, 232), dark=(36, 74, 160), light=(130, 180, 255))
RED = dict(main=(222, 70, 62), dark=(150, 40, 40), light=(255, 140, 120))
SKIN = (255, 214, 172)


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3])


def finish(img, size):
    return img.resize(size, Image.LANCZOS)


# ============================================================ FUNDO DA ARENA
PPU = 80                       # pixels por unidade do mundo
AW, AH = 15, 22                # largura/altura em unidades (centro = 0,0)
HW = 6                         # meia largura da arena (gramado)
LANE = 3.6                     # x das pistas (pontes e torres laterais)
ARENA_THEMES = [
    dict(g1=(118, 190, 84), g2=(106, 178, 76), out=(78, 140, 58), path=(222, 192, 140), path_e=(176, 146, 100),
         water=(70, 196, 214), water2=(130, 226, 236), bank=(150, 120, 80), wood=(150, 100, 56), deco="garden"),
    dict(g1=(168, 176, 120), g2=(156, 164, 110), out=(120, 124, 96), path=(206, 192, 162), path_e=(160, 146, 120),
         water=(80, 160, 200), water2=(150, 205, 230), bank=(130, 124, 116), wood=(128, 112, 100), deco="stone"),
    dict(g1=(230, 240, 248), g2=(214, 230, 242), out=(180, 204, 226), path=(184, 212, 236), path_e=(140, 176, 210),
         water=(120, 210, 240), water2=(220, 246, 255), bank=(150, 180, 210), wood=(150, 120, 96), deco="ice"),
    dict(g1=(110, 82, 72), g2=(98, 72, 64), out=(64, 44, 40), path=(150, 116, 96), path_e=(110, 84, 70),
         water=(255, 120, 30), water2=(255, 214, 100), bank=(60, 40, 36), wood=(80, 58, 50), deco="lava"),
]


def w2p(x, y):
    """mundo -> pixel (com supersampling)"""
    return ((x + AW / 2) * PPU * SS, (AH / 2 - y) * PPU * SS)


def wrect(d, x0, y0, x1, y1, fill, outline=None, width=0):
    a, b = w2p(x0, y1), w2p(x1, y0)
    d.rectangle([a[0], a[1], b[0], b[1]], fill=fill, outline=outline, width=width * SS)


def arena_bg(th, seed):
    rng = random.Random(seed)
    W, H = AW * PPU * SS, AH * PPU * SS
    img = Image.new("RGB", (W, H), th["out"])
    d = ImageDraw.Draw(img)

    # grama de fora com manchas
    for _ in range(260):
        x, y = rng.uniform(0, W), rng.uniform(0, H)
        r = rng.uniform(20, 60) * SS
        d.ellipse([x - r, y - r * 0.6, x + r, y + r * 0.6], fill=shade(th["out"], rng.uniform(0.9, 1.08)))

    # gramado da arena (quadriculado)
    for xi in range(-HW, HW):
        for yi in range(-10, 10):
            if yi < -10 or yi >= 10:
                continue
            col = th["g1"] if (xi + yi) % 2 == 0 else th["g2"]
            wrect(d, xi, yi, xi + 1, yi + 1, col)
    # leve variação (tufos)
    for _ in range(500):
        x, y = rng.uniform(-HW, HW), rng.uniform(-9.9, 9.9)
        px, py = w2p(x, y)
        r = rng.uniform(2, 5) * SS
        d.ellipse([px - r, py - r, px + r, py + r], fill=shade(th["g1"], rng.uniform(0.92, 1.08)))

    # caminhos com borda
    def path_rect(x0, y0, x1, y1):
        wrect(d, x0 - 0.06, y0 - 0.06, x1 + 0.06, y1 + 0.06, th["path_e"])
        wrect(d, x0, y0, x1, y1, th["path"])
    for lx in (-LANE, LANE):
        path_rect(lx - 0.55, -7.7, lx + 0.55, 7.7)
    for sy in (-1, 1):
        path_rect(-LANE, sy * 7.7 - 0.55, LANE, sy * 7.7 + 0.55)
        # bases de pedra das torres
        for tx, ty, s in [(-LANE, 6.5, 0.95), (LANE, 6.5, 0.95), (0, 8.4, 1.25)]:
            wrect(d, tx - s, sy * ty - s, tx + s, sy * ty + s, shade(th["path_e"], 0.9), OL, 2)
            wrect(d, tx - s + 0.12, sy * ty - s + 0.12, tx + s - 0.12, sy * ty + s - 0.12, shade(th["path"], 0.95))
    # pedrinhas nos caminhos
    for _ in range(140):
        lx = rng.choice([-LANE, LANE])
        x, y = lx + rng.uniform(-0.5, 0.5), rng.uniform(-7.6, 7.6)
        if abs(y) < 0.9:
            continue
        px, py = w2p(x, y)
        r = rng.uniform(1.5, 3.5) * SS
        d.ellipse([px - r, py - r, px + r, py + r], fill=shade(th["path_e"], rng.uniform(0.9, 1.1)))

    # rio com margens
    wrect(d, -8, -0.62, 8, 0.62, th["bank"])
    wrect(d, -8, -0.5, 8, 0.5, th["water"])
    for i in range(30):
        x = rng.uniform(-8, 8)
        y = rng.uniform(-0.35, 0.35)
        a, b = w2p(x, y), w2p(x + rng.uniform(0.4, 1.0), y)
        d.line([a, b], fill=th["water2"], width=3 * SS)
    wrect(d, -8, 0.36, 8, 0.46, th["water2"])

    # pontes de madeira
    for lx in (-LANE, LANE):
        wrect(d, lx - 0.75, -0.85, lx + 0.75, 0.85, shade(th["wood"], 0.7), OL, 3)
        for k in range(7):
            y0 = -0.85 + k * 0.243
            wrect(d, lx - 0.7, y0 + 0.02, lx + 0.7, y0 + 0.22, shade(th["wood"], 0.95 + 0.08 * (k % 2)))
        for sx in (-1, 1):
            wrect(d, lx + sx * 0.75 - 0.08, -0.95, lx + sx * 0.75 + 0.08, 0.95, shade(th["wood"], 0.6), OL, 2)

    # cerca em volta
    for sy in (-1, 1):
        wrect(d, -(HW + 0.15), sy * 9.85 - 0.08, HW + 0.15, sy * 9.85 + 0.08, shade(th["wood"], 0.85), OL, 2)
    for sx in (-1, 1):
        wrect(d, sx * (HW + 0.1) - 0.08, -9.9, sx * (HW + 0.1) + 0.08, 9.9, shade(th["wood"], 0.85), OL, 2)
    for i in range(23):
        y = -9.9 + i * 0.9
        for sx in (-1, 1):
            wrect(d, sx * (HW + 0.1) - 0.14, y - 0.14, sx * (HW + 0.1) + 0.14, y + 0.14, th["wood"], OL, 2)
    for i in range(14):
        x = -(HW + 0.1) + i * ((2 * HW + 0.2) / 13)
        for sy in (-1, 1):
            wrect(d, x - 0.14, sy * 9.85 - 0.14, x + 0.14, sy * 9.85 + 0.14, th["wood"], OL, 2)

    # decoração do lado de fora
    def bush(x, y, r):
        px, py = w2p(x, y)
        base = (60, 140, 62) if th["deco"] != "ice" else (90, 140, 110)
        if th["deco"] == "lava":
            base = (70, 56, 50)
        for k, (dx, dy, rr) in enumerate([(0, -0.25, 1.0), (-0.55, 0.1, 0.8), (0.55, 0.1, 0.8)]):
            cx, cy = px + dx * r * PPU * SS, py + dy * r * PPU * SS
            R = rr * r * PPU * SS
            d.ellipse([cx - R, cy - R, cx + R, cy + R], fill=shade(base, 0.9 + 0.1 * k), outline=OL, width=2 * SS)
        if th["deco"] == "ice":
            d.ellipse([px - r * 0.6 * PPU * SS, py - r * 1.2 * PPU * SS, px + r * 0.6 * PPU * SS, py - r * 0.6 * PPU * SS],
                      fill=(245, 250, 255))

    def rock(x, y, r):
        px, py = w2p(x, y)
        R = r * PPU * SS
        col = (150, 144, 136) if th["deco"] != "lava" else (60, 44, 40)
        d.ellipse([px - R * 1.3, py - R, px + R * 1.3, py + R * 0.7], fill=col, outline=OL, width=2 * SS)
        d.ellipse([px - R * 0.7, py - R * 0.8, px + R * 0.1, py - R * 0.3], fill=shade(col, 1.25))

    for _ in range(34):
        side = rng.choice([-1, 1])
        x = side * rng.uniform(HW + 0.6, AW / 2 - 0.2)
        y = rng.uniform(-10.8, 10.8)
        if rng.random() < 0.7:
            bush(x, y, rng.uniform(0.35, 0.6))
        else:
            rock(x, y, rng.uniform(0.2, 0.35))
    for _ in range(10):
        x = rng.uniform(-HW, HW)
        y = rng.choice([-1, 1]) * rng.uniform(10.3, 10.9)
        bush(x, y, rng.uniform(0.3, 0.5))
    if th["deco"] == "lava":
        for _ in range(12):
            x = rng.choice([-1, 1]) * rng.uniform(HW + 0.6, AW / 2 - 0.2)
            y = rng.uniform(-10, 10)
            px, py = w2p(x, y)
            d.ellipse([px - 30 * SS, py - 14 * SS, px + 30 * SS, py + 14 * SS], fill=(255, 130, 30))
            d.ellipse([px - 18 * SS, py - 8 * SS, px + 18 * SS, py + 8 * SS], fill=(255, 220, 110))

    out = img.resize((AW * PPU, AH * PPU), Image.LANCZOS)
    # vinheta suave
    vig = Image.new("L", out.size, 0)
    vd = ImageDraw.Draw(vig)
    vd.rectangle([0, 0, out.size[0], out.size[1]], fill=0)
    vd.rectangle([60, 60, out.size[0] - 60, out.size[1] - 60], fill=255)
    vig = vig.filter(ImageFilter.GaussianBlur(70))
    dark = Image.new("RGB", out.size, (20, 30, 20))
    return Image.composite(out, dark, vig)


# ============================================================ TORRES
def tower(team, king):
    S = 3
    W, H = (300, 460) if king else (220, 440)
    img = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    s = S
    stone, stone_d, stone_l = (176, 170, 162), (126, 120, 114), (206, 200, 192)
    cx = W / 2
    base_y = H - 30
    bw = (W - 40) / 2 if king else 70
    top_y = 220 if king else 230
    # sombra
    d.ellipse([(cx - bw - 10) * s, (base_y - 18) * s, (cx + bw + 10) * s, (base_y + 22) * s], fill=(0, 0, 0, 70))
    # corpo
    d.rounded_rectangle([(cx - bw) * s, top_y * s, (cx + bw) * s, base_y * s], radius=10 * s, fill=stone, outline=OL, width=4 * s)
    # tijolos
    for row in range(int((base_y - top_y) / 22)):
        y = top_y + 14 + row * 22
        d.line([((cx - bw + 6) * s, y * s), ((cx + bw - 6) * s, y * s)], fill=stone_d, width=2 * s)
        off = 0 if row % 2 else 18
        for x in range(int(cx - bw + 16 + off), int(cx + bw - 8), 36):
            d.line([(x * s, y * s), (x * s, (y + 22) * s)], fill=stone_d, width=2 * s)
    # lado direito mais escuro (volume)
    shade_layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shade_layer)
    sd.rounded_rectangle([(cx + bw * 0.4) * s, top_y * s, (cx + bw) * s, base_y * s], radius=10 * s, fill=(0, 0, 0, 45))
    img.alpha_composite(shade_layer)
    d = ImageDraw.Draw(img)
    # plataforma do topo
    ty = top_y - 26
    d.rounded_rectangle([(cx - bw - 12) * s, ty * s, (cx + bw + 12) * s, (top_y + 12) * s], radius=8 * s, fill=stone_l, outline=OL, width=4 * s)
    # ameias
    n = 6 if king else 4
    mw = (2 * bw + 24) / (n * 2 - 1)
    for i in range(n):
        x0 = cx - bw - 12 + i * mw * 2
        d.rounded_rectangle([x0 * s, (ty - 24) * s, (x0 + mw) * s, (ty + 4) * s], radius=4 * s, fill=stone_l, outline=OL, width=3 * s)
    # faixa / estandarte do time
    fx = cx
    d.polygon([((fx - 30) * s, (top_y + 18) * s), ((fx + 30) * s, (top_y + 18) * s), ((fx + 30) * s, (top_y + 110) * s),
               (fx * s, (top_y + 92) * s), ((fx - 30) * s, (top_y + 110) * s)], fill=team["main"], outline=OL)
    d.line([((fx - 30) * s, (top_y + 18) * s), ((fx - 30) * s, (top_y + 110) * s), (fx * s, (top_y + 92) * s),
            ((fx + 30) * s, (top_y + 110) * s), ((fx + 30) * s, (top_y + 18) * s)], fill=OL, width=3 * s)
    d.rectangle([(fx - 34) * s, (top_y + 12) * s, (fx + 34) * s, (top_y + 22) * s], fill=(120, 80, 40), outline=OL, width=2 * s)
    # emblema
    if king:
        # coroa
        pts = [(fx - 18, top_y + 66), (fx - 18, top_y + 40), (fx - 8, top_y + 54), (fx, top_y + 34), (fx + 8, top_y + 54),
               (fx + 18, top_y + 40), (fx + 18, top_y + 66)]
        d.polygon([(x * s, y * s) for x, y in pts], fill=(255, 210, 60), outline=OL)
    else:
        d.ellipse([(fx - 12) * s, (top_y + 42) * s, (fx + 12) * s, (top_y + 66) * s], fill=team["light"], outline=OL, width=2 * s)
    # porta
    d.rounded_rectangle([(cx - 22) * s, (base_y - 56) * s, (cx + 22) * s, base_y * s], radius=20 * s, fill=(90, 60, 40), outline=OL, width=3 * s)
    # topo: telhado (princesa) ou canhão (rei)
    if king:
        d.rounded_rectangle([(cx - 26) * s, (ty - 70) * s, (cx + 26) * s, (ty - 10) * s], radius=10 * s, fill=(70, 70, 80), outline=OL, width=4 * s)
        d.rounded_rectangle([(cx - 16) * s, (ty - 100) * s, (cx + 16) * s, (ty - 50) * s], radius=8 * s, fill=(50, 50, 60), outline=OL, width=4 * s)
        d.ellipse([(cx - 10) * s, (ty - 104) * s, (cx + 10) * s, (ty - 92) * s], fill=(20, 20, 24))
        d.ellipse([(cx - 22) * s, (ty - 64) * s, (cx - 6) * s, (ty - 48) * s], fill=team["main"], outline=OL, width=2 * s)
    else:
        roof_y = ty - 24
        d.polygon([((cx - bw + 4) * s, roof_y * s), ((cx + bw - 4) * s, roof_y * s), (cx * s, (roof_y - 90) * s)],
                  fill=team["main"], outline=OL)
        d.polygon([(cx * s, roof_y * s), ((cx + bw - 4) * s, roof_y * s), (cx * s, (roof_y - 90) * s)], fill=team["dark"])
        d.line([((cx - bw + 4) * s, roof_y * s), (cx * s, (roof_y - 90) * s), ((cx + bw - 4) * s, roof_y * s)], fill=OL, width=4 * s)
        d.line([((cx - bw + 4) * s, roof_y * s), ((cx + bw - 4) * s, roof_y * s)], fill=OL, width=4 * s)
        # bandeirinha
        d.line([(cx * s, (roof_y - 90) * s), (cx * s, (roof_y - 124) * s)], fill=OL, width=4 * s)
        d.polygon([(cx * s, (roof_y - 124) * s), ((cx + 30) * s, (roof_y - 114) * s), (cx * s, (roof_y - 104) * s)],
                  fill=team["light"], outline=OL)
    out = img.resize((W, H), Image.LANCZOS)
    return out.crop(out.getbbox())


def ruin():
    S = 3
    W, H = 220, 140
    img = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    rng = random.Random(4)
    d.ellipse([20 * S, 70 * S, 200 * S, 135 * S], fill=(0, 0, 0, 60))
    for _ in range(16):
        x, y = rng.uniform(40, 180), rng.uniform(60, 115)
        r = rng.uniform(10, 24)
        col = shade((170, 164, 156), rng.uniform(0.75, 1.1))
        d.rounded_rectangle([(x - r) * S, (y - r * 0.7) * S, (x + r) * S, (y + r * 0.7) * S], radius=6 * S, fill=col, outline=OL, width=3 * S)
    out = img.resize((W, H), Image.LANCZOS)
    return out.crop(out.getbbox())


# ============================================================ PERSONAGENS
def new_canvas():
    return Image.new("RGBA", (256 * SS, 256 * SS), (0, 0, 0, 0))


def P(*pts):
    return [(x * SS, y * SS) for x, y in pts]


def E(d, x0, y0, x1, y1, fill, w=4):
    d.ellipse([x0 * SS, y0 * SS, x1 * SS, y1 * SS], fill=fill, outline=OL, width=w * SS)


def R(d, x0, y0, x1, y1, fill, r=10, w=4):
    d.rounded_rectangle([x0 * SS, y0 * SS, x1 * SS, y1 * SS], radius=r * SS, fill=fill, outline=OL, width=w * SS)


def humanoid(d, team, tunic=None, head_y=96, head_r=50):
    tunic = tunic or team["main"]
    # pernas
    R(d, 104, 196, 124, 238, (70, 56, 50), 6)
    R(d, 132, 196, 152, 238, (70, 56, 50), 6)
    # corpo
    R(d, 88, 140, 168, 212, tunic, 18)
    R(d, 88, 186, 168, 198, (110, 74, 44), 4, 3)
    # braços
    E(d, 72, 150, 98, 192, SKIN)
    E(d, 158, 150, 184, 192, SKIN)
    # cabeça
    E(d, 128 - head_r, head_y - head_r, 128 + head_r, head_y + head_r, SKIN)
    # olhos
    for ex in (110, 146):
        E(d, ex - 7, head_y - 4, ex + 7, head_y + 14, (30, 24, 24), 0)
        E(d, ex - 3, head_y - 1, ex + 2, head_y + 4, (255, 255, 255), 0)
    # bochechas
    E(d, 96, head_y + 16, 108, head_y + 24, (255, 170, 150), 0)
    E(d, 148, head_y + 16, 160, head_y + 24, (255, 170, 150), 0)


def char_guerreiro(team):
    img = new_canvas(); d = ImageDraw.Draw(img)
    # espada atrás
    d.polygon(P((186, 60), (198, 66), (196, 170), (184, 170)), fill=(220, 226, 236), outline=OL)
    d.line(P((186, 60), (184, 170), (196, 170), (198, 66), (186, 60)), fill=OL, width=4 * SS)
    R(d, 172, 166, 210, 176, (150, 110, 50), 4)
    humanoid(d, team)
    # capacete
    d.chord([74 * SS, 40 * SS, 182 * SS, 150 * SS], 180, 360, fill=(196, 202, 212), outline=OL, width=4 * SS)
    R(d, 74, 88, 182, 100, (160, 166, 178), 4)
    # pluma
    E(d, 116, 18, 168, 52, team["main"])
    return img


def char_arqueira(team):
    img = new_canvas(); d = ImageDraw.Draw(img)
    # aljava
    R(d, 64, 120, 90, 190, (130, 86, 50), 6)
    for x in (70, 78, 86):
        d.line(P((x, 120), (x - 2, 104)), fill=(230, 230, 230), width=3 * SS)
    humanoid(d, team)
    # cabelo
    d.chord([76 * SS, 42 * SS, 180 * SS, 140 * SS], 180, 360, fill=(240, 140, 170), outline=OL, width=4 * SS)
    d.polygon(P((78, 92), (66, 160), (92, 150)), fill=(240, 140, 170), outline=OL)
    d.polygon(P((178, 92), (190, 160), (164, 150)), fill=(240, 140, 170), outline=OL)
    E(d, 112, 36, 144, 60, team["main"])
    # arco
    d.arc([168 * SS, 110 * SS, 222 * SS, 230 * SS], 270, 90, fill=(140, 90, 40), width=7 * SS)
    d.line(P((195, 112), (195, 228)), fill=(240, 240, 240), width=2 * SS)
    return img


def char_mago(team):
    img = new_canvas(); d = ImageDraw.Draw(img)
    # cajado
    R(d, 182, 70, 192, 236, (120, 80, 40), 4)
    E(d, 172, 44, 202, 74, (190, 140, 255))
    E(d, 180, 50, 190, 60, (240, 220, 255), 0)
    humanoid(d, team, tunic=team["dark"])
    # barba
    d.polygon(P((92, 112), (164, 112), (146, 176), (128, 190), (110, 176)), fill=(240, 240, 240), outline=OL)
    d.line(P((92, 112), (110, 176), (128, 190), (146, 176), (164, 112)), fill=OL, width=4 * SS)
    # chapéu pontudo
    d.polygon(P((62, 74), (194, 74), (150, 4), (128, 0)), fill=team["main"], outline=OL)
    d.line(P((62, 74), (128, 0), (150, 4), (194, 74), (62, 74)), fill=OL, width=4 * SS)
    R(d, 58, 66, 198, 84, team["dark"], 8)
    E(d, 132, 30, 146, 44, (255, 220, 80), 2)
    return img


def char_escudeiro(team):
    img = new_canvas(); d = ImageDraw.Draw(img)
    humanoid(d, team)
    d.chord([76 * SS, 40 * SS, 180 * SS, 146 * SS], 180, 360, fill=(170, 176, 186), outline=OL, width=4 * SS)
    R(d, 120, 30, 136, 60, team["main"], 4)
    # escudo grande na frente
    d.polygon(P((132, 118), (214, 118), (210, 190), (173, 230), (136, 190)), fill=(160, 110, 60), outline=OL)
    d.line(P((132, 118), (214, 118), (210, 190), (173, 230), (136, 190), (132, 118)), fill=OL, width=5 * SS)
    d.polygon(P((150, 132), (196, 132), (194, 182), (173, 206), (152, 182)), fill=team["main"])
    E(d, 162, 148, 184, 170, (255, 214, 80), 3)
    return img


def char_lanceiro(team):
    img = new_canvas(); d = ImageDraw.Draw(img)
    # lança
    R(d, 186, 30, 194, 236, (140, 96, 50), 3, 3)
    d.polygon(P((178, 40), (190, 4), (202, 40)), fill=(220, 226, 236), outline=OL)
    humanoid(d, team)
    # gorro
    d.chord([78 * SS, 44 * SS, 178 * SS, 140 * SS], 180, 360, fill=team["main"], outline=OL, width=4 * SS)
    E(d, 120, 34, 136, 50, team["light"], 3)
    return img


def char_rato(team):
    img = new_canvas(); d = ImageDraw.Draw(img)
    # rabo
    d.arc([150 * SS, 150 * SS, 250 * SS, 240 * SS], 180, 330, fill=(240, 160, 170), width=8 * SS)
    # corpo
    E(d, 64, 132, 196, 236, (150, 130, 120))
    E(d, 96, 170, 164, 232, (200, 186, 176), 0)
    # orelhas
    E(d, 56, 50, 106, 100, (150, 130, 120)); E(d, 66, 60, 96, 90, (250, 170, 180), 0)
    E(d, 150, 50, 200, 100, (150, 130, 120)); E(d, 160, 60, 190, 90, (250, 170, 180), 0)
    # cabeça
    E(d, 76, 64, 180, 164, (160, 140, 130))
    for ex in (108, 148):
        E(d, ex - 8, 100, ex + 8, 120, (30, 24, 24), 0)
        E(d, ex - 3, 103, ex + 2, 108, (255, 255, 255), 0)
    E(d, 120, 126, 136, 140, (240, 120, 140), 3)
    # bandana do time
    d.chord([76 * SS, 60 * SS, 180 * SS, 150 * SS], 200, 340, fill=team["main"], outline=OL, width=4 * SS)
    return img


def char_golem(team):
    img = new_canvas(); d = ImageDraw.Draw(img)
    rock = (140, 136, 128)
    # pernas
    R(d, 70, 196, 116, 242, shade(rock, 0.85), 12)
    R(d, 140, 196, 186, 242, shade(rock, 0.85), 12)
    # braços enormes
    E(d, 14, 96, 84, 206, shade(rock, 0.9))
    E(d, 172, 96, 242, 206, shade(rock, 0.9))
    # corpo
    R(d, 54, 52, 202, 214, rock, 40)
    # musgo e rachaduras
    E(d, 70, 52, 140, 80, (100, 160, 80), 3)
    d.line(P((100, 120), (114, 150), (104, 180)), fill=OL, width=3 * SS)
    d.line(P((166, 150), (154, 176)), fill=OL, width=3 * SS)
    # olhos brilhando na cor do time
    for ex in (104, 152):
        E(d, ex - 13, 92, ex + 13, 112, team["light"], 3)
        E(d, ex - 5, 97, ex + 5, 107, (255, 255, 255), 0)
    return img


def fireball_icon():
    img = new_canvas(); d = ImageDraw.Draw(img)
    for i, (r, col) in enumerate([(100, (255, 90, 20)), (78, (255, 150, 30)), (54, (255, 210, 80)), (28, (255, 250, 200))]):
        cx, cy = 140 - i * 4, 130 + i * 4
        d.ellipse([(cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS], fill=col)
    # rastro
    for k in range(5):
        x = 60 - k * 10
        y = 210 + k * 8
        r = 18 - k * 3
        d.ellipse([(x - r) * SS, (y - r) * SS, (x + r) * SS, (y + r) * SS], fill=(255, 130, 30, 200))
    return img


def arrow():
    W, H = 96, 20
    img = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle([10 * SS, 8 * SS, 78 * SS, 12 * SS], fill=(150, 100, 50))
    d.polygon(P((76, 2), (96, 10), (76, 18)), fill=(220, 226, 236), outline=OL)
    d.polygon(P((0, 2), (16, 8), (16, 12), (0, 18), (8, 10)), fill=(240, 240, 240))
    return img.resize((W, H), Image.LANCZOS)


def orb(col):
    img = new_canvas(); d = ImageDraw.Draw(img)
    for r, k in [(110, 0.35), (80, 0.7), (54, 1.0), (26, 1.3)]:
        c = shade(col, k) + (int(255 * min(1, k)),)
        d.ellipse([(128 - r) * SS, (128 - r) * SS, (128 + r) * SS, (128 + r) * SS], fill=c)
    return img.resize((64, 64), Image.LANCZOS)


def save_char(img, path):
    out = img.resize((256, 256), Image.LANCZOS)
    out.crop(out.getbbox()).save(path)


if __name__ == "__main__":
    import os
    os.makedirs(f"{OUT}/Batalha", exist_ok=True)
    os.makedirs(f"{OUT}/Tropas", exist_ok=True)
    for i, th in enumerate(ARENA_THEMES):
        arena_bg(th, 100 + i).save(f"{OUT}/Batalha/arena{i}.png")
    for name, team in (("azul", BLUE), ("vermelho", RED)):
        tower(team, False).save(f"{OUT}/Batalha/torre_{name}.png")
        tower(team, True).save(f"{OUT}/Batalha/rei_{name}.png")
        for key, fn in [("gue", char_guerreiro), ("arq", char_arqueira), ("mag", char_mago), ("esc", char_escudeiro),
                        ("lan", char_lanceiro), ("rat", char_rato), ("gol", char_golem)]:
            save_char(fn(team), f"{OUT}/Tropas/{key}_{name}.png")
    save_char(fireball_icon(), f"{OUT}/Tropas/fog.png")
    ruin().save(f"{OUT}/Batalha/ruina.png")
    arrow().save(f"{OUT}/Batalha/flecha.png")
    orb((255, 140, 40)).save(f"{OUT}/Batalha/bola_fogo.png")
    orb((190, 130, 255)).save(f"{OUT}/Batalha/magia.png")
    print("ok")
