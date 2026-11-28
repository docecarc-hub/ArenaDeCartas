using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Habilidades dos Campeões (Investida e Escudo de Vento).
public partial class ArenaGame
{
    // ---------------------------------------------------------------- Habilidade dos Campeões

    public bool CanUseAbility(int team)
    {
        var e = champ[team];
        if (e == null || !e.Alive || e.Deploying || !Running) return false;
        var c = CardOf(e);
        return c != null && abilityCd[team] <= 0f && elixir[team] >= c.abilityCost;
    }

    CardData CardOf(Entity e) => e == null ? null : System.Array.Find(deck, d => d.key == e.cardKey);

    public void UseAbility(int team)
    {
        if (!CanUseAbility(team)) return;
        var e = champ[team];
        var c = CardOf(e);
        elixir[team] -= c.abilityCost;
        abilityCd[team] = c.abilityCooldown;
        Sfx.Play("magia", 0.9f);
        FloatText(e.transform.position + Vector3.up * 1.6f, c.abilityName + "!", new Color(1f, 0.85f, 0.3f), 0.045f);
        if (c.key == "aur") StartCoroutine(Charge(e, team));
        else if (c.key == "ven") WindShield(e, team);
    }

    // Investida: avança 3,5 unidades, ferindo cada inimigo uma vez no caminho
    IEnumerator Charge(Entity e, int team)
    {
        Entity goal = FindTarget(e) ?? NearestEnemyTower(e);
        Vector2 from = e.transform.position;
        Vector2 dir = goal != null ? ((Vector2)goal.transform.position - from).normalized : (team == 0 ? Vector2.up : Vector2.down);
        Vector2 to = from + dir * 3.5f;
        to.x = Mathf.Clamp(to.x, -ArenaHalfW + 0.5f, ArenaHalfW - 0.5f);
        var hit = new HashSet<Entity>();
        float t = 0f;
        while (t < 0.45f && e != null && e.Alive)
        {
            t += Time.deltaTime;
            e.transform.position = Vector2.Lerp(from, to, t / 0.45f);
            if (Random.value < 0.5f) Dust(e.transform.position, 0.7f);
            foreach (var o in entities.ToArray())
            {
                if (o == null || o.team == team || !o.Alive || o.flying || hit.Contains(o)) continue;
                if (Vector2.Distance(o.transform.position, e.transform.position) > 1.0f + o.radius) continue;
                hit.Add(o);
                o.TakeDamage(o.isTower ? e.damage : e.damage * 1.6f);
                Sparks(o.transform.position);
            }
            yield return null;
        }
        shake = Mathf.Max(shake, 0.15f);
    }

    // Escudo de Vento: tropas aliadas por perto ganham escudo por 6 segundos
    void WindShield(Entity e, int team)
    {
        float amount = 250f * Profile.LevelMult(team == 0 ? Profile.CardLevel("ven") : botLevel);
        StartCoroutine(SpellFx(e.transform.position, 3.5f, new Color(0.5f, 0.95f, 1f)));
        foreach (var o in entities)
        {
            if (o == null || o.team != team || o.isTower || !o.Alive) continue;
            if (Vector2.Distance(o.transform.position, e.transform.position) > 3.5f) continue;
            o.shield = amount;
            o.shieldTime = 6f;
        }
    }

    void BotAbility()
    {
        if (Mode == MatchMode.Tutorial || !CanUseAbility(1)) return;
        var e = champ[1];
        int near = 0;
        foreach (var o in entities)
            if (o != null && o.team == 0 && o.Alive && Vector2.Distance(o.transform.position, e.transform.position) < 3.5f) near++;
        if (near >= 1 && Random.value < 0.02f) UseAbility(1);
    }

    // Botão da habilidade (aparece com o seu Campeão em campo)
    void DrawAbilityButton(Rect col, float u, int fs, float panelTop)
    {
        var e = champ[0];
        if (e == null || !e.Alive) return;
        var c = CardOf(e);
        if (c == null) return;
        var r = new Rect(col.xMax - u * 11f, panelTop - u * 11.5f, u * 10f, u * 10f);
        bool ready = CanUseAbility(0);
        float pulse = ready ? 1f + 0.05f * Mathf.Sin(Time.time * 6f) : 1f;
        var rr = new Rect(r.center.x - r.width * pulse / 2f, r.center.y - r.height * pulse / 2f, r.width * pulse, r.height * pulse);
        Ui.Disc(new Rect(rr.x - 3, rr.y - 3, rr.width + 6, rr.height + 6), ready ? Ui.Gold : new Color(0.3f, 0.3f, 0.35f));
        Ui.Disc(rr, new Color(0.15f, 0.12f, 0.25f));
        if (c.Portrait != null)
        {
            var old = GUI.color;
            GUI.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            GUI.DrawTexture(new Rect(rr.x + u, rr.y + u * 0.6f, rr.width - u * 2f, rr.height - u * 2f), c.Portrait, ScaleMode.ScaleToFit);
            GUI.color = old;
        }
        if (abilityCd[0] > 0f)
        {
            float k = abilityCd[0] / c.abilityCooldown;
            Ui.Fill(new Rect(rr.x + u, rr.yMax - u - (rr.height - u * 2f) * k, rr.width - u * 2f, (rr.height - u * 2f) * k), new Color(0f, 0f, 0f, 0.45f));
            Ui.Text(rr, Mathf.CeilToInt(abilityCd[0]).ToString(), fs + 2, Color.white);
        }
        var cost = new Rect(rr.xMax - u * 4f, rr.yMax - u * 4f, u * 4.2f, u * 4.2f);
        Ui.Disc(cost, Ui.Elixir);
        Ui.Text(cost, c.abilityCost.ToString(), fs - 1, Color.white);
        Ui.Text(new Rect(r.x - u * 4f, r.y - u * 3.2f, r.width + u * 8f, u * 3f), c.abilityName, fs - 5, ready ? Ui.Gold : Color.white);
        if (Running && GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            if (ready) UseAbility(0);
            else { Sfx.Play("erro"); Toast(abilityCd[0] > 0f ? "Habilidade recarregando" : "Elixir insuficiente"); }
        }
    }
}
