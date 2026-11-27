using UnityEngine;

// Quadros de animação de um personagem: [0] = de frente, [1] = de costas
public class CharacterFrames
{
    public Sprite[] idle = new Sprite[2];
    public Sprite[][] walk = { new Sprite[4], new Sprite[4] };
    public Sprite[][] atk = { new Sprite[2], new Sprite[2] };
    public bool Complete => idle[0] != null && idle[1] != null;
}

// Qualquer coisa que tem vida na arena: tropas e torres.
public class Entity : MonoBehaviour
{
    public int team;                 // 0 = você (embaixo), 1 = inimigo (em cima)
    public float maxHp, hp, damage, range, attackCooldown, speed;
    public float sight = 5f;         // distância em que a tropa "enxerga" inimigos
    public bool isTower, isKing, targetsTowersOnly;
    public float radius = 0.3f;
    public string projectile;        // "flecha", "magia", "bala" ou null (corpo a corpo)
    public float visualOffsetY;      // desloca o desenho (torres ficam com a base no chão)
    public float splash;             // dano em área ao redor do alvo
    public CharacterFrames frames;   // animação (null = sprite fixo)

    ArenaGame game;
    float cooldown;
    Entity target;
    bool kingAwake;

    SpriteRenderer body;
    Transform bodyT, hpFill, hpBar;
    TextMesh hpText;
    float bodyScale = 1f;
    float spawnT;                    // animação de entrada (queda)
    float deployLeft;                // tempo até a tropa começar a agir
    float hitT, attackT, walkT;
    bool moving;
    int facing;                      // 0 = de frente (descendo a tela), 1 = de costas (subindo)
    const float AttackDur = 0.28f;

    const float DropTime = 0.3f;
    const float DeployTime = 0.8f;

    public bool Alive => hp > 0f;
    public bool Deploying => deployLeft > 0f;

    public void Setup(ArenaGame g, int team, float size, Sprite art, float artWidth, Color fallback, Color teamColor,
        float barHeight = -1f)
    {
        game = g;
        this.team = team;
        hp = maxHp;
        radius = size * 0.5f;
        facing = team == 0 ? 1 : 0;      // suas tropas sobem a arena (de costas)
        if (frames != null && frames.Complete) art = frames.idle[facing];

        // Sombra
        var shadow = Child("Sombra", g.CircleSprite, new Color(0f, 0f, 0f, 0.28f), -400, transform);
        shadow.transform.localScale = new Vector3(size * (isTower ? 1.3f : 1.1f), size * (isTower ? 0.6f : 0.45f), 1f);

        // Marca do time no chão (tropas)
        if (!isTower)
        {
            var ring = Child("Time", g.CircleSprite, new Color(teamColor.r, teamColor.g, teamColor.b, 0.55f), -390, transform);
            ring.transform.localScale = new Vector3(size * 0.95f, size * 0.38f, 1f);
        }

        // Corpo
        var bodyGo = Child("Corpo", art != null ? art : g.CircleSprite, art != null ? Color.white : fallback, 0, transform);
        body = bodyGo.GetComponent<SpriteRenderer>();
        bodyT = bodyGo.transform;
        if (art != null) bodyScale = artWidth > 0f ? artWidth / art.bounds.size.x : 1f;
        else bodyScale = size;
        bodyT.localScale = Vector3.one * bodyScale;
        float artHeight = barHeight > 0f ? barHeight : (art != null ? art.bounds.size.y * bodyScale : size);

        // Barra de vida
        float barW = isTower ? 1.5f : Mathf.Clamp(size * 1.3f, 0.6f, 1.2f);
        float barY = isTower ? -1.05f : artHeight + 0.12f;
        var bg = Child("VidaFundo", g.SquareSprite, new Color(0f, 0f, 0f, 0.65f), 2900, transform);
        hpBar = bg.transform;
        hpBar.localPosition = new Vector3(0f, barY, 0f);
        hpBar.localScale = new Vector3(barW, isTower ? 0.2f : 0.12f, 1f);
        hpFill = Child("Vida", g.SquareSprite, teamColor, 2901, hpBar).transform;

        if (isTower)
            hpText = MakeText(g, Mathf.CeilToInt(hp).ToString(), new Vector3(0f, barY - 0.32f, 0f), 0.045f);

        if (!isTower)
        {
            spawnT = 0f;
            deployLeft = DeployTime;
        }
        else spawnT = DropTime;

        UpdateSorting();
    }

    TextMesh MakeText(ArenaGame g, string text, Vector3 localPos, float charSize)
    {
        var t = new GameObject("Texto");
        t.transform.SetParent(transform, false);
        t.transform.localPosition = localPos;
        var tm = t.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = g.LabelFont;
        tm.fontSize = 48;
        tm.characterSize = charSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = Color.white;
        var mr = t.GetComponent<MeshRenderer>();
        mr.sharedMaterial = g.LabelFont.material;
        mr.sortingOrder = 2950;
        return tm;
    }

    static GameObject Child(string name, Sprite sprite, Color color, int order, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    // Quem está mais embaixo na tela é desenhado na frente
    void UpdateSorting()
    {
        body.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 20f);
    }

