using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Montagem da partida, torres, tropas, feitiços e jogar cartas.
public partial class ArenaGame
{
    // ---------------------------------------------------------------- Partida

    // Buscando adversário -> tela de VS (some aos poucos revelando a arena) -> partida
    float camZoom;          // 0 = normal, 1 = zoom na torre do rei (final dramático)
    Vector2 camFocus;
    bool finale;
    public int CampaignPhase;

    enum Phase { None, Search, Versus }
    Phase phase;
    float phaseT, searchTime;
    MatchMode pendingMode;
    int tipIndex, botTrophies;
    const float VsTime = 3.4f;
    public bool Searching => phase == Phase.Search;

    static readonly string[] Tips =
    {
        "Construções atraem o Gigante e o Golem: use o Canhão para defender.",
        "Tropas corpo a corpo não acertam voadores. Leve Arqueiras ou a Atiradora.",
        "Derrubou uma torre lateral? Agora dá para soltar tropas no bolsão daquele lado.",
        "Cartas comuns em nível alto podem vencer cartas raras em nível baixo.",
        "No último minuto o elixir enche em dobro. Guarde uma jogada forte!",
        "A torre do rei só acorda quando leva dano ou perde uma torre lateral.",
        "Feitiços valem mais quando acertam várias tropas de uma vez."
    };

    public void StartMatch(MatchMode mode = MatchMode.Normal)
    {
        pendingMode = mode;
        phase = Phase.Search;
        phaseT = 0f;
        searchTime = mode == MatchMode.Tutorial ? 1.2f : Random.Range(2.2f, 3.8f);
        tipIndex = Random.Range(0, Tips.Length);
        fade = 1f;
    }

    void BuildMatch(MatchMode mode)
    {
        ClearReplay();
        replayPool.Clear();
        replayRoot = null;
        Time.timeScale = 1f;
        camZoom = 0f;
        finale = false;
        boss = null;
        bossSpawned = false;
        bossT = 0f;
        weatherT = 0f;
        champ[0] = champ[1] = null;
        abilityCd[0] = abilityCd[1] = 0f;
        botEvos.Clear();
        foreach (var k in Profile.EvoCards) if (Random.value < 0.15f + 0.1f * Profile.ArenaIndex) botEvos.Add(k);
        matchArena = Profile.ArenaIndex;
        if (mode == MatchMode.Campanha) matchArena = Mathf.Min(3, CampaignPhase / 3);
        ruleT = 0f;
        particles.Clear();
        botSkin = Random.value < 0.35f ? Profile.SkinIds[Random.Range(1, Profile.SkinIds.Length)] : "";
        Mode = mode;
        fade = 0f;
        dragSlot = -1;
        emoteMenu = false;
        confetti.Clear();
        for (int t = 0; t < 2; t++) emoteTime[t] = 0f;
        botEmoteDelay = -1f;
        if (matchRoot != null) Destroy(matchRoot.gameObject);
        StopAllCoroutines();
        entities.Clear();
        matchRoot = new GameObject("Partida").transform;

        BuildArena(matchArena);

        for (int team = 0; team < 2; team++)
        {
            float y = team == 0 ? -1f : 1f;
            SpawnTower(team, new Vector2(-LaneX, 6.5f * y), false);
            SpawnTower(team, new Vector2(LaneX, 6.5f * y), false);
            SpawnTower(team, new Vector2(0f, 8.4f * y), true);

            elixir[team] = 5f;
            crowns[team] = 0;
            hand[team].Clear();
            queue[team].Clear();

            // Você joga com o seu deck. O bot monta um deck com cartas da sua arena (luta justa).
            var order = new List<int>();
            for (int i = 0; i < deck.Length; i++)
            {
                bool inArena = deck[i].unlockArena <= Profile.HighestArena;
                bool ok;
                switch (mode)
                {
                    case MatchMode.DeckAleatorio: ok = inArena; break;
                    case MatchMode.SoDeLonge: ok = inArena && IsRangedCard(deck[i]); break;
                    default: ok = team == 0 ? Profile.Deck.Contains(deck[i].key) : inArena; break;
                }
                if (ok) order.Add(i);
            }
            // Completa com qualquer carta da arena se faltar (ex.: poucas cartas de longe no começo)
            for (int i = 0; order.Count < 8 && i < deck.Length; i++)
                if (!order.Contains(i) && deck[i].unlockArena <= Profile.HighestArena) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            if (mode == MatchMode.Tutorial && team == 0)
            {
                // mão combinada para o tutorial: Guerreiro, Arqueiras, Bola de Fogo, Escudeiro
                string[] first = { "gue", "arq", "fog", "esc" };
                order.Clear();
                foreach (var k in first) for (int i = 0; i < deck.Length; i++) if (deck[i].key == k) order.Add(i);
                for (int i = 0; i < deck.Length && order.Count < 8; i++)
                    if (deck[i].starter && !order.Contains(i)) order.Add(i);
            }
            if (order.Count > 8) order.RemoveRange(8, order.Count - 8);
            for (int i = 0; i < order.Count; i++)
            {
                if (i < 4) hand[team].Add(order[i]);
                else queue[team].Enqueue(order[i]);
            }
        }

        var persona = Bots[Random.Range(0, Bots.Length)];
        botName = mode == MatchMode.Tutorial ? "Boneco de Treino"
            : mode == MatchMode.Campanha ? "Guardião de " + Bosses[CampaignPhase].name
            : persona.name;
        botStyle = mode == MatchMode.Tutorial || mode == MatchMode.Campanha || mode == MatchMode.Treino ? BotStyle.Equilibrado : persona.style;
        botMotto = mode == MatchMode.Tutorial || mode == MatchMode.Campanha ? null : persona.motto;
        tutStep = mode == MatchMode.Tutorial ? 0 : -1;
        tutT = 0f;
        timeLeft = MatchTime;
        selected = -1;
        resultText = null;
        rewardText = null;
        aiThink = 1.5f;
        overtime = false;
        lastMinuteAnnounced = false;
        banner = null;
        bannerTime = 0f;
        crownFlies.Clear();
        for (int t = 0; t < 2; t++) for (int l = 0; l < 2; l++) princessDead[t, l] = false;
        botLevel = Mathf.Max(1f, Profile.DeckAverageLevel() + Random.Range(-0.3f, 0.3f));
        if (mode == MatchMode.Campanha) botLevel = 1f + CampaignPhase * 0.45f;

        // Prévia (fantasma) da carta
        ghost = new GameObject("Previa");
        ghost.transform.SetParent(matchRoot);
        ghostSr = ghost.AddComponent<SpriteRenderer>();
        ghostSr.sortingOrder = 2800;
        ghost.SetActive(false);

        // Contagem 3, 2, 1, BATALHA!
        countdown = 0f;
        phase = Phase.Versus;
        phaseT = 0f;
        botTrophies = Mathf.Max(0, Profile.Trophies + Random.Range(-40, 41));
        lastBeep = 99;
        Running = false;
        InBattle = true;
        Sfx.Music("musica_batalha");
        Sfx.Play("batalha", 0.8f);
    }

