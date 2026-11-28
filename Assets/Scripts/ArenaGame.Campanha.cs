using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Campanha dos chefes, clima, final dramático e regras de cada arena.
public partial class ArenaGame
{
    // ---------------------------------------------------------------- Campanha: chefes

    public struct BossInfo
    {
        public string name, model, minion, lore;
        public Color tint;
        public BossInfo(string n, string m, string mi, Color t, string l) { name = n; model = m; minion = mi; tint = t; lore = l; }
    }

    public static readonly BossInfo[] Bosses =
    {
        new BossInfo("Golem Ancião", "gol", "rat", new Color(0.85f, 0.95f, 0.8f), "Dorme no Campo Verde há mil anos. Chama ratinhos para limpar seu caminho."),
        new BossInfo("Yeti Furioso", "yet", "lob", new Color(0.8f, 0.9f, 1f), "Ruge e convoca lobos da montanha."),
        new BossInfo("Gigante Barbudo", "gig", "esc", new Color(1f, 0.85f, 0.75f), "Lento, mas cada passo faz a arena tremer."),
        new BossInfo("Dragão Ancestral", "dra", "mor", new Color(0.75f, 1f, 0.75f), "Voa alto e chama morcegos das cavernas."),
        new BossInfo("Cavaleiro Sombrio", "cav", "lan", new Color(0.8f, 0.75f, 1f), "Comanda uma tropa de lanceiros leais."),
        new BossInfo("Rainha do Gelo", "fei", "lob", new Color(0.7f, 0.9f, 1f), "Seu toque congela qualquer guerreiro."),
        new BossInfo("Salamandra Rainha", "sal", "rat", new Color(1f, 0.75f, 0.6f), "Mora no fundo do vulcão e cospe fogo sem parar."),
        new BossInfo("Golem de Lava", "gol", "sal", new Color(1f, 0.55f, 0.35f), "Feito de pedra derretida. Deixa fogo por onde passa."),
        new BossInfo("Mago Sombrio", "mag", "mor", new Color(0.7f, 0.55f, 1f), "Lança magias em área e invoca morcegos."),
        new BossInfo("Rei da Tempestade", "gig", "vik", new Color(0.65f, 0.7f, 1f), "O último desafio: um gigante com um exército viking."),
    };

    Entity boss;
    bool bossSpawned;
    float bossT, bossSummonT;

    void UpdateBoss(float dt)
    {
        bossT += dt;
        if (!bossSpawned && bossT >= 8f) SpawnBoss();
        if (boss != null && boss.Alive && !boss.Deploying)
        {
            bossSummonT += dt;
            if (bossSummonT >= 10f)
            {
                bossSummonT = 0f;
                var info = Bosses[CampaignPhase];
                var m = System.Array.Find(deck, d => d.key == info.minion);
                if (m != null)
                {
                    SpawnTroop(m, 1, (Vector2)boss.transform.position + new Vector2(0.8f, 0.6f));
                    Banner(info.name + " chamou reforços!");
                    Sfx.Play("alerta", 0.5f);
                }
            }
        }
    }

    void SpawnBoss()
    {
        bossSpawned = true;
        var info = Bosses[CampaignPhase];
        var card = System.Array.Find(deck, d => d.key == info.model);
        if (card == null) return;
        float lane = Random.value < 0.5f ? -LaneX : LaneX;
        int before = entities.Count;
        SpawnTroop(card, 1, new Vector2(lane, 6.2f));
        if (entities.Count <= before) return;
        boss = entities[entities.Count - 1];
        boss.maxHp = 2500f + 700f * CampaignPhase;
        boss.hp = boss.maxHp;
        boss.damage = card.damage * (2f + 0.25f * CampaignPhase);
        boss.isChampion = true;
        boss.tint = info.tint;
        boss.transform.localScale = Vector3.one * 1.45f;
        Banner("CHEFE: " + info.name.ToUpper());
        Sfx.Play("alerta");
        shake = 0.3f;
    }

