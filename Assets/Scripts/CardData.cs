using UnityEngine;

public enum CardKind { Troop, Spell, Building }

// Dados de uma carta. Todas as cartas do jogo ficam em All() para facilitar o balanceamento.
[System.Serializable]
public class CardData
{
    public string key;              // identificador curto (nome da pasta da arte: Resources/Tropas/<key>)
    public string name;
    public int cost;
    public CardKind kind;

    // Tropas
    public int count = 1;
    public float hp;
    public float damage;
    public float range;
    public float attackCooldown;
    public float speed;
    public float unitSize = 0.6f;
    public bool targetsTowersOnly;
    public float splash;            // dano em área ao redor do alvo (0 = só o alvo)
    public string projectile;       // "flecha", "bomba", "bala", "magia", "gelo", "fogo" ou null

    // Feitiços
    public float spellRadius;

    public Color color;
    public int unlockArena;         // arena em que passa a poder cair dos baús (0 = Arena 1)
    public bool starter;            // carta inicial (o jogador já começa com ela)
    public string description;

    // Raridade: 0 Comum, 1 Rara, 2 Épica, 3 Lendária (mais rara = cai menos e precisa de menos cópias)
    public int rarity;
    public bool flying;             // voa: ignora o rio e só é atingida por quem acerta o ar
    public bool hitsAir = true;     // consegue atacar unidades voadoras
    public float lifetime;          // construções perdem vida com o tempo (segundos de duração)

    public static readonly string[] RarityNames = { "Comum", "Rara", "Épica", "Lendária", "Campeão" };
    public static readonly Color[] RarityColors =
    {
        new Color(0.55f, 0.65f, 0.80f), new Color(1.00f, 0.60f, 0.15f),
        new Color(0.75f, 0.35f, 1.00f), new Color(0.30f, 1.00f, 0.90f), new Color(1.00f, 0.85f, 0.30f)
    };

    // Campeão: só 1 por deck e 1 na arena; tem uma habilidade com custo de elixir e recarga
    public bool champion;
    public string abilityName, abilityDesc;
    public int abilityCost;
    public float abilityCooldown;

    // Habilidades especiais de algumas tropas
    public bool healer;     // não ataca: cura as aliadas por perto
    public bool anywhere;   // pode ser solta em qualquer lugar da arena
    public bool stealer;    // rouba elixir ao golpear torres
    CardData Heal() { healer = true; return this; }
    CardData Anywhere() { anywhere = true; return this; }
    CardData Steal() { stealer = true; return this; }

    // Construções especiais
    public bool slows;          // disparos deixam o alvo lento (Torre de Gelo)
    public string spawnKey;     // solta esta tropa a cada spawnEvery segundos (Fornalha)
    public float spawnEvery;
    CardData Slow() { slows = true; return this; }
    CardData Spawns(string key, float every) { spawnKey = key; spawnEvery = every; return this; }

    // Tropas que não ficam no deck: só aparecem soltas por construções
    static CardData[] extras;
    public static CardData Extra(string key)
    {
        if (extras == null)
            extras = new[]
            {
                Troop("bra", "Brasinha", 0, 1, 150, 70, 0.5f, 1.0f, 2.0f, 0.45f, C(1.00f, 0.50f, 0.15f), 3, false,
                    "Espírito de fogo saltitante: queima em área pequena.", null, 0.8f),
            };
        foreach (var e in extras) if (e.key == key) { if (e.projectile == null) e.hitsAir = false; return e; }
        return null;
    }

    CardData Champ(string name, string desc, int cost, float cd)
    {
        champion = true; abilityName = name; abilityDesc = desc; abilityCost = cost; abilityCooldown = cd;
        return this;
    }

    CardData R(int r) { rarity = r; return this; }
    CardData Fly() { flying = true; return this; }
    CardData NoAir() { hitsAir = false; return this; }

    [System.NonSerialized] Texture2D portrait;

    // Desenho da carta (Assets/Resources/Cartas/<key>.png)
    public Texture2D Portrait
    {
        get
        {
            if (portrait == null) portrait = Resources.Load<Texture2D>("Cartas/" + key);
            return portrait;
        }
    }

