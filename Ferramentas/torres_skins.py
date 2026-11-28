import os, math
from multiprocessing import Pool
import personagens3d as P3
SKINS = {
    "gelo":   {"stone": (0.70, 0.86, 0.96, 0.25), "stone_d": (0.45, 0.62, 0.80, 0.2), "stone_l": (0.88, 0.96, 1.00, 0.3)},
    "vulcao": {"stone": (0.30, 0.26, 0.26, 0.05), "stone_d": (0.80, 0.32, 0.10, 0.1), "stone_l": (0.42, 0.36, 0.34, 0.05)},
    "ouro":   {"stone": (0.92, 0.74, 0.30, 0.45), "stone_d": (0.62, 0.45, 0.18, 0.3), "stone_l": (1.00, 0.88, 0.50, 0.5)},
}
def job(a):
    skin, team, king = a
    old = dict(P3.TOWER_MATS)
    P3.TOWER_MATS.clear(); P3.TOWER_MATS.update(SKINS[skin])
    im, ext, cy = P3.tower_image(team, king)
    P3.TOWER_MATS.clear(); P3.TOWER_MATS.update(old)
    os.makedirs("out8/Batalha", exist_ok=True)
    name = f"out8/Batalha/{'rei' if king else 'torre'}_{team}_{skin}.png"
    im.save(name)
    return name
if __name__ == "__main__":
    jobs = [(s, t, k) for s in SKINS for t in ("azul", "vermelho") for k in (False, True)]
    with Pool(2) as p:
        for r in p.imap_unordered(job, jobs): print(r, flush=True)
    print("FIM", flush=True)
