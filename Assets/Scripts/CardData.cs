using UnityEngine;

public enum CardKind { Troop, Spell }

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
        return new[]
        {
            // ---------------- Iniciais (Arena 1)
            Troop("gue", "Guerreiro", 3, 1, 700, 75, 0.6f, 1.1f, 1.2f, 0.75f, C(0.95f, 0.78f, 0.30f), 0, true,
                "Lutador corpo a corpo resistente."),
            Troop("arq", "Arqueiras", 3, 2, 220, 45, 4.5f, 1.0f, 1.2f, 0.55f, C(0.40f, 0.85f, 0.40f), 0, true,
                "Duas arqueiras que atacam de longe.", "flecha"),
            Troop("rat", "Ratinhos", 2, 3, 130, 50, 0.5f, 0.9f, 2.2f, 0.45f, C(0.75f, 0.60f, 0.45f), 0, true,
                "Três ratinhos rápidos e baratos."),
            Troop("esc", "Escudeiro", 2, 1, 520, 40, 0.6f, 1.0f, 1.4f, 0.6f, C(0.50f, 0.70f, 0.90f), 0, true,
                "Segura o dano com seu escudo."),
            Troop("bom", "Bombardeiro", 3, 1, 230, 90, 4.5f, 1.6f, 1.2f, 0.55f, C(0.55f, 0.55f, 0.65f), 0, true,
                "Joga bombas que acertam grupos.", "bomba", 1.2f),
            Troop("ati", "Atiradora", 4, 1, 360, 115, 6f, 1.1f, 1.2f, 0.55f, C(0.45f, 0.55f, 0.95f), 0, true,
                "Tiro certeiro de muito longe.", "bala"),
            Spell("fog", "Bola de Fogo", 4, 280, 1.6f, C(1.00f, 0.50f, 0.10f), 0, true,
                "Explode e causa dano em área."),
            Spell("fle", "Chuva de Flechas", 3, 140, 2.6f, C(0.80f, 0.70f, 0.40f), 0, true,
                "Área grande, ótima contra enxames."),

            // ---------------- Podem cair dos baús na Arena 1
            Troop("gig", "Gigante", 5, 1, 2000, 130, 0.7f, 1.5f, 0.9f, 1.1f, C(0.95f, 0.65f, 0.35f), 0, false,
                "Muita vida. Só ataca torres.", null, 0f, true),
            Troop("mag", "Mago", 5, 1, 450, 120, 5f, 1.4f, 1.1f, 0.65f, C(0.55f, 0.40f, 0.95f), 0, false,
                "Bolas de magia com dano em área.", "magia", 1.3f),
            Troop("vik", "Vikings", 5, 4, 400, 70, 0.6f, 1.3f, 1.3f, 0.6f, C(0.80f, 0.50f, 0.30f), 0, false,
                "Quatro guerreiros do norte."),

            // ---------------- Arena 2: Vale das Pedras
            Troop("gol", "Golem", 8, 1, 4000, 220, 0.8f, 2.5f, 0.6f, 1.4f, C(0.60f, 0.55f, 0.50f), 1, false,
                "Gigante de pedra. Só ataca torres.", null, 0f, true),
            Troop("lan", "Lanceiros", 4, 3, 300, 60, 1.2f, 1.1f, 1.3f, 0.5f, C(0.85f, 0.40f, 0.40f), 1, false,
                "Três lanceiros com alcance curto."),
            Spell("rai", "Raio", 6, 700, 1.0f, C(1.00f, 0.95f, 0.40f), 1, false,
                "Dano enorme num ponto pequeno."),

            // ---------------- Arena 3: Fortaleza de Gelo
            Troop("lob", "Lobos", 3, 3, 200, 60, 0.5f, 0.9f, 2.4f, 0.5f, C(0.60f, 0.62f, 0.70f), 2, false,
                "Matilha veloz."),
            Troop("yet", "Yeti", 6, 1, 2200, 160, 0.8f, 1.6f, 0.9f, 1.1f, C(0.85f, 0.92f, 1.00f), 2, false,
                "Tanque gelado com golpe em área.", null, 1.0f),
            Troop("fei", "Feiticeira do Gelo", 4, 1, 400, 90, 5f, 1.2f, 1.2f, 0.55f, C(0.55f, 0.85f, 1.00f), 2, false,
                "Esferas de gelo em área.", "gelo", 1.2f),

            // ---------------- Arena 4: Vulcão Rugidor
            Troop("sal", "Salamandra", 4, 1, 600, 110, 3.5f, 1.5f, 1.4f, 0.65f, C(1.00f, 0.55f, 0.20f), 3, false,
                "Cospe fogo em área.", "fogo", 1.0f),
            Troop("cav", "Cavaleiro Negro", 6, 1, 1600, 260, 0.9f, 1.6f, 1.1f, 0.9f, C(0.35f, 0.35f, 0.45f), 3, false,
                "Golpes devastadores."),
            Spell("met", "Meteoro", 6, 520, 2.3f, C(0.90f, 0.30f, 0.20f), 3, false,
                "Uma pedra em chamas cai do céu."),
        };
    }
}
