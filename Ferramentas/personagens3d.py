"""Personagens originais montados com primitivas 3D, com poses de animação."""
import math
import numpy as np
from render3d import Prim, Scene, render, rot_x, rot_z, rot_y

TEAMS = {
    "azul": dict(team=(0.22, 0.47, 0.95), team_d=(0.12, 0.26, 0.62), team_l=(0.55, 0.75, 1.0)),
    "vermelho": dict(team=(0.90, 0.24, 0.22), team_d=(0.58, 0.12, 0.12), team_l=(1.0, 0.6, 0.5)),
}

BASE_MATS = {
    "skin": (1.0, 0.80, 0.64, 0.10),
    "dark": (0.12, 0.09, 0.09, 0.4),
    "white": (0.97, 0.97, 0.97, 0.5),
    "metal": (0.74, 0.78, 0.84, 0.9),
    "gold": (1.0, 0.78, 0.25, 0.9),
    "wood": (0.55, 0.36, 0.20, 0.1),
    "leather": (0.42, 0.27, 0.16, 0.1),
    "boot": (0.30, 0.22, 0.18, 0.1),
    "pink": (0.98, 0.55, 0.70, 0.2),
    "beard": (0.95, 0.95, 0.95, 0.1),
    "orange_hair": (0.92, 0.45, 0.15, 0.2),
    "brown_hair": (0.45, 0.28, 0.15, 0.2),
    "black_hair": (0.15, 0.12, 0.14, 0.3),
    "rock": (0.56, 0.54, 0.50, 0.05),
    "moss": (0.36, 0.60, 0.30, 0.05),
    "fur": (0.55, 0.48, 0.44, 0.05),
    "fur_l": (0.82, 0.76, 0.70, 0.05),
    "ice": (0.70, 0.90, 1.0, 0.9),
    "fire": (1.0, 0.55, 0.12, 0.3),
    "purple": (0.55, 0.32, 0.85, 0.6),
    "green": (0.30, 0.70, 0.35, 0.2),
    "glow": (1.0, 0.95, 0.6, 0.2),
}


def mats_for(team):
    m = dict(BASE_MATS)
    t = TEAMS[team]
    m["team"] = t["team"] + (0.25,)
    m["team_d"] = t["team_d"] + (0.25,)
    m["team_l"] = t["team_l"] + (0.4,)
    return m


def V(*a):
    return np.array(a, dtype=np.float32)


class Pose:
    """Ângulos de animação (radianos)."""
    def __init__(self, leg=0.0, arm=0.0, atk=0.0, bob=0.0, lean=0.0):
        self.leg, self.arm, self.atk, self.bob, self.lean = leg, arm, atk, bob, lean


def limb(origin, length, ang_x, ang_z=0.0):
    """Ponta de um membro que pende para baixo, girado (x: frente/trás, z: abrir)."""
    v = rot_z(ang_z) @ (rot_x(ang_x) @ V(0, -length, 0))
    return origin + v


def humanoid(pose, scale=1.0, tunic="team", skin="skin", legs="boot", head_r=0.36, body_w=0.28, arms=True):
    s = scale
    P = []
    y0 = pose.bob
    hip_y = 0.50 * s + y0
    # pernas
    for side, sgn in ((-1, 1), (1, -1)):
        hip = V(side * 0.12 * s, hip_y, 0)
        foot = limb(hip, 0.40 * s, pose.leg * sgn)
        P.append(Prim("capsule", legs, a=hip, b=foot, r=0.085 * s))
        P.append(Prim("ellipsoid", "boot", c=foot + V(0, -0.02 * s, -0.05 * s), r=V(0.11, 0.08, 0.15) * s))
    # tronco
    P.append(Prim("ellipsoid", tunic, k=0.05 * s, c=V(0, hip_y + 0.28 * s, 0), r=V(body_w, 0.32, 0.22) * s))
    P.append(Prim("cyl", "leather", c=V(0, hip_y + 0.08 * s, 0), r=body_w * 0.98 * s, h=0.045 * s, rr=0.02 * s))
    P.append(Prim("box", "gold", c=V(0, hip_y + 0.08 * s, -0.21 * s), h=V(0.06, 0.05, 0.03) * s, r=0.01 * s))
    # cabeça
    head_c = V(0, hip_y + 0.86 * s, 0)
    P.append(Prim("sphere", skin, c=head_c, r=head_r * s))
    # olhos e nariz (olhando para -z)
    for side in (-1, 1):
        P.append(Prim("ellipsoid", "dark", c=head_c + V(side * 0.13, 0.02, -0.31) * s, r=V(0.045, 0.07, 0.03) * s))
        P.append(Prim("sphere", "white", c=head_c + V(side * 0.12 + 0.015, 0.05, -0.335) * s, r=0.015 * s))
    P.append(Prim("sphere", skin, c=head_c + V(0, -0.06, -0.35) * s, r=0.05 * s))
    hands = []
    if arms:
        for side, sgn in ((-1, -1), (1, 1)):
            sh = V(side * (body_w + 0.04) * s, hip_y + 0.48 * s, 0)
            ang = pose.arm * sgn
            if side == 1:
                ang += pose.atk
            hand = limb(sh, 0.36 * s, ang, side * 0.25)
            P.append(Prim("capsule", tunic, a=sh, b=sh + (hand - sh) * 0.5, r=0.08 * s))
            P.append(Prim("capsule", skin, a=sh + (hand - sh) * 0.5, b=hand, r=0.07 * s))
            P.append(Prim("sphere", skin, c=hand, r=0.085 * s))
            hands.append(hand)
    return P, head_c, hands


def tool_along(hand, ang_x, length, back=0.1):
    """Arma presa na mão, apontando para frente/cima conforme o ângulo do braço."""
    dirv = rot_x(ang_x) @ V(0, 0.15, -1.0)
    dirv = dirv / np.linalg.norm(dirv)
    return hand - dirv * back, hand + dirv * length


# ------------------------------------------------------------------ personagens
# Cada função recebe (pose, team) e devolve lista de primitivas.

def guerreiro(pose):
    P, head, (lh, rh) = humanoid(pose)
    P.append(Prim("sphere", "metal", c=head + V(0, 0.14, 0.08), r=0.36))
    P.append(Prim("ellipsoid", "team", c=head + V(0, 0.42, 0.05), r=V(0.06, 0.16, 0.2)))       # pluma
    a, b = tool_along(rh, -1.2 + pose.atk * 1.4, 0.75)
    P.append(Prim("capsule", "metal", a=a, b=b, r=0.045))
    P.append(Prim("capsule", "gold", a=rh + V(-0.12, 0, 0), b=rh + V(0.12, 0, 0), r=0.035))
    P.append(Prim("ellipsoid", "team_d", c=lh + V(-0.06, 0.05, -0.06), r=V(0.05, 0.2, 0.18)))  # escudinho
    return P


