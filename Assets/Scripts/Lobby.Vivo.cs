using System.Collections.Generic;
using UnityEngine;

// Avisos que deslizam e lobby animado.
public partial class Lobby
{
    // ------------------------------------------------------------ Avisos

    void CheckNotes()
    {
        bool chestReady = Profile.FreeChestReady || Profile.CrownChestReady;
        foreach (var c in Profile.Chests) if (c.type >= 0 && c.Ready) chestReady = true;
        Notify("bau", chestReady, "Baú pronto para abrir!", Ui.Gold, 2);
        Notify("missao", Profile.AnyMissionToClaim, "Missão completa! Pegue o prêmio", new Color(0.3f, 0.85f, 0.45f), 4);
        Notify("passe", Profile.PassTiersReady() > 0, "Prêmio do passe liberado!", new Color(0.75f, 0.45f, 1f), 3);
        Notify("conq", Profile.AchievementsReady() > 0, "Conquista desbloqueada!", new Color(0.3f, 0.8f, 1f), -1);
        Notify("evoluir", AnyUpgradeReady(), "Uma carta pode evoluir!", Ui.Orange, 1);
        Notify("maestria", Profile.AnyMasteryReady(), "Prêmio de maestria liberado!", Ui.Gold, 1);
    }

    void Notify(string key, bool now, string text, Color c, int tab)
    {
        bool before = noteState.TryGetValue(key, out bool b) && b;
        noteState[key] = now;
        if (now && !before && notes.Count < 3)
        {
            notes.Add(new Note { text = text, color = c, tab = tab });
            Sfx.Play("bip", 0.4f, 0.05f);
        }
    }

    void DrawNotes(Rect col, float u, int fs)
    {
        if (Event.current.type == EventType.Repaint)
            foreach (var n in notes) n.t += Time.deltaTime;
        notes.RemoveAll(n => n.t > 4f);
        for (int i = 0; i < notes.Count; i++)
        {
            var n = notes[i];
            float inT = Mathf.Clamp01(n.t / 0.3f), outT = Mathf.Clamp01((n.t - 3.5f) / 0.5f);
            float slide = (1f - inT * inT) * -u * 8f;
            float a = 1f - outT;
            var r = new Rect(col.x + u * 4f, col.y + u * 12.5f + i * u * 6.2f + slide, col.width - u * 8f, u * 5.4f);
            Ui.Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), new Color(0.05f, 0.05f, 0.1f, 0.8f * a));
            Ui.Fill(r, new Color(0.13f, 0.15f, 0.25f, 0.95f * a));
            Ui.Fill(new Rect(r.x, r.y, u * 1.2f, r.height), new Color(n.color.r, n.color.g, n.color.b, a));
            Ui.Disc(new Rect(r.x + u * 2.2f, r.y + u * 1f, u * 3.4f, u * 3.4f), new Color(n.color.r, n.color.g, n.color.b, a));
            Ui.Text(new Rect(r.x + u * 2.2f, r.y + u * 1f, u * 3.4f, u * 3.4f), "!", fs, new Color(1f, 1f, 1f, a));
            Ui.Text(new Rect(r.x + u * 7f, r.y, r.width - u * 8f, r.height), n.text, fs - 2, new Color(1f, 1f, 1f, a), TextAnchor.MiddleLeft);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                if (n.tab >= 0) tab = n.tab; else profileOpen = true;
                notes.Remove(n);
                Sfx.Play("botao");
                break;
            }
        }
    }

    // ------------------------------------------------------------ Lobby vivo: nuvens e tropas do deck passeando

    void DrawClouds(Rect r, float u)
    {
        GUI.BeginGroup(r);              // corta as nuvens nas bordas da área da ilha
        var r0 = r;
        r = new Rect(0, 0, r0.width, r0.height);
        for (int i = 0; i < 4; i++)
        {
            float speed = u * (1.2f + i * 0.5f);
            float w = u * (9f + i * 2f);
            float x = r.x - w + Mathf.Repeat(Time.time * speed + i * 37f * u, r.width + w * 2f);
            float y = r.y + u * (2f + i * 6.5f);
            var c = new Color(1f, 1f, 1f, 0.10f + 0.04f * (i % 2));
            Ui.Disc(new Rect(x, y, w, u * 3.5f), c);
            Ui.Disc(new Rect(x + w * 0.25f, y - u * 1.6f, w * 0.5f, u * 3.5f), c);
        }
        GUI.EndGroup();
    }

    readonly Dictionary<string, Texture2D> walkerTex = new Dictionary<string, Texture2D>();

    // Três tropas do deck andam de um lado para o outro na frente da ilha (quadros da folha de animação)
    void DrawWalkers(Rect r, float u)
    {
        var deck = Profile.Deck;
        int n = 0;
        for (int i = 0; i < deck.Count && n < 3; i++)
        {
            var c = Profile.Find(deck[i]);
            if (c == null || c.kind == CardKind.Spell || c.kind == CardKind.Building || c.flying) continue;
            if (!walkerTex.TryGetValue(c.key, out var tex)) walkerTex[c.key] = tex = Resources.Load<Texture2D>("Tropas/" + c.key);
            if (tex == null) continue;
            float period = 16f + n * 3f;
            float k = Mathf.Repeat(Time.time / period + n * 0.31f, 1f);
            bool right = k < 0.5f;
            float pos = right ? k * 2f : (1f - k) * 2f;
            float size = u * 9f;
            float x = r.x + u + pos * (r.width - size - u * 2f);
            float y = r.yMax - size - u * 2.5f + n * u * 1.2f;
            int frame = 1 + (int)(Time.time * 8f + n) % 4;           // quadros de andar: colunas 1 a 4
            // linha 0 = azul de frente (olha para a direita); para ir à esquerda, espelha
            var uv = right ? new Rect(frame / 7f, 0.75f, 1f / 7f, 0.25f) : new Rect((frame + 1) / 7f, 0.75f, -1f / 7f, 0.25f);
            var dst = new Rect(x, y, size, size);
            Ui.Disc(new Rect(x + size * 0.2f, y + size * 0.8f, size * 0.6f, size * 0.18f), new Color(0f, 0f, 0f, 0.25f));
            GUI.DrawTextureWithTexCoords(dst, tex, uv);
            n++;
        }
    }
}