    static CardData Troop(string key, string name, int cost, int count, float hp, float damage, float range,
        float cd, float speed, float size, Color color, int arena, bool starter, string desc,
        string projectile = null, float splash = 0f, bool towersOnly = false)
    {
        return new CardData
        {
            key = key, name = name, cost = cost, kind = CardKind.Troop, count = count, hp = hp, damage = damage,
            range = range, attackCooldown = cd, speed = speed, unitSize = size, color = color,
            unlockArena = arena, starter = starter, description = desc, projectile = projectile,
            splash = splash, targetsTowersOnly = towersOnly
        };
    }

    static CardData Building(string key, string name, int cost, float hp, float damage, float range, float cd,
        float lifetime, float size, Color color, int arena, bool starter, string desc, string projectile, float splash = 0f)
    {
        return new CardData
        {
            key = key, name = name, cost = cost, kind = CardKind.Building, count = 1, hp = hp, damage = damage,
            range = range, attackCooldown = cd, speed = 0f, unitSize = size, color = color, lifetime = lifetime,
            unlockArena = arena, starter = starter, description = desc, projectile = projectile, splash = splash
        };
    }

    static CardData Spell(string key, string name, int cost, float damage, float radius, Color color, int arena,
        bool starter, string desc)
    {
        return new CardData
        {
            key = key, name = name, cost = cost, kind = CardKind.Spell, damage = damage, spellRadius = radius,
            color = color, unlockArena = arena, starter = starter, description = desc
        };
    }

    static Color C(float r, float g, float b) => new Color(r, g, b);