def arqueira(pose):
    P, head, (lh, rh) = humanoid(pose, legs="team_d", body_w=0.25)
    P.append(Prim("sphere", "pink", c=head + V(0, 0.12, 0.09), r=0.36))
    P.append(Prim("capsule", "pink", a=head + V(0, 0.0, 0.25), b=head + V(0, -0.6, 0.35), r=0.12))  # rabo de cavalo
    P.append(Prim("capsule", "leather", a=V(0.18, 0.6, 0.25), b=V(-0.15, 1.2, 0.3), r=0.08))      # aljava
    # arco na mão esquerda
    c = lh + V(0, 0.1, -0.12)
    P.append(Prim("capsule", "wood", a=c + V(0, -0.45, 0.05), b=c + V(0, 0.45, 0.05), r=0.03))
    P.append(Prim("capsule", "wood", a=c + V(0, 0.45, 0.05), b=c + V(0, 0.2, -0.12), r=0.03))
    P.append(Prim("capsule", "wood", a=c + V(0, -0.45, 0.05), b=c + V(0, -0.2, -0.12), r=0.03))
    return P


def ratinho(pose):
    P = []
    y = pose.bob
    P.append(Prim("ellipsoid", "fur", c=V(0, 0.32 + y, 0.05), r=V(0.30, 0.28, 0.36)))
    P.append(Prim("ellipsoid", "fur_l", c=V(0, 0.28 + y, -0.18), r=V(0.2, 0.2, 0.16)))
    head = V(0, 0.62 + y, -0.22)
    P.append(Prim("sphere", "fur", c=head, r=0.27, k=0.08))
    P.append(Prim("ellipsoid", "fur", c=head + V(0, -0.06, -0.24), r=V(0.12, 0.1, 0.14), k=0.05))
    P.append(Prim("sphere", "pink", c=head + V(0, -0.05, -0.38), r=0.05))
    for side in (-1, 1):
        P.append(Prim("ellipsoid", "fur", c=head + V(side * 0.2, 0.22, 0.04), r=V(0.15, 0.15, 0.04)))
        P.append(Prim("ellipsoid", "pink", c=head + V(side * 0.2, 0.22, 0.0), r=V(0.10, 0.10, 0.03)))
        P.append(Prim("ellipsoid", "dark", c=head + V(side * 0.1, 0.05, -0.22), r=V(0.045, 0.06, 0.03)))
        foot = V(side * 0.15, 0.05, -0.1 + side * pose.leg * 0.25)
        P.append(Prim("ellipsoid", "pink", c=foot, r=V(0.07, 0.05, 0.1)))
    P.append(Prim("cyl", "team", c=head + V(0, 0.08, 0), r=0.27, h=0.05, rr=0.02))   # bandana
    P.append(Prim("capsule", "pink", a=V(0, 0.2 + y, 0.4), b=V(0.15, 0.4 + y, 0.75), r=0.03))   # rabo
    return P


def escudeiro(pose):
    P, head, (lh, rh) = humanoid(pose, body_w=0.30)
    P.append(Prim("sphere", "metal", c=head + V(0, 0.14, 0.08), r=0.36))
    P.append(Prim("box", "metal", c=head + V(0, 0.1, -0.33), h=V(0.03, 0.12, 0.03), r=0.01))
    # escudo grande na frente
    c = lh + V(0.12, 0.1, -0.2)
    P.append(Prim("cyl", "wood", c=c, r=0.36, h=0.04, rr=0.02, R=rot_x(math.pi / 2)))
    P.append(Prim("cyl", "team", c=c + V(0, 0, -0.04), r=0.27, h=0.02, rr=0.01, R=rot_x(math.pi / 2)))
    P.append(Prim("sphere", "gold", c=c + V(0, 0, -0.07), r=0.08))
    a, b = tool_along(rh, -1.3 + pose.atk * 1.4, 0.5)
    P.append(Prim("capsule", "metal", a=a, b=b, r=0.04))
    return P


def mago(pose):
    P, head, (lh, rh) = humanoid(pose, tunic="team_d", body_w=0.3)
    P.append(Prim("ellipsoid", "team_d", c=V(0, 0.55 + pose.bob, 0), r=V(0.33, 0.3, 0.28)))   # túnica longa
    P.append(Prim("ellipsoid", "beard", c=head + V(0, -0.28, -0.18), r=V(0.22, 0.28, 0.14), k=0.06))
    P.append(Prim("cyl", "team", c=head + V(0, 0.2, 0.0), r=0.5, h=0.04, rr=0.02))                  # aba do chapéu
    P.append(Prim("cone", "team", c=head + V(0, 0.22, 0.05), r=0.34, h=0.75))
    P.append(Prim("sphere", "gold", c=head + V(0, 0.3, -0.28), r=0.06))
    # cajado
    a, b = tool_along(rh, -1.55 + pose.atk * 0.8, 0.2, back=0.7)
    P.append(Prim("capsule", "wood", a=a, b=b + V(0, 0.45, 0), r=0.04))
    P.append(Prim("sphere", "purple", c=b + V(0, 0.55, 0), r=0.12 + pose.atk * 0.05))
    return P


def atiradora(pose):
    P, head, (lh, rh) = humanoid(pose, body_w=0.26)
    P.append(Prim("sphere", "black_hair", c=head + V(0, 0.1, 0.1), r=0.36))
    P.append(Prim("cyl", "team", c=head + V(0, 0.28, 0.02), r=0.42, h=0.03, rr=0.02))               # chapéu
    P.append(Prim("cyl", "team", c=head + V(0, 0.36, 0.02), r=0.24, h=0.1, rr=0.04))
    P.append(Prim("ellipsoid", "white", c=head + V(0.2, 0.42, 0.08), r=V(0.04, 0.16, 0.08)))         # pena
    # mosquete apoiado nas mãos
    mid = (lh + rh) / 2
    a = mid + V(0, 0.05, 0.25)
    b = mid + V(0, 0.18 + pose.atk * 0.1, -0.7)
    P.append(Prim("capsule", "wood", a=a, b=mid + V(0, 0.1, -0.1), r=0.06))
    P.append(Prim("capsule", "dark", a=mid + V(0, 0.1, -0.1), b=b, r=0.035))
    if pose.atk > 0.5:
        P.append(Prim("sphere", "glow", c=b + V(0, 0.03, -0.12), r=0.12))
    return P


