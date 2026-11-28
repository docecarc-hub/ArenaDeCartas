using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MatchMode { Normal, Treino, ElixirTriplo, DeckAleatorio, SoDeLonge, Tutorial, Campanha, Desafio }

// Fluxo do jogo: Lobby -> Batalha -> Recompensas -> Lobby.
// A classe é dividida em vários arquivos (ArenaGame.*.cs) por assunto.
// Não precisa montar nada na cena: ao apertar Play, este script se cria sozinho.
public partial class ArenaGame : MonoBehaviour
{
    public const float LaneX = 3.6f;
    const float ArenaHalfW = 6f;
    const float ArenaHalfH = 9.5f;
    const float MatchTime = 180f;
    const float ElixirMax = 10f;
    const float ElixirSeconds = 2.8f;   // segundos por 1 de elixir (metade no último minuto)

    // Adversários com personalidade: cada um joga de um jeito
    public enum BotStyle { Equilibrado, Agressivo, Defensivo, Feiticeiro }
    static readonly string[] StyleNames = { "Equilibrado", "Agressivo", "Defensivo", "Feiticeiro" };
    static readonly (string name, BotStyle style, string motto)[] Bots =
    {
        ("Capitão Engrenagem", BotStyle.Equilibrado, "Um pouco de tudo, na hora certa."),
        ("Mestre Parafuso", BotStyle.Equilibrado, "Paciência vence batalhas."),
        ("Ferreiro Brás", BotStyle.Agressivo, "Ataque é a melhor defesa!"),
        ("Lia Ventoforte", BotStyle.Agressivo, "Rápida como o vento."),
        ("Dona Rosa Muralha", BotStyle.Defensivo, "Ninguém passa da minha ponte."),
        ("Velho Tobias", BotStyle.Defensivo, "Espero você errar."),
        ("Mestre Faísca", BotStyle.Feiticeiro, "Fogo, gelo e raio!"),
        ("Bruxinha Nina", BotStyle.Feiticeiro, "Meus feitiços nunca falham."),
    };
    BotStyle botStyle;
    string botMotto;

    public Sprite CircleSprite { get; private set; }
    public Sprite SquareSprite { get; private set; }
    public Font LabelFont { get; private set; }
    public bool Running { get; private set; }
    public bool InBattle { get; private set; }
    public CardData[] Deck => deck;
    public MatchMode Mode { get; private set; }

    public static readonly string[] ModeNames = { "Batalha", "Treino", "Elixir Triplo", "Deck Aleatório", "Só de Longe", "Tutorial", "Campanha", "Desafio Semanal" };
    public static readonly string[] ModeDescriptions =
    {
        "", "Treine contra um bot calmo. Não vale troféus.",
        "O elixir enche três vezes mais rápido a partida inteira.",
        "Os dois lados recebem 8 cartas sorteadas da sua arena.",
        "Só cartas que atacam de longe, feitiços e construções.",
        "Aprenda a jogar com o Capitão Tomás.",
        "Derrote o chefe da fase!",
        "Até 12 vitórias; 3 derrotas encerram o desafio."
    };

    // Arrastar carta, emotes, confete e transições
    int dragSlot = -1;
    bool dragWasSelected;
    Vector2 dragStart;
    static readonly string[] Emotes = { "Boa!", "Uau!", "Hehe", "Obrigado!" };
    bool emoteMenu;
    readonly string[] emoteText = new string[2];
    readonly float[] emoteTime = new float[2];
    float botEmoteDelay = -1f;
    struct Confetto { public Vector2 p, v; public Color c; public float rot, size; }
    readonly List<Confetto> confetti = new List<Confetto>();
    float fade;
    float lastVibrate;

    readonly List<Entity> entities = new List<Entity>();
    Transform matchRoot;
    Camera cam;
    SpriteRenderer deployZone;
    Lobby lobby;