    void Update()
    {
        if (!Alive || game == null) return;
        Animate();
        if (!game.Running) return;

        cooldown -= Time.deltaTime;
        if (deployLeft > 0f)
        {
            deployLeft -= Time.deltaTime;
            return;
        }

        // A torre do rei só acorda quando leva dano ou perde uma torre lateral
        if (isKing && !kingAwake)
        {
            if (hp < maxHp || game.PrincessLost(team)) kingAwake = true;
            else return;
        }

        moving = false;
        if (target == null || !target.Alive || !InReach(target, isTower ? range : sight))
            target = game.FindTarget(this);

        if (target != null && InReach(target, range))
        {
            Face(target.transform.position);
            if (cooldown <= 0f)
            {
                Attack(target);
                cooldown = attackCooldown;
            }
            return;
        }

        if (speed <= 0f) return;

        Entity goal = target != null ? target : game.NearestEnemyTower(this);
        if (goal == null) return;

        Vector2 dest = game.PathPoint(transform.position, goal.transform.position);
        Face(dest);
        transform.position = Vector2.MoveTowards(transform.position, dest, speed * Time.deltaTime);
        moving = true;
        UpdateSorting();
    }

    void Face(Vector2 point)
    {
        if (isTower) return;
        float dx = point.x - transform.position.x;
        float dy = point.y - transform.position.y;
        if (frames != null && Mathf.Abs(dy) > 0.15f) facing = dy > 0f ? 1 : 0;
        // Os desenhos de frente olham para a direita; os de costas, para a esquerda
        bool facesRight = facing == 0 ? ArenaGame.FrontFacesRight : !ArenaGame.FrontFacesRight;
        if (Mathf.Abs(dx) > 0.05f) body.flipX = (dx < 0f) == facesRight;
    }

    void Animate()
    {
        float dt = Time.deltaTime;
        Vector3 pos = Vector3.zero;
        Vector3 scale = Vector3.one * bodyScale;

        // Queda ao entrar na arena + "amassadinha" ao tocar o chão
        if (spawnT < DropTime)
        {
            spawnT += dt;
            float t = Mathf.Clamp01(spawnT / DropTime);
            pos.y = (1f - t) * (1f - t) * 2.5f;
        }
        else if (spawnT < DropTime + 0.15f)
        {
            spawnT += dt;
            float t = (spawnT - DropTime) / 0.15f;
            float squash = Mathf.Sin(t * Mathf.PI) * 0.18f;
            scale = new Vector3(bodyScale * (1f + squash), bodyScale * (1f - squash), 1f);
        }

        if (frames != null && frames.Complete)
        {
            // Animação quadro a quadro
            Sprite s;
            if (attackT > 0f)
            {
                attackT -= dt;
                s = frames.atk[facing][attackT > AttackDur * 0.45f ? 0 : 1];
            }
            else if (moving)
            {
                walkT += dt * Mathf.Max(1f, speed) * 5f;
                s = frames.walk[facing][(int)walkT % 4];
            }
            else s = frames.idle[facing];
            if (s != null) body.sprite = s;
        }
        else
        {
            // Sem quadros: balanço simples
            if (moving)
            {
                walkT += dt * speed * 9f;
                pos.y += Mathf.Abs(Mathf.Sin(walkT)) * 0.08f;
                bodyT.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(walkT) * 5f);
            }
            else bodyT.localRotation = Quaternion.identity;
            if (attackT > 0f)
            {
                attackT -= dt;
                float k = Mathf.Sin((attackT / AttackDur) * Mathf.PI) * 0.15f;
                scale *= 1f + k;
            }
        }

        // Piscar vermelho ao levar dano
        if (hitT > 0f) hitT -= dt;
        body.color = hitT > 0f ? new Color(1f, 0.55f, 0.55f) : Color.white;

        pos.y += visualOffsetY;
        bodyT.localPosition = pos;
        bodyT.localScale = scale;

        // Tropas "em preparo" ficam meio transparentes
        if (deployLeft > 0f && spawnT >= DropTime) body.color = new Color(1f, 1f, 1f, 0.75f);
    }

    bool InReach(Entity other, float reach)
    {
        float d = Vector2.Distance(transform.position, other.transform.position) - radius - other.radius;
        return d <= reach;
    }

    void Attack(Entity other)
    {
        attackT = AttackDur;
        game.AttackSound(this);
        if (projectile != null) game.SpawnProjectile(this, other, damage, projectile, splash);
        else if (splash > 0f) game.Splash(team, other.transform.position, splash, damage);
        else other.TakeDamage(damage);
    }

    public void TakeDamage(float amount)
    {
        if (!Alive) return;
        hp -= amount;
        if (isTower && team == 1) Profile.Track("dano", Mathf.RoundToInt(amount));   // missão diária
        hitT = 0.12f;
        game.FloatText(transform.position + Vector3.up * (isTower ? 1.5f : 0.9f),
            Mathf.RoundToInt(amount).ToString(), isTower ? new Color(1f, 0.9f, 0.4f) : Color.white, isTower ? 0.055f : 0.04f);

        float f = Mathf.Clamp01(hp / maxHp);
        hpFill.localScale = new Vector3(f, 1f, 1f);
        hpFill.localPosition = new Vector3(-(1f - f) * 0.5f, 0f, 0f);
        if (hpText != null) hpText.text = Mathf.CeilToInt(Mathf.Max(hp, 0f)).ToString();

        if (hp <= 0f)
        {
            game.OnEntityDied(this);
            Destroy(gameObject);
        }
    }
}
