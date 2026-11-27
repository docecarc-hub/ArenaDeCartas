# Ferramentas de arte e som

Toda a arte e todos os sons do ArenaDeCartas são **gerados por código** (nada foi baixado da internet).
Estes scripts recriam os arquivos que ficam em `Assets/Resources/`.

## Requisitos

Python 3.10+ com:

```
pip install numpy pillow scipy
```

## Scripts

| Script | O que gera | Para onde copiar |
|---|---|---|
| `gerar_arenas.py out` | Ilhas flutuantes do lobby (`arena0..3.png`) | `Resources/Arenas/` |
| `gerar_caminho.py out2` | Plataformas do Caminho de Troféus e ícones (moedas, gemas, baús) | `Resources/Caminho/` e `Resources/Icones/` |
| `gerar_batalha.py out3` | Fundos das arenas de batalha, ruína, flecha, bola de fogo | `Resources/Batalha/` |
| `lote.py` | **Personagens e torres em 3D** (448 quadros de animação + retratos + torres) | veja abaixo |
| `empacotar_folhas.py` | Junta os quadros em uma folha por personagem | `out4/Folhas/*` → `Resources/Tropas/` |
| `icones_feiticos.py` | Ícones dos feitiços e projéteis (bomba, gelo, fogo, magia) | `out4/Cartas` → `Resources/Cartas/`, `out4/Batalha` → `Resources/Batalha/` |
| `gerar_sons.py sons` | 26 efeitos sonoros + 3 músicas | `Resources/Sons/` |

## Como funcionam os personagens 3D

`render3d.py` é um mini renderizador 3D feito com **SDF + raymarching em numpy**:
cada personagem é montado com primitivas (esferas, cápsulas, caixas arredondadas, cones),
iluminado com luz do sol, sombra suave, oclusão ambiente e contorno estilo cartoon.

`personagens3d.py` define o "esqueleto" de cada personagem e as poses de animação
(andar e atacar). Para criar um personagem novo:

1. Escreva uma função `meu_personagem(pose)` que devolve uma lista de `Prim(...)`.
2. Adicione em `ROSTER` com uma chave de 3 letras (ex.: `"dra"`).
3. Se ele for maior/menor que um humano, ajuste `FRAMING` (e o dicionário `Framing` em `ArenaGame.cs`).
4. Rode `python lote.py` e `python empacotar_folhas.py`.
5. Crie a carta em `CardData.All()` com a mesma chave.

O render é pesado (cerca de 2 segundos por quadro em um PC simples). O `lote.py` usa 2 núcleos.