def bombardeiro(pose):
    P, head, (lh, rh) = humanoid(pose, body_w=0.27, tunic="team_d")
    P.append(Prim("cyl", "team", c=head + V(0, 0.22, 0.0), r=0.32, h=0.16, rr=0.08))                 # gorro
    P.append(Prim("ellipsoid", "dark", c=head + V(0, 0.08, -0.3), r=V(0.26, 0.08, 0.05)))            # óculos
    for side in (-1, 1):
        P.append(Prim("sphere", "glow", c=head + V(side * 0.12, 0.08, -0.34), r=0.06))
    # bomba na mão
    bomb = rh + V(0, 0.12 + pose.atk * 0.3, -0.05)
    P.append(Prim("sphere", "dark", c=bomb, r=0.17))
    P.append(Prim("capsule", "wood", a=bomb + V(0, 0.15, 0), b=bomb + V(0.05, 0.27, 0), r=0.02))
    P.append(Prim("sphere", "fire", c=bomb + V(0.06, 0.3, 0), r=0.05))
    return P


def gigante(pose):
    P, head, (lh, rh) = humanoid(pose, scale=1.75, tunic="leather", body_w=0.36, head_r=0.3)
    P.append(Prim("ellipsoid", "skin", c=V(0, 1.5 + pose.bob, -0.05), r=V(0.58, 0.5, 0.4), k=0.1))  # peitoral
    P.append(Prim("ellipsoid", "orange_hair", c=head + V(0, -0.3, -0.2), r=V(0.26, 0.24, 0.16), k=0.05))
    P.append(Prim("ellipsoid", "orange_hair", c=head + V(0, 0.3, 0.06), r=V(0.3, 0.12, 0.26)))
    P.append(Prim("cyl", "team", c=V(0, 0.95 + pose.bob, 0), r=0.55, h=0.08, rr=0.03))                   # faixa
    return P


def vikings(pose):
    P, head, (lh, rh) = humanoid(pose, body_w=0.3, tunic="leather")
    P.append(Prim("sphere", "metal", c=head + V(0, 0.15, 0.08), r=0.35))
    for side in (-1, 1):
        P.append(Prim("capsule", "white", a=head + V(side * 0.3, 0.2, 0), b=head + V(side * 0.48, 0.55, 0.05), r=0.06))
    P.append(Prim("ellipsoid", "orange_hair", c=head + V(0, -0.27, -0.2), r=V(0.25, 0.25, 0.15), k=0.05))
    P.append(Prim("box", "team", c=V(0, 0.85 + pose.bob, -0.2), h=V(0.2, 0.08, 0.04), r=0.02))
    a, b = tool_along(rh, -1.2 + pose.atk * 1.4, 0.55)
    P.append(Prim("capsule", "wood", a=a, b=b, r=0.04))
    P.append(Prim("box", "metal", c=b, h=V(0.03, 0.14, 0.1), r=0.02))
    return P


def lanceiro(pose):
    P, head, (lh, rh) = humanoid(pose, body_w=0.26)
    P.append(Prim("sphere", "team", c=head + V(0, 0.15, 0.08), r=0.35))
    P.append(Prim("sphere", "team_l", c=head + V(0, 0.48, 0.04), r=0.08))
    a, b = tool_along(rh, -1.45 + pose.atk * 1.2, 1.1, back=0.5)
    P.append(Prim("capsule", "wood", a=a, b=b, r=0.03))
    P.append(Prim("cone", "metal", c=b, r=0.07, h=0.22, R=None))
    return P


def golem(pose):
    P = []
    y = pose.bob
    for side, sgn in ((-1, 1), (1, -1)):
        hip = V(side * 0.32, 0.55 + y, 0)
        foot = limb(hip, 0.45, pose.leg * sgn * 0.6)
        P.append(Prim("capsule", "rock", a=hip, b=foot, r=0.2))
    P.append(Prim("ellipsoid", "rock", c=V(0, 1.25 + y, 0), r=V(0.7, 0.62, 0.5), k=0.1))
    P.append(Prim("ellipsoid", "moss", c=V(-0.15, 1.75 + y, 0.05), r=V(0.4, 0.14, 0.3)))
    head = V(0, 1.6 + y, -0.4)
    P.append(Prim("ellipsoid", "rock", c=head, r=V(0.32, 0.26, 0.22), k=0.1))
    for side in (-1, 1):
        P.append(Prim("sphere", "team_l", c=head + V(side * 0.12, 0.02, -0.18), r=0.07))
        sh = V(side * 0.72, 1.45 + y, 0)
        ang = pose.arm * side + (pose.atk if side == 1 else 0)
        hand = limb(sh, 0.6, ang, side * 0.15)
        P.append(Prim("capsule", "rock", a=sh, b=hand, r=0.2, k=0.05))
        P.append(Prim("sphere", "rock", c=hand, r=0.28))
    return P


def lobo(pose):
    P = []
    y = pose.bob
    P.append(Prim("ellipsoid", "fur", c=V(0, 0.55 + y, 0.05), r=V(0.24, 0.24, 0.48)))
    head = V(0, 0.82 + y, -0.42)
    P.append(Prim("sphere", "fur", c=head, r=0.22, k=0.06))
    P.append(Prim("ellipsoid", "fur_l", c=head + V(0, -0.07, -0.2), r=V(0.1, 0.09, 0.16), k=0.04))
    P.append(Prim("sphere", "dark", c=head + V(0, -0.04, -0.36), r=0.04))
    for side in (-1, 1):
        P.append(Prim("cone", "fur", c=head + V(side * 0.12, 0.15, 0.02), r=0.08, h=0.18))
        P.append(Prim("sphere", "team_l", c=head + V(side * 0.09, 0.05, -0.18), r=0.035))
        for fz, ph in ((-0.25, 1), (0.3, -1)):
            hip = V(side * 0.15, 0.5 + y, fz)
            foot = limb(hip, 0.42, pose.leg * ph * side)
            P.append(Prim("capsule", "fur", a=hip, b=foot, r=0.06))
    P.append(Prim("capsule", "fur", a=V(0, 0.65 + y, 0.5), b=V(0, 0.85 + y, 0.85), r=0.07))
    P.append(Prim("cyl", "team", c=head + V(0, -0.18, 0.12), r=0.17, h=0.03, rr=0.01, R=rot_x(0.5)))  # coleira
    return P


def yeti(pose):
    P, head, (lh, rh) = humanoid(pose, scale=1.35, tunic="white", skin="white", legs="white", body_w=0.4, head_r=0.32)
    P.append(Prim("ellipsoid", "white", c=V(0, 1.05 + pose.bob, 0), r=V(0.62, 0.52, 0.45), k=0.12))
    P.append(Prim("ellipsoid", "ice", c=head + V(0, -0.06, -0.25), r=V(0.2, 0.16, 0.12)))      # rosto azulado
    P.append(Prim("cyl", "team", c=V(0, 0.72 + pose.bob, 0), r=0.55, h=0.06, rr=0.03))
    return P


