"""Renderiza todos os quadros de animação, retratos de cartas e torres."""
import math, os, sys, json
from multiprocessing import Pool
from PIL import Image
from personagens3d import *

OUT = "out4"
SIZE = 176

def job(args):
    key, team, facing = args
    os.makedirs(f"{OUT}/Tropas/{key}", exist_ok=True)
    fr = frames(key, team, facing, size=SIZE)
    for name, im in fr.items():
        im.save(f"{OUT}/Tropas/{key}/{team}_{facing}_{name}.png")
    return key, team, facing

def portrait(key):
    os.makedirs(f"{OUT}/Cartas", exist_ok=True)
    ext, cy = FRAMING.get(key, (1.25, 0.9))
    im = render(make(key, "azul", IDLE, math.radians(-25)), size=256, extent=ext * 0.85, center=(0, cy, 0))
    im = im.crop(im.getbbox())
    im.save(f"{OUT}/Cartas/{key}.png")
    return key

def tower(args):
    team, king = args
    os.makedirs(f"{OUT}/Batalha", exist_ok=True)
    im, ext, cy = tower_image(team, king, 320)
    im.save(f"{OUT}/Batalha/{'rei' if king else 'torre'}_{team}.png")
    return team, king, ext, cy

if __name__ == "__main__":
    jobs = [(k, t, f) for k in ROSTER for t in ("azul", "vermelho") for f in ("frente", "costas")]
    with Pool(2) as pool:
        for r in pool.imap_unordered(job, jobs):
            print("quadros", r, flush=True)
        for r in pool.imap_unordered(portrait, list(ROSTER)):
            print("retrato", r, flush=True)
        for r in pool.imap_unordered(tower, [(t, k) for t in ("azul", "vermelho") for k in (False, True)]):
            print("torre", r, flush=True)
    # informações de enquadramento para o Unity (pivô dos pés e escala)
    info = {}
    pit = math.radians(36)
    for k in ROSTER:
        ext, cy = FRAMING.get(k, (1.25, 0.9))
        info[k] = dict(ext=ext, pivot=0.5 - cy * math.cos(pit) / (2 * ext))
    for king in (False, True):
        ext = 2.15 if king else 1.8
        cy = 1.75 if king else 1.65
        info["rei" if king else "torre"] = dict(ext=ext, pivot=0.5 - cy * math.cos(pit) / (2 * ext))
    json.dump(info, open(f"{OUT}/info.json", "w"), indent=1)
    print("FIM", flush=True)