    public static bool IsRangedCard(CardData c) =>
        c.kind != CardKind.Troop || c.range > 1.5f;

    public void Vibrate()
    {
        if (Time.time - lastVibrate < 0.4f) return;
        lastVibrate = Time.time;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    void ReturnToLobby()
    {
        replaying = false;
        replayHidden.Clear();
        if (pendingChest != null) { lobby.ShowChest(pendingChest); pendingChest = null; }
        Time.timeScale = 1f;
        camZoom = 0f;
        finale = false;
        particles.Clear();
        phase = Phase.None;
        fade = 1f;
        confetti.Clear();
        Sfx.Music("musica_lobby");
        if (matchRoot != null) Destroy(matchRoot.gameObject);
        matchRoot = null;
        entities.Clear();
        InBattle = false;
        Running = false;
    }

    void BuildArena(int arena)
    {
        // Fundo desenhado (Assets/Resources/Batalha/arenaN.png, 80 px por unidade)
        var tex = Resources.Load<Texture2D>("Batalha/arena" + arena);
        if (tex != null)
        {
            var bg = new GameObject("FundoArena");
            bg.transform.SetParent(matchRoot);
            var sr = bg.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 80f);
            sr.sortingOrder = -1000;
            deployZone = MakeRect(new Vector2(0f, -5.25f), new Vector2(ArenaHalfW * 2f, 8.5f), new Color(1f, 1f, 1f, 0.15f), -900);
            deployZone.enabled = false;
            MakePockets();
            return;
        }
        BuildSimpleArena(arena);
    }

    // Áreas extras (bolsões) liberadas quando você derruba uma torre lateral do inimigo
    // Lado inimigo fica avermelhado enquanto você segura uma tropa (onde NÃO dá para soltar)
    readonly SpriteRenderer[] forbidFar = new SpriteRenderer[2], forbidNear = new SpriteRenderer[2];

    void MakeForbidden()
    {
        var red = new Color(1f, 0.25f, 0.2f, 0.22f);
        for (int lane = 0; lane < 2; lane++)
        {
            float x = lane == 0 ? -ArenaHalfW / 2f : ArenaHalfW / 2f;
            forbidFar[lane] = MakeRect(new Vector2(x, (5f + ArenaHalfH) / 2f), new Vector2(ArenaHalfW, ArenaHalfH - 5f), red, -890);
            forbidNear[lane] = MakeRect(new Vector2(x, 2.5f), new Vector2(ArenaHalfW, 5f), red, -890);
            forbidFar[lane].enabled = forbidNear[lane].enabled = false;
        }
    }

    void MakePockets()
    {
        MakeForbidden();
        for (int lane = 0; lane < 2; lane++)
        {
            pocketZones[lane] = MakeRect(new Vector2(lane == 0 ? -ArenaHalfW / 2f : ArenaHalfW / 2f, 2f),
                new Vector2(ArenaHalfW, 6f), new Color(1f, 1f, 1f, 0.15f), -900);
            pocketZones[lane].enabled = false;
        }
    }

