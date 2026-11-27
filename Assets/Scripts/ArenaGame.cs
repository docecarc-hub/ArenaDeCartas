using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Fluxo do jogo: Lobby -> Batalha -> Recompensas -> Lobby.
// Não precisa montar nada na cena: ao apertar Play, este script se cria sozinho.
public class ArenaGame : MonoBehaviour
{
    public const float LaneX = 3.6f;
    const float ArenaHalfW = 6f;
    const float ArenaHalfH = 9.5f;
    const float MatchTime = 180f;
    const float ElixirMax = 10f;
    const float ElixirSeconds = 2.8f;   // segundos por 1 de elixir (metade no último minuto)

    static readonly string[] BotNames = { "Robô Treinador", "Bot Aprendiz", "Capitão Engrenagem", "Mestre Parafuso" };

    public Sprite CircleSprite { get; private set; }
    public Sprite SquareSprite { get; private set; }
    public Font LabelFont { get; private set; }
    public bool Running { get; private set; }
    public bool InBattle { get; private set; }
    public CardData[] Deck => deck;

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
    }

    void UpdateCameraRect()
    {
        Rect col = Ui.Column();
        cam.rect = new Rect(col.x / Screen.width, 0f, col.width / Screen.width, 1f);
        float aspect = col.width / col.height;
        float size = Mathf.Max(12.8f, 6.6f / aspect);
        cam.orthographicSize = size;
        // Topo da arena (com a torre do rei) no topo da tela, painel de cartas embaixo
        Vector2 jitter = shake > 0f ? Random.insideUnitCircle * shake : Vector2.zero;
        cam.transform.position = new Vector3(jitter.x, 10.6f - size + jitter.y, -10f);
    }

    // ---------------------------------------------------------------- Partida

    public void StartMatch()
    {
        if (matchRoot != null) Destroy(matchRoot.gameObject);
        StopAllCoroutines();
        entities.Clear();
        matchRoot = new GameObject("Partida").transform;

        BuildArena(Profile.ArenaIndex);

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
                if (team == 0 ? Profile.Deck.Contains(deck[i].key) : deck[i].unlockArena <= Profile.HighestArena)
                    order.Add(i);
            }
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            if (order.Count > 8) order.RemoveRange(8, order.Count - 8);
            for (int i = 0; i < order.Count; i++)
            {
                if (i < 4) hand[team].Add(order[i]);
                else queue[team].Enqueue(order[i]);
            }
        }

        botName = BotNames[Random.Range(0, BotNames.Length)];
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

        // Prévia (fantasma) da carta
        ghost = new GameObject("Previa");
        ghost.transform.SetParent(matchRoot);
        ghostSr = ghost.AddComponent<SpriteRenderer>();
        ghostSr.sortingOrder = 2800;
        ghost.SetActive(false);

        // Contagem 3, 2, 1, BATALHA!
        countdown = 3.6f;
        lastBeep = 99;
        Running = false;
        InBattle = true;
        Sfx.Music("musica_batalha");
    }

    void ReturnToLobby()
    {
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
    void MakePockets()
    {
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
        Color c = team == 0 ? new Color(0.25f, 0.5f, 1f) : new Color(1f, 0.3f, 0.3f);
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
        Sprite art = Rendered($"Batalha/{(king ? "rei" : "torre")}_{color}", king ? 2.15f : 1.8f, king ? 1.75f : 1.65f, TowerWorld);
        e.Setup(this, team, king ? 2.2f : 1.6f, art, 0f, TeamColor(team, 0.8f), TeamColor(team));
        entities.Add(e);
    }

    void SpawnTroop(CardData c, int team, Vector2 pos)
    {
        for (int i = 0; i < c.count; i++)
        {
            Vector2 p = c.count > 1 ? pos + Random.insideUnitCircle * 0.5f : pos;
            var go = new GameObject(c.name);
            go.transform.SetParent(matchRoot);
            go.transform.position = p;
            var e = go.AddComponent<Entity>();
            float lm = Profile.LevelMult(team == 0 ? Profile.CardLevel(c.key) : botLevel);
            e.maxHp = c.hp * lm;
            e.damage = c.damage * lm;
            e.range = c.range;
            e.attackCooldown = c.attackCooldown;
            e.speed = c.speed;
            e.targetsTowersOnly = c.targetsTowersOnly;
            e.projectile = c.projectile;
            e.splash = c.splash;
            e.frames = Frames(c.key, team == 0 ? "azul" : "vermelho");
            e.Setup(this, team, c.unitSize, null, 0f, c.color, TeamColor(team), BarHeight(c.key));
            entities.Add(e);
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
            default: StartCoroutine(Fireball(c, team, pos)); break;
        }
    }

    void DamageArea(int team, Vector2 pos, float radius, float damage, float towerFactor)
    {
        foreach (var e in entities.ToArray())
        {
            if (e == null || e.team == team || !e.Alive) continue;
            if (Vector2.Distance(pos, e.transform.position) <= radius + e.radius)
                e.TakeDamage(e.isTower ? damage * towerFactor : damage);
        }
    }

    // Dano em área de tropas (bombas, magia, gelo, fogo, golpe do Yeti)
    public void Splash(int team, Vector2 pos, float radius, float damage)
    {
        if (matchRoot == null) return;
        if (radius >= 1.1f) Sfx.Play("bomba", 0.5f);
        StartCoroutine(SpellFx(pos, radius, new Color(1f, 0.85f, 0.6f)));
        DamageArea(team, pos, radius, damage, 1f);
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
        go.AddComponent<Projectile>().Init(to, damage, rotate, speed, splash, from.team, this);
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

    IEnumerator FloatTextRoutine(Vector3 pos, string text, Color color, float size)
    {
        var go = new GameObject("TextoFlutuante");
        go.transform.SetParent(matchRoot);
        go.transform.position = pos + (Vector3)(Random.insideUnitCircle * 0.15f);
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = LabelFont;
        tm.fontSize = 48;
        tm.characterSize = size;
        tm.fontStyle = FontStyle.Bold;
        tm.anchor = TextAnchor.MiddleCenter;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = LabelFont.material;
        mr.sortingOrder = 3000;
        float t = 0f;
        while (t < 0.7f && go != null)
        {
            t += Time.deltaTime;
            go.transform.position += Vector3.up * Time.deltaTime * 1.2f;
            tm.color = new Color(color.r, color.g, color.b, 1f - t / 0.7f);
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

    // ---------------------------------------------------------------- Regras usadas pelas tropas

    public Entity FindTarget(Entity a)
    {
        Entity best = null;
        float bestD = float.MaxValue;
        float reach = a.isTower ? a.range : a.sight;
        foreach (var e in entities)
        {
            if (e == null || e.team == a.team || !e.Alive) continue;
            if (a.targetsTowersOnly && !e.isTower) continue;
            float d = Vector2.Distance(a.transform.position, e.transform.position) - e.radius - a.radius;
            if (d <= reach && d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    public Entity NearestEnemyTower(Entity a)
    {
        Entity best = null;
        float bestD = float.MaxValue;
        foreach (var e in entities)
        {
            if (e == null || !e.isTower || e.team == a.team || !e.Alive) continue;
            float d = Vector2.Distance(a.transform.position, e.transform.position);
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    // Faz as tropas atravessarem o rio só pelas pontes.
    public Vector2 PathPoint(Vector2 from, Vector2 to)
    {
        if (Mathf.Sign(from.y) != Mathf.Sign(to.y))
        {
            float lx = from.x < 0f ? -LaneX : LaneX;
            if (Mathf.Abs(from.y) > 0.95f) return new Vector2(lx, Mathf.Sign(from.y) * 0.9f);
            return new Vector2(lx, Mathf.Sign(to.y) * 1.0f);
        }
        return to;
    }

    public bool PrincessLost(int team) => crowns[1 - team] > 0;

    public void OnEntityDied(Entity e)
    {
        entities.Remove(e);
        if (!e.isTower)
        {
            StartCoroutine(Puff(e.transform.position, Mathf.Max(0.8f, e.radius * 2f)));
            Sfx.Play("puf", 0.6f);
            return;
        }
        Sfx.Play("torre");

        // Torre destruída vira ruína
        StartCoroutine(Puff(e.transform.position, 2.5f));
        shake = 0.35f;
        var ruin = new GameObject("Ruina");
        ruin.transform.SetParent(matchRoot);
        ruin.transform.position = e.transform.position + Vector3.down * (e.isKing ? 0.9f : 0.6f);
        var rsr = ruin.AddComponent<SpriteRenderer>();
        rsr.sprite = Art("Batalha/ruina", new Vector2(0.5f, 0.2f));
        rsr.sortingOrder = 1000 - Mathf.RoundToInt(e.transform.position.y * 20f);
        if (rsr.sprite != null) ruin.transform.localScale = Vector3.one * ((e.isKing ? 2.4f : 1.7f) / rsr.sprite.bounds.size.x);

        int other = 1 - e.team;
        if (!e.isKing) princessDead[e.team, e.transform.position.x < 0f ? 0 : 1] = true;
        crowns[other] = e.isKing ? 3 : crowns[other] + 1;
        crownFlies.Add(new CrownFly { from = e.transform.position + Vector3.up * 1.5f, team = other, start = Time.time });
        if (other == 0) Profile.Track("torres");
        // Na prorrogação, a primeira torre derrubada decide
        if (e.isKing || overtime) EndMatch(other);
    }

    void EndMatch(int winner)
    {
        if (!Running) return;
        Running = false;
        selected = -1;
        deployZone.enabled = false;
        resultText = winner == 0 ? "VITÓRIA!" : winner == 1 ? "DERROTA" : "EMPATE";
        if (ghost != null) ghost.SetActive(false);
        foreach (var p in pocketZones) if (p != null) p.enabled = false;
        Sfx.Play(winner == 0 ? "vitoria" : "derrota");

        // Missões, baú de coroas e estatísticas
        Profile.Track("batalhas");
        if (winner == 0) { Profile.Track("vencer"); Profile.WinsTotal++; }
        else if (winner == 1) Profile.LossesTotal++;
        Profile.Track("coroas", crowns[0]);
        Profile.AddCrowns(crowns[0]);

        int trophies = 0, coins = 0, xp;
        string chest = "";
        if (winner == 0)
        {
            trophies = 30;
            coins = 20;
            xp = 20;
            int type = Profile.RandomChestType();
            chest = Profile.TryAddChest(type)
                ? $"\nVocê ganhou um Baú de {Profile.ChestNames[type]}!"
                : "\nEspaços de baú cheios: abra algum no lobby.";
        }
        else if (winner == 1)
        {
            // Nunca cai abaixo do mínimo da arena que você já alcançou
            trophies = -Mathf.Min(15, Profile.Trophies - Profile.ArenaFloor);
            xp = 5;
        }
        else xp = 10;

        Profile.Trophies += trophies;
        Profile.Coins += coins;
        bool levelUp = Profile.AddXp(xp);
        string arenaMsg = Profile.UpdateProgress(deck);
        Profile.Save();

        rewardText = $"Troféus {(trophies >= 0 ? "+" : "")}{trophies}    Moedas +{coins}    XP +{xp}{chest}"
                     + (levelUp ? $"\n\nSUBIU PARA O NÍVEL {Profile.Level}! (+5 gemas)" : "")
                     + (arenaMsg != null ? "\n\n" + arenaMsg : "")
                     + $"\nBaú de coroas: {Profile.CrownsToday}/{Profile.CrownChestGoal}";
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
        if (c.kind == CardKind.Troop)
        {
            SpawnTroop(c, team, pos);
            Sfx.Play("queda", team == 0 ? 0.8f : 0.5f);
        }
        else CastSpell(c, team, pos);

        queue[team].Enqueue(hand[team][slot]);
        hand[team][slot] = queue[team].Dequeue();
        return true;
    }

    // Onde dá para soltar: seu lado, ou o bolsão da pista onde a torre inimiga já caiu
    bool CanDeploy(int team, CardData c, Vector2 pos)
    {
        bool insideArena = Mathf.Abs(pos.x) <= ArenaHalfW - 0.3f && Mathf.Abs(pos.y) <= ArenaHalfH - 0.3f;
        if (!insideArena) return false;
        if (c.kind == CardKind.Spell) return true;
        bool ownSide = team == 0 ? pos.y <= -1f : pos.y >= 1f;
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

    // ---------------------------------------------------------------- Loop

    void Update()
    {
        if (shake > 0f) shake = Mathf.Max(0f, shake - Time.deltaTime);
        UpdateCameraRect();
        float dt = Time.deltaTime;
        if (toastTime > 0f) toastTime -= dt;
        if (bannerTime > 0f) bannerTime -= dt;

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

        timeLeft -= dt;
        if (!lastMinuteAnnounced && !overtime && timeLeft <= 60f)
        {
            lastMinuteAnnounced = true;
            Banner("ÚLTIMO MINUTO!\nElixir em dobro");
            Sfx.Play("alerta");
            Sfx.Music("musica_final");
        }
        float mult = overtime ? 3f : (timeLeft <= 60f ? 2f : 1f);
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

        UpdateAI(dt);
        bool troopSel = selected >= 0 && deck[hand[0][selected]].kind == CardKind.Troop;
        deployZone.enabled = troopSel;
        for (int l = 0; l < 2; l++)
            if (pocketZones[l] != null) pocketZones[l].enabled = troopSel && princessDead[1, l];
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

    // ---------------------------------------------------------------- IA do bot
    // Defende com a carta certa (área contra enxame, dano alto contra tanque), usa feitiços em grupos,
    // junta elixir e ataca pela pista mais fraca com tanque na frente e suporte atrás.
    // Fica mais esperta conforme seus troféus sobem.

    static readonly HashSet<string> TankKeys = new HashSet<string> { "gig", "gol", "yet", "cav", "esc", "gue" };
    Entity aiTank;          // tanque que o bot acabou de jogar (para mandar suporte atrás)

    void UpdateAI(float dt)
    {
        aiThink -= dt;
        if (aiThink > 0f) return;
        float skill = Mathf.Clamp01(Profile.Trophies / 1000f);
        aiThink = Random.Range(1.2f, 2.3f) * (1f - 0.45f * skill);

        // Ameaças: suas tropas no lado do bot ou chegando na ponte
        var threats = new List<Entity>();
        Entity lead = null;
        foreach (var e in entities)
        {
            if (e == null || e.team != 0 || e.isTower || !e.Alive || e.Deploying) continue;
            if (e.transform.position.y < -1.5f) continue;
            threats.Add(e);
            if (lead == null || e.transform.position.y > lead.transform.position.y) lead = e;
        }

        if (TryAISpell(threats, skill)) return;
        if (lead != null) { AIDefend(lead, threats, skill); return; }
        AIAttack(skill);
    }

    // Feitiço quando vale a pena (vários alvos ou muito dano somado)
    bool TryAISpell(List<Entity> threats, float skill)
    {
        if (threats.Count == 0) return false;
        for (int slot = 0; slot < 4; slot++)
        {
            CardData c = deck[hand[1][slot]];
            if (c.kind != CardKind.Spell || c.cost > elixir[1]) continue;
            float dmg = SpellDamage(c, 1);
            foreach (var center in threats)
            {
                int count = 0;
                float value = 0f;
                foreach (var t in threats)
                {
                    if (Vector2.Distance(center.transform.position, t.transform.position) > c.spellRadius) continue;
                    count++;
                    value += Mathf.Min(t.hp, dmg);
                }
                bool worth = count >= 3 || value >= dmg * 1.3f || (value >= c.cost * 120f);
                if (!worth || Random.value > 0.35f + 0.6f * skill) continue;
                // mira um pouco à frente (as tropas estão andando)
                Vector2 aim = (Vector2)center.transform.position + Vector2.up * 0.4f;
                if (TryPlay(1, slot, aim)) return true;
            }
        }
        return false;
    }

    void AIDefend(Entity lead, List<Entity> threats, float skill)
    {
        int near = 0;
        foreach (var t in threats)
            if (Vector2.Distance(t.transform.position, lead.transform.position) < 2.2f) near++;
        bool swarm = near >= 3;
        bool tank = lead.maxHp >= 1400f;

        int best = -1;
        float bestScore = float.MinValue;
        for (int slot = 0; slot < 4; slot++)
        {
            CardData c = deck[hand[1][slot]];
            if (c.kind != CardKind.Troop || c.cost > elixir[1]) continue;
            float score = Random.value * (1.5f - skill);
            if (swarm && c.splash > 0f) score += 2f;
            if (swarm && c.count >= 3) score -= 0.5f;
            if (tank && (c.count >= 3 || c.damage >= 110f)) score += 2f;
            if (c.targetsTowersOnly) score -= 3f;
            score -= c.cost * 0.15f;          // prefere responder barato
            if (score > bestScore) { bestScore = score; best = slot; }
        }
        if (best < 0) return;

        CardData card = deck[hand[1][best]];
        Vector2 lp = lead.transform.position;
        Vector2 pos = card.range > 1.5f
            ? new Vector2(Mathf.Clamp(lp.x * 0.6f, -5.3f, 5.3f), Mathf.Clamp(lp.y + 4f, 2f, 8.6f))     // de longe: atrás
            : new Vector2(Mathf.Clamp(lp.x + Random.Range(-0.5f, 0.5f), -5.3f, 5.3f), Mathf.Clamp(lp.y + 1.8f, 1.2f, 8.6f));
        TryPlay(1, best, pos);
    }

    int WeakestPlayerLane()
    {
        float[] hp = { 0f, 0f };
        bool[] alive = { false, false };
        foreach (var e in entities)
        {
            if (e == null || e.team != 0 || !e.isTower || e.isKing) continue;
            int l = e.transform.position.x < 0f ? 0 : 1;
            hp[l] = e.hp;
            alive[l] = true;
        }
        if (!alive[0] && alive[1]) return 0;
        if (!alive[1] && alive[0]) return 1;
        if (Mathf.Abs(hp[0] - hp[1]) < 100f) return Random.value < 0.5f ? 0 : 1;
        return hp[0] < hp[1] ? 0 : 1;
    }

    void AIAttack(float skill)
    {
        // Suporte atrás do tanque que já está indo
        if (aiTank != null && aiTank.Alive && aiTank.transform.position.y < 6f)
        {
            for (int slot = 0; slot < 4; slot++)
            {
                CardData c = deck[hand[1][slot]];
                if (c.kind != CardKind.Troop || c.cost > elixir[1] || TankKeys.Contains(c.key) || c.targetsTowersOnly) continue;
                Vector2 tp = aiTank.transform.position;
                Vector2 pos = new Vector2(tp.x, Mathf.Clamp(tp.y + 1.6f, 1.2f, 8.6f));
                if (!CanDeploy(1, c, pos)) pos.y = Mathf.Max(pos.y, 1.2f);
                if (TryPlay(1, slot, pos)) { aiTank = null; return; }
            }
        }

        // Junta elixir antes de atacar (bot esperto espera mais)
        float want = Mathf.Lerp(6f, 9f, skill);
        if (elixir[1] < want && elixir[1] < 9.7f) return;

        int lane = WeakestPlayerLane();
        float lx = lane == 0 ? -LaneX : LaneX;
        bool pocket = princessDead[0, lane];

        // Tanque primeiro, lá atrás (vai juntando suporte no caminho)
        for (int slot = 0; slot < 4; slot++)
        {
            CardData c = deck[hand[1][slot]];
            if (c.kind != CardKind.Troop || c.cost > elixir[1] || !(TankKeys.Contains(c.key) || c.targetsTowersOnly)) continue;
            Vector2 pos = pocket ? new Vector2(lx, -3.5f) : new Vector2(lx, Random.value < skill ? 8.3f : 4f);
            int before = entities.Count;
            if (TryPlay(1, slot, pos))
            {
                if (entities.Count > before) aiTank = entities[before];
                return;
            }
        }

        // Sem tanque: manda o que tiver
        for (int slot = 0; slot < 4; slot++)
        {
            CardData c = deck[hand[1][slot]];
            if (c.kind != CardKind.Troop || c.cost > elixir[1]) continue;
            Vector2 pos = pocket ? new Vector2(lx, -3f) : new Vector2(lx, c.range > 1.5f ? 6.5f : 3f);
            if (TryPlay(1, slot, pos)) return;
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

        if (InBattle) DrawBattleHud(col, u, fs);
        else lobby.Draw(col);

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
        var enemy = new Rect(col.x + u, col.y + u, col.width * 0.45f, u * 5f);
        Ui.Fill(enemy, new Color(0f, 0f, 0f, 0.45f));
        Ui.Text(new Rect(enemy.x + u, enemy.y, enemy.width, u * 3f), botName, fs - 1, new Color(1f, 0.6f, 0.6f), TextAnchor.MiddleLeft);
        Ui.Text(new Rect(enemy.x + u, enemy.y + u * 2.6f, enemy.width, u * 2.4f), $"Nível {Profile.Level}", fs - 5, Color.white, TextAnchor.MiddleLeft, false);

        var timer = new Rect(col.xMax - col.width * 0.28f - u, col.y + u, col.width * 0.28f, u * 5f);
        Ui.Fill(timer, new Color(0f, 0f, 0f, 0.45f));
        int sec = Mathf.CeilToInt(timeLeft);
        bool x2 = timeLeft <= 60f && Running;
        string label = overtime ? "Prorrogação x3" : x2 ? "Elixir x2!" : "Tempo";
        Ui.Text(new Rect(timer.x, timer.y, timer.width, u * 2.2f), label, fs - 5, overtime ? Ui.Orange : x2 ? Ui.Elixir : Color.white, TextAnchor.MiddleCenter, false);
        Ui.Text(new Rect(timer.x, timer.y + u * 1.8f, timer.width, u * 3.2f), $"{sec / 60}:{sec % 60:00}", fs + 2, Color.white);

        // Coroas na lateral direita
        crownRectEnemy = new Rect(col.xMax - u * 6f, col.y + col.height * 0.3f, u * 5f, u * 5f);
        crownRectMine = new Rect(col.xMax - u * 6f, col.y + col.height * 0.5f, u * 5f, u * 5f);
        DrawCrowns(crownRectEnemy, crowns[1], TeamColor(1), fs);
        DrawCrowns(crownRectMine, crowns[0], TeamColor(0), fs);

        // Painel de cartas
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
            if (DrawCard(r, c, elixir[0] >= c.cost, selected == i, u, Mathf.RoundToInt(cardW / 7.5f)) && Running)
                selected = selected == i ? -1 : i;
        }

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

        // Posição do mouse (para a prévia da carta)
        Event ev = Event.current;
        if (ev.type == EventType.Repaint || ev.type == EventType.MouseMove || ev.type == EventType.MouseDrag)
        {
            mouseGui = ev.mousePosition;
            mouseInArena = col.Contains(mouseGui) && mouseGui.y < panelTop;
        }

        DrawCrownFlies(u, fs);

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

        // Tela de resultado
        if (!Running && resultText != null)
        {
            Ui.Fill(col, new Color(0f, 0f, 0f, 0.65f));
            Color rc = resultText == "VITÓRIA!" ? Ui.Gold : resultText == "DERROTA" ? new Color(1f, 0.45f, 0.45f) : Color.white;
            Ui.Text(new Rect(col.x, col.height * 0.25f, col.width, u * 8f), resultText, fs * 2 + 4, rc);
            Ui.Text(new Rect(col.x, col.height * 0.34f, col.width, u * 5f), $"Coroas  {crowns[0]}  x  {crowns[1]}", fs + 2, Color.white);
            Ui.Text(new Rect(col.x + u * 3f, col.height * 0.41f, col.width - u * 6f, u * 22f), rewardText, fs - 1, Color.white);
            if (Ui.Button(new Rect(col.center.x - col.width * 0.2f, col.height * 0.66f, col.width * 0.4f, u * 8f), Ui.Orange, "OK", fs + 4))
                ReturnToLobby();
        }
    }

    // Moldura + desenho do personagem + nome. Usado na mão, no lobby e no Caminho de Troféus.
    public static void DrawCardFace(Rect r, CardData c, int fs)
    {
        Ui.Fill(r, new Color(0.08f, 0.10f, 0.16f));
        var inner = new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6);
        Ui.Fill(inner, Ui.Shade(c.color, 0.55f));
        Ui.Fill(new Rect(inner.x, inner.y, inner.width, inner.height * 0.5f), new Color(1f, 1f, 1f, 0.10f));
        var portrait = c.Portrait;
        if (portrait != null)
            GUI.DrawTexture(new Rect(inner.x + 2, inner.y + 2, inner.width - 4, inner.height * 0.78f), portrait, ScaleMode.ScaleToFit);
        else
            Ui.Text(new Rect(inner.x, inner.y, inner.width, inner.height * 0.7f), c.name, fs, Color.white);
        var strip = new Rect(inner.x, inner.yMax - inner.height * 0.22f, inner.width, inner.height * 0.22f);
        Ui.Fill(strip, new Color(0f, 0f, 0f, 0.45f));
        Ui.Text(strip, c.name, Mathf.Max(7, Mathf.RoundToInt(fs * 0.8f)), Color.white);
    }

    // Coroa voa da torre derrubada até o placar
    void DrawCrownFlies(float u, int fs)
    {
        for (int i = crownFlies.Count - 1; i >= 0; i--)
        {
            var f = crownFlies[i];
            float t = (Time.time - f.start) / 0.9f;
            if (t >= 1f)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    crownFlies.RemoveAt(i);
                    Sfx.Play("coroa");
                }
                continue;
            }
            Vector3 sp = cam.WorldToScreenPoint(f.from);
            var from = new Vector2(sp.x, Screen.height - sp.y);
            var to = (f.team == 0 ? crownRectMine : crownRectEnemy).center;
            float e = t * t * (3f - 2f * t);
            Vector2 p = Vector2.Lerp(from, to, e) + Vector2.up * (-Mathf.Sin(t * Mathf.PI) * u * 10f);
            float s = u * (6f + 3f * Mathf.Sin(t * Mathf.PI));
            var r = new Rect(p.x - s / 2f, p.y - s / 2f, s, s);
            Ui.Disc(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), Color.black);
            Ui.Disc(r, Ui.Gold);
            Ui.Text(r, "+1", fs, new Color(0.35f, 0.2f, 0f));
        }
    }

    static void DrawCrowns(Rect r, int count, Color c, int fs)
    {
        Ui.Disc(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), Color.black);
        Ui.Disc(r, c);
        Ui.Text(r, count.ToString(), fs + 2, Color.white);
    }

    // Desenha uma carta. Retorna true quando clicada.
    static bool DrawCard(Rect r, CardData c, bool canPay, bool selected, float u, int fs)
    {
        if (selected)
        {
            r.y -= u * 1.5f;
            Ui.Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Color.yellow);
        }
        DrawCardFace(r, c, fs);

        float d = r.width * 0.4f;
        var cost = new Rect(r.x - d * 0.15f, r.yMax - d * 0.85f, d, d);
        Ui.Disc(cost, Ui.Elixir);
        Ui.Text(cost, c.cost.ToString(), Mathf.RoundToInt(fs * 1.2f), Color.white);

        if (!canPay) Ui.Fill(r, new Color(0f, 0f, 0f, 0.45f));
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }
}
