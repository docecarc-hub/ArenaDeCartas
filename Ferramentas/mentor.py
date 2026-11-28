import math, os
from personagens3d import *
from render3d import Scene, render
M = mats_for("azul")
M.update({"teal": (0.15, 0.65, 0.6, 0.25), "teal_d": (0.08, 0.42, 0.4, 0.2), "gray_hair": (0.78, 0.78, 0.8, 0.15),
          "red": (0.85, 0.2, 0.2, 0.2), "paper": (0.95, 0.9, 0.75, 0.05)})
def capitao(pose, wave=True):
    P, head, (lh, rh) = humanoid(pose, tunic="teal", body_w=0.32, head_r=0.38)
    P.append(Prim("ellipsoid", "teal_d", c=V(0, 0.95 + pose.bob, 0.12), r=V(0.36, 0.42, 0.18)))        # capa
    P.append(Prim("box", "leather", c=V(0, 0.82 + pose.bob, -0.02), h=V(0.3, 0.04, 0.27), r=0.02))      # cinto
    P.append(Prim("box", "gold", c=V(0, 0.82 + pose.bob, -0.29), h=V(0.06, 0.05, 0.02), r=0.01))
    for s in (-1, 1):                                                                                   # bigode
        P.append(Prim("ellipsoid", "gray_hair", c=head + V(s * 0.12, -0.1, -0.33), r=V(0.14, 0.05, 0.06), k=0.04))
        P.append(Prim("sphere", "gray_hair", c=head + V(s * 0.3, 0.05, 0.05), r=0.1))                  # costeletas
    P.append(Prim("ellipsoid", "metal", c=head + V(0, 0.3, 0.04), r=V(0.36, 0.2, 0.36), k=0.03))         # elmo redondo
    P.append(Prim("cyl", "metal", c=head + V(0, 0.2, 0.04), r=0.42, h=0.03, rr=0.02))
    P.append(Prim("ellipsoid", "red", c=head + V(0, 0.58, 0.12), r=V(0.06, 0.18, 0.16), k=0.02))          # pluma
    for s in (-1, 1):                                                                                   # olhos e sobrancelhas
        P.append(Prim("sphere", "white", c=head + V(s * 0.13, 0.06, -0.31), r=0.07))
        P.append(Prim("sphere", "dark", c=head + V(s * 0.13, 0.06, -0.36), r=0.04))
        P.append(Prim("ellipsoid", "gray_hair", c=head + V(s * 0.14, 0.16, -0.32), r=V(0.09, 0.03, 0.04)))
    P.append(Prim("ellipsoid", "paper", c=lh + V(0, 0.05, -0.05), r=V(0.08, 0.08, 0.26)))               # mapa enrolado
    return P
if __name__ == "__main__":
    os.makedirs("out10", exist_ok=True)
    for name, pose in [("mentor", Pose(arm=0.25, bob=0.0)), ("mentor_fala", Pose(arm=-0.5, bob=0.02))]:
        im = render(Scene(capitao(pose), M, yaw=math.radians(-20)), size=300, extent=1.15, center=(0, 0.95, 0), pitch=15)
        im.crop(im.getbbox()).save(f"out10/{name}.png"); print(name)
