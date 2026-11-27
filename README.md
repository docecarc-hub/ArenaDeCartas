# ⚔️ ArenaDeCartas

Jogo de **batalha de cartas em tempo real** feito em **Unity 6.3 LTS (C#)**, inspirado no gênero dos jogos de arena para celular,
com **arte, personagens, sons e músicas 100% originais gerados por código**.

> Projeto pessoal de **Kaio Kelven** — estudante de Engenharia de Software (Anhanguera).

<p align="center">
  <img src="Docs/lobby.png" width="280" alt="Lobby" />
  &nbsp;&nbsp;
  <img src="Docs/batalha.png" width="280" alt="Batalha" />
</p>

## 🎮 O jogo

Dois jogadores, três torres de cada lado, uma arena com duas pistas e um rio.
Você gasta **elixir** para soltar **cartas** (tropas e feitiços); quem derrubar a **torre do rei** vence.

### Funcionalidades

**Batalha**
- 20 cartas: 16 tropas com animação 3D (andar, atacar, de frente e de costas) e 4 feitiços
- Elixir que recarrega (x2 no último minuto, x3 na prorrogação)
- Prorrogação com morte súbita, contagem "3, 2, 1, BATALHA!"
- Prévia da carta ao mirar e área de soltura que cresce ao derrubar uma torre
- Dano em área, projéteis, bola de fogo, chuva de flechas, raio e meteoro
- **IA** que defende com a carta certa, usa feitiços em grupos e ataca pela pista mais fraca —
  e fica mais esperta conforme seus troféus sobem

**Progressão**
- 4 arenas com **Caminho de Troféus** (recompensas a cada 50 troféus)
- Piso de troféus por arena e **temporadas**
- Coleção de cartas: a arena **desbloqueia**, os **baús** entregam
- **Evolução das cartas** com cópias + moedas e **nível de rei** que fortalece as torres
- Baús com tempo de abertura, **baú grátis** a cada 4h e **baú de coroas** diário
- **Loja** com ofertas que mudam todo dia e **missões diárias**
- Abertura de baú animada, montador de deck, ajustes de volume e nome

## 🧱 Arquitetura

| Arquivo | Responsabilidade |
|---|---|
| `ArenaGame.cs` | Fluxo do jogo (lobby → batalha → recompensas), regras da partida, IA, efeitos e HUD |
| `Entity.cs` | Tropas e torres: alvo, movimento pelas pontes, ataque, vida e animação quadro a quadro |
| `Projectile.cs` | Flechas, bolas de magia, bombas e balas |
| `CardData.cs` | Dados de todas as cartas (balanceamento em um só lugar) |
| `Profile.cs` | Progresso salvo: troféus, coleção, deck, níveis, baús, missões, loja, temporada |
| `Lobby.cs` | Telas do lobby: batalha, cartas, loja, missões, ajustes |
| `TrophyRoad.cs` | Caminho de Troféus com rolagem e recompensas |
| `ChestOpening.cs` | Animação de abrir baú |
| `Sfx.cs` | Efeitos sonoros e músicas |
| `Ui.cs` | Kit de interface (painéis, botões, textos) |

A cena não precisa de configuração: o `ArenaGame` se cria sozinho ao apertar **Play**
(`[RuntimeInitializeOnLoadMethod]`) e monta tudo por código.

## 🎨 Arte gerada por código

Os personagens e torres são **modelos 3D renderizados com um raymarcher SDF escrito em Python/numpy**
(`Ferramentas/render3d.py`), com iluminação, sombras, oclusão ambiente e contorno cartoon.
Arenas, ícones, efeitos sonoros e músicas também são sintetizados por scripts — veja `Ferramentas/LEIAME.md`.

## ▶️ Como rodar

1. Abra a pasta no **Unity Hub** com o **Unity 6.3 LTS** (6000.3.x).
2. Abra `Assets/Scenes/SampleScene` e aperte **Play**.
3. Dica: em *Play Maximized* fica no formato de celular, em tela cheia.

## 🗺️ Próximos passos

- Multiplayer online
- Tropas voadoras e construções
- Build para Android