    // Plano B: arena feita de quadrados (se a imagem não existir)
    void BuildSimpleArena(int arena)
    {
        Color grass = Profile.ArenaGrass[arena];
        Color grass2 = Ui.Shade(grass, 0.93f);
        var path = new Color(0.80f, 0.67f, 0.47f);
        var wood = new Color(0.45f, 0.30f, 0.18f);

        // Grama quadriculada
        for (int x = -5; x < 5; x++)
        for (int y = -10; y < 10; y++)
        {
            if (y > 9 || y < -10) continue;
            MakeRect(new Vector2(x + 0.5f, y + 0.5f), Vector2.one, ((x + y) & 1) == 0 ? grass : grass2, 0);
        }

        // Caminhos de terra (pistas e ligação entre as torres)
        MakeRect(new Vector2(-LaneX, 0f), new Vector2(1.1f, 15.4f), path, 1);
        MakeRect(new Vector2(LaneX, 0f), new Vector2(1.1f, 15.4f), path, 1);
        MakeRect(new Vector2(0f, -7.7f), new Vector2(LaneX * 2f + 1.1f, 1.1f), path, 1);
        MakeRect(new Vector2(0f, 7.7f), new Vector2(LaneX * 2f + 1.1f, 1.1f), path, 1);

        // Rio e pontes
        MakeRect(Vector2.zero, new Vector2(ArenaHalfW * 2f, 1f), new Color(0.25f, 0.55f, 0.85f), 2);
        MakeRect(new Vector2(0f, 0.42f), new Vector2(ArenaHalfW * 2f, 0.16f), new Color(0.55f, 0.78f, 0.95f), 2);
        MakeRect(new Vector2(-LaneX, 0f), new Vector2(1.4f, 1.4f), wood, 3);
        MakeRect(new Vector2(LaneX, 0f), new Vector2(1.4f, 1.4f), wood, 3);

        // Cerca em volta
        MakeRect(new Vector2(0f, ArenaHalfH + 0.4f), new Vector2(ArenaHalfW * 2f + 0.6f, 0.3f), wood, 2);
        MakeRect(new Vector2(0f, -ArenaHalfH - 0.4f), new Vector2(ArenaHalfW * 2f + 0.6f, 0.3f), wood, 2);
        MakeRect(new Vector2(-ArenaHalfW - 0.15f, 0f), new Vector2(0.3f, ArenaHalfH * 2f + 1f), wood, 2);
        MakeRect(new Vector2(ArenaHalfW + 0.15f, 0f), new Vector2(0.3f, ArenaHalfH * 2f + 1f), wood, 2);

        deployZone = MakeRect(new Vector2(0f, -5.25f), new Vector2(ArenaHalfW * 2f, 8.5f), new Color(1f, 1f, 1f, 0.15f), 4);
        deployZone.enabled = false;
    }

