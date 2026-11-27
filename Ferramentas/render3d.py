"""Mini renderizador 3D (SDF + raymarching em numpy) para gerar sprites com cara de 3D.

Cada modelo é uma lista de primitivas (esfera, elipsoide, cápsula, caixa arredondada, cilindro, cone)
com um material. A câmera é ortográfica, inclinada de cima (como em jogos de arena vistos do alto).
"""
import math
import numpy as np
from PIL import Image, ImageFilter

# ------------------------------------------------------------------ matemática

def norm(v):
    v = np.asarray(v, dtype=np.float32)
    return v / np.linalg.norm(v)


def rot_y(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]], dtype=np.float32)


def rot_x(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]], dtype=np.float32)


def rot_z(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]], dtype=np.float32)


# ------------------------------------------------------------------ primitivas (p: Nx3)

class Prim:
    def __init__(self, kind, mat, k=0.0, **kw):
        self.kind, self.mat, self.k, self.kw = kind, mat, k, kw
        self.R = kw.get("R")        # rotação local opcional (3x3), aplicada em torno de "c"

    def local(self, p, c):
        q = p - np.asarray(c, dtype=np.float32)
        if self.R is not None:
            q = q @ np.asarray(self.R, dtype=np.float32)  # q * R == R^T q (inversa)
        return q

    def dist(self, p):
        kw = self.kw
        t = self.kind
        if t == "sphere":
            return np.linalg.norm(p - np.asarray(kw["c"], np.float32), axis=1) - kw["r"]
        if t == "ellipsoid":
            q = self.local(p, kw["c"])
            r = np.asarray(kw["r"], np.float32)
            k0 = np.linalg.norm(q / r, axis=1)
            k1 = np.linalg.norm(q / (r * r), axis=1)
            return k0 * (k0 - 1.0) / np.maximum(k1, 1e-6)
        if t == "capsule":
            a = np.asarray(kw["a"], np.float32)
            b = np.asarray(kw["b"], np.float32)
            pa, ba = p - a, b - a
            h = np.clip((pa @ ba) / float(ba @ ba), 0.0, 1.0)
            return np.linalg.norm(pa - h[:, None] * ba, axis=1) - kw["r"]
        if t == "box":
            q = np.abs(self.local(p, kw["c"])) - (np.asarray(kw["h"], np.float32) - kw.get("r", 0.0))
            out = np.linalg.norm(np.maximum(q, 0.0), axis=1)
            inside = np.minimum(np.max(q, axis=1), 0.0)
            return out + inside - kw.get("r", 0.0)
        if t == "cyl":   # cilindro vertical (eixo y local)
            q = self.local(p, kw["c"])
            d = np.stack([np.linalg.norm(q[:, [0, 2]], axis=1) - kw["r"], np.abs(q[:, 1]) - kw["h"]], axis=1)
            d = d + kw.get("rr", 0.0)
            return np.minimum(np.max(d, axis=1), 0.0) + np.linalg.norm(np.maximum(d, 0.0), axis=1) - kw.get("rr", 0.0)
        if t == "cone":  # cone vertical: base em c (raio r), ponta em c + (0,h,0)
            q = self.local(p, kw["c"])
            h, r = kw["h"], kw["r"]
            qx = np.linalg.norm(q[:, [0, 2]], axis=1)
            qy = q[:, 1]
            # distância 2D ao triângulo (0,0)-(r,0)-(0,h)
            ax, ay = r, -h
            L = math.hypot(ax, ay)
            nx, ny = h / L, r / L          # normal externa da lateral
            side = (qx - r) * nx + qy * ny
            d = np.maximum(side, -qy)
            d = np.maximum(d, qy - h)
            return d
        raise ValueError(t)


def smin(a, b, k):
    if k <= 0:
        return np.minimum(a, b)
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0.0, 1.0)
    return b * (1 - h) + a * h - k * h * (1 - h)


class Scene:
    def __init__(self, prims, mats, yaw=0.0):
        self.prims = prims
        self.mats = mats            # nome -> (r,g,b) 0..1, opcional spec
        self.M = rot_y(yaw)

    def eval(self, p, want_mat=False):
        p = p @ self.M
        d = None
        mat = None
        for i, pr in enumerate(self.prims):
            di = pr.dist(p)
            if d is None:
                d = di
                if want_mat:
                    mat = np.zeros(len(p), dtype=np.int32)
                continue
            if want_mat:
                closer = di < d
                mat = np.where(closer, i, mat)
            d = smin(d, di, pr.k) if pr.k > 0 else np.minimum(d, di)
        return (d, mat) if want_mat else d


# ------------------------------------------------------------------ render