    CardData[] deck;
    readonly float[] elixir = new float[2];
    readonly List<int>[] hand = { new List<int>(), new List<int>() };
    readonly Queue<int>[] queue = { new Queue<int>(), new Queue<int>() };
    readonly int[] crowns = new int[2];
    float timeLeft;
    int selected = -1;

    // Início, avisos e prorrogação
    float countdown;
    int lastBeep;
    bool overtime, lastMinuteAnnounced;
    string banner;
    float bannerTime;

    // Nível das cartas do bot (igual à média do seu deck: luta justa)
    float botLevel = 1f;
    // Campeões em campo, recarga da habilidade e cartas EVO do bot
    readonly Entity[] champ = new Entity[2];
    readonly float[] abilityCd = new float[2];
    readonly HashSet<string> botEvos = new HashSet<string>();
    int matchArena;
    float ruleT;
    string botSkin = "";

    // Torres laterais destruídas [time, pista 0=esquerda 1=direita] -> libera área de soltura
    readonly bool[,] princessDead = new bool[2, 2];
    readonly SpriteRenderer[] pocketZones = new SpriteRenderer[2];

    // Prévia da carta selecionada
    GameObject ghost;
    SpriteRenderer ghostSr;
    Vector2 mouseGui;
    bool mouseInArena;

    // Coroa voando até o placar
    struct CrownFly { public Vector3 from; public int team; public float start; }
    readonly List<CrownFly> crownFlies = new List<CrownFly>();
    Rect crownRectEnemy, crownRectMine;
    string resultText, rewardText, botName;
    // Tela de fim de partida
    int resWinner, resTrophies, resCoins, resXp, resChest = -1;
    bool resChestFull;
    float resultT;
    int crownsShown;
    string campaignMsg;
    public Profile.ChestResult pendingChest;   // baú ganho na campanha: abre ao voltar ao lobby
    float aiThink;
    string toast;
    float toastTime;
    float shake;

    readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    readonly Dictionary<string, CharacterFrames> frameCache = new Dictionary<string, CharacterFrames>();