    SpriteRenderer MakeRect(Vector2 pos, Vector2 size, Color color, int order)
    {
        var go = new GameObject("Cenario");
        go.transform.SetParent(matchRoot);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SquareSprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    static Color TeamColor(int team, float shade = 1f)
    {
        // Modo daltônico: o inimigo fica laranja (azul x laranja é fácil de distinguir)
        Color c = team == 0 ? new Color(0.25f, 0.5f, 1f) : Profile.ColorBlind ? new Color(1f, 0.6f, 0.1f) : new Color(1f, 0.3f, 0.3f);
        return Ui.Shade(c, shade);
    }

    void SpawnTower(int team, Vector2 pos, bool king)
    {
        var go = new GameObject(king ? "TorreRei" : "Torre");
        go.transform.SetParent(matchRoot);
        go.transform.position = pos;
        var e = go.AddComponent<Entity>();
        e.isTower = true;
        e.isKing = king;
        float tm = Profile.TowerMult(Profile.Level);   // torres mais fortes com o nível de rei
        e.maxHp = (king ? 2400f : 1400f) * tm;
        e.damage = (king ? 60f : 50f) * tm;
        e.range = king ? 6.5f : 5.5f;   // torre lateral defende a própria pista e o centro, não a outra
        e.attackCooldown = king ? 1f : 0.8f;
        e.speed = 0f;
        e.projectile = king ? "bala" : "flecha";
        string color = team == 0 ? "azul" : "vermelho";
        // Torres renderizadas em 3D (enquadramento igual ao de personagens3d.py)
        string skin = team == 0 ? Profile.TowerSkin : botSkin;
        string basePath = $"Batalha/{(king ? "rei" : "torre")}_{color}";
        Sprite art = null;
        if (!string.IsNullOrEmpty(skin)) art = Rendered(basePath + "_" + skin, king ? 2.15f : 1.8f, king ? 1.75f : 1.65f, TowerWorld);
        if (art == null) art = Rendered(basePath, king ? 2.15f : 1.8f, king ? 1.75f : 1.65f, TowerWorld);
        e.level = Profile.Level;
        e.Setup(this, team, king ? 2.2f : 1.6f, art, 0f, TeamColor(team, 0.8f), TeamColor(team));
        entities.Add(e);
    }

    void SpawnTroop(CardData c, int team, Vector2 pos, int forcedLevel = -1)
    {
        for (int i = 0; i < c.count; i++)
        {
            Vector2 p = c.count > 1 ? pos + Random.insideUnitCircle * 0.5f : pos;
            var go = new GameObject(c.name);
            go.transform.SetParent(matchRoot);
            go.transform.position = p;
            var e = go.AddComponent<Entity>();
            float lm = forcedLevel >= 0 ? Profile.LevelMult(forcedLevel) : Profile.LevelMult(team == 0 ? Profile.CardLevel(c.key) : botLevel);
            e.maxHp = c.hp * lm;
            e.damage = c.damage * lm;
            e.range = c.range;
            e.attackCooldown = c.attackCooldown;
            e.speed = c.speed;
            e.targetsTowersOnly = c.targetsTowersOnly;
            e.projectile = c.projectile;
            e.splash = c.splash;
            e.flying = c.flying;
            e.hitsAir = c.hitsAir;
            e.isBuilding = c.kind == CardKind.Building;
            e.lifetime = c.lifetime;
            e.cardKey = c.key;
            if (matchArena == 2 && !e.isBuilding) e.speed *= 1.15f;        // chão de gelo: escorrega mais rápido
            bool evo = team == 0 ? Profile.Evolved(c.key) : botEvos.Contains(c.key);
            if (evo) ApplyEvo(e, c.key);
            if (c.champion) { e.isChampion = true; champ[team] = e; }
            e.healer = c.healer;
            e.stealer = c.stealer;
            if (c.anywhere) { Dust(p, 1.4f); Debris(p, 5, new Color(0.5f, 0.4f, 0.3f)); }   // sai de baixo da terra
            if (e.isBuilding) e.sight = c.range;
            e.slows = c.slows;
            if (c.spawnKey != null) { e.spawnKey = c.spawnKey; e.spawnEvery = c.spawnEvery; e.spawnTimer = 1.2f; }
            e.frames = Frames(c.key, team == 0 ? "azul" : "vermelho");
            e.level = forcedLevel >= 0 ? forcedLevel : team == 0 ? Profile.CardLevel(c.key) : Mathf.RoundToInt(botLevel);
            e.Setup(this, team, c.unitSize, null, 0f, c.color, TeamColor(team), BarHeight(c.key));
            entities.Add(e);
        }
    }

    // Fornalha: solta uma tropa ao lado da construção (mesmo nível da carta que a criou)
    public void SpawnFromBuilding(Entity b)
    {
        var c = CardData.Extra(b.spawnKey);
        if (c == null || matchRoot == null) return;
        Vector2 p = (Vector2)b.transform.position + new Vector2(Random.Range(-0.4f, 0.4f), b.team == 0 ? 0.7f : -0.7f);
        SpawnTroop(c, b.team, p, b.level);
        Sparks(p);
        Sfx.Play("puf", 0.45f);
    }

    // Curandeira: cura as tropas aliadas por perto. Retorna true se curou alguém.
    public bool HealAround(Entity h)
    {
        bool any = false;
        foreach (var o in entities)
        {
            if (o == null || o == h || o.team != h.team || o.isTower || !o.Alive || o.hp >= o.maxHp) continue;
            if (Vector2.Distance(o.transform.position, h.transform.position) > h.range + o.radius) continue;
            o.Heal(h.damage);
            any = true;
        }
        if (any) StartCoroutine(SpellFx(h.transform.position, h.range, new Color(0.45f, 1f, 0.5f)));
        return any;
    }

    // Ladrão: rouba 1 de elixir do adversário
    public void StealElixir(int team, Vector2 pos)
    {
        int other = 1 - team;
        float got = Mathf.Min(1f, elixir[other]);
        elixir[other] -= got;
        elixir[team] = Mathf.Min(ElixirMax, elixir[team] + got);
        if (got > 0f)
        {
            FloatText(pos + Vector2.up * 1.2f, "+1 elixir", new Color(0.95f, 0.5f, 1f), 0.04f);
            Sfx.Play("moeda", 0.6f);
        }
    }

    void ApplyEvo(Entity e, string key)
    {
        e.evolved = true;
        switch (key)
        {
            case "gue": e.maxHp *= 1.3f; e.damage *= 1.15f; break;
            case "arq": e.attackCooldown *= 0.6f; break;
            case "esc": e.maxHp *= 1.5f; break;
            case "bom": e.splash *= 1.4f; e.damage *= 1.1f; break;
        }
    }

    float SpellDamage(CardData c, int team) =>
        c.damage * Profile.LevelMult(team == 0 ? Profile.CardLevel(c.key) : botLevel);

    void CastSpell(CardData c, int team, Vector2 pos)
    {
        if (c.key == "rai") Sfx.Play("raio");
        if (c.key == "fle") Sfx.Play("flecha", 1f, 0.02f);
        switch (c.key)
        {
            case "fle": StartCoroutine(ArrowRain(c, team, pos)); break;
            case "rai": StartCoroutine(Lightning(c, team, pos)); break;
            case "met": StartCoroutine(Meteor(c, team, pos)); break;
            case "gel": FreezeSpell(c, team, pos); break;
            case "cur": StartCoroutine(HealSpell(c, team, pos)); break;
            case "tor": StartCoroutine(TornadoSpell(c, team, pos)); break;
            default: StartCoroutine(Fireball(c, team, pos)); break;
        }
    }

    void DamageArea(int team, Vector2 pos, float radius, float damage, float towerFactor, bool hitAir = true)
    {
        foreach (var e in entities.ToArray())
        {
            if (e == null || e.team == team || !e.Alive) continue;
            if (e.flying && !hitAir) continue;
            if (Vector2.Distance(pos, e.transform.position) <= radius + e.radius)
                e.TakeDamage(e.isTower ? damage * towerFactor : damage);
        }
    }

    // Dano em área de tropas (bombas, magia, gelo, fogo, golpe do Yeti)
    public void Splash(int team, Vector2 pos, float radius, float damage, bool hitAir = true)
    {
        if (matchRoot == null) return;
        if (radius >= 1.1f) Sfx.Play("bomba", 0.5f);
        StartCoroutine(SpellFx(pos, radius, new Color(1f, 0.85f, 0.6f)));
        DamageArea(team, pos, radius, damage, 1f, hitAir);
        if (radius >= 1.1f) Scorch(pos, radius * 0.6f);
    }

    // Gelo: dano leve e congela tudo do inimigo na área
    void FreezeSpell(CardData c, int team, Vector2 pos)
    {
        Sfx.Play("gelo");
        StartCoroutine(SpellFx(pos, c.spellRadius, new Color(0.6f, 0.9f, 1f)));
        float dmg = SpellDamage(c, team);
        foreach (var e in entities.ToArray())
        {
            if (e == null || e.team == team || !e.Alive) continue;
            if (Vector2.Distance(pos, e.transform.position) > c.spellRadius + e.radius) continue;
            e.TakeDamage(e.isTower ? dmg * 0.35f : dmg);
            if (e != null && e.Alive) e.frozen = e.isTower ? 2.5f : 3f;
        }
    }

    // Cura: quatro pulsos verdes que recuperam a vida das suas tropas e construções
    IEnumerator HealSpell(CardData c, int team, Vector2 pos)
    {
        Sfx.Play("magia", 0.7f);
        float total = SpellDamage(c, team);
        for (int i = 0; i < 4; i++)
        {
            StartCoroutine(SpellFx(pos, c.spellRadius, new Color(0.4f, 1f, 0.5f)));
            foreach (var e in entities.ToArray())
            {
                if (e == null || e.team != team || e.isTower || !e.Alive) continue;
                if (Vector2.Distance(pos, e.transform.position) <= c.spellRadius + e.radius) e.Heal(total / 4f);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // Tornado: gira e puxa as tropas inimigas para o centro, com dano pequeno
    IEnumerator TornadoSpell(CardData c, int team, Vector2 pos)
    {
        Sfx.Play("puf", 0.8f);
        var go = new GameObject("Tornado");
        go.transform.SetParent(matchRoot);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CircleSprite;
        sr.color = new Color(0.8f, 0.85f, 1f, 0.3f);
        sr.sortingOrder = 2400;
        go.transform.localScale = new Vector3(c.spellRadius * 2f, c.spellRadius * 1.2f, 1f);
        var swirl = new List<Transform>();
        for (int i = 0; i < 6; i++)
        {
            var s = new GameObject("Giro");
            s.transform.SetParent(matchRoot);
            var ssr = s.AddComponent<SpriteRenderer>();
            ssr.sprite = SquareSprite;
            ssr.color = new Color(1f, 1f, 1f, 0.6f);
            ssr.sortingOrder = 2401;
            s.transform.localScale = new Vector3(0.6f, 0.1f, 1f);
            swirl.Add(s.transform);
        }
        float dur = 1.6f, t = 0f, tick = 0f;
        float dps = SpellDamage(c, team) / dur;
        while (t < dur && go != null)
        {
            t += Time.deltaTime;
            tick += Time.deltaTime;
            for (int i = 0; i < swirl.Count; i++)
            {
                float a = t * 9f + i * Mathf.PI / 3f;
                float r = c.spellRadius * (0.3f + 0.12f * i);
                swirl[i].position = pos + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.6f);
                swirl[i].rotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);
            }
            foreach (var e in entities.ToArray())
            {
                if (e == null || e.team == team || e.isTower || e.isBuilding || !e.Alive) continue;
                Vector2 ep = e.transform.position;
                if (Vector2.Distance(pos, ep) > c.spellRadius + e.radius) continue;
                e.transform.position = Vector2.MoveTowards(ep, pos, 3.2f * Time.deltaTime);
                if (tick >= 0.4f) e.TakeDamage(dps * 0.4f);
            }
            if (tick >= 0.4f) tick = 0f;
            yield return null;
        }
        foreach (var s in swirl) if (s != null) Destroy(s.gameObject);
        if (go != null) Destroy(go);
    }

    // Chuva de flechas: várias flechas caem na área
    IEnumerator ArrowRain(CardData c, int team, Vector2 pos)
    {
        var arrows = new List<Transform>();
        var targets = new List<Vector2>();
        for (int i = 0; i < 16; i++)
        {
            var go = new GameObject("FlechaChuva");
            go.transform.SetParent(matchRoot);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art("Batalha/flecha", new Vector2(0.5f, 0.5f));
            sr.sortingOrder = 2600;
            Vector2 t = pos + Random.insideUnitCircle * c.spellRadius;
            targets.Add(t);
            go.transform.position = t + new Vector2(-1.5f, 6f) + Random.insideUnitCircle * 0.5f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, -76f);
            go.transform.localScale = Vector3.one * 0.8f;
            arrows.Add(go.transform);
        }
        float dur = 0.45f, t0 = 0f;
        var starts = new List<Vector2>();
        foreach (var tr in arrows) starts.Add(tr.position);
        while (t0 < dur)
        {
            t0 += Time.deltaTime;
            for (int i = 0; i < arrows.Count; i++)
            {
                if (arrows[i] == null) yield break;
                float f = Mathf.Clamp01((t0 - i * 0.012f) / (dur - 0.2f));
                arrows[i].position = Vector2.Lerp(starts[i], targets[i], f);
            }
            yield return null;
        }
        foreach (var tr in arrows) if (tr != null) Destroy(tr.gameObject);
        DamageArea(team, pos, c.spellRadius, SpellDamage(c, team), 0.35f);
        Scorch(pos, c.spellRadius * 0.8f);
        Debris(pos, 6, new Color(0.45f, 0.38f, 0.3f));
    }

    // Raio: clarão vertical e dano alto num ponto
    IEnumerator Lightning(CardData c, int team, Vector2 pos)
    {
        var go = new GameObject("Raio");
        go.transform.SetParent(matchRoot);
        var bolts = new List<SpriteRenderer>();
        Vector2 p = pos + Vector2.up * 12f;
        for (int i = 0; i < 8; i++)
        {
            Vector2 q = Vector2.Lerp(pos + Vector2.up * 12f, pos, (i + 1) / 8f) + (i < 7 ? Vector2.right * Random.Range(-0.5f, 0.5f) : Vector2.zero);
            var seg = new GameObject("Seg");
            seg.transform.SetParent(go.transform);
            var sr = seg.AddComponent<SpriteRenderer>();
            sr.sprite = SquareSprite;
            sr.sortingOrder = 2700;
            seg.transform.position = (p + q) / 2f;
            Vector2 d = q - p;
            seg.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            seg.transform.localScale = new Vector3(d.magnitude + 0.1f, 0.22f, 1f);
            bolts.Add(sr);
            p = q;
        }
        shake = 0.3f;
        DamageArea(team, pos, c.spellRadius, SpellDamage(c, team), 0.3f);
        StartCoroutine(SpellFx(pos, c.spellRadius, c.color));
        float t = 0f;
        while (t < 0.35f && go != null)
        {
            t += Time.deltaTime;
            float a = (Mathf.Sin(t * 60f) > 0f ? 1f : 0.6f) * (1f - t / 0.35f);
            foreach (var b in bolts) b.color = new Color(1f, 1f, 0.7f, a);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // Meteoro: cai do céu e explode grande
    IEnumerator Meteor(CardData c, int team, Vector2 pos)
    {
        var go = new GameObject("Meteoro");
        go.transform.SetParent(matchRoot);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art("Batalha/bola_fogo", new Vector2(0.5f, 0.5f));
        if (sr.sprite == null) sr.sprite = CircleSprite;
        sr.sortingOrder = 2500;
        Vector2 from = pos + new Vector2(3f, 12f);
        float dur = 0.9f, t = 0f;
        while (t < dur && go != null)
        {
            t += Time.deltaTime;
            float f = t / dur;
            go.transform.position = Vector2.Lerp(from, pos, f * f);
            go.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 2.6f, f);
            go.transform.Rotate(0f, 0f, 400f * Time.deltaTime);
            yield return null;
        }
        if (go == null) yield break;
        Destroy(go);
        shake = 0.5f;
        Sfx.Play("explosao");
        StartCoroutine(SpellFx(pos, c.spellRadius, c.color));
        StartCoroutine(Puff(pos, 3f));
        DamageArea(team, pos, c.spellRadius, SpellDamage(c, team), 0.35f);
        Scorch(pos, c.spellRadius * 0.8f);
        Debris(pos, 6, new Color(0.45f, 0.38f, 0.3f));
    }

    // A bola de fogo sai da torre do rei, voa em arco e explode no alvo
    IEnumerator Fireball(CardData c, int team, Vector2 pos)
    {
        Vector2 from = new Vector2(0f, team == 0 ? -7.5f : 9.5f);
        var go = new GameObject("BolaDeFogo");
        go.transform.SetParent(matchRoot);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art("Batalha/bola_fogo", new Vector2(0.5f, 0.5f));
        if (sr.sprite == null) { sr.sprite = CircleSprite; sr.color = c.color; }
        sr.sortingOrder = 2500;
        float dur = 0.65f, t = 0f;
        while (t < dur && go != null)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / dur);
            Vector2 p = Vector2.Lerp(from, pos, f) + Vector2.up * Mathf.Sin(f * Mathf.PI) * 2.5f;
            go.transform.position = p;
            go.transform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1.1f, f);
            go.transform.Rotate(0f, 0f, 720f * Time.deltaTime);
            yield return null;
        }
        if (go == null) yield break;
        Destroy(go);

