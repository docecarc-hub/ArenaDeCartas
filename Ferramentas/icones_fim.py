import math, os
import numpy as np
from personagens3d import *
from render3d import Scene, render
M = dict(BASE_MATS); M.update({"ruby": (0.9, 0.15, 0.25, 0.9), "sapph": (0.2, 0.5, 1.0, 0.9),
     "cloth": (0.75, 0.15, 0.25, 0.15), "cloth_d": (0.55, 0.1, 0.18, 0.1), "silver": (0.8, 0.84, 0.9, 0.9)})
def coroa(mat="gold"):
    P = [Prim("cyl", mat, c=V(0, 0.35, 0), r=0.55, h=0.2, rr=0.06)]
    for i in range(5):
        a = i / 5 * math.tau
        x, z = math.sin(a) * 0.5, math.cos(a) * 0.5
        P.append(Prim("cone", mat, c=V(x, 0.62, z), r=0.17, h=0.42))
        P.append(Prim("sphere", mat, c=V(x, 0.98, z), r=0.09))
        P.append(Prim("sphere", "sapph" if i % 2 else "ruby", c=V(math.sin(a) * 0.58, 0.38, math.cos(a) * 0.58), r=0.08))
    return P
def almofada():
    return [Prim("ellipsoid", "cloth", c=V(0, 0.2, 0), r=V(0.75, 0.2, 0.55), k=0.1),
            Prim("sphere", "gold", c=V(-0.68, 0.2, 0.3), r=0.07), Prim("sphere", "gold", c=V(0.68, 0.2, 0.3), r=0.07),
            Prim("sphere", "gold", c=V(-0.68, 0.2, -0.3), r=0.07), Prim("sphere", "gold", c=V(0.68, 0.2, -0.3), r=0.07)]
def trofeu():
    return [Prim("box", "wood", c=V(0, 0.12, 0), h=V(0.32, 0.12, 0.32), r=0.03),
            Prim("cyl", "gold", c=V(0, 0.4, 0), r=0.07, h=0.2, rr=0.02),
            Prim("ellipsoid", "gold", c=V(0, 0.8, 0), r=V(0.4, 0.34, 0.4), k=0.02),
            Prim("cyl", "gold", c=V(0, 1.08, 0), r=0.4, h=0.04, rr=0.03),
            Prim("cyl", "leather", c=V(0, 1.11, 0), r=0.33, h=0.02, rr=0.01),
            Prim("capsule", "gold", a=V(-0.36, 0.95, 0), b=V(-0.5, 0.75, 0), r=0.06),
            Prim("capsule", "gold", a=V(0.36, 0.95, 0), b=V(0.5, 0.75, 0), r=0.06),
            Prim("sphere", "ruby", c=V(0, 0.8, -0.36), r=0.08)]
def estrela():
    P = [Prim("sphere", "sapph", c=V(0, 0.6, 0), r=0.32)]
    for i in range(5):
        a = i / 5 * math.tau
        P.append(Prim("cone", "sapph", c=V(math.sin(a) * 0.3, 0.6 + math.cos(a) * 0.3, 0), r=0.16, h=0.3,
                      R=rot_z(-a)))
    return P
def go(name, prims, ext, cy, yaw=-25):
    im = render(Scene(prims, M, yaw=math.radians(yaw)), size=256, extent=ext, center=(0, cy, 0))
    os.makedirs("out9/Icones", exist_ok=True)
    im.crop(im.getbbox()).save(f"out9/Icones/{name}.png")
    print(name, flush=True)
if __name__ == "__main__":
    from multiprocessing import Pool
    jobs = [("trofeu", trofeu(), 0.75, 0.6), ("almofada", almofada(), 0.85, 0.25)]
    with Pool(2) as p: p.starmap(go, jobs)
