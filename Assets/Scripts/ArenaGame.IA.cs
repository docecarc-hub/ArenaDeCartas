using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// IA do bot: defesa, feitiços e ataques.
public partial class ArenaGame
{
    // ---------------------------------------------------------------- IA do bot
    // Defende com a carta certa (área contra enxame, dano alto contra tanque), usa feitiços em grupos,
    // junta elixir e ataca pela pista mais fraca com tanque na frente e suporte atrás.
    // Fica mais esperta conforme seus troféus sobem.

    static readonly HashSet<string> TankKeys = new HashSet<string> { "gig", "gol", "yet", "cav", "esc", "gue" };
    Entity aiTank;          // tanque que o bot acabou de jogar (para mandar suporte atrás)

    void UpdateAI(float dt)
    {
        if (Mode == MatchMode.Tutorial && tutStep < TutFinal) return;
        aiThink -= dt;
        if (aiThink > 0f) return;
        float skill = Mode == MatchMode.Campanha ? 0.25f + 0.07f * CampaignPhase
            : Mode == MatchMode.Treino || Mode == MatchMode.Tutorial ? 0f : Mathf.Clamp01(Profile.Trophies / 1000f);
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
        // Agressivo ignora ameaças pequenas e continua atacando
        if (lead != null && botStyle == BotStyle.Agressivo && threats.Count == 1 && lead.maxHp < 800f && elixir[1] >= 5f && Random.value < 0.6f)
            lead = null;
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
            if (c.kind != CardKind.Spell || c.cost > elixir[1] || c.key == "cur") continue;
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
                bool magic = botStyle == BotStyle.Feiticeiro;
                bool worth = count >= (magic ? 2 : 3) || value >= dmg * (magic ? 0.9f : 1.3f) || (value >= c.cost * (magic ? 80f : 120f));
                if (!worth || Random.value > (magic ? 0.6f : 0.35f) + 0.6f * skill) continue;
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
        if (lead.isBuilding) return;   // construção inimiga não é ameaça que ande

        int best = -1;
        float bestScore = float.MinValue;
        for (int slot = 0; slot < 4; slot++)
        {
            CardData c = deck[hand[1][slot]];
            if (c.kind == CardKind.Spell || c.cost > elixir[1]) continue;
            if (lead.flying && !c.hitsAir) continue;          // não adianta mandar quem não acerta o ar
            float score = Random.value * (1.5f - skill);
            if (c.kind == CardKind.Building) score += lead.targetsTowersOnly ? 2.5f : 0.3f;   // construção puxa o tanque
            if (c.kind == CardKind.Building && botStyle == BotStyle.Defensivo) score += 1.5f;
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
        Vector2 pos = card.kind == CardKind.Building ? new Vector2(Mathf.Clamp(lp.x * 0.35f, -2f, 2f), 4.2f)
            : card.range > 1.5f
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
        if (botStyle == BotStyle.Agressivo) want -= 2f;
        if (botStyle == BotStyle.Defensivo) want = 9.6f;
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

        // Mão sem tropas e elixir cheio: não fica parado — solta uma construção na defesa
        // ou um feitiço de dano na torre lateral mais fraca
        if (elixir[1] < 9.8f) return;
        for (int slot = 0; slot < 4; slot++)
        {
            CardData c = deck[hand[1][slot]];
            if (c.cost > elixir[1]) continue;
            if (c.kind == CardKind.Building && TryPlay(1, slot, new Vector2(Random.Range(-1.5f, 1.5f), 4.2f))) return;
            if (c.kind == CardKind.Spell && c.key != "cur" && c.key != "gel" && c.key != "tor")
            {
                foreach (var e in entities)
                    if (e != null && e.team == 0 && e.isTower && !e.isKing && e.Alive
                        && (e.transform.position.x < 0f) == (lane == 0) && TryPlay(1, slot, e.transform.position)) return;
            }
        }
    }
}