def feiticeira(pose):
    P, head, (lh, rh) = humanoid(pose, tunic="ice", body_w=0.26)
    P.append(Prim("ellipsoid", "ice", c=V(0, 0.5 + pose.bob, 0), r=V(0.32, 0.3, 0.28)))
    P.append(Prim("sphere", "white", c=head + V(0, 0.12, 0.09), r=0.36))
    P.append(Prim("capsule", "white", a=head + V(0, 0, 0.25), b=head + V(0, -0.65, 0.3), r=0.16))
    P.append(Prim("cone", "team", c=head + V(0, 0.34, 0.02), r=0.12, h=0.25))    # tiara
    orb = rh + V(0, 0.2 + pose.atk * 0.2, -0.15)
    P.append(Prim("sphere", "ice", c=orb, r=0.14 + pose.atk * 0.06))
    return P


def salamandra(pose):
    P = []
    y = pose.bob
    P.append(Prim("ellipsoid", "fire", c=V(0, 0.42 + y, 0.1), r=V(0.3, 0.26, 0.55)))
    P.append(Prim("ellipsoid", "glow", c=V(0, 0.34 + y, 0.0), r=V(0.2, 0.15, 0.4)))
    head = V(0, 0.62 + y, -0.5)
    P.append(Prim("ellipsoid", "fire", c=head, r=V(0.24, 0.2, 0.3), k=0.08))
    for side in (-1, 1):
        P.append(Prim("sphere", "dark", c=head + V(side * 0.14, 0.1, -0.12), r=0.05))
        for fz, ph in ((-0.25, 1), (0.35, -1)):
            hip = V(side * 0.25, 0.38 + y, fz)
            foot = hip + V(side * 0.12, -0.32, ph * pose.leg * 0.2 * side)
            P.append(Prim("capsule", "fire", a=hip, b=foot, r=0.07))
    P.append(Prim("capsule", "fire", a=V(0, 0.4 + y, 0.6), b=V(0.1, 0.5 + y, 1.1), r=0.1))
    P.append(Prim("ellipsoid", "team", c=V(0, 0.62 + y, 0.15), r=V(0.16, 0.06, 0.3)))   # sela
    if pose.atk > 0.5:
        P.append(Prim("sphere", "glow", c=head + V(0, 0, -0.45), r=0.18))
    return P


def cavaleiro_negro(pose):
    P, head, (lh, rh) = humanoid(pose, scale=1.15, tunic="dark", legs="dark", body_w=0.32)
    P.append(Prim("sphere", "dark", c=head + V(0, 0.05, 0.0), r=0.39))
    P.append(Prim("box", "team_l", c=head + V(0, 0.02, -0.36), h=V(0.18, 0.03, 0.02), r=0.01))     # viseira brilhando
    P.append(Prim("cone", "team", c=head + V(0, 0.32, 0.0), r=0.1, h=0.3))
    for side in (-1, 1):
        P.append(Prim("sphere", "dark", c=V(side * 0.4, 1.12 + pose.bob, 0), r=0.17))                  # ombreiras
    a, b = tool_along(rh, -1.1 + pose.atk * 1.5, 0.95)
    P.append(Prim("capsule", "metal", a=a, b=b, r=0.06))
    P.append(Prim("capsule", "gold", a=rh + V(-0.16, 0, 0), b=rh + V(0.16, 0, 0), r=0.04))
    return P


ROSTER = {
    "gue": guerreiro, "arq": arqueira, "rat": ratinho, "esc": escudeiro, "bom": bombardeiro,
    "ati": atiradora, "gig": gigante, "mag": mago, "vik": vikings, "lan": lanceiro, "gol": golem,
    "lob": lobo, "yet": yeti, "fei": feiticeira, "sal": salamandra, "cav": cavaleiro_negro,
}

# tamanho da área de render (unidades) e altura central por personagem
FRAMING = {
    "gig": (2.2, 1.55), "gol": (2.1, 1.25), "yet": (1.8, 1.25), "cav": (1.5, 1.05),
    "rat": (1.0, 0.45), "lob": (1.25, 0.65), "sal": (1.3, 0.5),
}


def make(key, team, pose, yaw):
    prims = ROSTER[key](pose)
    return Scene(prims, mats_for(team), yaw=yaw)


WALK = [Pose(leg=0.55, arm=0.45, bob=0.03), Pose(leg=0.0, arm=0.0, bob=0.07),
        Pose(leg=-0.55, arm=-0.45, bob=0.03), Pose(leg=0.0, arm=0.0, bob=0.07)]
ATTACK = [Pose(atk=-1.3, bob=0.02), Pose(atk=1.0, bob=0.0, leg=0.2)]
IDLE = Pose(bob=0.0)


def frames(key, team, facing, size=192):
    """facing: 'frente' (vem em direção à câmera) ou 'costas' (vai para longe)."""
    yaw = math.radians(-35) if facing == "frente" else math.radians(180 + 35)
    ext, cy = FRAMING.get(key, (1.25, 0.9))
    out = {}
    out["idle"] = render(make(key, team, IDLE, yaw), size=size, extent=ext, center=(0, cy, 0))
    for i, p in enumerate(WALK):
        out[f"walk{i}"] = render(make(key, team, p, yaw), size=size, extent=ext, center=(0, cy, 0))
    for i, p in enumerate(ATTACK):
        out[f"atk{i}"] = render(make(key, team, p, yaw), size=size, extent=ext, center=(0, cy, 0))
    return out