        StartCoroutine(SpellFx(pos, c.spellRadius, c.color));
        Sfx.Play("explosao", 0.85f);
        shake = 0.25f;
        DamageArea(team, pos, c.spellRadius, SpellDamage(c, team), 0.35f);
        Scorch(pos, c.spellRadius * 0.8f);
        Debris(pos, 6, new Color(0.45f, 0.38f, 0.3f));
    }

    IEnumerator SpellFx(Vector2 pos, float radius, Color c)
    {
        var go = new GameObject("Magia");
        go.transform.SetParent(matchRoot);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * radius * 2f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CircleSprite;
        sr.sortingOrder = 2400;
        var core = new GameObject("Nucleo");
        core.transform.SetParent(go.transform, false);
        var cr = core.AddComponent<SpriteRenderer>();
        cr.sprite = CircleSprite;
        cr.sortingOrder = 2401;
        float t = 0f;
        while (t < 0.5f && go != null)
        {
            t += Time.deltaTime;
            float f = t / 0.5f;
            go.transform.localScale = Vector3.one * radius * 2f * Mathf.Lerp(0.4f, 1.1f, Mathf.Sqrt(f));
            sr.color = new Color(c.r, c.g * 0.8f, c.b, 0.75f * (1f - f));
            core.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 0.2f, f);
            cr.color = new Color(1f, 0.95f, 0.6f, 0.9f * (1f - f));
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    public void SpawnProjectile(Entity from, Entity to, float damage, string kind, float splash = 0f)
    {
        var go = new GameObject("Projetil");
        go.transform.SetParent(matchRoot);
        go.transform.position = from.transform.position + Vector3.up * (from.isTower ? 2.2f : 0.6f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2600;
        bool rotate = false;
        float speed = 11f;
        switch (kind)
        {
            case "flecha":
                sr.sprite = Art("Batalha/flecha", new Vector2(0.5f, 0.5f));
                go.transform.localScale = Vector3.one * 0.8f;
                rotate = true;
                speed = 13f;
                break;
            case "magia":
            case "gelo":
            case "fogo":
                sr.sprite = Art("Batalha/" + kind, new Vector2(0.5f, 0.5f));
                go.transform.localScale = Vector3.one * 0.8f;
                speed = 8f;
                break;
            case "bomba":
                sr.sprite = Art("Batalha/bomba", new Vector2(0.5f, 0.5f));
                go.transform.localScale = Vector3.one * 0.7f;
                speed = 7f;
                break;
            default: // bala (atiradora / canhão do rei)
                sr.sprite = CircleSprite;
                sr.color = from.isTower ? new Color(0.2f, 0.2f, 0.24f) : new Color(1f, 0.9f, 0.5f);
                go.transform.localScale = Vector3.one * (from.isTower ? 0.32f : 0.16f);
                speed = from.isTower ? 10f : 16f;
                break;
        }
        if (sr.sprite == null) { sr.sprite = CircleSprite; go.transform.localScale = Vector3.one * 0.25f; }
        var pr = go.AddComponent<Projectile>();
        pr.Init(to, damage, rotate, speed, splash, from.team, this);
        if (from.slows) pr.slow = 2.5f;
    }

    public void AttackSound(Entity a)
    {
        switch (a.projectile)
        {
            case "flecha": Sfx.Play("flecha", a.isTower ? 0.3f : 0.5f); break;
            case "bala": Sfx.Play(a.isTower ? "canhao" : "tiro", a.isTower ? 0.45f : 0.55f); break;
            case "magia":
            case "fogo": Sfx.Play("magia", 0.5f); break;
            case "gelo": Sfx.Play("gelo", 0.5f); break;
            case "bomba": break;
            default: Sfx.Play(a.radius >= 0.5f ? "golpe" : "espada", 0.6f); break;
        }
    }

    // Número subindo e sumindo (dano, custo de elixir...)
    public void FloatText(Vector3 pos, string text, Color color, float size)
    {
        if (matchRoot == null) return;
        StartCoroutine(FloatTextRoutine(pos, text, color, size));
    }

    // Número com contorno escuro, "pulo" ao aparecer e subida em arco
    IEnumerator FloatTextRoutine(Vector3 pos, string text, Color color, float size)
    {
        var go = new GameObject("TextoFlutuante");
        go.transform.SetParent(matchRoot);
        Vector3 start = pos + (Vector3)(Random.insideUnitCircle * 0.15f);
        go.transform.position = start;
        TextMesh MakeTm(Transform parent, Vector3 offset, Color c, int order)
        {
            var o = new GameObject("t");
            o.transform.SetParent(parent, false);
            o.transform.localPosition = offset;
            var t = o.AddComponent<TextMesh>();
            t.text = text;
            t.font = LabelFont;
            t.fontSize = 48;
            t.characterSize = size;
            t.fontStyle = FontStyle.Bold;
            t.anchor = TextAnchor.MiddleCenter;
            t.color = c;
            var r = o.GetComponent<MeshRenderer>();
            r.sharedMaterial = LabelFont.material;
            r.sortingOrder = order;
            return t;
        }
        float d = size * 0.9f;
        var outline = new List<TextMesh>();
        foreach (var off in new[] { new Vector3(d, 0), new Vector3(-d, 0), new Vector3(0, d), new Vector3(0, -d), new Vector3(d, -d) })
            outline.Add(MakeTm(go.transform, off, new Color(0.08f, 0.04f, 0.08f, 1f), 2999));
        var tm = MakeTm(go.transform, Vector3.zero, color, 3000);
        float side = Random.Range(-0.4f, 0.4f);
        float t0 = 0f, dur = 0.8f;
        while (t0 < dur && go != null)
        {
            t0 += Time.deltaTime;
            float k = t0 / dur;
            float pop = t0 < 0.12f ? Mathf.Lerp(1.7f, 1f, t0 / 0.12f) : 1f;
            go.transform.localScale = Vector3.one * pop;
            go.transform.position = start + new Vector3(side * k, 1.1f * Mathf.Sin(k * Mathf.PI * 0.5f), 0f);
            float a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            tm.color = new Color(color.r, color.g, color.b, a);
            foreach (var o in outline) o.color = new Color(0.08f, 0.04f, 0.08f, a);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // Fumacinha quando uma tropa morre
    IEnumerator Puff(Vector2 pos, float size)
    {
        var parts = new List<Transform>();
        var srs = new List<SpriteRenderer>();
        for (int i = 0; i < 6; i++)
        {
            var go = new GameObject("Fumaca");
            go.transform.SetParent(matchRoot);
            go.transform.position = pos + Random.insideUnitCircle * 0.2f * size + Vector2.up * 0.3f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CircleSprite;
            sr.sortingOrder = 2300;
            parts.Add(go.transform);
            srs.Add(sr);
        }
        var dirs = new Vector2[parts.Count];
        for (int i = 0; i < dirs.Length; i++) dirs[i] = Random.insideUnitCircle.normalized * Random.Range(0.6f, 1.2f);
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float f = t / 0.5f;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] == null) yield break;
                parts[i].position += (Vector3)(dirs[i] * Time.deltaTime * size);
                parts[i].localScale = Vector3.one * size * Mathf.Lerp(0.3f, 0.6f, f);
                srs[i].color = new Color(0.95f, 0.95f, 0.95f, 0.8f * (1f - f));
            }
            yield return null;
        }
        foreach (var p in parts) if (p != null) Destroy(p.gameObject);
    }

    // ---------------------------------------------------------------- Jogar cartas

    bool TryPlay(int team, int slot, Vector2 pos)
    {
        CardData c = deck[hand[team][slot]];

        if (elixir[team] < c.cost)
        {
            if (team == 0) Toast("Elixir insuficiente");
            return false;
        }

        if (c.champion && champ[team] != null && champ[team].Alive)
        {
            if (team == 0) { Sfx.Play("erro"); Toast("Seu Campeão já está na arena"); }
            return false;
        }

        if (!CanDeploy(team, c, pos))
        {
            if (team == 0)
            {
                Sfx.Play("erro");
                Toast(c.kind == CardKind.Troop ? "Solte tropas no seu lado da arena" : "Fora da arena");
            }
            return false;
        }

        elixir[team] -= c.cost;
        if (team == 0)
        {
            FloatText(pos + Vector2.up * 0.5f, "-" + c.cost, new Color(0.95f, 0.5f, 1f), 0.06f);
            Profile.Track("cartas");
            if (c.kind == CardKind.Spell) Profile.Track("feitico");
        }
        if (c.kind != CardKind.Spell)
        {
            SpawnTroop(c, team, pos);
            Sfx.Play("queda", team == 0 ? 0.8f : 0.5f);
        }
        else CastSpell(c, team, pos);

        if (team == 0 && Mode == MatchMode.Tutorial) TutorialCardPlayed(c);
        queue[team].Enqueue(hand[team][slot]);
        hand[team][slot] = queue[team].Dequeue();
        return true;
    }

    // Onde dá para soltar: seu lado, ou o bolsão da pista onde a torre inimiga já caiu
    bool CanDeploy(int team, CardData c, Vector2 pos)
    {
        bool insideArena = Mathf.Abs(pos.x) <= ArenaHalfW - 0.3f && Mathf.Abs(pos.y) <= ArenaHalfH - 0.3f;
        if (!insideArena) return false;
        if (c.kind == CardKind.Spell || c.anywhere) return true;
        bool ownSide = team == 0 ? pos.y <= -1f : pos.y >= 1f;
        if (c.kind == CardKind.Building) return ownSide && Mathf.Abs(pos.y) <= ArenaHalfH - 1.2f;
        int lane = pos.x < 0f ? 0 : 1;
        bool pocket = princessDead[1 - team, lane] && (team == 0 ? pos.y <= 5f : pos.y >= -5f);
        return ownSide || pocket;
    }

    void Banner(string text)
    {
        banner = text;
        bannerTime = 2.4f;
    }

    public void Toast(string msg)
    {
        toast = msg;
        toastTime = 1.8f;
    }
}
