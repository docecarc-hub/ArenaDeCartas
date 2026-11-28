import math, os, sys
from multiprocessing import Pool
from PIL import Image
from personagens3d import *
OUT = sys.argv[1] if len(sys.argv) > 1 and sys.argv[1].startswith("out") else "out12"; SIZE = 176
KEYS = [k for k in sys.argv[1:] if not k.startswith("out")] or ["dra", "mor", "can", "tbo"]
def job(args):
    key, team, facing = args
    os.makedirs(f"{OUT}/Tropas/{key}", exist_ok=True)
    for name, im in frames(key, team, facing, size=SIZE).items():
        im.save(f"{OUT}/Tropas/{key}/{team}_{facing}_{name}.png")
    return args
def portrait(key):
    os.makedirs(f"{OUT}/Cartas", exist_ok=True)
    ext, cy = FRAMING[key]
    im = render(make(key, "azul", IDLE, math.radians(-25)), size=256, extent=ext * 0.85, center=(0, cy, 0))
    im.crop(im.getbbox()).save(f"{OUT}/Cartas/{key}.png")
    return key
if __name__ == "__main__":
    jobs = [(k, t, f) for k in KEYS for t in ("azul", "vermelho") for f in ("frente", "costas")]
    with Pool(2) as pool:
        for r in pool.imap_unordered(job, jobs): print("quadros", r, flush=True)
        for r in pool.imap_unordered(portrait, KEYS): print("retrato", r, flush=True)
    ROWS = [("azul", "frente"), ("azul", "costas"), ("vermelho", "frente"), ("vermelho", "costas")]
    COLS = ["idle", "walk0", "walk1", "walk2", "walk3", "atk0", "atk1"]
    os.makedirs(f"{OUT}/Folhas", exist_ok=True)
    for k in KEYS:
        sheet = Image.new("RGBA", (SIZE * 7, SIZE * 4), (0, 0, 0, 0))
        for r, (t, f) in enumerate(ROWS):
            for c, n in enumerate(COLS):
                sheet.alpha_composite(Image.open(f"{OUT}/Tropas/{k}/{t}_{f}_{n}.png"), (c * SIZE, r * SIZE))
        sheet.save(f"{OUT}/Folhas/{k}.png", optimize=True)
    print("FIM", flush=True)
