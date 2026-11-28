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
    public bool flying;              // voa sobre o rio; só quem acerta o ar consegue atingir
    public bool hitsAir = true;      // consegue atacar voadores
    public bool isBuilding;          // construção: não anda e perde vida com o tempo
    public float lifetime;           // segundos até a construção sumir sozinha
    public string cardKey;           // carta de origem (para emotes/estatísticas)
    public int level = 1;
    public float frozen;             // segundos congelado (feitiço Gelo)
    public float slowT;              // segundos lento (Torre de Gelo): anda e ataca mais devagar
    public bool slows;               // seus disparos deixam o alvo lento
    public string spawnKey;          // construção que solta tropas (Fornalha)
    public float spawnEvery, spawnTimer;
    float SpeedNow => speed * (slowT > 0f ? 0.55f : 1f);
    public float shield, shieldTime;  // Escudo de Vento: absorve dano por alguns segundos
    public bool evolved, isChampion;
    public Color tint = Color.white;
    public bool healer, stealer;     // Curandeira e Ladrão
    float lastSteal = -9f;   // cor do corpo (chefes da campanha)
    SpriteRenderer shieldFx, evoFx;            // nível mostrado no selo ao lado da barra de vida
    Transform clock, clockHand;
    bool landed;
    float towerShake;      // relógio de preparo (tropa recém-solta)

    ArenaGame game;
    float cooldown;
    Entity target;
    bool kingAwake;

    SpriteRenderer body;
    public SpriteRenderer Body => body;
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
            hpText = MakeText(g, Mathf.CeilToInt(hp).ToString(), new Vector3(0f, barY, 0f), 0.04f);

        // Selo de nível (quadradinho da cor do time à esquerda da barra)
        float badge = isTower ? 0.36f : 0.24f;
        var lv = Child("Nivel", g.SquareSprite, Color.Lerp(teamColor, Color.black, 0.35f), 2902, transform);
        lv.transform.localPosition = new Vector3(-barW / 2f - badge * 0.45f, barY, 0f);
        lv.transform.localScale = new Vector3(badge, badge, 1f);
        var lvb = Child("NivelBorda", g.SquareSprite, new Color(1f, 0.85f, 0.3f), 2901, transform);
        lvb.transform.localPosition = lv.transform.localPosition;
        lvb.transform.localScale = new Vector3(badge + 0.05f, badge + 0.05f, 1f);
        var lt = MakeText(g, level.ToString(), lv.transform.localPosition, isTower ? 0.036f : 0.026f);
        lt.GetComponent<MeshRenderer>().sortingOrder = 2955;

        // Brilho roxo das cartas EVO e aura dourada dos Campeões
        if (evolved || isChampion)
        {
            var glow = Child("Aura", g.CircleSprite, evolved ? new Color(0.75f, 0.35f, 1f, 0.55f) : new Color(1f, 0.85f, 0.3f, 0.55f), -395, transform);
            glow.transform.localScale = new Vector3(size * 1.35f, size * 0.55f, 1f);
            evoFx = glow.GetComponent<SpriteRenderer>();
        }
        var sh = Child("Escudo", g.CircleSprite, new Color(0.5f, 0.95f, 1f, 0.3f), 2050, transform);
        sh.transform.localPosition = new Vector3(0f, artHeight * 0.45f, 0f);
        sh.transform.localScale = new Vector3(size * 1.6f, artHeight * 1.1f + 0.3f, 1f);
        shieldFx = sh.GetComponent<SpriteRenderer>();
        shieldFx.enabled = false;

        // Relógio de preparo
        if (!isTower)
        {
            var c = Child("Relogio", g.CircleSprite, Color.white, 2960, transform);
            clock = c.transform;
            clock.localPosition = new Vector3(0f, artHeight * 0.5f, 0f);
            clock.localScale = Vector3.one * 0.34f;
            var ring = Child("RelogioBorda", g.CircleSprite, new Color(0.25f, 0.25f, 0.3f), 2959, clock);
            ring.transform.localScale = Vector3.one * 1.2f;
            var hand = Child("Ponteiro", g.SquareSprite, new Color(0.9f, 0.2f, 0.2f), 2961, clock);
            clockHand = hand.transform;
        }

        if (!isTower)
        {
            spawnT = 0f;
            deployLeft = DeployTime;
        }
        else { spawnT = DropTime + 1f; landed = true; }

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

        if (slowT > 0f) slowT -= Time.deltaTime;
        cooldown -= Time.deltaTime * (slowT > 0f ? 0.7f : 1f);
        if (isBuilding && lifetime > 0f && deployLeft <= 0f)
        {
            hp -= maxHp / lifetime * Time.deltaTime;
            RefreshBar();
            if (hp <= 0f) { game.OnEntityDied(this); Destroy(gameObject); return; }
        }
        if (deployLeft > 0f)
        {
            deployLeft -= Time.deltaTime;
            return;
        }

        if (frozen > 0f)
        {
            frozen -= Time.deltaTime;
            moving = false;
            return;
        }

        // Fornalha: solta uma tropa de tempos em tempos
        if (spawnKey != null)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f) { spawnTimer = spawnEvery; game.SpawnFromBuilding(this); }
        }
        // Construção que não ataca (Muralha, Fornalha): só fica parada segurando os inimigos
        if (isBuilding && damage <= 0f) return;

        // A torre do rei só acorda quando leva dano ou perde uma torre lateral
        if (isKing && !kingAwake)
        {
            if (hp < maxHp || game.PrincessLost(team)) kingAwake = true;
            else return;
        }

        moving = false;

        // Curandeira: não ataca; cura em pulsos e fica atrás quando há inimigos perto
        if (healer)
        {
            if (cooldown <= 0f && game.HealAround(this)) { cooldown = attackCooldown; attackT = AttackDur; }
            var threat = game.FindTarget(this);
            if (threat != null && InReach(threat, 3f)) { Face(threat.transform.position); return; }
            var g2 = game.NearestEnemyTower(this);
            if (g2 == null || speed <= 0f) return;
            Vector2 d2 = flying ? (Vector2)g2.transform.position : game.PathPoint(transform.position, g2.transform.position);
            Face(d2);
            transform.position = Vector2.MoveTowards(transform.position, d2, SpeedNow * Time.deltaTime);
            moving = true;
            UpdateSorting();
            return;
        }

        if (target == null || !target.Alive || !InReach(target, (isTower || isBuilding) ? range : sight))
            target = game.FindTarget(this);
        if (target != null && target.flying && !hitsAir) target = null;

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

        Vector2 dest = flying ? (Vector2)goal.transform.position : game.PathPoint(transform.position, goal.transform.position);
        Face(dest);
        transform.position = Vector2.MoveTowards(transform.position, dest, SpeedNow * Time.deltaTime);
        moving = true;
        UpdateSorting();
    }

    void Face(Vector2 point)
    {
        if (isTower) return;
        if (isBuilding && frames == null) return;
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
            if (!landed) { landed = true; game.Dust(transform.position, Mathf.Max(0.8f, radius * 2f)); }
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
                // Torres e construções não "pulsam" ao atirar: só tropas sem quadros fazem o golpe
                if (!isTower && !isBuilding)
                {
                    float k = Mathf.Sin((attackT / AttackDur) * Mathf.PI) * 0.15f;
                    scale *= 1f + k;
                }
            }
        }

        // Piscar vermelho ao levar dano
        if (hitT > 0f) hitT -= dt;
        body.color = hitT > 0f ? new Color(1f, 0.55f, 0.55f) : frozen > 0f ? new Color(0.6f, 0.85f, 1f) : slowT > 0f ? Color.Lerp(tint, new Color(0.55f, 0.8f, 1f), 0.6f) : tint;

        if (towerShake > 0f)
        {
            towerShake = Mathf.Max(0f, towerShake - dt);
            pos.x += Mathf.Sin(Time.time * 70f) * towerShake * 0.25f;
        }
        pos.y += visualOffsetY;
        if (flying) pos.y += 0.25f + Mathf.Sin(Time.time * 3f + transform.GetInstanceID()) * 0.08f;
        bodyT.localPosition = pos;
        bodyT.localScale = scale;

        if (evoFx != null)
        {
            float k = 0.45f + 0.2f * Mathf.Sin(Time.time * 4f + transform.GetInstanceID());
            var c0 = evoFx.color;
            evoFx.color = new Color(c0.r, c0.g, c0.b, k);
        }
        if (shieldTime > 0f)
        {
            shieldTime -= dt;
            if (shieldTime <= 0f) shield = 0f;
        }
        shieldFx.enabled = shield > 0f;
        if (shieldFx.enabled) shieldFx.color = new Color(0.5f, 0.95f, 1f, 0.22f + 0.1f * Mathf.Sin(Time.time * 6f));

        if (clock != null)
        {
            bool show = deployLeft > 0f && spawnT >= DropTime;
            clock.gameObject.SetActive(show);
            if (show)
            {
                float f = 1f - deployLeft / DeployTime;
                clockHand.localRotation = Quaternion.Euler(0f, 0f, -360f * f);
                clockHand.localScale = new Vector3(0.12f, 0.45f, 1f);
                clockHand.localPosition = clockHand.localRotation * new Vector3(0f, 0.22f, 0f);
            }
        }

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
        if (stealer && other.isTower && Time.time - lastSteal > 2.5f)
        {
            lastSteal = Time.time;
            game.StealElixir(team, transform.position);
        }
        if (projectile != null) game.SpawnProjectile(this, other, damage, projectile, splash);
        else if (splash > 0f) game.Splash(team, other.transform.position, splash, damage, hitsAir);
        else
        {
            other.TakeDamage(damage);
            game.Sparks(Vector2.Lerp(transform.position, other.transform.position, 0.6f));
        }
    }

    public void Heal(float amount)
    {
        if (!Alive || hp >= maxHp) return;
        float before = hp;
        hp = Mathf.Min(maxHp, hp + amount);
        RefreshBar();
        game.FloatText(transform.position + Vector3.up * 0.9f, "+" + Mathf.RoundToInt(hp - before), new Color(0.4f, 1f, 0.5f), 0.04f);
    }

    void RefreshBar()
    {
        float f = Mathf.Clamp01(hp / maxHp);
        hpFill.localScale = new Vector3(f, 1f, 1f);
        hpFill.localPosition = new Vector3(-(1f - f) * 0.5f, 0f, 0f);
        if (hpText != null) hpText.text = Mathf.CeilToInt(Mathf.Max(hp, 0f)).ToString();
    }

    public void TakeDamage(float amount)
    {
        if (!Alive) return;
        if (shield > 0f)
        {
            float absorbed = Mathf.Min(shield, amount);
            shield -= absorbed;
            amount -= absorbed;
            if (amount <= 0f)
            {
                game.FloatText(transform.position + Vector3.up * 0.9f, "bloqueado", new Color(0.5f, 0.95f, 1f), 0.03f);
                return;
            }
        }
        hp -= amount;
        if (isTower && team == 1) Profile.Track("dano", Mathf.RoundToInt(amount));   // missão diária
        hitT = 0.12f;
        if (isTower && amount >= 40f)
        {
            towerShake = Mathf.Min(0.35f, towerShake + amount / 600f);
            game.Debris((Vector2)transform.position + Vector2.up * 1.1f, amount >= 150f ? 6 : 3,
                team == 0 ? new Color(0.6f, 0.62f, 0.7f) : new Color(0.65f, 0.55f, 0.5f));
        }
        game.FloatText(transform.position + Vector3.up * (isTower ? 1.5f : 0.9f),
            Mathf.RoundToInt(amount).ToString(),
            amount >= 200f ? new Color(1f, 0.38f, 0.25f) : isTower ? new Color(1f, 0.9f, 0.4f) : Color.white,
            (isTower ? 0.055f : 0.04f) * (amount >= 200f ? 1.45f : 1f));

        RefreshBar();
        if (team == 0 && (isTower || amount > 150f)) game.Vibrate();

        if (hp <= 0f)
        {
            game.OnEntityDied(this);
            Destroy(gameObject);
        }
    }
}
