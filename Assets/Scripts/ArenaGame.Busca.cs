using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Telas de buscando adversário e VS.
public partial class ArenaGame
{
    // ---------------------------------------------------------------- Buscando adversário e VS

    void DrawSearch(Rect col, float u, int fs)
    {
        // Fundo de estádio: azul à esquerda, rosa à direita, com holofotes
        int bands = 24;
        for (int i = 0; i < bands; i++)
        {
            float t = i / (float)(bands - 1);
            Ui.Fill(new Rect(col.x + col.width * t, col.y, col.width / bands + 1, col.height),
                Color.Lerp(new Color(0.12f, 0.25f, 0.75f), new Color(0.85f, 0.2f, 0.55f), t));
        }
        Ui.Fill(new Rect(col.x, col.y, col.width, col.height * 0.12f), new Color(0.05f, 0.05f, 0.2f, 0.6f));
        Ui.Fill(new Rect(col.x, col.yMax - col.height * 0.2f, col.width, col.height * 0.2f), new Color(0.25f, 0.05f, 0.4f, 0.55f));
        for (int i = 0; i < 4; i++)
        {
            var m = GUI.matrix;
            float ang = (i < 2 ? 25f : -25f) + Mathf.Sin(Time.time * 0.8f + i) * 8f;
            var pivot = new Vector2(col.x + col.width * (0.1f + i * 0.27f), col.y + col.height * 0.15f);
            GUIUtility.RotateAroundPivot(ang, pivot);
            Ui.Fill(new Rect(pivot.x - u * 2f, pivot.y, u * 4f, col.height * 0.55f), new Color(1f, 1f, 1f, 0.07f));
            GUI.matrix = m;
            Ui.Disc(new Rect(pivot.x - u * 2f, pivot.y - u * 2f, u * 4f, u * 4f), new Color(1f, 1f, 1f, 0.6f));
        }

        // Faixa "Buscando adversário" com lupa
        var bar = new Rect(col.x + u * 2f, col.y + col.height * 0.13f, col.width - u * 4f, u * 10f);
        Ui.Panel(bar, new Color(0.12f, 0.12f, 0.14f, 0.92f));
        var lens = new Rect(bar.x - u * 0.5f, bar.y - u * 2f, u * 11f, u * 11f);
        var m2 = GUI.matrix;
        GUIUtility.RotateAroundPivot(-40f, lens.center);
        Ui.Fill(new Rect(lens.center.x - u * 0.9f, lens.center.y + u * 4f, u * 1.8f, u * 5f), new Color(0.55f, 0.32f, 0.15f));
        GUI.matrix = m2;
        Ui.Disc(lens, new Color(0.85f, 0.85f, 0.9f));
        Ui.Disc(new Rect(lens.x + u, lens.y + u, lens.width - u * 2f, lens.height - u * 2f), new Color(0.6f, 0.8f, 1f));
        Ui.Text(new Rect(lens.xMax + u, bar.y, bar.width - lens.width - u * 2f, bar.height), "Buscando\nadversário",
            fs + 6, Ui.Orange, TextAnchor.MiddleLeft);

        // Ilha da arena flutuando
        var tex = Resources.Load<Texture2D>("Arenas/arena" + Profile.ArenaIndex);
        float bob = Mathf.Sin(Time.time * 2f) * u * 1.2f;
        var isl = new Rect(col.x + col.width * 0.1f, col.y + col.height * 0.3f + bob, col.width * 0.8f, col.height * 0.38f);
        Ui.Disc(new Rect(isl.x + isl.width * 0.15f, isl.yMax - u * 2f - bob, isl.width * 0.7f, u * 4f), new Color(0f, 0f, 0f, 0.25f));
        if (tex != null) GUI.DrawTexture(isl, tex, ScaleMode.ScaleToFit);
        Ui.Text(new Rect(col.x, isl.yMax - u, col.width, u * 4f), ModeNames[(int)pendingMode] + "  ·  " + Profile.ArenaNames[Profile.ArenaIndex],
            fs - 1, Color.white);
        if (ArenaRules[Profile.ArenaIndex] != null && pendingMode != MatchMode.Tutorial)
            Ui.Text(new Rect(col.x + u * 3f, isl.yMax + u * 6.5f, col.width - u * 6f, u * 4f), "Regra: " + ArenaRules[Profile.ArenaIndex],
                fs - 4, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter, false);
        // pontinhos de espera
        int dots = 1 + (int)(Time.time * 2f) % 3;
        Ui.Text(new Rect(col.x, isl.yMax + u * 3f, col.width, u * 3f), new string('.', dots), fs + 6, Color.white);

        if (Ui.Button(new Rect(col.center.x - col.width * 0.22f, col.y + col.height * 0.74f, col.width * 0.44f, u * 8f),
                new Color(0.9f, 0.25f, 0.25f), "Cancelar", fs + 3))
            phase = Phase.None;

        // Dica
        var tip = new Rect(col.x + u * 2f, col.y + col.height * 0.85f, col.width - u * 4f, u * 11f);
        Ui.Panel(tip, new Color(0.62f, 0.8f, 1f));
        Ui.Text(new Rect(tip.x, tip.y - u * 3f, tip.width, u * 4f), "Dica", fs + 1, Color.white);
        Ui.Text(new Rect(tip.x + u * 2f, tip.y + u, tip.width - u * 9f, tip.height - u * 2f), Tips[tipIndex], fs - 3,
            new Color(0.08f, 0.12f, 0.25f), TextAnchor.MiddleCenter, true);
        if (Ui.Button(new Rect(tip.xMax - u * 6f, tip.yMax - u * 6f, u * 5f, u * 5f), new Color(0.35f, 0.55f, 0.9f), ">", fs))
            tipIndex = (tipIndex + 1) % Tips.Length;
    }