def render(scene, size=256, extent=2.0, center=(0.0, 1.0, 0.0), pitch=36.0, steps=72,
           light=(-0.55, 0.85, -0.45), outline=3, ss=2):
    """Renderiza e devolve uma imagem RGBA (PIL)."""
    N = size * ss
    pit = math.radians(pitch)
    d = np.array([0.0, -math.sin(pit), math.cos(pit)], dtype=np.float32)     # olhando para +z e para baixo
    right = np.array([1.0, 0.0, 0.0], dtype=np.float32)
    up = np.cross(d, right)
    up = up / np.linalg.norm(up)
    if up[1] < 0:
        up = -up
    xs = (np.arange(N) + 0.5) / N * 2 - 1
    u, v = np.meshgrid(xs * extent, -xs * extent)
    c = np.asarray(center, np.float32)
    orig = c[None, :] + u.reshape(-1, 1) * right[None, :] + v.reshape(-1, 1) * up[None, :] - d[None, :] * 8.0

    t = np.zeros(N * N, dtype=np.float32)
    hit = np.zeros(N * N, dtype=bool)
    alive = np.ones(N * N, dtype=bool)
    for _ in range(steps):
        idx = np.nonzero(alive)[0]
        if len(idx) == 0:
            break
        p = orig[idx] + t[idx, None] * d[None, :]
        dist = scene.eval(p)
        t[idx] += dist * 0.9
        h = dist < 0.002
        hit[idx[h]] = True
        far = t[idx] > 16.0
        alive[idx[h | far]] = False

    img = np.zeros((N * N, 4), dtype=np.float32)
    hi = np.nonzero(hit)[0]
    if len(hi):
        p = orig[hi] + t[hi, None] * d[None, :]
        _, mat = scene.eval(p, want_mat=True)
        e = 0.003
        nrm = np.stack([
            scene.eval(p + [e, 0, 0]) - scene.eval(p - [e, 0, 0]),
            scene.eval(p + [0, e, 0]) - scene.eval(p - [0, e, 0]),
            scene.eval(p + [0, 0, e]) - scene.eval(p - [0, 0, e]),
        ], axis=1)
        nrm /= np.linalg.norm(nrm, axis=1, keepdims=True) + 1e-8

        # oclusão ambiente (poucas amostras ao longo da normal)
        ao = np.ones(len(hi), dtype=np.float32)
        for k, step in enumerate((0.04, 0.09, 0.16)):
            dd = scene.eval(p + nrm * step)
            ao -= (step - np.minimum(dd, step)) * (2.2 / (k + 1))
        ao = np.clip(ao, 0.25, 1.0)

        L = norm(light)
        lam = np.clip(nrm @ L, 0.0, 1.0)
        # sombra suave do sol (marcha curta)
        sh = np.ones(len(hi), dtype=np.float32)
        ts = np.full(len(hi), 0.03, dtype=np.float32)
        for _ in range(22):
            q = p + nrm * 0.004 + ts[:, None] * L[None, :]
            dd = scene.eval(q)
            sh = np.minimum(sh, np.clip(10.0 * dd / ts, 0.0, 1.0))
            ts += np.clip(dd, 0.02, 0.2)
        sh = np.clip(sh, 0.0, 1.0)

        names = [pr.mat for pr in scene.prims]
        base = np.array([scene.mats[n][:3] for n in names], dtype=np.float32)[mat]
        spec_k = np.array([scene.mats[n][3] if len(scene.mats[n]) > 3 else 0.15 for n in names], np.float32)[mat]

        sky = np.array([0.62, 0.72, 0.95], np.float32)
        ground = np.array([0.45, 0.38, 0.30], np.float32)
        hemi = 0.5 + 0.5 * nrm[:, 1]
        amb = (sky[None, :] * hemi[:, None] + ground[None, :] * (1 - hemi[:, None])) * 0.55
        direct = (lam * sh)[:, None] * np.array([1.0, 0.96, 0.88], np.float32) * 1.0
        # suaviza (visual "toon" leve)
        col = base * (amb * ao[:, None] + direct)
        hvec = norm(L - d)
        spec = np.power(np.clip(nrm @ hvec, 0, 1), 40) * spec_k * sh
        col += spec[:, None]
        rim = np.power(1.0 - np.clip(-(nrm @ d), 0, 1), 3.0) * 0.35
        col += rim[:, None] * np.array([1.0, 0.95, 0.85], np.float32)
        col = np.clip(col, 0, 1)
        img[hi, :3] = col
        img[hi, 3] = 1.0

    arr = (img.reshape(N, N, 4) * 255).astype(np.uint8)
    im = Image.fromarray(arr, "RGBA")
    if ss > 1:
        im = im.resize((size, size), Image.LANCZOS)
    if outline:
        a = im.split()[3]
        grown = a.filter(ImageFilter.MaxFilter(outline * 2 + 1))
        ol = Image.new("RGBA", im.size, (30, 22, 26, 0))
        ol.putalpha(grown)
        ol.alpha_composite(im)
        im = ol
    return im
