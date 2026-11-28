using System.Collections.Generic;
using UnityEngine;

// Aba Cartas (deck, evolução, EVO) e Livro de cartas.
public partial class Lobby
{
    // ------------------------------------------------------------ Aba Cartas (montar deck e evoluir)

    string picked;          // carta da coleção escolhida para entrar no deck
    Vector2 cardScroll;

    void DrawCards(Rect body, float u, int fs)
    {
        float avg = 0f;
        foreach (var k in Profile.Deck) avg += Profile.Find(k).cost;
        avg /= Mathf.Max(1, Profile.Deck.Count);
        Ui.Text(new Rect(body.x + u, body.y, body.width * 0.3f, u * 4f), "Deck", fs + 2, Color.white, TextAnchor.MiddleLeft);
        for (int d = 0; d < 3; d++)
        {
            var b = new Rect(body.x + body.width * 0.17f + d * u * 6f, body.y + u * 0.3f, u * 5.2f, u * 3.6f);
            bool act = d == Profile.ActiveDeck;
            if (Ui.Button(b, act ? Ui.Orange : new Color(0.25f, 0.3f, 0.42f), (d + 1).ToString(), fs - 1) && !act)
            {
                Profile.ActiveDeck = d;
                picked = null;
                Profile.Save();
            }
        }
        Ui.Text(new Rect(body.x, body.y, body.width - u, u * 4f), $"Custo médio: {avg:0.0}", fs - 2, Ui.Elixir, TextAnchor.MiddleRight);

        float cw = (body.width - u * 5f) / 4f, ch = cw * 1.22f, tileH = ch + u * 2.6f;
        for (int i = 0; i < Profile.Deck.Count; i++)
        {
            var c = Profile.Find(Profile.Deck[i]);
            var r = new Rect(body.x + u + (i % 4) * (cw + u), body.y + u * 4.5f + (i / 4) * (tileH + u * 0.6f), cw, ch);
            if (picked != null)
            {
                float pulse = Mathf.PingPong(Time.time * 2f, 1f);
                Ui.Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Color.Lerp(Ui.Orange, Color.yellow, pulse));
            }
            DrawCollectionCard(r, c, u, fs, true);
            if (GUI.Button(new Rect(r.x, r.y, r.width, tileH), GUIContent.none, GUIStyle.none))
            {
                if (picked != null && Profile.Find(picked).champion && Profile.ChampionInDeck(Profile.Deck, i))
                {
                    Sfx.Play("erro");
                    game.Toast("Só 1 Campeão por deck: troque pelo Campeão atual");
                }
                else if (picked != null)
                {
                    Profile.Deck[i] = picked;
                    picked = null;
                    Profile.Save();
                    Sfx.Play("carta");
                }
                else cardInfo = c.key;
            }
        }

        float listTop = body.y + u * 5f + 2f * (tileH + u * 0.6f);
        string hint = picked != null ? $"Toque numa carta do deck para trocar por {Profile.Find(picked).name}"
                                     : "Toque numa carta para ver detalhes, evoluir ou usar no deck";
        Ui.Text(new Rect(body.x, listTop, body.width, u * 3.5f), hint, fs - 4, picked != null ? Color.yellow : new Color(0.8f, 0.9f, 1f),
            TextAnchor.MiddleCenter, false);

        var view = new Rect(body.x, listTop + u * 4f, body.width, body.yMax - listTop - u * 4f);
        var owned = new List<CardData>();
        var findable = new List<CardData>();
        var locked = new List<CardData>();
        foreach (var c in game.Deck)
        {
            if (Profile.Deck.Contains(c.key)) continue;
            if (Profile.Owns(c.key)) owned.Add(c);
            else if (Profile.CanDrop(c)) findable.Add(c);
            else locked.Add(c);
        }

        float rowH = tileH + u * 1.2f;
        float contentH = 0f;
        foreach (var list in new[] { owned, findable, locked })
            if (list.Count > 0) contentH += u * 4.5f + Mathf.Ceil(list.Count / 4f) * rowH;
        contentH = Mathf.Max(contentH, view.height);