    // Tela de VS: duas faixas entram, escudo "VS" aparece, e tudo some revelando a arena
    void DrawVersus(Rect col, float u, int fs)
    {
        float t = phaseT;
        float inT = Mathf.Clamp01(t / 0.45f);
        float ease = 1f - (1f - inT) * (1f - inT) * (1f - inT);
        float outT = Mathf.Clamp01((t - (VsTime - 0.8f)) / 0.8f);
        float alpha = 1f - outT;
        float slideOut = outT * outT * col.width * 1.2f;

        // Fundo: vermelho em cima, azul embaixo, divisão inclinada
        Ui.Fill(col, new Color(0.08f, 0.06f, 0.15f, 0.55f * alpha));
        Ui.Fill(new Rect(col.x, col.y, col.width, col.height * 0.5f), new Color(0.85f, 0.15f, 0.35f, 0.55f * alpha));
        Ui.Fill(new Rect(col.x, col.center.y, col.width, col.height * 0.5f), new Color(0.15f, 0.3f, 0.9f, 0.55f * alpha));

        DrawVsBanner(col, u, fs, true, (1f - ease) * col.width + slideOut, alpha);
        DrawVsBanner(col, u, fs, false, -(1f - ease) * col.width - slideOut, alpha);

        // Escudo VS
        float pop = Mathf.Clamp01((t - 0.35f) / 0.25f);
        float s = (pop < 1f ? Mathf.Lerp(1.6f, 1f, pop) : 1f) * u * 14f * (1f - outT * 0.5f);
        if (pop > 0f)
        {
            var sh = new Rect(col.center.x - s / 2f, col.center.y - s * 0.55f, s, s * 1.1f);
            var a = new Color(1f, 1f, 1f, alpha * pop);
            Ui.Fill(new Rect(sh.x - 4, sh.y - 4, sh.width + 8, sh.height + 8), new Color(0.95f, 0.75f, 0.2f, a.a));
            Ui.Fill(sh, new Color(0.2f, 0.4f, 0.95f, a.a));
            Ui.Fill(new Rect(sh.x, sh.y, sh.width, sh.height * 0.45f), new Color(1f, 1f, 1f, 0.15f * a.a));
            Ui.Text(sh, "VS", Mathf.RoundToInt(fs * 2.6f * s / (u * 14f)), a);
        }
    }

