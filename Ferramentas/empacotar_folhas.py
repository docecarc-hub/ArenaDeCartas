"""Junta os quadros renderizados (out4/Tropas/<key>/*.png) em uma folha por personagem.

Folha: 7 colunas (parado, andar x4, ataque x2) e 4 linhas
(azul de frente, azul de costas, vermelho de frente, vermelho de costas).
Saída: out4/Folhas/<key>.png  ->  copie para Assets/Resources/Tropas/
"""
import os
from PIL import Image

ROWS = [("azul", "frente"), ("azul", "costas"), ("vermelho", "frente"), ("vermelho", "costas")]
COLS = ["idle", "walk0", "walk1", "walk2", "walk3", "atk0", "atk1"]
CELL = 176

os.makedirs("out4/Folhas", exist_ok=True)
for key in sorted(os.listdir("out4/Tropas")):
    if not os.path.isdir(f"out4/Tropas/{key}"):
        continue
    sheet = Image.new("RGBA", (CELL * len(COLS), CELL * len(ROWS)), (0, 0, 0, 0))
    for r, (team, facing) in enumerate(ROWS):
        for c, name in enumerate(COLS):
            sheet.alpha_composite(Image.open(f"out4/Tropas/{key}/{team}_{facing}_{name}.png"), (c * CELL, r * CELL))
    sheet.save(f"out4/Folhas/{key}.png", optimize=True)
    print("folha", key)