        Event ev = Event.current;
        if (view.Contains(ev.mousePosition) && GUI.enabled)
        {
            if (ev.type == EventType.MouseDrag) { cardScroll.y -= ev.delta.y; ev.Use(); }
            else if (ev.type == EventType.ScrollWheel) { cardScroll.y += ev.delta.y * u * 2f; ev.Use(); }
        }
        cardScroll.y = Mathf.Clamp(cardScroll.y, 0f, Mathf.Max(0f, contentH - view.height));
        cardScroll = GUI.BeginScrollView(view, cardScroll, new Rect(0, 0, view.width - 1f, contentH), false, false, GUIStyle.none, GUIStyle.none);

        float y = 0f;
        y = DrawSection("Coleção", owned, y, view.width, cw, ch, tileH, rowH, u, fs, 0);
        y = DrawSection("Encontre em baús", findable, y, view.width, cw, ch, tileH, rowH, u, fs, 1);
        DrawSection("Bloqueadas", locked, y, view.width, cw, ch, tileH, rowH, u, fs, 2);
        GUI.EndScrollView();
    }

    // state: 0 = sua, 1 = desbloqueada mas não encontrada, 2 = bloqueada
    float DrawSection(string title, List<CardData> list, float y, float width,
        float cw, float ch, float tileH, float rowH, float u, int fs, int state)
    {
        if (list.Count == 0) return y;
        Ui.Text(new Rect(u, y, width, u * 4f), title, fs, Color.white, TextAnchor.MiddleLeft);
        y += u * 4.5f;
        for (int i = 0; i < list.Count; i++)
        {
            var c = list[i];
            var r = new Rect(u + (i % 4) * (cw + u), y + (i / 4) * rowH, cw, ch);
            if (state == 0 && picked == c.key)
                Ui.Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Color.yellow);
            DrawCollectionCard(r, c, u, fs, state == 0);
            if (state == 1)
            {
                Ui.Fill(r, new Color(0.05f, 0.08f, 0.15f, 0.55f));
                Ui.Text(r, "?", fs * 2, new Color(1f, 1f, 1f, 0.8f));
            }
            else if (state == 2)
            {
                Ui.Fill(r, new Color(0f, 0f, 0f, 0.7f));
                Ui.Text(r, $"Arena {c.unlockArena + 1}", fs - 2, Color.white);
            }
            if (GUI.Button(new Rect(r.x, r.y, r.width, tileH), GUIContent.none, GUIStyle.none))
            {
                if (state == 0)
                {
                    if (picked == c.key) picked = null;
                    else cardInfo = c.key;
                }
                else if (state == 1) game.Toast($"{c.name}: pode cair de qualquer baú!");
                else game.Toast($"{c.name}: chegue na {Profile.ArenaNames[c.unlockArena]} para desbloquear");
            }
        }
        return y + Mathf.Ceil(list.Count / 4f) * rowH;
    }

    static void DrawCollectionCard(Rect r, CardData c, float u, int fs, bool owned)
    {
        ArenaGame.DrawCardFace(r, c, Mathf.RoundToInt(r.width / 6.5f));
        var cost = new Rect(r.x - u * 0.5f, r.y - u * 0.5f, u * 4f, u * 4f);
        Ui.Disc(cost, Ui.Elixir);
        Ui.Text(cost, c.cost.ToString(), fs - 1, Color.white);
        if (!owned) return;

        var lvl = new Rect(r.xMax - u * 6f, r.y + u * 0.3f, u * 5.6f, u * 2.6f);
        Ui.Fill(lvl, new Color(0f, 0f, 0f, 0.6f));
        Ui.Text(lvl, "Nv " + Profile.CardLevel(c.key), fs - 5, Color.white);

        // barra de cópias para evoluir
        var bar = new Rect(r.x, r.yMax + u * 0.4f, r.width, u * 2.2f);
        if (Profile.CardLevel(c.key) >= Profile.MaxCardLevel)
        {
            Ui.Bar(bar, 1f, Ui.Gold);
            Ui.Text(bar, "MÁX", fs - 6, Color.white);
            return;
        }
        bool ready = Profile.HasCopiesToUpgrade(c.key);
        Color fill = ready ? Color.Lerp(new Color(0.3f, 0.85f, 0.35f), new Color(0.6f, 1f, 0.6f), Mathf.PingPong(Time.time, 0.5f))
                           : new Color(0.3f, 0.6f, 1f);
        Ui.Bar(bar, (float)Profile.CardCount(c.key) / Profile.CopiesNeeded(c.key), fill);
        Ui.Text(bar, ready ? "EVOLUIR!" : $"{Profile.CardCount(c.key)}/{Profile.CopiesNeeded(c.key)}", fs - 6, Color.white);
    }

    // Painel de detalhes da carta: atributos, evolução e "usar no deck"
    void DrawCardInfo(Rect col, float u, int fs)
    {
        var c = Profile.Find(cardInfo);
        Ui.Fill(col, new Color(0f, 0f, 0f, 0.7f));
        var p = new Rect(col.x + u * 2f, col.y + col.height * 0.1f, col.width - u * 4f, col.height * 0.74f);
        Ui.Panel(p, new Color(0.13f, 0.25f, 0.4f));

        var art = new Rect(p.x + u * 2f, p.y + u * 2f, p.width * 0.36f, p.width * 0.36f * 1.25f);
        ArenaGame.DrawCardFace(art, c, fs - 3);
        int lvl = Profile.CardLevel(c.key);
        float m = Profile.LevelMult(lvl), mNext = Profile.LevelMult(lvl + 1);
        bool canNext = lvl < Profile.MaxCardLevel;

        var info = new Rect(art.xMax + u * 2f, art.y, p.xMax - art.xMax - u * 4f, art.height);
        Ui.Text(new Rect(info.x, info.y, info.width, u * 4.5f), c.name, fs + 3, Color.white, TextAnchor.MiddleLeft);
        Ui.Text(new Rect(info.x, info.y + u * 4.5f, info.width, u * 3.5f), $"Nível {lvl}" + (canNext ? "" : " (máximo)") + "  ·  " +
            CardData.RarityNames[c.rarity], fs - 1, CardData.RarityColors[c.rarity], TextAnchor.MiddleLeft);
        Ui.Text(new Rect(info.x, info.y + u * 8.5f, info.width, info.height - u * 8.5f), c.description, fs - 3,
            new Color(0.85f, 0.92f, 1f), TextAnchor.UpperLeft, false);

        string Stat(string name, float baseValue)
        {
            int now = Mathf.RoundToInt(baseValue * m);
            return canNext ? $"{name}: {now}  (+{Mathf.RoundToInt(baseValue * mNext) - now})" : $"{name}: {now}";
        }
        string stats = c.kind == CardKind.Spell
            ? $"{Stat("Dano", c.damage)}\nRaio: {c.spellRadius}\nCusto: {c.cost} de elixir"
            : $"{Stat("Vida", c.hp)}\n" + (c.damage <= 0f ? "Não ataca" : $"{Stat("Dano", c.damage)}\nAlcance: {(c.range > 1.5f ? c.range.ToString("0.#") : "corpo a corpo")}") +
              $"\nQuantidade: x{c.count}   Custo: {c.cost} de elixir" +
              (c.splash > 0 ? "\nDano em área" : "") + (c.targetsTowersOnly ? "\nAtaca só torres e construções" : "") +
              (c.flying ? "\nVoa" : "") + (c.kind == CardKind.Building ? $"\nConstrução: dura {c.lifetime:0}s" : "") +
              (!c.hitsAir && c.damage > 0f ? "\nNão acerta voadores" : "") +
              (c.slows ? "\nDeixa o alvo lento" : "") +
              (c.spawnKey != null ? $"\nSolta 1 {CardData.Extra(c.spawnKey)?.name} a cada {c.spawnEvery:0}s" : "") +
              (c.champion ? $"\nHabilidade: {c.abilityName} ({c.abilityCost} de elixir, recarga {c.abilityCooldown:0}s)\n{c.abilityDesc}" : "") +
              (Profile.HasEvo(c.key) ? $"\nEVO: {Profile.EvoBonus[c.key]}" + (Profile.Evolved(c.key) ? " (ativo)" : "") : "");
        // Livro de cartas: história e personagem animado
        if (Ui.Button(new Rect(p.xMax - u * 12f, p.y + u * 1.2f, u * 10.5f, u * 4.2f), new Color(0.45f, 0.32f, 0.2f), "História", fs - 4))
        {
            bookCard = c.key;
            bookT = 0f;
        }

        // Despertar EVO (Guerreiro, Arqueiras, Escudeiro, Bombardeiro)
        if (Profile.HasEvo(c.key) && Profile.Owns(c.key) && !Profile.Evolved(c.key))
        {
            var eb = new Rect(info.x, art.yMax - u * 6f, info.width, u * 6f);
            bool can = Profile.CanAwaken(c.key);
            string label = Profile.CardLevel(c.key) < Profile.EvoMinLevel
                ? $"EVO no nível {Profile.EvoMinLevel}"
                : $"Despertar EVO\n{Profile.EvoCoins} moedas + {Profile.EvoGems} gemas";
            if (Ui.Button(eb, can ? new Color(0.6f, 0.25f, 0.95f) : new Color(0.35f, 0.3f, 0.45f), label, fs - 5))
            {
                if (Profile.Awaken(c.key)) { Sfx.Play("evoluir"); popup = $"{c.name} despertou a forma EVO!\n\n{Profile.EvoBonus[c.key]}"; }
                else { Sfx.Play("erro"); game.Toast(Profile.CardLevel(c.key) < Profile.EvoMinLevel ? $"Precisa do nível {Profile.EvoMinLevel}" : "Moedas ou gemas insuficientes"); }
            }
        }

        var st = new Rect(p.x + u * 2f, art.yMax + u * 1.5f, p.width - u * 4f, u * 16f);
        Ui.Fill(st, new Color(0f, 0f, 0f, 0.25f));
        Ui.Text(new Rect(st.x + u, st.y + u * 0.5f, st.width - u * 2f, st.height - u), stats, fs - 2, Color.white, TextAnchor.UpperLeft, false);

        var bar = new Rect(p.x + u * 2f, st.yMax + u * 1.2f, p.width - u * 4f, u * 3f);
        if (canNext)
        {
            Ui.Bar(bar, (float)Profile.CardCount(c.key) / Profile.CopiesNeeded(c.key), new Color(0.3f, 0.75f, 0.4f));
            Ui.Text(bar, $"Cópias: {Profile.CardCount(c.key)}/{Profile.CopiesNeeded(c.key)}", fs - 3, Color.white);
        }

        // Maestria: vitórias com esta carta no deck
        if (Profile.Owns(c.key))
        {
            int mt = Profile.MasteryClaimedTier(c.key), wins = Profile.CardWins(c.key);
            var mr = new Rect(p.x + u * 2f, bar.yMax + u * 1.2f, p.width - u * 4f, u * 3f);
            if (mt < Profile.MasteryGoals.Length)
            {
                int goal = Profile.MasteryGoals[mt];
                Ui.Bar(mr, (float)wins / goal, Ui.Gold);
                Ui.Text(mr, $"Maestria {mt}/3: {Mathf.Min(wins, goal)}/{goal} vitórias  ·  {Profile.MasteryRewards[mt]}", fs - 5, Color.white);
                if (Profile.MasteryReady(c.key) &&
                    Ui.Button(new Rect(mr.xMax - u * 12f, mr.y - u * 0.3f, u * 12f, u * 3.6f), Ui.Orange, "Pegar", fs - 4))
                {
                    string got = Profile.ClaimMastery(c.key);
                    Sfx.Play("moeda");
                    if (got != null) game.Toast($"Maestria: +{got}");
                }
            }
            else Ui.Text(mr, $"Maestria completa!  ({wins} vitórias)", fs - 4, Ui.Gold);
        }

        float bw = (p.width - u * 8f) / 3f;
        var by = p.yMax - u * 9f;
        if (canNext)
        {
            bool ok = Profile.CanUpgrade(c.key);
            var b = new Rect(p.x + u * 2f, by, bw, u * 7f);
            if (Ui.Button(b, ok ? new Color(0.2f, 0.65f, 0.3f) : new Color(0.4f, 0.4f, 0.45f), $"Evoluir\n{Profile.CoinsNeeded(c.key)} moedas", fs - 4))
            {
                if (Profile.Upgrade(c.key))
                {
                    Sfx.Play("evoluir");
                    Profile.Track("evoluir");
                    Profile.Save();
                    game.Toast($"{c.name} subiu para o nível {Profile.CardLevel(c.key)}!");
                }
                else
                {
                    Sfx.Play("erro");
                    game.Toast(Profile.HasCopiesToUpgrade(c.key) ? "Moedas insuficientes" : "Faltam cópias (ganhe em baús e na loja)");
                }
            }
        }
        if (!Profile.Deck.Contains(c.key))
        {
            var b = new Rect(p.x + u * 4f + bw, by, bw, u * 7f);
            if (Ui.Button(b, Ui.Orange, "Usar", fs))
            {
                picked = c.key;
                cardInfo = null;
                return;
            }
        }
        var close = new Rect(p.xMax - u * 2f - bw, by, bw, u * 7f);
        if (Ui.Button(close, new Color(0.35f, 0.38f, 0.45f), "Fechar", fs)) cardInfo = null;
    }

    // ------------------------------------------------------------ Livro de cartas (história + personagem animado)

    static readonly Dictionary<string, string> Lore = new Dictionary<string, string>
    {
        { "gue", "Treinou na vila do Campo Verde desde criança. Nunca recua de um duelo." },
        { "mur", "Erguida pelos pedreiros do Vale das Pedras em uma única noite. Dizem que ronca quando ninguém olha." },
        { "tgl", "O cristal no topo veio do coração da Fortaleza de Gelo e nunca derrete, nem no verão." },
        { "frn", "Os ferreiros do Vulcão Rugidor acendem esse forno com uma brasa que queima há cem anos." },
        { "arq", "Duas irmãs que competem para ver quem acerta mais alvos por batalha." },
        { "rat", "Três ratinhos curiosos que roubaram um capacete de cada soldado." },
        { "esc", "Carrega um escudo maior que ele mesmo e tem orgulho disso." },
        { "bom", "Fabrica as próprias bombas com pólvora do vulcão. Cuidado com a fumaça!" },
        { "ati", "Atiradora de elite que acerta uma moeda a cem passos." },
        { "fog", "Uma esfera de fogo conjurada pelos magos da torre." },
        { "fle", "Os arqueiros da guarda disparam todos ao mesmo tempo." },
        { "gig", "Gigante gentil que só se irrita quando vê uma torre." },
        { "mag", "Estudou magia por 70 anos e ainda se atrapalha com o chapéu." },
        { "vik", "Chegaram de barco numa tempestade e nunca mais foram embora." },
        { "mor", "Moram nas cavernas do vale e saem ao entardecer." },
        { "can", "Construído pelos ferreiros do Campo Verde para proteger as muralhas." },
        { "gol", "Pedra viva despertada por um raio no Vale das Pedras." },
        { "lan", "Formam uma muralha de lanças que ninguém atravessa." },
        { "rai", "Quando o céu do vale fica roxo, é melhor se esconder." },
        { "dra", "Pequeno, teimoso e muito orgulhoso do próprio fogo." },
        { "lob", "A matilha da Fortaleza de Gelo nunca caça sozinha." },
        { "yet", "Guardião das montanhas geladas. Adora sopa quente." },
        { "fei", "Congela o mar com um sopro e o descongela com outro." },
        { "tbo", "Torre de vigia onde o Bombardeiro passa as noites de guarda." },
        { "sal", "Nasceu dentro de uma pedra de lava e nunca sentiu frio." },
        { "cav", "Ninguém sabe quem está dentro da armadura negra." },
        { "met", "Uma rocha incandescente que cai do céu do vulcão." },
        { "gel", "Um sopro gelado que para o tempo por alguns segundos." },
        { "cur", "Ervas mágicas do vale que fecham qualquer ferida." },
        { "tor", "Um redemoinho que nasce nas planícies e leva tudo consigo." },
        { "aur", "Campeã do torneio real. Sua investida é lenda em todas as arenas." },
        { "ven", "Bruxa dos ventos que conversa com as folhas e protege os amigos." },
    };

    readonly Dictionary<string, Texture2D> bookTex = new Dictionary<string, Texture2D>();

    void DrawBook(Rect col, float u, int fs)
    {
        var c = Profile.Find(bookCard);
        if (Event.current.type == EventType.Repaint) bookT += Time.deltaTime;
        Ui.Fill(col, new Color(0f, 0f, 0f, 0.75f));
        var p = new Rect(col.x + u * 2f, col.y + col.height * 0.08f, col.width - u * 4f, col.height * 0.84f);
        // "página" de livro
        Ui.Fill(new Rect(p.x - 3, p.y - 3, p.width + 6, p.height + 6), new Color(0.35f, 0.22f, 0.12f));
        Ui.Fill(p, new Color(0.96f, 0.91f, 0.8f));
        Ui.Fill(new Rect(p.x, p.y, p.width, u * 7f), Ui.Shade(CardData.RarityColors[c.rarity], 0.8f));
        Ui.Text(new Rect(p.x, p.y, p.width, u * 4.5f), c.name, fs + 4, Color.white);
        Ui.Text(new Rect(p.x, p.y + u * 4f, p.width, u * 3f), CardData.RarityNames[c.rarity] + (Profile.Evolved(c.key) ? "  ·  EVO" : ""),
            fs - 4, Color.white, TextAnchor.MiddleCenter, false);

        // Palco com o personagem animado (gira entre frente e costas e alterna andar/atacar)
        var stage = new Rect(p.x + u * 4f, p.y + u * 9f, p.width - u * 8f, p.height * 0.48f);
        Ui.Fill(stage, new Color(0.55f, 0.75f, 0.45f));
        Ui.Disc(new Rect(stage.center.x - stage.width * 0.28f, stage.yMax - stage.height * 0.2f, stage.width * 0.56f, stage.height * 0.14f),
            new Color(0f, 0f, 0f, 0.2f));
        if (c.kind == CardKind.Spell)
        {
            if (c.Portrait != null)
            {
                float bob = Mathf.Sin(bookT * 3f) * u;
                GUI.DrawTexture(new Rect(stage.x + stage.width * 0.2f, stage.y + stage.height * 0.1f + bob, stage.width * 0.6f, stage.height * 0.75f), c.Portrait, ScaleMode.ScaleToFit);
            }
        }
        else
        {
            if (!bookTex.TryGetValue(c.key, out var tex)) bookTex[c.key] = tex = Resources.Load<Texture2D>("Tropas/" + c.key);
            if (tex != null)
            {
                float cycle = Mathf.Repeat(bookT, 8f);
                int row = cycle < 4f ? 0 : 1;                       // de frente, depois de costas
                bool attack = Mathf.Repeat(bookT, 4f) > 2.8f;
                int colF = attack ? 5 + (int)(bookT * 6f) % 2 : 1 + (int)(bookT * 8f) % 4;
                bool flip = Mathf.Repeat(bookT, 16f) >= 8f;        // vira para o outro lado (efeito de girar)
                var uv = new Rect((flip ? colF + 1 : colF) / 7f, 1f - (row + 1) * 0.25f, (flip ? -1f : 1f) / 7f, 0.25f);
                float s = Mathf.Min(stage.width, stage.height) * 1.05f;
                GUI.DrawTextureWithTexCoords(new Rect(stage.center.x - s / 2f, stage.yMax - s * 0.98f, s, s), tex, uv);
            }
        }

        string lore = Lore.TryGetValue(c.key, out var lt) ? lt : c.description;
        Ui.Text(new Rect(p.x + u * 3f, stage.yMax + u * 2f, p.width - u * 6f, u * 10f), lore, fs - 1, new Color(0.25f, 0.15f, 0.08f), TextAnchor.UpperCenter, false);
        Ui.Text(new Rect(p.x + u * 3f, stage.yMax + u * 12f, p.width - u * 6f, u * 6f),
            $"Custo {c.cost}  ·  Arena {c.unlockArena + 1}" + (c.kind != CardKind.Spell ? $"  ·  Vida {c.hp:0}  ·  Dano {c.damage:0}" : $"  ·  Dano {c.damage:0}"),
            fs - 4, new Color(0.4f, 0.28f, 0.15f), TextAnchor.UpperCenter, false);
        if (Ui.Button(new Rect(p.center.x - p.width * 0.2f, p.yMax - u * 8.5f, p.width * 0.4f, u * 6.5f), new Color(0.45f, 0.32f, 0.2f), "Fechar", fs + 1))
            bookCard = null;
    }
}