    void DrawBossBar(Rect col, float u, int fs)
    {
        if (Mode != MatchMode.Campanha || boss == null) return;
        var info = Bosses[CampaignPhase];
        var r = new Rect(col.x + u * 4f, col.y + u * 9f, col.width - u * 8f, u * 4.2f);
        Ui.Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), new Color(0.1f, 0.05f, 0.1f, 0.9f));
        float k = boss.Alive ? Mathf.Clamp01(boss.hp / boss.maxHp) : 0f;
        Ui.Fill(r, new Color(0.25f, 0.08f, 0.1f));
        Ui.Fill(new Rect(r.x, r.y, r.width * k, r.height), new Color(0.85f, 0.2f, 0.3f));
        Ui.Fill(new Rect(r.x, r.y, r.width * k, r.height * 0.4f), new Color(1f, 1f, 1f, 0.15f));
        Ui.Text(r, boss.Alive ? $"{info.name}  {Mathf.CeilToInt(boss.hp)}" : $"{info.name} derrotado!", fs - 3, Color.white);
    }

    // ---------------------------------------------------------------- Clima das arenas

    float weatherT;

    void UpdateWeather(float dt)
    {
        weatherT += dt;
        float rate = matchArena == 2 ? 9f : matchArena == 3 ? 5f : 2f;
        while (weatherT > 1f / rate)
        {
            weatherT -= 1f / rate;
            float x = Random.Range(-7.5f, 7.5f);
            switch (matchArena)
            {
                case 0:   // folhas voando
                {
                    var p = Spawn(new Vector2(x, 12f), SquareSprite,
                        Random.value < 0.5f ? new Color(0.45f, 0.75f, 0.3f, 0.9f) : new Color(0.9f, 0.8f, 0.3f, 0.9f), 0.12f, 9f, 2500);
                    if (p == null) return;
                    p.t.localScale = new Vector3(0.18f, 0.09f, 1f);
                    p.v = new Vector2(Random.Range(0.4f, 1.2f), -Random.Range(1.2f, 2f));
                    p.spin = Random.Range(-200f, 200f);
                    break;
                }
                case 1:   // poeira no ar
                {
                    var p = Spawn(new Vector2(-8f, Random.Range(-12f, 11f)), CircleSprite, new Color(0.9f, 0.85f, 0.7f, 0.25f),
                        Random.Range(0.06f, 0.14f), 8f, 2500);
                    if (p == null) return;
                    p.v = new Vector2(Random.Range(1.5f, 2.5f), Random.Range(-0.2f, 0.2f));
                    break;
                }
                case 2:   // neve
                {
                    var p = Spawn(new Vector2(x, 12f), CircleSprite, new Color(1f, 1f, 1f, 0.85f), Random.Range(0.06f, 0.14f), 14f, 2500);
                    if (p == null) return;
                    p.v = new Vector2(Random.Range(-0.3f, 0.3f), -Random.Range(1.2f, 2.2f));
                    break;
                }
                default:  // brasas subindo e cinzas caindo
                {
                    bool ember = Random.value < 0.6f;
                    var p = Spawn(new Vector2(x, ember ? -13f : 12f), CircleSprite,
                        ember ? new Color(1f, 0.55f, 0.15f, 0.9f) : new Color(0.4f, 0.38f, 0.38f, 0.6f), Random.Range(0.05f, 0.11f), 12f, 2500);
                    if (p == null) return;
                    p.v = ember ? new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(1.5f, 2.6f)) : new Vector2(Random.Range(-0.3f, 0.3f), -Random.Range(0.8f, 1.4f));
                    break;
                }
            }
        }
    }

    // ---------------------------------------------------------------- Final dramático (torre do rei caindo)

    IEnumerator KingFinale(Vector2 pos, int winner)
    {
        if (finale) yield break;
        finale = true;
        camFocus = pos;
        Time.timeScale = 0.3f;
        Sfx.Play("explosao", 1f, 0f, 0f);
        float t = 0f;
        while (t < 0.6f) { t += Time.unscaledDeltaTime; camZoom = t / 0.6f; yield return null; }
        camZoom = 1f;
        for (int i = 0; i < 3; i++)
        {
            Debris(pos + Random.insideUnitCircle * 0.8f + Vector2.up, 8, new Color(0.6f, 0.55f, 0.5f));
            Dust(pos + Random.insideUnitCircle * 0.6f, 1.6f);
            float w = 0f;
            while (w < 0.35f) { w += Time.unscaledDeltaTime; yield return null; }
        }
        t = 0f;
        while (t < 0.5f) { t += Time.unscaledDeltaTime; camZoom = 1f - t / 0.5f; yield return null; }
        camZoom = 0f;
        Time.timeScale = 1f;
        finale = false;
        EndMatch(winner);
    }

    // ---------------------------------------------------------------- Regras das arenas

    public static readonly string[] ArenaRules =
    {
        null,
        "Pedras rolam pelas pistas de vez em quando!",
        "Chão de gelo: as tropas andam 15% mais rápido.",
        "Chuva de lava: pedras quentes caem no meio da arena!"
    };

    void UpdateArenaRule(float dt)
    {
        if (Mode == MatchMode.Tutorial || matchArena == 0 || matchArena == 2) return;
        ruleT += dt;
        if (matchArena == 1 && ruleT >= 22f) { ruleT = 0f; StartCoroutine(RollingBoulder()); }
        if (matchArena == 3 && ruleT >= 18f) { ruleT = 0f; StartCoroutine(LavaRock()); }
    }

    IEnumerator RollingBoulder()
    {
        float y = Random.Range(-5.5f, 5.5f);
        bool leftToRight = Random.value < 0.5f;
        var go = new GameObject("Pedra");
        go.transform.SetParent(matchRoot);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CircleSprite;
        sr.color = new Color(0.55f, 0.5f, 0.45f);
        sr.sortingOrder = 1000 - Mathf.RoundToInt(y * 20f);
        go.transform.localScale = Vector3.one * 1.1f;
        var spot = new GameObject("Mancha");
        spot.transform.SetParent(go.transform, false);
        var ssr = spot.AddComponent<SpriteRenderer>();
        ssr.sprite = CircleSprite;
        ssr.color = new Color(0.4f, 0.36f, 0.32f);
        ssr.sortingOrder = sr.sortingOrder + 1;
        spot.transform.localScale = Vector3.one * 0.35f;
        Sfx.Play("queda", 0.6f);
        var hit = new HashSet<Entity>();
        float t = 0f, dur = 2.6f;
        while (t < dur && go != null)
        {
            t += Time.deltaTime;
            float x = Mathf.Lerp(-7f, 7f, leftToRight ? t / dur : 1f - t / dur);
            go.transform.position = new Vector2(x, y + Mathf.Abs(Mathf.Sin(t * 9f)) * 0.15f);
            spot.transform.localPosition = new Vector3(Mathf.Cos(t * 12f) * 0.25f, Mathf.Sin(t * 12f) * 0.25f, 0f);
            if (Random.value < 0.3f) Dust(new Vector2(x, y - 0.3f), 0.6f);
            foreach (var o in entities.ToArray())
            {
                if (o == null || o.isTower || o.flying || !o.Alive || hit.Contains(o)) continue;
                if (Vector2.Distance(o.transform.position, go.transform.position) > 0.6f + o.radius) continue;
                hit.Add(o);
                o.TakeDamage(120f);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator LavaRock()
    {
        Vector2 pos = new Vector2(Random.Range(-4.5f, 4.5f), Random.Range(-3f, 3f));
        var warn = new GameObject("Aviso");
        warn.transform.SetParent(matchRoot);
        warn.transform.position = pos;
        var wr = warn.AddComponent<SpriteRenderer>();
        wr.sprite = CircleSprite;
        wr.sortingOrder = -800;
        warn.transform.localScale = new Vector3(2.6f, 1.6f, 1f);
        Sfx.Play("alerta", 0.4f);
        float t = 0f;
        while (t < 1.5f && warn != null)
        {
            t += Time.deltaTime;
            wr.color = new Color(1f, 0.3f, 0.1f, 0.25f + 0.2f * Mathf.Sin(t * 14f));
            yield return null;
        }
        if (warn != null) Destroy(warn);
        if (matchRoot == null) yield break;
        StartCoroutine(SpellFx(pos, 1.3f, new Color(1f, 0.45f, 0.1f)));
        Sfx.Play("explosao", 0.8f);
        shake = Mathf.Max(shake, 0.2f);
        Scorch(pos, 1.1f);
        Debris(pos, 8, new Color(0.35f, 0.2f, 0.15f));
        foreach (var o in entities.ToArray())
        {
            if (o == null || o.isTower || o.flying || !o.Alive) continue;
            if (Vector2.Distance(o.transform.position, pos) <= 1.3f + o.radius) o.TakeDamage(150f);
        }
    }
}
