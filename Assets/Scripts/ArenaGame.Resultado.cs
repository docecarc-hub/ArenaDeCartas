using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Tela de fim de partida e toque/arrasto das cartas.
public partial class ArenaGame
{
    // ---------------------------------------------------------------- Fim de partida

    // Ícones 3D da tela de fim (Resources/Icones)
    Texture2D texCrown, texCrownGray, texCushion, texTrophy, texCoins;
    readonly Texture2D[] texChest = new Texture2D[3];

    void LoadResultIcons()
    {
        if (texCrown != null) return;
        texCrown = Resources.Load<Texture2D>("Icones/coroa");
        texCrownGray = Resources.Load<Texture2D>("Icones/coroa_cinza");
        texCushion = Resources.Load<Texture2D>("Icones/almofada");
        texTrophy = Resources.Load<Texture2D>("Icones/trofeu");
        texCoins = Resources.Load<Texture2D>("Icones/moedas");
        for (int i = 0; i < 3; i++) texChest[i] = Resources.Load<Texture2D>("Icones/bau" + i);
    }

    static void Tex(Rect r, Texture t, float a = 1f)
    {
        if (t == null) return;
        var old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, a);
        GUI.DrawTexture(r, t, ScaleMode.ScaleToFit);
        GUI.color = old;
    }

    // Texto grande com contorno escuro
    static void Outlined(Rect r, string text, int size, Color c, float a = 1f)
    {
        var o = new Color(0.08f, 0.05f, 0.15f, a);
        for (int dx = -2; dx <= 2; dx += 2)
            for (int dy = -2; dy <= 3; dy += 2)
                if (dx != 0 || dy != 0) Ui.Text(new Rect(r.x + dx, r.y + dy, r.width, r.height), text, size, o, TextAnchor.MiddleCenter);
        Ui.Text(r, text, size, new Color(c.r, c.g, c.b, a), TextAnchor.MiddleCenter);
    }

    // Faixa com pontas dobradas (placa do jogador)
    static void Ribbon(Rect r, Color c, float u)
    {
        var dark = Ui.Shade(c, 0.6f);
        float tail = u * 3f;
        Ui.Fill(new Rect(r.x - tail, r.y + u * 1.4f, tail + u, r.height), dark);
        Ui.Fill(new Rect(r.xMax - u, r.y + u * 1.4f, tail + u, r.height), dark);
        Ui.Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(0.06f, 0.04f, 0.1f));
        Ui.Fill(r, c);
        Ui.Fill(new Rect(r.x, r.y, r.width, r.height * 0.42f), new Color(1f, 1f, 1f, 0.14f));
        Ui.Fill(new Rect(r.x, r.yMax - u * 0.7f, r.width, u * 0.7f), new Color(0f, 0f, 0f, 0.2f));
    }

    void DrawCrownRow(Rect row, int count, float u, bool winner)
    {
        float cw = row.height * 1.15f, gap = u * 2f;
        float x0 = row.center.x - (cw * 3f + gap * 2f) / 2f;
        for (int i = 0; i < 3; i++)
        {
            var r = new Rect(x0 + i * (cw + gap), row.y, cw, row.height);
            Tex(new Rect(r.x - u, r.yMax - r.height * 0.42f, r.width + u * 2f, r.height * 0.42f), texCushion);
            if (i >= count) continue;
            float appear = resultT - 0.5f - i * 0.4f;
            if (appear <= 0f) continue;
            float pop = Mathf.Clamp01(appear / 0.22f);
            float s = Mathf.Lerp(1.8f, 1f, pop * pop) * (winner ? 1f + 0.04f * Mathf.Sin(Time.time * 4f + i) : 1f);
            float drop = (1f - pop) * -u * 6f;
            var cr = new Rect(r.center.x - r.width * 0.5f * s, r.y + r.height * 0.05f + drop, r.width * s, r.height * 0.8f * s);
            Tex(cr, texCrown, Mathf.Clamp01(appear * 6f));
        }
    }

    void DrawPlate(Rect r, bool enemy, float u, int fs)
    {
        Ribbon(r, enemy ? new Color(0.82f, 0.18f, 0.36f) : new Color(0.2f, 0.42f, 0.9f), u);
        var shield = new Rect(r.x + u * 1.2f, r.y + u * 0.8f, r.height - u * 1.6f, r.height - u * 1.6f);
        Ui.Panel(shield, Ui.Shade(Ui.Gold, 0.85f));
        Ui.Text(shield, enemy ? (Profile.ArenaIndex + 1).ToString() : Profile.Level.ToString(), fs + 1, Color.white);
        Ui.Text(new Rect(r.x, r.y + u * 0.4f, r.width, r.height * 0.58f), enemy ? botName : Profile.Name, fs + 3, Color.white);
        Ui.Text(new Rect(r.x, r.y + r.height * 0.52f, r.width, r.height * 0.42f),
            enemy ? "Bot" : Profile.ArenaNames[Profile.ArenaIndex], fs - 4, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter, false);
        if (!enemy && Mode == MatchMode.Normal && resultT > 1.9f)
        {
            var t = new Rect(r.xMax - u * 14f, r.y, u * 13f, r.height);
            Tex(new Rect(t.x, t.center.y - u * 2.4f, u * 4.6f, u * 4.8f), texTrophy);
            Outlined(new Rect(t.x + u * 4.8f, t.y, u * 8f, t.height), (resTrophies >= 0 ? "+" : "") + resTrophies, fs + 2,
                resTrophies > 0 ? Ui.Gold : resTrophies < 0 ? new Color(1f, 0.5f, 0.5f) : Color.white);
        }
    }

    void DrawResult(Rect col, float u, int fs)
    {
        LoadResultIcons();
        if (Event.current.type == EventType.Repaint) resultT += Time.deltaTime;
        float a = Mathf.Clamp01(resultT * 3f);

        // Fundo próprio: losangos + vermelho em cima e azul embaixo
        Ui.DiamondBackground(col, col.width / 5f);
        for (int i = 0; i < 20; i++)
        {
            float t = i / 19f;
            var band = new Rect(col.x, col.y + col.height * t, col.width, col.height / 20f + 1f);
            Ui.Fill(band, Color.Lerp(new Color(0.55f, 0.08f, 0.25f, 0.55f), new Color(0.08f, 0.2f, 0.6f, 0.55f), t));
        }
        Ui.Fill(col, new Color(0f, 0f, 0f, 0.15f * (1f - a)));

        // Som de cada coroa
        int target = Mathf.Clamp(Mathf.FloorToInt((resultT - 0.5f) / 0.4f) + 1, 0, 3);
        if (target > crownsShown && Event.current.type == EventType.Repaint)
        {
            crownsShown = target;
            if (crownsShown <= Mathf.Max(crowns[0], crowns[1])) Sfx.Play("coroa", 0.8f);
        }

        bool enemyWon = resWinner == 1, iWon = resWinner == 0;
        float top = col.y + col.height * 0.06f;

        // Adversário
        var enemyCrowns = new Rect(col.x, top + u * 6f, col.width, u * 11f);
        DrawCrownRow(enemyCrowns, crowns[1], u, enemyWon);
        var ep = new Rect(col.x + u * 5f, enemyCrowns.yMax + u * 1.5f, col.width - u * 10f, u * 8.5f);
        DrawPlate(ep, true, u, fs);
        if (emoteTime[1] > 0f && emoteText[1] != null)
        {
            var b = new Rect(ep.x - u * 2f, enemyCrowns.y - u * 1f, u * 13f, u * 5f);
            Ui.Panel(b, Color.white);
            Ui.Text(b, emoteText[1], fs - 2, new Color(0.7f, 0.15f, 0.15f), TextAnchor.MiddleCenter, false);
        }

        // Você
        var myCrowns = new Rect(col.x, ep.yMax + u * 13f, col.width, u * 11f);
        DrawCrownRow(myCrowns, crowns[0], u, iWon);
        var mp = new Rect(col.x + u * 5f, myCrowns.yMax + u * 1.5f, col.width - u * 10f, u * 8.5f);
        DrawPlate(mp, false, u, fs);

        // Faixa do vencedor
        if (resultT > 0.25f)
        {
            float pop = Mathf.Clamp01((resultT - 0.25f) / 0.25f);
            int size = Mathf.RoundToInt((fs + 10) * Mathf.Lerp(1.6f, 1f, pop) * (1f + 0.04f * Mathf.Sin(Time.time * 4f)));
            string label = resWinner == -1 ? "EMPATE" : "VENCEDOR!";
            float y = resWinner == -1 ? ep.yMax + u * 3.5f : enemyWon ? top - u * 1f : ep.yMax + u * 4.5f;
            Outlined(new Rect(col.x, y, col.width, u * 7f), label, size,
                resWinner == -1 ? Color.white : new Color(0.45f, 0.95f, 1f), pop);
        }
        if (!enemyWon && resWinner != -1)
            Ui.Text(new Rect(col.x, top - u * 1f, col.width, u * 6f), "VS", fs + 2, new Color(1f, 1f, 1f, 0.35f));

        // Recompensas
        if (resultT > 2f)
        {
            float ry = mp.yMax + u * 3f;
            Ui.Text(new Rect(col.x, ry, col.width, u * 3f), "Recompensas", fs - 2, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleCenter);
            ry += u * 3.8f;
            int items = (resChest >= 0 ? 1 : 0) + 2;
            float boxW = col.width * 0.26f, boxH = u * 14f, gap = u * 2f;
            float x = col.center.x - (items * boxW + (items - 1) * gap) / 2f;
            int idx = 0;
            void Box(Texture tex, string big, string small, Color bigC)
            {
                float appear = Mathf.Clamp01((resultT - 2f - idx * 0.15f) / 0.2f);
                idx++;
                var r = new Rect(x, ry + (1f - appear) * u * 3f, boxW, boxH);
                x += boxW + gap;
                Ui.Panel(r, new Color(0.12f, 0.16f, 0.32f, appear));
                var ic = new Rect(r.x + u, r.y + u * 0.8f, r.width - u * 2f, r.height * 0.52f);
                if (tex != null) Tex(ic, tex, appear);
                else
                {
                    var d = new Rect(ic.center.x - ic.height * 0.45f, ic.y + ic.height * 0.05f, ic.height * 0.9f, ic.height * 0.9f);
                    Ui.Disc(new Rect(d.x - 2, d.y - 2, d.width + 4, d.height + 4), new Color(0.05f, 0.1f, 0.3f, appear));
                    Ui.Disc(d, new Color(0.3f, 0.6f, 1f, appear));
                    Ui.Text(d, "XP", fs, new Color(1f, 1f, 1f, appear));
                }
                Outlined(new Rect(r.x, ic.yMax, r.width, u * 3.4f), big, fs, bigC, appear);
                Ui.Text(new Rect(r.x, ic.yMax + u * 3f, r.width, u * 2.6f), small, fs - 5, new Color(0.85f, 0.9f, 1f, appear), TextAnchor.MiddleCenter, false);
            }
            if (resChest >= 0) Box(texChest[resChest], "Baú", Profile.ChestNames[resChest], Ui.Gold);
            Box(texCoins, "+" + resCoins, "moedas", Ui.Gold);
            Box(null, "+" + resXp, "experiência", new Color(0.6f, 0.85f, 1f));
            if (!string.IsNullOrEmpty(rewardText))
                Ui.Text(new Rect(col.x + u * 2f, ry + boxH + u * 0.5f, col.width - u * 4f, u * 5f), rewardText, fs - 5,
                    new Color(1f, 1f, 1f, 0.85f), TextAnchor.UpperCenter, false);
        }

        if (resultT > 2.4f)
        {
            bool rep = HasReplay;
            var ok = rep ? new Rect(col.center.x + u, col.yMax - u * 8.5f, col.width * 0.36f, u * 7f)
                         : new Rect(col.center.x - col.width * 0.2f, col.yMax - u * 8.5f, col.width * 0.4f, u * 7f);
            if (rep && Ui.Button(new Rect(col.center.x - u - col.width * 0.36f, col.yMax - u * 8.5f, col.width * 0.36f, u * 7f),
                    new Color(0.45f, 0.3f, 0.7f), "Replay", fs + 2))
                StartReplay();
            else if (Ui.Button(ok, new Color(0.2f, 0.55f, 0.95f), "OK", fs + 5))
                ReturnToLobby();
        }
    }

    // Segurar uma carta e arrastar até a arena (mouse ou toque), lido pelo Input System
    readonly Rect[] handRects = new Rect[4];
    Rect hudCol;
    float hudPanelTop;

    // Segurar/arrastar a carta. Aperto e soltura vêm dos eventos da interface (ficam na fila,
    // nenhum toque rápido se perde); a posição durante o arrasto também.
    void UpdateDrag()
    {
        if (!Running) dragSlot = -1;
    }

    void HandlePointer(Event ev)
    {
        if (!Running) return;
        Vector2 gui = ev.mousePosition;
        bool inArena = hudCol.Contains(gui) && gui.y < hudPanelTop;
        // aperto numa carta da mão: seleciona e começa a arrastar
        if (ev.type == EventType.MouseDown && ev.button == 0)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!handRects[i].Contains(gui)) continue;
                dragWasSelected = selected == i;
                selected = i;
                dragSlot = i;
                dragStart = gui;
                mouseGui = gui;
                mouseInArena = false;
                Sfx.Play("carta", 0.4f);
                ev.Use();
                return;
            }
        }
        if (dragSlot < 0) return;
        if (ev.type == EventType.MouseDrag || ev.type == EventType.MouseMove)
        {
            mouseGui = gui;
            mouseInArena = inArena;
        }
        else if (ev.rawType == EventType.MouseUp)
        {
            int slot = dragSlot;
            dragSlot = -1;
            if (inArena)
            {
                Vector3 w = cam.ScreenToWorldPoint(new Vector3(gui.x, Screen.height - gui.y, 0f));
                if (TryPlay(0, slot, w)) selected = -1;
            }
            else if (dragWasSelected && Vector2.Distance(gui, dragStart) < 10f) selected = -1;   // tocar de novo desmarca
            ev.Use();
        }
    }
}