    void DrawVsBanner(Rect col, float u, int fs, bool enemy, float dx, float alpha)
    {
        float h = col.height * 0.18f;
        float y = enemy ? col.y + col.height * 0.14f : col.y + col.height * 0.62f;
        var r = new Rect(col.x + dx - u * 4f, y, col.width + u * 8f, h);
        Color baseC = enemy ? new Color(0.75f, 0.12f, 0.3f) : new Color(0.12f, 0.35f, 0.85f);
        var m = GUI.matrix;
        GUIUtility.RotateAroundPivot(enemy ? -4f : 4f, r.center);
        Ui.Fill(new Rect(r.x, r.y - 4, r.width, r.height + 8), new Color(1f, 0.85f, 0.35f, alpha));
        Ui.Fill(r, new Color(baseC.r, baseC.g, baseC.b, alpha));
        Ui.Fill(new Rect(r.x, r.y, r.width, r.height * 0.4f), new Color(1f, 1f, 1f, 0.12f * alpha));
        // padrão de setas (marca nossa)
        for (int i = 0; i < 6; i++)
        {
            var d = new Rect(r.x + r.width * (0.45f + i * 0.09f), r.center.y - u * 2f, u * 4f, u * 4f);
            Ui.Disc(d, new Color(1f, 1f, 1f, 0.12f * alpha));
        }
        // personagem do deck (retrato da carta)
        CardData hero = HeroCard(enemy ? 1 : 0);
        var tex = hero.Portrait;
        if (tex != null)
        {
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            var pr = enemy ? new Rect(col.x + dx + col.width * 0.08f, r.y - h * 0.35f, h * 1.3f, h * 1.3f)
                           : new Rect(col.x + dx + col.width * 0.55f, r.y - h * 0.35f, h * 1.3f, h * 1.3f);
            GUI.DrawTexture(pr, tex, ScaleMode.ScaleToFit);
            GUI.color = old;
        }
        GUI.matrix = m;

        // nome e troféus
        var white = new Color(1f, 1f, 1f, alpha);
        var gold = new Color(1f, 0.85f, 0.3f, alpha);
        string name = enemy ? botName : Profile.Name;
        int trophies = enemy ? botTrophies : Profile.Trophies;
        float ty = r.yMax + u * 2f;
        var anchor = enemy ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        var tr = new Rect(col.x + dx + u * 3f, ty, col.width - u * 6f, u * 5f);
        Ui.Text(tr, name, fs + 6, white, anchor);
        Ui.Text(new Rect(tr.x, tr.yMax - u * 0.5f, tr.width, u * 4f), $"{trophies} troféus", fs + 1, gold, anchor);
        if (enemy && botMotto != null)
        {
            // estilo de jogo e frase do adversário
            // fica só na metade esquerda para não passar por trás do escudo VS
            float w = col.width * 0.4f;
            Ui.Text(new Rect(tr.x, tr.yMax + u * 3f, w, u * 3f), "Estilo: " + StyleNames[(int)botStyle], fs - 2,
                new Color(1f, 0.6f, 0.8f, alpha), anchor);
            Ui.Text(new Rect(tr.x, tr.yMax + u * 6f, w, u * 6f), "\"" + botMotto + "\"", fs - 4,
                new Color(1f, 1f, 1f, 0.8f * alpha), TextAnchor.UpperLeft, false);
        }
        if (!enemy)
        {
            // selos do jogador: nível, arena e conquistas
            string[] seals = { Profile.Level.ToString(), (Profile.ArenaIndex + 1).ToString(), AchievementsDone().ToString() };
            Color[] cs = { Ui.Teal, Ui.Gold, new Color(0.7f, 0.4f, 1f) };
            for (int i = 0; i < 3; i++)
            {
                var sr = new Rect(col.x + dx + u * 3f + i * u * 7f, r.yMax + u * 2f, u * 6f, u * 6f);
                Ui.Disc(new Rect(sr.x - 2, sr.y - 2, sr.width + 4, sr.height + 4), new Color(0f, 0f, 0f, 0.6f * alpha));
                Ui.Disc(sr, new Color(cs[i].r, cs[i].g, cs[i].b, alpha));
                Ui.Text(sr, seals[i], fs, white);
            }
        }
    }

    // Personagem do banner: a tropa mais cara da mão (feitiço não vira banner)
    CardData HeroCard(int team)
    {
        CardData best = null;
        foreach (int i in hand[team])
            if (deck[i].kind != CardKind.Spell && (best == null || deck[i].cost > best.cost)) best = deck[i];
        foreach (int i in queue[team])
            if (best == null && deck[i].kind != CardKind.Spell) best = deck[i];
        return best ?? deck[hand[team][0]];
    }

    static int AchievementsDone()
    {
        int n = 0;
        foreach (var a in Profile.Achievements) if (Profile.AchievementClaimed(a)) n++;
        return n;
    }

    public void ShowEmote(int team, string text)
    {
        emoteText[team] = text;
        emoteTime[team] = 2.2f;
        Sfx.Play("bip", 0.5f, 0.1f);
    }

