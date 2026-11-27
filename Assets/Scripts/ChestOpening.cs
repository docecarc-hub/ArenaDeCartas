using UnityEngine;

// Abertura de baú animada: o baú balança, abre, e as cartas aparecem uma por uma.
public class ChestOpening
{
    readonly Profile.ChestResult res;
    readonly Texture2D chestTex, coinTex, gemTex;
    int stage;                  // 0 = balançando, 1 = moedas/gemas, 2.. = cartas, depois resumo
    float stageStart;
    float lastShake;
    bool soundPlayed;

    float Now => Time.realtimeSinceStartup;
    int CardStages => res.cards.Count;
    int SummaryStage => 2 + CardStages;

    public ChestOpening(Profile.ChestResult result)
    {
        res = result;
        chestTex = Resources.Load<Texture2D>("Icones/bau" + result.type);
        coinTex = Resources.Load<Texture2D>("Icones/moedas");
        gemTex = Resources.Load<Texture2D>("Icones/gemas");
        stageStart = Now;
    }

    void Next()
    {
        stage++;
        stageStart = Now;
        soundPlayed = false;
    }

    // Retorna false quando terminou
    public bool Draw(Rect col)
    {
        float u = col.height / 100f;
        int fs = Mathf.RoundToInt(u * 2.3f);
        float t = Now - stageStart;
        Ui.Fill(col, new Color(0.03f, 0.06f, 0.12f, 0.92f));

        // raios de luz girando atrás
        if (stage >= 1)
        {
            var old = GUI.matrix;
            var center = new Vector2(col.center.x, col.y + col.height * 0.38f);
            for (int i = 0; i < 8; i++)
            {
                GUIUtility.RotateAroundPivot(Now * 20f + i * 45f, center);
                Ui.Fill(new Rect(center.x - u * 1.5f, center.y - col.height * 0.4f, u * 3f, col.height * 0.4f), new Color(1f, 0.9f, 0.5f, 0.07f));
                GUI.matrix = old;
            }
        }

        Event ev = Event.current;
        bool clicked = ev.type == EventType.MouseDown && ev.button == 0 && col.Contains(ev.mousePosition);

        if (stage == 0)
        {
            // baú tremendo cada vez mais
            if (Now - lastShake > 0.25f)
            {
                lastShake = Now;
                Sfx.Play("bau_treme", 1f, 0.1f, 0.1f);
            }
            float k = Mathf.Clamp01(t / 1.1f);
            float angle = Mathf.Sin(t * 40f) * 8f * k;
            float scale = 1f + 0.08f * Mathf.Sin(t * 12f) * k;
            DrawChest(col, u, angle, scale);
            Ui.Text(new Rect(col.x, col.y + col.height * 0.66f, col.width, u * 5f), $"Baú de {Profile.ChestNames[res.type]}", fs + 4, Color.white);
            if (t > 1.2f || clicked)
            {
                if (clicked) ev.Use();
                Sfx.Play("bau");
                Next();
            }
            return true;
        }

        if (stage == 1)
        {
            float pop = EaseOut(Mathf.Clamp01(t / 0.35f));
            DrawChest(col, u, 0f, 1.15f);
            // clarão
            if (t < 0.3f) Ui.Fill(col, new Color(1f, 1f, 0.9f, 0.6f * (1f - t / 0.3f)));
            var row = new Rect(col.x, col.y + col.height * 0.62f, col.width, u * 8f);
            float iw = u * 8f * pop;
            if (coinTex != null) GUI.DrawTexture(new Rect(row.center.x - u * 20f, row.center.y - iw / 2, iw, iw), coinTex, ScaleMode.ScaleToFit);
            Ui.Text(new Rect(row.center.x - u * 11f, row.y, u * 12f, row.height), $"+{res.coins}", fs + 5, Ui.Gold, TextAnchor.MiddleLeft);
            if (res.gems > 0)
            {
                if (gemTex != null) GUI.DrawTexture(new Rect(row.center.x + u * 3f, row.center.y - iw / 2, iw, iw), gemTex, ScaleMode.ScaleToFit);
                Ui.Text(new Rect(row.center.x + u * 12f, row.y, u * 12f, row.height), $"+{res.gems}", fs + 5, new Color(0.4f, 1f, 0.5f), TextAnchor.MiddleLeft);
            }
            if (!soundPlayed && t > 0.2f) { Sfx.Play("moeda"); soundPlayed = true; }
            Hint(col, u, fs);
            if (clicked && t > 0.25f) { ev.Use(); Next(); }
            return true;
        }

        if (stage < SummaryStage)
        {
            var drop = res.cards[stage - 2];
            var card = Profile.Find(drop.key);
            float pop = EaseOut(Mathf.Clamp01(t / 0.35f));
            if (!soundPlayed) { Sfx.Play(drop.isNew ? "evoluir" : "carta"); soundPlayed = true; }

            float cw = col.width * 0.45f * pop, ch = cw * 1.25f;
            var r = new Rect(col.center.x - cw / 2, col.y + col.height * 0.38f - ch / 2, cw, ch);
            if (drop.isNew)
            {
                float g = Mathf.PingPong(Now * 2f, 1f);
                Ui.Fill(new Rect(r.x - 6, r.y - 6, r.width + 12, r.height + 12), Color.Lerp(Ui.Gold, Color.white, g));
            }
            if (cw > 4f) ArenaGame.DrawCardFace(r, card, Mathf.RoundToInt(cw / 7f));

            Ui.Text(new Rect(col.x, r.yMax + u * 2f, col.width, u * 5f), card.name, fs + 4, Color.white);
            Ui.Text(new Rect(col.x, r.yMax + u * 7f, col.width, u * 5f),
                drop.isNew ? "NOVA CARTA!" : $"x{drop.count}", fs + 5, drop.isNew ? Ui.Gold : new Color(0.6f, 0.85f, 1f));
            if (!drop.isNew && Profile.Owns(drop.key))
            {
                var bar = new Rect(col.center.x - col.width * 0.25f, r.yMax + u * 13f, col.width * 0.5f, u * 2.6f);
                bool ready = Profile.HasCopiesToUpgrade(drop.key);
                Ui.Bar(bar, (float)Profile.CardCount(drop.key) / Mathf.Max(1, Profile.CopiesNeeded(drop.key)), ready ? new Color(0.35f, 0.85f, 0.4f) : new Color(0.3f, 0.6f, 1f));
                Ui.Text(bar, ready ? "Pronta para evoluir!" : $"{Profile.CardCount(drop.key)}/{Profile.CopiesNeeded(drop.key)}", fs - 4, Color.white);
            }
            Ui.Text(new Rect(col.x, col.y + u * 4f, col.width, u * 4f), $"{stage - 1}/{CardStages}", fs, new Color(1f, 1f, 1f, 0.6f));
            Hint(col, u, fs);
            if (clicked && t > 0.25f) { ev.Use(); Next(); }
            return true;
        }

        // Resumo
        Ui.Text(new Rect(col.x, col.y + u * 8f, col.width, u * 6f), $"Baú de {Profile.ChestNames[res.type]}", fs + 5, Color.white);
        Ui.Text(new Rect(col.x, col.y + u * 14f, col.width, u * 4f),
            $"+{res.coins} moedas" + (res.gems > 0 ? $"    +{res.gems} gemas" : ""), fs + 1, Ui.Gold);
        float sw = (col.width - u * 6f) / 4f, sh = sw * 1.25f;
        for (int i = 0; i < res.cards.Count; i++)
        {
            var d = res.cards[i];
            var r = new Rect(col.x + u * 1.5f + (i % 4) * (sw + u), col.y + u * 20f + (i / 4) * (sh + u * 5f), sw, sh);
            if (d.isNew) Ui.Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Ui.Gold);
            ArenaGame.DrawCardFace(r, Profile.Find(d.key), Mathf.RoundToInt(sw / 7f));
            Ui.Text(new Rect(r.x, r.yMax, r.width, u * 4f), d.isNew ? "NOVA!" : "x" + d.count, fs - 1, d.isNew ? Ui.Gold : Color.white);
        }
        if (Ui.Button(new Rect(col.center.x - col.width * 0.18f, col.yMax - u * 14f, col.width * 0.36f, u * 7f), Ui.Orange, "OK", fs + 3))
            return false;
        return true;
    }

    void DrawChest(Rect col, float u, float angle, float scale)
    {
        if (chestTex == null) return;
        float size = col.width * 0.5f * scale;
        var center = new Vector2(col.center.x, col.y + col.height * 0.38f);
        var old = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, center);
        GUI.DrawTexture(new Rect(center.x - size / 2, center.y - size / 2, size, size), chestTex, ScaleMode.ScaleToFit);
        GUI.matrix = old;
    }

    static void Hint(Rect col, float u, int fs)
    {
        float a = 0.4f + 0.4f * Mathf.PingPong(Time.realtimeSinceStartup, 1f);
        Ui.Text(new Rect(col.x, col.yMax - u * 9f, col.width, u * 4f), "Toque para continuar", fs - 2, new Color(1f, 1f, 1f, a));
    }

    static float EaseOut(float x) => 1f - (1f - x) * (1f - x) * (1f - x) + Mathf.Sin(x * Mathf.PI) * 0.12f;
}