# ------------------------------------------------------------------ torres
def tower_scene(team, king, yaw_figure):
    P = []
    st, std, stl = "stone", "stone_d", "stone_l"
    if king:
        B = 0.95
        H = 0.85
    else:
        B = 0.62
        H = 0.78
    P.append(Prim("box", std, c=V(0, 0.12, 0), h=V(B + 0.14, 0.12, B + 0.14), r=0.04))
    P.append(Prim("box", st, c=V(0, 0.24 + H, 0), h=V(B, H, B), r=0.06))
    top = 0.24 + 2 * H
    P.append(Prim("box", stl, c=V(0, top + 0.06, 0), h=V(B + 0.12, 0.09, B + 0.12), r=0.04))
    n = 3 if king else 2
    for i in range(-n, n + 1):
        x = i * (B + 0.05) / n
        for z in (-(B + 0.05), B + 0.05):
            P.append(Prim("box", stl, c=V(x, top + 0.24, z), h=V(0.1, 0.1, 0.07), r=0.025))
            P.append(Prim("box", stl, c=V(z, top + 0.24, x), h=V(0.07, 0.1, 0.1), r=0.025))
    # estandarte do time na frente e atrás
    for z in (-(B + 0.01), B + 0.01):
        P.append(Prim("box", "team", c=V(0, 0.24 + H * 1.15, z), h=V(0.26, 0.42, 0.02), r=0.01))
        P.append(Prim("box", "gold", c=V(0, 0.24 + H * 1.15 + 0.42, z), h=V(0.3, 0.03, 0.03), r=0.01))
        P.append(Prim("sphere", "gold", c=V(0, 0.24 + H * 1.25, z - 0.02 * (1 if z < 0 else -1)), r=0.08 if king else 0.06))
    # porta
    P.append(Prim("box", "wood", c=V(0, 0.42, -(B + 0.005)), h=V(0.17, 0.26, 0.03), r=0.06))
    # bandeirinhas nos cantos
    for sx in (-1, 1):
        base = V(sx * (B + 0.05), top + 0.3, -(B + 0.05))
        P.append(Prim("capsule", "wood", a=base, b=base + V(0, 0.55, 0), r=0.025))
        P.append(Prim("box", "team", c=base + V(sx * 0.14, 0.45, 0), h=V(0.13, 0.08, 0.01), r=0.01))
    figure = []
    if king:
        # canhão
        P.append(Prim("cyl", "dark", c=V(0, top + 0.35, -0.25), r=0.17, h=0.35, rr=0.05, R=rot_x(1.1)))
        P.append(Prim("cyl", "dark", c=V(0, top + 0.25, 0.0), r=0.28, h=0.12, rr=0.04))
        # rei atrás do canhão
        prims, head, hands = humanoid(IDLE, scale=0.8, tunic="team")
        for p in prims:
            figure.append(("rei", p, V(0, top + 0.12, 0.45)))
        crown_c = head * 1.0
        figure.append(("rei", Prim("cyl", "gold", c=crown_c + V(0, 0.27, 0), r=0.2, h=0.08, rr=0.02), V(0, top + 0.12, 0.45)))
        for k in range(5):
            a = k / 5 * math.tau
            figure.append(("rei", Prim("cone", "gold", c=crown_c + V(math.cos(a) * 0.16, 0.33, math.sin(a) * 0.16), r=0.05, h=0.12),
                           V(0, top + 0.12, 0.45)))
        figure.append(("rei", Prim("ellipsoid", "beard", c=crown_c + V(0, -0.2, -0.15), r=V(0.2, 0.2, 0.12), k=0.05), V(0, top + 0.12, 0.45)))
    else:
        for p in arqueira(IDLE):
            figure.append(("arq", p, V(0, top + 0.12, 0)))
    # aplica deslocamento/rotação da figura (pré-transformando as primitivas)
    for _, p, off in figure:
        P.append(OffsetPrim(p, off, yaw_figure, 0.62 if not king else 1.0))
    return P


class OffsetPrim(Prim):
    """Primitiva deslocada, girada e escalada (para personagens em cima das torres)."""
    def __init__(self, inner, off, yaw, scale):
        self.inner, self.off, self.scale = inner, np.asarray(off, np.float32), scale
        self.M = rot_y(yaw)
        self.mat, self.k = inner.mat, inner.k * scale

    def dist(self, p):
        q = ((p - self.off) @ self.M) / self.scale
        return self.inner.dist(q) * self.scale


TOWER_MATS = {"stone": (0.66, 0.64, 0.60, 0.05), "stone_d": (0.46, 0.44, 0.42, 0.05), "stone_l": (0.70, 0.68, 0.64, 0.05)}


def tower_image(team, king, size=320):
    mats = mats_for(team)
    mats.update(TOWER_MATS)
    yaw_fig = math.radians(180 + 20) if team == "azul" else math.radians(-20)
    sc = Scene(tower_scene(team, king, yaw_fig), mats, yaw=math.radians(0))
    ext = 2.15 if king else 1.8
    cy = 1.75 if king else 1.65
    return render(sc, size=size, extent=ext, center=(0, cy, 0)), ext, cy


# ------------------------------------------------------------------ voadores e construções

def dragao(pose):
    """Dragãozinho voando: as asas batem com pose.leg; ataque abre a boca e solta fogo."""
    P = []
    y = 0.95 + pose.bob * 2
    body = V(0, y, 0.05)
    P.append(Prim("ellipsoid", "green", c=body, r=V(0.3, 0.28, 0.42), k=0.08))
    P.append(Prim("ellipsoid", "fire", c=body + V(0, -0.08, -0.05), r=V(0.2, 0.17, 0.32)))      # barriga
    head = body + V(0, 0.28, -0.42)
    P.append(Prim("sphere", "green", c=head, r=0.24, k=0.08))
    P.append(Prim("ellipsoid", "green", c=head + V(0, -0.06, -0.22), r=V(0.14, 0.1, 0.14), k=0.05))   # focinho
    for side in (-1, 1):
        P.append(Prim("sphere", "white", c=head + V(side * 0.11, 0.08, -0.17), r=0.07))
        P.append(Prim("sphere", "dark", c=head + V(side * 0.11, 0.08, -0.22), r=0.035))
        P.append(Prim("cone", "gold", c=head + V(side * 0.12, 0.15, 0.06), r=0.05, h=0.18))      # chifres
        # asas (membrana na cor do time) batendo
        flap = pose.leg * 1.2
        root = body + V(side * 0.22, 0.12, 0.05)
        tip = root + rot_z(side * (0.35 + flap)) @ V(side * 0.75, 0.0, 0.0)
        P.append(Prim("capsule", "green", a=root, b=tip, r=0.04))
        P.append(Prim("ellipsoid", "team", c=(root + tip) / 2 + V(0, -0.05, 0.12),
                      r=V(0.36, 0.05, 0.22), R=rot_z(side * (0.35 + flap))))
    P.append(Prim("capsule", "green", a=body + V(0, 0, 0.35), b=body + V(0, -0.15, 0.85), r=0.08))   # rabo
    if pose.atk > 0.5:
        P.append(Prim("sphere", "fire", c=head + V(0, -0.05, -0.45), r=0.16))
        P.append(Prim("sphere", "glow", c=head + V(0, -0.05, -0.4), r=0.09))
    return P


