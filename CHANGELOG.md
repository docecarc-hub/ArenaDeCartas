# Histórico de versões

Todas as mudanças importantes do **Arena de Cartas**. Formato inspirado em [Keep a Changelog](https://keepachangelog.com/pt-BR/).

## [1.1.0] — 2026-11-28

### Novidades
- 3 construções novas: **Muralha** (2 de elixir, segura os inimigos), **Torre de Gelo** (deixa o alvo lento)
  e **Fornalha** (solta uma Brasinha a cada 5 s)
- **8 adversários com personalidade**: Equilibrado, Agressivo, Defensivo e Feiticeiro, com nome e frase na tela de VS
- **Replay** da partida na tela de fim (velocidade x1, x2 e x4)
- Menu **Gerar .apk (Android)**

### Ajustes
- Balanceamento: Ratinhos, Vikings, Lobos e Escavador mais fracos; Mago, Bombardeiro, Dragãozinho, Feiticeira,
  Atiradora, Golem, Cavaleiro Negro, Bruxa Ventania e Salamandra mais fortes
- Qualidade baixa (30 FPS e menos partículas), contador de FPS e botão **Rever história**
- Troca suave de música entre lobby, batalha e último minuto
- Introdução: texto mais nítido e imagem sem passar da borda

## [1.0.0] — 2026-11-27

### Visual
- Tela de carregamento com emblema 3D, ícone próprio no `.exe` e história de abertura do *Reino das Cinco Ilhas*
- Importação de imagens nítida (compressão de alta qualidade, mipmaps e filtro trilinear)
- Pós-processamento leve (brilho, vinheta e cores) com opção de desligar em PCs fracos
- Clima em cada arena, final em câmera lenta, números de dano com contorno, lobby animado

### Jogabilidade
- 33 cartas, incluindo voadoras, construções, Campeões com habilidade e tropas especiais
  (Curandeira, Escavador, Ladrão, Balão de Bombas)
- Despertar EVO, regras próprias por arena, arrastar e soltar cartas, emotes
- Modos: Batalha, Treino, Evento do Dia, Desafio Semanal, Campanha com 10 chefes e Tutorial

### Progressão
- Caminho de Troféus, 4 arenas e 4 ligas, temporadas, baús, loja, missões e passe da temporada
- Conquistas, maestria de cartas, prêmio de login diário, 3 decks, visual das torres

### Acessibilidade
- Tamanho do texto, modo daltônico e opção sem tremor de tela

### Técnico
- Código dividido em classes parciais por assunto (`ArenaGame.*.cs`, `Lobby.*.cs`)
- Testes automáticos (EditMode) para cartas, regras, campanha, desafio e maestria
- Menu do editor para gerar o `.exe` e reimportar a arte com nitidez