    // Enquadramento usado ao renderizar os personagens 3D (área e altura do centro, em unidades do modelo).
    // Precisa bater com FRAMING em personagens3d.py.
    static readonly Dictionary<string, Vector2> Framing = new Dictionary<string, Vector2>
    {
        { "gig", new Vector2(2.2f, 1.55f) }, { "gol", new Vector2(2.1f, 1.25f) }, { "yet", new Vector2(1.8f, 1.25f) },
        { "cav", new Vector2(1.5f, 1.05f) }, { "rat", new Vector2(1.0f, 0.45f) }, { "lob", new Vector2(1.25f, 0.65f) },
        { "sal", new Vector2(1.3f, 0.5f) },
        { "dra", new Vector2(1.4f, 0.95f) }, { "mor", new Vector2(0.95f, 0.7f) }, { "can", new Vector2(1.1f, 0.5f) },
        { "tbo", new Vector2(1.45f, 1.05f) },
        { "aur", new Vector2(1.35f, 0.95f) }, { "ven", new Vector2(1.35f, 0.95f) },
        { "cun", new Vector2(1.25f, 0.9f) }, { "esv", new Vector2(1.25f, 0.9f) }, { "lad", new Vector2(1.2f, 0.85f) },
        { "bal", new Vector2(1.75f, 1.45f) },
        { "mur", new Vector2(1.3f, 0.55f) }, { "tgl", new Vector2(1.5f, 1.1f) }, { "frn", new Vector2(1.35f, 0.8f) },
        { "bra", new Vector2(0.85f, 0.45f) },
    };
    static readonly Vector2 DefaultFraming = new Vector2(1.25f, 0.9f);
    const float CamPitchCos = 0.809f;   // cos(36°): inclinação da câmera do render
    const float UnitWorld = 0.85f;      // 1 unidade do modelo = 0,85 unidade da arena
    const float TowerWorld = 1.0f;
    public const bool FrontFacesRight = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<ArenaGame>() == null)
            new GameObject("ArenaGame").AddComponent<ArenaGame>();
    }

    void Awake()
    {
        Profile.Load();
        CircleSprite = Sprite.Create(Ui.CircleTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        SquareSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        LabelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        deck = CardData.All();
        Profile.EnsureCollection(deck);
        lobby = new Lobby(this);
        Sfx.Init(gameObject);
        SetupCamera();
        ApplyQuality();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;   // celular: a tela não apaga no meio da partida
    }

    // ---------------------------------------------------------------- Arte (Assets/Resources)

    // Carrega uma imagem de Resources como Sprite (cache).
    public Sprite Art(string path, Vector2 pivot, float ppu = 100f)
    {
        string key = path + pivot + ppu;
        if (spriteCache.TryGetValue(key, out var s)) return s;
        var tex = Resources.Load<Texture2D>(path);
        s = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu) : null;
        spriteCache[key] = s;
        return s;
    }

    // Sprite renderizado em 3D: pivô nos pés e escala coerente entre personagens.
    Sprite Rendered(string path, float ext, float cy, float worldPerUnit)
    {
        var tex = Resources.Load<Texture2D>(path);
        if (tex == null) return null;
        float pivotY = 0.5f - cy * CamPitchCos / (2f * ext);
        return Art(path, new Vector2(0.5f, pivotY), tex.width / (2f * ext * worldPerUnit));
    }

    // Quadros de animação de um personagem.
    // Folha Resources/Tropas/<key>.png: 7 colunas (parado, andar x4, ataque x2)
    // e 4 linhas (azul de frente, azul de costas, vermelho de frente, vermelho de costas).
    public CharacterFrames Frames(string key, string team)
    {
        string id = key + team;
        if (frameCache.TryGetValue(id, out var f)) return f;
        f = new CharacterFrames();
        frameCache[id] = f;
        var tex = Resources.Load<Texture2D>("Tropas/" + key);
        if (tex == null) return f;

        var fr = Framing.TryGetValue(key, out var v) ? v : DefaultFraming;
        float cell = tex.width / 7f;
        float pivotY = 0.5f - fr.y * CamPitchCos / (2f * fr.x);
        float ppu = cell / (2f * fr.x * UnitWorld);
        int baseRow = team == "azul" ? 0 : 2;
        for (int facing = 0; facing < 2; facing++)
        {
            int row = baseRow + facing;
            float y = tex.height - (row + 1) * cell;     // textura começa embaixo
            Sprite Cut(int col) => Sprite.Create(tex, new Rect(col * cell, y, cell, cell), new Vector2(0.5f, pivotY), ppu);
            f.idle[facing] = Cut(0);
            for (int k = 0; k < 4; k++) f.walk[facing][k] = Cut(1 + k);
            for (int k = 0; k < 2; k++) f.atk[facing][k] = Cut(5 + k);
        }
        return f;
    }

    static float BarHeight(string key)
    {
        var fr = Framing.TryGetValue(key, out var v) ? v : DefaultFraming;
        return 2f * fr.y * CamPitchCos * UnitWorld + 0.12f;
    }

    // ---------------------------------------------------------------- Câmera (formato celular 9:16)

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.20f, 0.14f, 0.10f);
        UpdateCameraRect();
        SetupPostFx();
    }

    void UpdateCameraRect()
    {
        Rect col = Ui.Column();
        cam.rect = new Rect(col.x / Screen.width, 0f, col.width / Screen.width, 1f);
        float aspect = col.width / col.height;
        float size = Mathf.Max(13.4f, 7.6f / aspect);   // cabe a torre do rei inimiga inteira, abaixo do placar
        // Topo da arena (com a torre do rei) no topo da tela, painel de cartas embaixo
        Vector2 jitter = shake > 0f && !Profile.ReduceShake ? Random.insideUnitCircle * shake : Vector2.zero;
        Vector2 basePos = new Vector2(0f, 11.4f - size);
        float z = Mathf.SmoothStep(0f, 1f, camZoom);
        cam.orthographicSize = Mathf.Lerp(size, size * 0.5f, z);
        Vector2 p = Vector2.Lerp(basePos, camFocus + Vector2.down * 2.5f, z);
        cam.transform.position = new Vector3(p.x + jitter.x, p.y + jitter.y, -10f);
    }

    // ---------------------------------------------------------------- Loop

    void Update()
    {
        if (shake > 0f) shake = Mathf.Max(0f, shake - Time.deltaTime);
        UpdateCameraRect();
        TrackFps();
        float dt = Time.deltaTime;
        if (toastTime > 0f) toastTime -= dt;
        if (fade > 0f) fade = Mathf.Max(0f, fade - dt * 2.5f);
        UpdateParticles(dt);
        for (int t = 0; t < 2; t++) if (emoteTime[t] > 0f) emoteTime[t] -= dt;
        if (botEmoteDelay > 0f)
        {
            botEmoteDelay -= dt;
            if (botEmoteDelay <= 0f) ShowEmote(1, Running ? Emotes[Random.Range(0, Emotes.Length)] : (crowns[1] > crowns[0] ? "Hehe" : "Boa!"));
        }
        UpdateConfetti(dt);
        if (bannerTime > 0f) bannerTime -= dt;
        if (replaying) { UpdateReplay(dt); return; }

        if (phase == Phase.Search)
        {
            phaseT += dt;
            if (phaseT >= searchTime) BuildMatch(pendingMode);
            return;
        }
        if (phase == Phase.Versus)
        {
            phaseT += dt;
            if (phaseT >= VsTime)
            {
                phase = Phase.None;
                Running = true;
                if (Mode == MatchMode.Tutorial) SetTutStep(0);
                else if (ArenaRules[matchArena] != null) Banner("REGRA DA ARENA\n" + ArenaRules[matchArena]);
            }
            return;
        }

        // Contagem antes de começar
        if (InBattle && countdown > 0f)
        {
            countdown -= dt;
            int n = Mathf.CeilToInt(countdown - 0.6f);
            if (n != lastBeep && n >= 0)
            {
                lastBeep = n;
                Sfx.Play(n > 0 ? "bip" : "batalha", 1f, 0f, 0f);
            }
            if (countdown <= 0f) Running = true;
            return;
        }
        if (!Running) return;

        if (finale) { RecordReplay(dt); return; }
        UpdateWeather(dt);
        if (Mode == MatchMode.Campanha) UpdateBoss(dt);
        timeLeft -= dt;
        if (!lastMinuteAnnounced && !overtime && timeLeft <= 60f)
        {
            lastMinuteAnnounced = true;
            Banner("ÚLTIMO MINUTO!\nElixir em dobro");
            Sfx.Play("alerta");
            Sfx.Music("musica_final");
        }
        float mult = overtime ? 3f : (timeLeft <= 60f ? 2f : 1f);
        if (Mode == MatchMode.ElixirTriplo) mult = 3f;
        float rate = mult / ElixirSeconds;
        for (int t = 0; t < 2; t++) elixir[t] = Mathf.Min(ElixirMax, elixir[t] + rate * dt);

        if (timeLeft <= 0f)
        {
            if (!overtime && crowns[0] == crowns[1])
            {
                // Empate: prorrogação com morte súbita
                overtime = true;
                timeLeft = 60f;
                Banner("PRORROGAÇÃO!\nA próxima torre decide");
                Sfx.Play("alerta");
            }
            else
            {
                timeLeft = 0f;
                EndMatch(crowns[0] > crowns[1] ? 0 : crowns[1] > crowns[0] ? 1 : -1);
                return;
            }
        }

        UpdateDrag();
        for (int t = 0; t < 2; t++) if (abilityCd[t] > 0f) abilityCd[t] -= dt;
        UpdateArenaRule(dt);
        BotAbility();
        UpdateTutorial(dt);
        UpdateAI(dt);
        RecordReplay(dt);
        bool troopSel = selected >= 0 && deck[hand[0][selected]].kind != CardKind.Spell;
        deployZone.enabled = troopSel;
        for (int l = 0; l < 2; l++)
        {
            if (pocketZones[l] != null) pocketZones[l].enabled = troopSel && princessDead[1, l];
            if (forbidFar[l] != null) forbidFar[l].enabled = troopSel;
            if (forbidNear[l] != null) forbidNear[l].enabled = troopSel && !princessDead[1, l];
        }
        UpdateGhost();
    }

    // Mostra onde a carta vai cair (tropa transparente ou círculo do feitiço)
    void UpdateGhost()
    {
        if (ghost == null) return;
        bool show = Running && selected >= 0 && mouseInArena;
        ghost.SetActive(show);
        if (!show) return;
        CardData c = deck[hand[0][selected]];
        Vector2 w = cam.ScreenToWorldPoint(new Vector3(mouseGui.x, Screen.height - mouseGui.y, 0f));
        ghost.transform.position = w;
        bool ok = CanDeploy(0, c, w);
        if (c.kind == CardKind.Spell)
        {
            ghostSr.sprite = CircleSprite;
            ghost.transform.localScale = Vector3.one * c.spellRadius * 2f;
            ghostSr.color = ok ? new Color(1f, 1f, 1f, 0.25f) : new Color(1f, 0.3f, 0.3f, 0.25f);
        }
        else
        {
            var f = Frames(c.key, "azul");
            ghostSr.sprite = f.idle[1] != null ? f.idle[1] : CircleSprite;
            ghost.transform.localScale = Vector3.one;
            ghostSr.color = ok ? new Color(1f, 1f, 1f, 0.55f) : new Color(1f, 0.35f, 0.35f, 0.45f);
        }
    }

    // ---------------------------------------------------------------- Interface

    void OnGUI()
    {
        Rect col = Ui.Column();
        float u = col.height / 100f;
        int fs = Mathf.RoundToInt(u * 2.3f);

        // Faixas pretas nas laterais (fora da "tela de celular")
        Ui.Fill(new Rect(0, 0, col.x, Screen.height), Color.black);
        Ui.Fill(new Rect(col.xMax, 0, Screen.width - col.xMax, Screen.height), Color.black);

        if (DrawSplash(col, u, fs)) return;
        if (phase == Phase.Search) DrawSearch(col, u, fs);
        else if (InBattle && replaying) DrawReplayHud(col, u, fs);
        else if (InBattle)
        {
            DrawBattleHud(col, u, fs);
            if (phase == Phase.Versus) DrawVersus(col, u, fs);
        }
        else lobby.Draw(col);

        DrawConfetti();
        if (fade > 0f) Ui.Fill(col, new Color(0f, 0f, 0f, fade));
        DrawFps(col, u, fs);

        if (toastTime > 0f)
        {
            var r = new Rect(col.x + col.width * 0.1f, col.y + col.height * 0.62f, col.width * 0.8f, u * 6f);
            Ui.Fill(r, new Color(0f, 0f, 0f, 0.6f));
            Ui.Text(r, toast, fs, Color.white);
        }
    }

    void DrawBattleHud(Rect col, float u, int fs)
    {
        float panelTop = col.yMax - u * 20f;

        // Topo: nome do adversário e tempo
        // Placa do adversário: escudo da arena, nome, modo e troféus
        var enemy = new Rect(col.x + u, col.y + u, col.width * 0.55f, u * 7.2f);
        var shield = new Rect(enemy.x, enemy.y + u * 0.6f, u * 5.4f, u * 6f);
        Ui.Panel(shield, Ui.Shade(Ui.Gold, 0.8f));
        Ui.Text(shield, (Profile.ArenaIndex + 1).ToString(), fs + 1, Color.white);
        Ui.Text(new Rect(shield.xMax + u, enemy.y, enemy.width, u * 2.8f), botName, fs, new Color(1f, 0.45f, 0.75f), TextAnchor.MiddleLeft);
        Ui.Text(new Rect(shield.xMax + u, enemy.y + u * 2.5f, enemy.width, u * 2.4f),
            Mode == MatchMode.Normal ? Profile.ArenaNames[Profile.ArenaIndex] : ModeNames[(int)Mode], fs - 4, Color.white, TextAnchor.MiddleLeft);
        Ui.Disc(new Rect(shield.xMax + u, enemy.y + u * 5.1f, u * 2f, u * 2f), Ui.Gold);
        Ui.Text(new Rect(shield.xMax + u * 3.6f, enemy.y + u * 4.8f, enemy.width, u * 2.6f), botTrophies.ToString(), fs - 3, Color.white, TextAnchor.MiddleLeft);

        var timer = new Rect(col.xMax - col.width * 0.26f - u, col.y + u, col.width * 0.26f, u * 6f);
        Ui.Fill(new Rect(timer.x - 2, timer.y - 2, timer.width + 4, timer.height + 4), new Color(1f, 1f, 1f, 0.5f));
        Ui.Fill(timer, new Color(0.05f, 0.06f, 0.1f, 0.85f));
        int sec = Mathf.CeilToInt(timeLeft);
        bool x2 = timeLeft <= 60f && Running;
        string label = overtime ? "Prorrogação x3" : x2 ? "Elixir x2!" : "Restante:";
        Ui.Text(new Rect(timer.x, timer.y, timer.width, u * 2.2f), label, fs - 5, overtime ? Ui.Orange : x2 ? Ui.Elixir : Color.white, TextAnchor.MiddleCenter, false);
        Ui.Text(new Rect(timer.x, timer.y + u * 2f, timer.width, u * 3.8f), $"{sec / 60}:{sec % 60:00}", fs + 5,
            timeLeft <= 10f && Running ? new Color(1f, 0.4f, 0.4f) : Color.white);

        // Coroas na lateral direita
        crownRectEnemy = new Rect(col.xMax - u * 6f, col.y + col.height * 0.3f, u * 5f, u * 5f);
        crownRectMine = new Rect(col.xMax - u * 6f, col.y + col.height * 0.5f, u * 5f, u * 5f);
        DrawCrowns(crownRectEnemy, crowns[1], TeamColor(1), fs);
        DrawCrowns(crownRectMine, crowns[0], TeamColor(0), fs);

        // Toque/arrasto das cartas: tratado antes dos botões das cartas, que "consomem" o aperto
        hudCol = col;
        hudPanelTop = panelTop;
        HandlePointer(Event.current);

        // Painel de cartas (sem cliques quando a partida não está rodando: a tela de fim fica por cima)
        GUI.enabled = Running;
        Ui.Fill(new Rect(col.x, panelTop, col.width, u * 20f), new Color(0.10f, 0.22f, 0.40f));
        Ui.Fill(new Rect(col.x, panelTop, col.width, u * 0.6f), new Color(0.05f, 0.12f, 0.25f));

        var nextRect = new Rect(col.x + u, panelTop + u * 4f, col.width * 0.12f, u * 8f);
        Ui.Text(new Rect(nextRect.x, panelTop + u * 1f, nextRect.width, u * 3f), "Próxima", fs - 5, Color.white);
        DrawCard(nextRect, deck[queue[0].Peek()], true, false, u, fs - 5);

        float startX = nextRect.xMax + u * 1.5f;
        float gap = u * 0.8f;
        float cardW = (col.xMax - u - startX - gap * 3f) / 4f;
        float cardH = Mathf.Min(u * 12f, cardW * 1.3f);
        for (int i = 0; i < 4; i++)
        {
            var r = new Rect(startX + i * (cardW + gap), panelTop + u * 1.8f, cardW, cardH);
            CardData c = deck[hand[0][i]];
            handRects[i] = r;   // usado em Update para segurar/arrastar a carta
            bool lifted = dragSlot == i && mouseInArena;
            if (!lifted) DrawCard(r, c, elixir[0] >= c.cost, selected == i, u, Mathf.RoundToInt(cardW / 7.5f));
            else Ui.Fill(r, new Color(0f, 0f, 0f, 0.35f));
        }
        hudCol = col;
        hudPanelTop = panelTop;
        GUI.enabled = true;

        // Barra de elixir em 10 segmentos
        float barY = panelTop + u * 15.2f;
        var drop = new Rect(startX - u * 1.5f, barY - u * 0.8f, u * 4.6f, u * 4.6f);
        float segX = drop.xMax + u * 0.5f;
        float segW = (col.xMax - u - segX) / 10f;
        for (int i = 0; i < 10; i++)
        {
            var s = new Rect(segX + i * segW + 1, barY, segW - 2, u * 3f);
            Ui.Fill(s, new Color(0f, 0f, 0f, 0.55f));
            Ui.Fill(new Rect(s.x, s.y, s.width * Mathf.Clamp01(elixir[0] - i), s.height), Ui.Elixir);
        }
        Ui.Disc(drop, Ui.Shade(Ui.Elixir, 0.8f));
        Ui.Text(drop, Mathf.FloorToInt(elixir[0]).ToString(), fs + 1, Color.white);
        Ui.Text(new Rect(drop.x - u, drop.yMax - u * 0.4f, drop.width + u * 2f, u * 2.2f), "Máx.: 10", fs - 6, Color.white, TextAnchor.MiddleCenter, false);
        if (elixir[0] >= ElixirMax - 0.01f && Running)
        {
            float a = 0.6f + 0.4f * Mathf.Sin(Time.time * 5f);
            Ui.Text(new Rect(segX, barY, segW * 10f, u * 3f), "A barra de elixir está cheia!", fs - 4, new Color(1f, 1f, 1f, a));
        }

        // Posição do mouse (para a prévia da carta)
        Event ev = Event.current;
        if (ev.type == EventType.Repaint || ev.type == EventType.MouseMove || ev.type == EventType.MouseDrag)
        {
            mouseGui = ev.mousePosition;
            mouseInArena = col.Contains(mouseGui) && mouseGui.y < panelTop;
        }

        DrawCrownFlies(u, fs);
        DrawEmotes(col, u, fs, panelTop);
        DrawAbilityButton(col, u, fs, panelTop);
        DrawBossBar(col, u, fs);

        // Contagem inicial
        if (countdown > 0f)
        {
            int n = Mathf.CeilToInt(countdown - 0.6f);
            string txt = n > 0 ? n.ToString() : "BATALHA!";
            float pulse = 1f + 0.25f * Mathf.Repeat(countdown - 0.6f, 1f);
            Ui.Fill(new Rect(col.x, col.y + col.height * 0.36f, col.width, u * 14f), new Color(0f, 0f, 0f, 0.35f));
            Ui.Text(new Rect(col.x, col.y + col.height * 0.36f, col.width, u * 14f), txt, Mathf.RoundToInt((fs * 3 + 6) * pulse),
                n > 0 ? Color.white : Ui.Gold);
        }

        // Faixa de aviso (último minuto / prorrogação)
        if (bannerTime > 0f && banner != null)
        {
            float a = Mathf.Clamp01(bannerTime / 0.4f) * Mathf.Clamp01((2.4f - bannerTime) / 0.2f + 0.01f);
            var b = new Rect(col.x, col.y + col.height * 0.4f, col.width, u * 11f);
            Ui.Fill(b, new Color(0.55f, 0.1f, 0.6f, 0.75f * a));
            Ui.Text(b, banner, fs + 4, new Color(1f, 1f, 1f, a));
        }

        // Clique na arena para soltar a carta selecionada
        if (Running && selected >= 0 && ev.type == EventType.MouseDown && ev.button == 0
            && col.Contains(ev.mousePosition) && ev.mousePosition.y < panelTop)
        {
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(ev.mousePosition.x, Screen.height - ev.mousePosition.y, 0f));
            if (TryPlay(0, selected, w)) selected = -1;
            ev.Use();
        }

        if (tutStep >= 0 && resultText == null && phase == Phase.None) DrawTutorial(col, u, fs, panelTop);

        // Tela de fim de partida
        if (!Running && resultText != null) DrawResult(col, u, fs);
    }

}