def morcego(pose):
    P = []
    y = 0.7 + pose.bob * 2
    body = V(0, y, 0)
    P.append(Prim("ellipsoid", "purple", c=body, r=V(0.18, 0.2, 0.16), k=0.05))
    head = body + V(0, 0.2, -0.04)
    P.append(Prim("sphere", "purple", c=head, r=0.15, k=0.05))
    for side in (-1, 1):
        P.append(Prim("cone", "purple", c=head + V(side * 0.09, 0.1, 0), r=0.06, h=0.18))
        P.append(Prim("sphere", "glow", c=head + V(side * 0.06, 0.02, -0.12), r=0.035))
        flap = pose.leg * 1.4
        root = body + V(side * 0.12, 0.05, 0)
        tip = root + rot_z(side * (0.25 + flap)) @ V(side * 0.5, 0, 0)
        P.append(Prim("ellipsoid", "team", c=(root + tip) / 2, r=V(0.27, 0.04, 0.16), R=rot_z(side * (0.25 + flap))))
    if pose.atk > 0.5:
        P.append(Prim("cone", "white", c=head + V(0, -0.1, -0.13), r=0.03, h=0.06))
    return P


def canhao(pose):
    P = []
    P.append(Prim("box", "wood", c=V(0, 0.18, 0), h=V(0.45, 0.12, 0.4), r=0.04))
    for sx in (-1, 1):
        for sz in (-1, 1):
            P.append(Prim("cyl", "leather", c=V(sx * 0.42, 0.16, sz * 0.25), r=0.15, h=0.05, rr=0.02, R=rot_z(math.pi / 2)))
    P.append(Prim("box", "team", c=V(0, 0.33, 0), h=V(0.3, 0.05, 0.28), r=0.02))
    recoil = 0.12 if pose.atk > 0.5 else (-0.05 if pose.atk < -0.5 else 0.0)
    P.append(Prim("cyl", "dark", c=V(0, 0.52, -0.15 + recoil), r=0.15, h=0.42, rr=0.05, R=rot_x(1.25)))
    P.append(Prim("sphere", "dark", c=V(0, 0.48, 0.18 + recoil), r=0.2))
    P.append(Prim("cyl", "metal", c=V(0, 0.62, -0.55 + recoil), r=0.17, h=0.05, rr=0.02, R=rot_x(1.25)))
    if pose.atk > 0.5:
        P.append(Prim("sphere", "glow", c=V(0, 0.68, -0.8), r=0.16))
    # bandeirinha
    P.append(Prim("capsule", "wood", a=V(0.38, 0.3, 0.3), b=V(0.38, 0.95, 0.3), r=0.02))
    P.append(Prim("box", "team", c=V(0.52, 0.85, 0.3), h=V(0.13, 0.08, 0.01), r=0.01))
    return P


def torre_bombas(pose):
    P = []
    P.append(Prim("box", "stone_d", c=V(0, 0.1, 0), h=V(0.5, 0.1, 0.5), r=0.04))
    P.append(Prim("box", "stone", c=V(0, 0.6, 0), h=V(0.4, 0.45, 0.4), r=0.05))
    P.append(Prim("box", "team", c=V(0, 0.75, -0.41), h=V(0.2, 0.22, 0.02), r=0.01))
    P.append(Prim("box", "stone_l", c=V(0, 1.1, 0), h=V(0.5, 0.06, 0.5), r=0.03))
    for sx in (-1, 1):
        for sz in (-1, 1):
            P.append(Prim("box", "stone_l", c=V(sx * 0.4, 1.22, sz * 0.4), h=V(0.08, 0.08, 0.08), r=0.02))
    for p in bombardeiro(pose):
        P.append(OffsetPrim(p, V(0, 1.12, 0), 0.0, 0.55))
    return P


ROSTER.update({"dra": dragao, "mor": morcego, "can": canhao, "tbo": torre_bombas})
FRAMING.update({"dra": (1.4, 0.95), "mor": (0.95, 0.7), "can": (1.1, 0.5), "tbo": (1.45, 1.05)})


def mats_for_extra(team):
    m = mats_for(team)
    m.update(TOWER_MATS)
    return m


def make(key, team, pose, yaw):
    prims = ROSTER[key](pose)
    return Scene(prims, mats_for_extra(team), yaw=yaw)


# ------------------------------------------------------------------ campeões (originais)
BASE_MATS.update({"teal_robe": (0.12, 0.6, 0.58, 0.25), "leaf": (0.45, 0.85, 0.35, 0.2),
                  "white_cape": (0.95, 0.95, 0.98, 0.2), "copper_hair": (0.85, 0.42, 0.18, 0.2)})

def aurora(pose):
    """Cavaleira Aurora: armadura dourada, capa branca, trança acobreada e espada larga."""
    P, head, (lh, rh) = humanoid(pose, tunic="gold", legs="metal", body_w=0.3)
    P.append(Prim("ellipsoid", "white_cape", c=V(0, 0.95 + pose.bob, 0.16), r=V(0.36, 0.48, 0.12)))
    P.append(Prim("sphere", "copper_hair", c=head + V(0, 0.05, 0.1), r=0.37))
    P.append(Prim("capsule", "copper_hair", a=head + V(0, -0.05, 0.3), b=head + V(0, -0.65, 0.38), r=0.09))   # trança
    P.append(Prim("ellipsoid", "gold", c=head + V(0, 0.22, 0.04), r=V(0.36, 0.2, 0.36), k=0.03))              # elmo
    for s in (-1, 1):
        P.append(Prim("ellipsoid", "white", c=head + V(s * 0.36, 0.3, 0.08), r=V(0.04, 0.16, 0.2), R=rot_z(s * 0.5)))  # asinhas
    for s in (-1, 1):
        P.append(Prim("sphere", "gold", c=V(s * 0.34, 1.08 + pose.bob, 0), r=0.14))                             # ombreiras
    a, b = tool_along(rh, -1.1 + pose.atk * 1.5, 0.95)
    P.append(Prim("capsule", "white", a=a, b=b, r=0.07))
    P.append(Prim("capsule", "gold", a=rh + V(-0.16, 0, 0), b=rh + V(0.16, 0, 0), r=0.045))
    if pose.atk > 0.5:
        P.append(Prim("sphere", "glow", c=b, r=0.18))
    return P