    void DrawEmotes(Rect col, float u, int fs, float panelTop)
    {
        // Balões perto das torres do rei
        for (int t = 0; t < 2; t++)
        {
            if (emoteTime[t] <= 0f || emoteText[t] == null) continue;
            Vector3 sp = cam.WorldToScreenPoint(new Vector3(-1.8f, t == 0 ? -6.6f : 9.6f, 0f));
            var c = new Vector2(sp.x, Screen.height - sp.y);
            float pop = Mathf.Min(1f, (2.2f - emoteTime[t]) * 6f);
            var r = new Rect(c.x - u * 8f * pop, c.y - u * 3f * pop, u * 16f * pop, u * 6f * pop);
            Ui.Panel(r, Color.white);
            Ui.Text(r, emoteText[t], Mathf.RoundToInt(fs * pop), t == 0 ? new Color(0.15f, 0.3f, 0.7f) : new Color(0.7f, 0.15f, 0.15f), TextAnchor.MiddleCenter, false);
        }
        if (!Running) return;
        // Botão de emotes acima do painel de cartas
        var btn = new Rect(col.x + u, panelTop - u * 6.5f, u * 6f, u * 5.5f);
        if (Ui.Button(btn, new Color(0.2f, 0.45f, 0.8f), ":)", fs)) emoteMenu = !emoteMenu;
        if (!emoteMenu) return;
        for (int i = 0; i < Emotes.Length; i++)
        {
            var r = new Rect(btn.xMax + u + i * u * 11.5f, btn.y, u * 11f, btn.height);
            if (Ui.Button(r, Ui.Teal, Emotes[i], fs - 3))
            {
                ShowEmote(0, Emotes[i]);
                emoteMenu = false;
                if (Random.value < 0.6f) botEmoteDelay = Random.Range(0.8f, 1.6f);   // o bot responde
            }
        }
    }

    void SpawnConfetti()
    {
        Color[] cs = { Ui.Gold, Ui.Orange, Ui.Elixir, Ui.Teal, new Color(0.4f, 0.8f, 1f), Color.white };
        Rect col = Ui.Column();
        for (int i = 0; i < 120; i++)
            confetti.Add(new Confetto
            {
                p = new Vector2(col.x + Random.value * col.width, col.y - Random.value * col.height * 0.5f),
                v = new Vector2(Random.Range(-40f, 40f), Random.Range(80f, 220f)) * (col.height / 700f),
                c = cs[Random.Range(0, cs.Length)], rot = Random.value * 6f, size = Random.Range(5f, 10f) * (col.height / 700f)
            });
    }

    void UpdateConfetti(float dt)
    {
        for (int i = confetti.Count - 1; i >= 0; i--)
        {
            var c = confetti[i];
            c.p += c.v * dt;
            c.p.x += Mathf.Sin(Time.time * 4f + i) * 30f * dt;
            c.rot += dt * 5f;
            confetti[i] = c;
            if (c.p.y > Screen.height + 20f) confetti.RemoveAt(i);
        }
    }

    void DrawConfetti()
    {
        foreach (var c in confetti)
            Ui.Fill(new Rect(c.p.x, c.p.y, c.size, c.size * Mathf.Abs(Mathf.Cos(c.rot)) + 1f), c.c);
    }

    // Moldura + desenho do personagem + nome. Usado na mão, no lobby e no Caminho de Troféus.
    public static void DrawCardFace(Rect r, CardData c, int fs)
    {
        bool evo = Profile.Evolved(c.key);
        Ui.Fill(r, evo ? new Color(0.7f, 0.3f, 1f) : Ui.Shade(CardData.RarityColors[c.rarity], 0.85f));
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
        // Estrelas de maestria no canto
        int mt = Profile.MasteryClaimedTier(c.key);
        for (int i = 0; i < mt; i++)
        {
            var st = new Rect(inner.xMax - (i + 1) * inner.width * 0.16f, inner.y + 2, inner.width * 0.15f, inner.width * 0.15f);
            Ui.Disc(new Rect(st.x - 1, st.y - 1, st.width + 2, st.height + 2), new Color(0.3f, 0.2f, 0f));
            Ui.Disc(st, Ui.Gold);
        }
        if (evo)
        {
            var tag = new Rect(inner.x + 2, inner.y + 2, inner.width * 0.42f, inner.height * 0.16f);
            Ui.Fill(tag, new Color(0.55f, 0.2f, 0.9f));
            Ui.Text(tag, "EVO", Mathf.Max(7, Mathf.RoundToInt(fs * 0.75f)), Color.white);
        }
        if (c.champion)
        {
            // coroinha de Campeão no topo da carta
            var cr = new Rect(r.center.x - r.width * 0.2f, r.y - r.height * 0.06f, r.width * 0.4f, r.height * 0.12f);
            Ui.Fill(new Rect(cr.x, cr.y + cr.height * 0.45f, cr.width, cr.height * 0.55f), new Color(1f, 0.8f, 0.25f));
            for (int i = 0; i < 3; i++)
                Ui.Disc(new Rect(cr.x + cr.width * (0.02f + i * 0.36f), cr.y, cr.width * 0.26f, cr.width * 0.26f), new Color(1f, 0.8f, 0.25f));
        }
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