    // Todas as cartas do jogo.
    //  - starter = true: você começa com ela (deck inicial de 8)
    //  - unlockArena: a partir dessa arena ela pode CAIR DOS BAÚS (o caminho só desbloqueia, não entrega)
    public static CardData[] All()
    {
        var all = new[]
        {
            // ---------------- Iniciais (Arena 1)
            Troop("gue", "Guerreiro", 3, 1, 700, 75, 0.6f, 1.1f, 1.2f, 0.75f, C(0.95f, 0.78f, 0.30f), 0, true,
                "Lutador corpo a corpo resistente.").R(0),
            Troop("arq", "Arqueiras", 3, 2, 220, 45, 4.5f, 1.0f, 1.2f, 0.55f, C(0.40f, 0.85f, 0.40f), 0, true,
                "Duas arqueiras que atacam de longe. Acertam voadores.", "flecha").R(0),
            Troop("rat", "Ratinhos", 2, 3, 130, 38, 0.5f, 0.9f, 2.2f, 0.45f, C(0.75f, 0.60f, 0.45f), 0, true,
                "Três ratinhos rápidos e baratos.").R(0),
            Troop("esc", "Escudeiro", 2, 1, 520, 40, 0.6f, 1.0f, 1.4f, 0.6f, C(0.50f, 0.70f, 0.90f), 0, true,
                "Segura o dano com seu escudo.").R(0),
            Troop("bom", "Bombardeiro", 3, 1, 270, 95, 4.5f, 1.6f, 1.2f, 0.55f, C(0.55f, 0.55f, 0.65f), 0, true,
                "Joga bombas que acertam grupos no chão.", "bomba", 1.2f).R(0).NoAir(),
            Troop("ati", "Atiradora", 4, 1, 400, 115, 6f, 1.1f, 1.2f, 0.55f, C(0.45f, 0.55f, 0.95f), 0, true,
                "Tiro certeiro de muito longe. Acerta voadores.", "bala").R(1),
            Spell("fog", "Bola de Fogo", 4, 280, 1.6f, C(1.00f, 0.50f, 0.10f), 0, true,
                "Explode e causa dano em área.").R(1),
            Spell("fle", "Chuva de Flechas", 3, 140, 2.6f, C(0.80f, 0.70f, 0.40f), 0, true,
                "Área grande, ótima contra enxames.").R(0),

            // ---------------- Podem cair dos baús na Arena 1
            Troop("gig", "Gigante", 5, 1, 2000, 130, 0.7f, 1.5f, 0.9f, 1.1f, C(0.95f, 0.65f, 0.35f), 0, false,
                "Muita vida. Só ataca construções e torres.", null, 0f, true).R(1),
            Troop("mag", "Mago", 5, 1, 540, 135, 5f, 1.4f, 1.1f, 0.65f, C(0.55f, 0.40f, 0.95f), 0, false,
                "Bolas de magia com dano em área.", "magia", 1.3f).R(1),
            Troop("vik", "Vikings", 5, 4, 340, 70, 0.6f, 1.3f, 1.3f, 0.6f, C(0.80f, 0.50f, 0.30f), 0, false,
                "Quatro guerreiros do norte.").R(0),
            Troop("lad", "Ladrão", 3, 1, 450, 80, 0.6f, 0.9f, 1.9f, 0.55f, C(0.30f, 0.28f, 0.38f), 0, false,
                "Muito rápido. Cada golpe numa torre rouba 1 de elixir para você.").R(1).Steal(),
            Troop("mor", "Morcegos", 2, 4, 80, 45, 0.5f, 1.1f, 2.4f, 0.45f, C(0.55f, 0.35f, 0.75f), 0, false,
                "Quatro morcegos voadores e rápidos.").R(0).Fly(),
            Building("can", "Canhão", 3, 700, 85, 5.5f, 0.9f, 30f, 1.1f, C(0.45f, 0.45f, 0.5f), 0, false,
                "Construção defensiva. Atrai o Gigante. Não acerta voadores.", "bala").R(0).NoAir(),
            Building("mur", "Muralha", 2, 1400, 0, 0f, 1f, 25f, 1.1f, C(0.62f, 0.58f, 0.52f), 0, false,
                "Paredão de pedra barato. Não ataca, mas segura e desvia os inimigos.", null).R(0),

            // ---------------- Arena 2: Vale das Pedras
            Troop("gol", "Golem", 8, 1, 4400, 260, 0.8f, 2.5f, 0.6f, 1.4f, C(0.60f, 0.55f, 0.50f), 1, false,
                "Gigante de pedra. Só ataca construções e torres.", null, 0f, true).R(2),
            Troop("lan", "Lanceiros", 4, 3, 300, 60, 1.2f, 1.1f, 1.3f, 0.5f, C(0.85f, 0.40f, 0.40f), 1, false,
                "Três lanceiros com alcance curto.").R(0),
            Spell("rai", "Raio", 6, 700, 1.0f, C(1.00f, 0.95f, 0.40f), 1, false,
                "Dano enorme num ponto pequeno.").R(2),
            Spell("cur", "Cura", 3, 400, 2.6f, C(0.40f, 0.90f, 0.45f), 1, false,
                "Cura suas tropas na área aos poucos.").R(1),
            Troop("cun", "Curandeira", 4, 1, 520, 70, 2.5f, 1.5f, 1.1f, 0.6f, C(0.55f, 0.90f, 0.55f), 1, false,
                "Não ataca: a cada 1,5 s cura as suas tropas por perto.").R(1).Heal(),
            Troop("aur", "Cavaleira Aurora", 4, 1, 1100, 170, 0.8f, 1.1f, 1.3f, 0.75f, C(1.00f, 0.80f, 0.30f), 1, false,
                "Campeã de armadura dourada. Forte e resistente.").R(4)
                .Champ("Investida", "Avança com tudo, atravessando e ferindo os inimigos no caminho.", 2, 12f),
            Troop("dra", "Dragãozinho", 4, 1, 700, 105, 3.5f, 1.6f, 1.6f, 0.8f, C(0.35f, 0.70f, 0.35f), 1, false,
                "Voa e cospe fogo em área.", "fogo", 1.0f).R(2).Fly(),

            // ---------------- Arena 3: Fortaleza de Gelo
            Troop("lob", "Lobos", 3, 3, 200, 50, 0.5f, 0.9f, 2.4f, 0.5f, C(0.60f, 0.62f, 0.70f), 2, false,
                "Matilha veloz.").R(0),
            Troop("yet", "Yeti", 6, 1, 2200, 160, 0.8f, 1.6f, 0.9f, 1.1f, C(0.85f, 0.92f, 1.00f), 2, false,
                "Tanque gelado com golpe em área.", null, 1.0f).R(2),
            Troop("fei", "Feiticeira do Gelo", 4, 1, 450, 95, 5f, 1.2f, 1.2f, 0.55f, C(0.55f, 0.85f, 1.00f), 2, false,
                "Esferas de gelo em área.", "gelo", 1.2f).R(1),
            Spell("gel", "Gelo", 4, 90, 2.4f, C(0.60f, 0.85f, 1.00f), 2, false,
                "Congela tropas e torres inimigas por alguns segundos.").R(2),
            Troop("ven", "Bruxa Ventania", 4, 1, 700, 110, 4.5f, 1.3f, 1.2f, 0.6f, C(0.25f, 0.75f, 0.70f), 2, false,
                "Campeã que ataca de longe com magia de vento.", "magia", 0.8f).R(4)
                .Champ("Escudo de Vento", "Protege as suas tropas próximas com um escudo por 6 segundos.", 2, 14f),
            Troop("esv", "Escavador", 3, 1, 780, 130, 0.7f, 1.2f, 1.4f, 0.7f, C(0.80f, 0.65f, 0.35f), 2, false,
                "Pode ser solto em qualquer lugar da arena: sai de baixo da terra.").R(2).Anywhere(),
            Troop("bal", "Balão de Bombas", 5, 1, 1100, 300, 0.4f, 2.5f, 0.9f, 0.9f, C(0.35f, 0.55f, 0.95f), 2, false,
                "Voa até construções e torres e solta bombas pesadas.", null, 1.0f, true).R(2).Fly().NoAir(),
            Building("tbo", "Torre de Bombas", 4, 1000, 110, 5f, 1.6f, 35f, 1.2f, C(0.55f, 0.52f, 0.5f), 2, false,
                "Construção que joga bombas em grupos no chão.", "bomba", 1.2f).R(1).NoAir(),
            Building("tgl", "Torre de Gelo", 4, 900, 70, 5.5f, 1.2f, 35f, 1.1f, C(0.60f, 0.85f, 1.00f), 2, false,
                "Atira cristais de gelo que deixam o alvo lento. Acerta voadores.", "gelo").R(1).Slow(),

            // ---------------- Arena 4: Vulcão Rugidor
            Troop("sal", "Salamandra", 4, 1, 640, 115, 3.5f, 1.5f, 1.4f, 0.65f, C(1.00f, 0.55f, 0.20f), 3, false,
                "Cospe fogo em área.", "fogo", 1.0f).R(1),
            Troop("cav", "Cavaleiro Negro", 6, 1, 1750, 260, 0.9f, 1.6f, 1.1f, 0.9f, C(0.35f, 0.35f, 0.45f), 3, false,
                "Golpes devastadores.").R(3),
            Spell("tor", "Tornado", 3, 80, 3.0f, C(0.70f, 0.75f, 0.90f), 3, false,
                "Puxa as tropas inimigas para o centro do giro.").R(2),
            Spell("met", "Meteoro", 6, 520, 2.3f, C(0.90f, 0.30f, 0.20f), 3, false,
                "Uma pedra em chamas cai do céu.").R(3),
            Building("frn", "Fornalha", 5, 900, 0, 0f, 1f, 40f, 1.2f, C(0.85f, 0.35f, 0.15f), 3, false,
                "Não ataca: a cada 5 s solta uma Brasinha que queima em área.", null).R(2).Spawns("bra", 5f),
        };
        // Quem ataca corpo a corpo não acerta o ar (exceto voadores). Quem atira acerta.
        foreach (var card in all)
            if (card.kind != CardKind.Spell && card.hitsAir && card.projectile == null && !card.flying) card.hitsAir = false;
        return all;
    }
}