def ventania(pose):
    """Bruxa Ventania: chapéu largo verde-azulado, manto, cajado com orbe e folhas girando."""
    P, head, (lh, rh) = humanoid(pose, tunic="teal_robe", body_w=0.28)
    P.append(Prim("ellipsoid", "teal_robe", c=V(0, 0.5 + pose.bob, 0), r=V(0.36, 0.34, 0.32)))
    P.append(Prim("sphere", "black_hair", c=head + V(0, 0.02, 0.12), r=0.37))
    P.append(Prim("cyl", "purple", c=head + V(0, 0.22, 0.02), r=0.6, h=0.03, rr=0.02))
    P.append(Prim("cone", "purple", c=head + V(0, 0.24, 0.08), r=0.3, h=0.7, R=rot_x(0.25)))
    P.append(Prim("ellipsoid", "leaf", c=head + V(0.2, 0.3, -0.2), r=V(0.1, 0.03, 0.06)))
    a, b = tool_along(rh, -1.5 + pose.atk * 0.8, 0.2, back=0.75)
    P.append(Prim("capsule", "wood", a=a, b=b + V(0, 0.5, 0), r=0.04))
    P.append(Prim("sphere", "leaf", c=b + V(0, 0.62, 0), r=0.13 + pose.atk * 0.06))
    for i in range(4):
        ang = i / 4 * math.tau + pose.leg * 2
        P.append(Prim("ellipsoid", "leaf", c=V(math.cos(ang) * 0.6, 0.9 + 0.2 * i % 2 + pose.bob, math.sin(ang) * 0.6),
                      r=V(0.09, 0.025, 0.05), R=rot_z(ang)))
    return P

ROSTER.update({"aur": aurora, "ven": ventania})
FRAMING.update({"aur": (1.35, 0.95), "ven": (1.35, 0.95)})


# ------------------------------------------------------------------ tropas novas (originais)
BASE_MATS.update({"leaf_d": (0.25, 0.55, 0.25, 0.2), "robe_w": (0.93, 0.95, 0.9, 0.15), "lamp": (1.0, 0.95, 0.5, 0.1),
                  "hood": (0.25, 0.22, 0.3, 0.15), "canvas": (0.85, 0.78, 0.6, 0.1)})

def curandeira(pose):
    """Curandeira: manto branco e verde, coroa de folhas e cajado com flor."""
    P, head, (lh, rh) = humanoid(pose, tunic="robe_w", body_w=0.27)
    P.append(Prim("ellipsoid", "robe_w", c=V(0, 0.5 + pose.bob, 0), r=V(0.34, 0.32, 0.3)))
    P.append(Prim("ellipsoid", "team", c=V(0, 0.78 + pose.bob, -0.05), r=V(0.3, 0.06, 0.27)))           # faixa
    P.append(Prim("sphere", "orange_hair", c=head + V(0, 0.08, 0.1), r=0.37))
    for i in range(6):
        a = i / 6 * math.tau
        P.append(Prim("ellipsoid", "leaf", c=head + V(math.cos(a) * 0.32, 0.28, math.sin(a) * 0.32), r=V(0.08, 0.03, 0.05)))
    a, b = tool_along(rh, -1.4 + pose.atk * 0.6, 0.2, back=0.7)
    P.append(Prim("capsule", "wood", a=a, b=b + V(0, 0.45, 0), r=0.035))
    for i in range(5):
        ang = i / 5 * math.tau
        P.append(Prim("sphere", "pink", c=b + V(math.cos(ang) * 0.09, 0.55, math.sin(ang) * 0.09), r=0.06))
    P.append(Prim("sphere", "glow" if pose.atk > 0.5 else "gold", c=b + V(0, 0.55, 0), r=0.06))
    return P

def escavador(pose):
    """Escavador: capacete com lanterna, macacão e picareta."""
    P, head, (lh, rh) = humanoid(pose, tunic="team", legs="leather", body_w=0.3)
    P.append(Prim("ellipsoid", "gold", c=head + V(0, 0.18, 0.03), r=V(0.38, 0.22, 0.38), k=0.03))
    P.append(Prim("cyl", "gold", c=head + V(0, 0.1, 0.03), r=0.42, h=0.03, rr=0.02))
    P.append(Prim("sphere", "lamp", c=head + V(0, 0.26, -0.36), r=0.08))
    P.append(Prim("ellipsoid", "brown_hair", c=head + V(0, -0.2, -0.25), r=V(0.2, 0.12, 0.1), k=0.05))        # barba
    P.append(Prim("box", "leather", c=V(0, 0.78 + pose.bob, -0.2), h=V(0.18, 0.12, 0.05), r=0.02))          # bolso
    a, b = tool_along(rh, -1.2 + pose.atk * 1.6, 0.7)
    P.append(Prim("capsule", "wood", a=a, b=b, r=0.04))
    P.append(Prim("capsule", "metal", a=b + V(-0.25, 0.05, 0), b=b + V(0.25, -0.05, 0), r=0.05))
    return P

def ladrao(pose):
    """Ladrão: capuz escuro, máscara e saco de moedas."""
    P, head, (lh, rh) = humanoid(pose, tunic="hood", legs="dark", body_w=0.25)
    P.append(Prim("sphere", "hood", c=head + V(0, 0.08, 0.06), r=0.42))
    P.append(Prim("cone", "hood", c=head + V(0, 0.2, 0.25), r=0.2, h=0.4, R=rot_x(-1.0)))
    P.append(Prim("box", "team", c=head + V(0, 0.04, -0.33), h=V(0.3, 0.05, 0.04), r=0.02))                 # máscara
    P.append(Prim("sphere", "canvas", c=lh + V(0, -0.15, 0.05), r=0.18))                                     # saco
    P.append(Prim("sphere", "gold", c=lh + V(0, 0.02, 0.05), r=0.06))
    a, b = tool_along(rh, -1.1 + pose.atk * 1.3, 0.35)
    P.append(Prim("capsule", "metal", a=a, b=b, r=0.03))
    return P

def balao(pose):
    """Balão de Bombas: balão listrado na cor do time e cesta com um piloto bombardeiro."""
    P = []
    y = 1.3 + pose.bob * 2
    P.append(Prim("ellipsoid", "team", c=V(0, y + 0.35, 0), r=V(0.55, 0.62, 0.55), k=0.02))
    for i in range(4):
        a = i / 4 * math.tau
        P.append(Prim("ellipsoid", "white", c=V(math.cos(a) * 0.5, y + 0.38, math.sin(a) * 0.5), r=V(0.07, 0.5, 0.07),
                      R=rot_y(a)))
    P.append(Prim("box", "wood", c=V(0, y - 0.55, 0), h=V(0.24, 0.14, 0.24), r=0.04))
    for sx in (-1, 1):
        for sz in (-1, 1):
            P.append(Prim("capsule", "leather", a=V(sx * 0.2, y - 0.45, sz * 0.2), b=V(sx * 0.35, y - 0.05, sz * 0.35), r=0.015))
    P.append(Prim("sphere", "skin", c=V(0, y - 0.33, 0), r=0.13))
    P.append(Prim("sphere", "metal", c=V(0, y - 0.27, 0), r=0.12))
    if pose.atk > 0.5:
        P.append(Prim("sphere", "dark", c=V(0, y - 0.85, 0), r=0.12))
        P.append(Prim("sphere", "fire", c=V(0.05, y - 0.72, 0), r=0.04))
    return P

ROSTER.update({"cun": curandeira, "esv": escavador, "lad": ladrao, "bal": balao})
FRAMING.update({"cun": (1.25, 0.9), "esv": (1.25, 0.9), "lad": (1.2, 0.85), "bal": (1.75, 1.45)})


# ------------------------------------------------------------------ construções novas (lote 16)
BASE_MATS.update({"ember": (1.0, 0.75, 0.25, 0.1), "crystal": (0.65, 0.88, 1.0, 0.95), "soot": (0.2, 0.17, 0.16, 0.2), "brick": (0.72, 0.36, 0.24, 0.05)})

def muralha(pose):
    """Muralha: paredão de pedras empilhadas com ameias e um estandarte do time."""
    P = []
    P.append(Prim("box", "stone_d", c=V(0, 0.12, 0), h=V(0.85, 0.12, 0.32), r=0.04))
    for i, x in enumerate((-0.6, -0.2, 0.2, 0.6)):
        P.append(Prim("box", "stone" if i % 2 else "stone_l", c=V(x, 0.38, 0), h=V(0.2, 0.15, 0.27), r=0.04))
    for i, x in enumerate((-0.4, 0.0, 0.4)):
        P.append(Prim("box", "stone_l" if i % 2 else "stone", c=V(x, 0.66, 0), h=V(0.2, 0.14, 0.25), r=0.04))
    for x in (-0.68, -0.23, 0.23, 0.68):
        P.append(Prim("box", "stone_l", c=V(x * 0.95, 0.9, 0), h=V(0.12, 0.1, 0.22), r=0.03))
    P.append(Prim("box", "moss", c=V(-0.45, 0.25, -0.3), h=V(0.12, 0.04, 0.02), r=0.02))
    P.append(Prim("box", "team", c=V(0.05, 0.6, -0.29), h=V(0.13, 0.2, 0.01), r=0.01))
    P.append(Prim("cone", "team", c=V(0.05, 0.36, -0.29), r=0.1, h=0.1, R=rot_x(math.pi)))
    return P

def torre_gelo(pose):
    """Torre de Gelo: base de pedra nevada com um grande cristal azul que brilha ao atirar."""
    P = []
    P.append(Prim("box", "stone_d", c=V(0, 0.1, 0), h=V(0.48, 0.1, 0.48), r=0.04))
    P.append(Prim("cyl", "stone", c=V(0, 0.55, 0), r=0.34, h=0.45, rr=0.05))
    P.append(Prim("cyl", "white", c=V(0, 1.0, 0), r=0.4, h=0.05, rr=0.04))
    P.append(Prim("box", "team", c=V(0, 0.6, -0.34), h=V(0.16, 0.18, 0.02), r=0.01))
    glow = 0.06 if pose.atk > 0.5 else 0.0
    y = 1.45 + pose.bob * 2
    P.append(Prim("cone", "crystal", c=V(0, y + 0.15, 0), r=0.27 + glow, h=0.55))
    P.append(Prim("cone", "crystal", c=V(0, y - 0.22, 0), r=0.27 + glow, h=0.4, R=rot_x(math.pi)))
    for k in range(3):
        a = k / 3 * math.tau
        P.append(Prim("cone", "ice", c=V(math.cos(a) * 0.32, 1.12, math.sin(a) * 0.32), r=0.08, h=0.25, R=rot_z(0.4 * math.cos(a)) @ rot_x(0.4 * math.sin(a))))
    if pose.atk > 0.5:
        P.append(Prim("sphere", "glow", c=V(0, y, -0.1), r=0.14))
    return P

def fornalha(pose):
    """Fornalha: forno de tijolos com chaminé, boca em brasa e fumaça."""
    P = []
    P.append(Prim("box", "stone_d", c=V(0, 0.1, 0), h=V(0.55, 0.1, 0.5), r=0.04))
    P.append(Prim("ellipsoid", "brick", c=V(0, 0.55, 0), r=V(0.52, 0.48, 0.48), k=0.02))
    P.append(Prim("box", "soot", c=V(0, 0.42, -0.42), h=V(0.24, 0.2, 0.08), r=0.06))
    heat = 0.08 if pose.atk > 0.5 else 0.0
    P.append(Prim("ellipsoid", "ember", c=V(0, 0.42, -0.52), r=V(0.19 + heat, 0.15 + heat, 0.03), k=0.02))
    P.append(Prim("sphere", "fire", c=V(0, 0.38, -0.5), r=0.08 + heat))
    P.append(Prim("cyl", "stone_d", c=V(0.25, 1.05, 0.15), r=0.12, h=0.3, rr=0.03))
    P.append(Prim("cyl", "dark", c=V(0.25, 1.36, 0.15), r=0.14, h=0.04, rr=0.02))
    s = 0.12 + pose.bob * 0.6
    P.append(Prim("sphere", "fur_l", c=V(0.27, 1.55 + pose.bob * 2, 0.15), r=s))
    P.append(Prim("box", "team", c=V(-0.3, 0.75, -0.36), h=V(0.12, 0.12, 0.02), r=0.02))
    return P

def brasinha(pose):
    """Brasinha: espírito de fogo saltitante com carinha e chamas no topo."""
    P = []
    y = 0.32 + abs(pose.leg) * 0.35 + pose.bob
    P.append(Prim("sphere", "fire", c=V(0, y, 0), r=0.24))
    P.append(Prim("sphere", "ember", c=V(0, y + 0.02, -0.08), r=0.17))
    for k, (dx, h) in enumerate(((-0.1, 0.22), (0.0, 0.32), (0.1, 0.2))):
        P.append(Prim("cone", "fire", c=V(dx, y + 0.28, 0), r=0.09, h=h))
    for sx in (-1, 1):
        P.append(Prim("sphere", "dark", c=V(sx * 0.08, y + 0.04, -0.22), r=0.04))
    P.append(Prim("sphere", "team", c=V(0, y - 0.2, 0), r=0.1))
    if pose.atk > 0.5:
        P.append(Prim("sphere", "glow", c=V(0, y, -0.25), r=0.12))
    return P

ROSTER.update({"mur": muralha, "tgl": torre_gelo, "frn": fornalha, "bra": brasinha})
FRAMING.update({"mur": (1.3, 0.55), "tgl": (1.5, 1.1), "frn": (1.35, 0.8), "bra": (0.85, 0.45)})
