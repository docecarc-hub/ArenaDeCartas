using System.Collections.Generic;
using UnityEngine;

// Caminho de Troféus: trilha vertical com recompensas a cada 50 troféus e as arenas no meio.
public class TrophyRoad
{
    public enum RewardKind { Coins, Gems, Chest }

    public struct Reward
    {
        public int trophies;
        public RewardKind kind;
        public int amount;      // moedas/gemas, ou tipo do baú (0 Madeira, 1 Prata, 2 Ouro)

        public Reward(int trophies, RewardKind kind, int amount)
        {
            this.trophies = trophies;
            this.kind = kind;
            this.amount = amount;
        }
    }

    // Recompensas do caminho (edite aqui para balancear)
    public static readonly Reward[] Rewards =
    {
        new Reward(50, RewardKind.Coins, 50),
        new Reward(100, RewardKind.Chest, 0),
        new Reward(150, RewardKind.Gems, 10),
        new Reward(200, RewardKind.Coins, 100),
        new Reward(250, RewardKind.Chest, 1),

        new Reward(350, RewardKind.Coins, 150),
        new Reward(400, RewardKind.Chest, 1),
        new Reward(450, RewardKind.Gems, 15),
        new Reward(500, RewardKind.Coins, 200),
        new Reward(550, RewardKind.Chest, 2),

        new Reward(650, RewardKind.Coins, 250),
        new Reward(700, RewardKind.Chest, 1),
        new Reward(750, RewardKind.Gems, 20),
        new Reward(800, RewardKind.Chest, 2),
        new Reward(850, RewardKind.Coins, 300),
        new Reward(900, RewardKind.Gems, 25),
        new Reward(950, RewardKind.Chest, 2),
    };

    struct Entry
    {
        public bool isArena;
        public int arena;
        public Reward reward;
        public int trophies;
        public float y, height, anchor;
    }

    readonly Lobby lobby;
    readonly CardData[] cards;
    readonly List<Entry> entries = new List<Entry>();
    readonly Texture2D[] tiles = new Texture2D[4];
    readonly Texture2D[] arenaArt = new Texture2D[4];
    readonly Texture2D[] chestIcons = new Texture2D[3];
    Texture2D coinIcon, gemIcon;
    float builtFor = -1f;
    float contentHeight;
    Vector2 scroll;
    bool center;

    public bool IsOpen { get; private set; }

    public TrophyRoad(Lobby lobby, CardData[] cards)
    {
        this.lobby = lobby;
        this.cards = cards;
    }

    public void Open()
    {
        IsOpen = true;
        center = true;
    }

    public static bool AnyClaimable()
    {
        foreach (var r in Rewards)
            if (Profile.BestTrophies >= r.trophies && !Profile.IsClaimed(r.trophies)) return true;
        return false;
    }

    void LoadArt()
    {
        if (coinIcon != null) return;
        for (int i = 0; i < 4; i++)
        {
            tiles[i] = Resources.Load<Texture2D>("Caminho/trilha" + i);
            arenaArt[i] = Resources.Load<Texture2D>("Arenas/arena" + i);
        }
        for (int i = 0; i < 3; i++) chestIcons[i] = Resources.Load<Texture2D>("Icones/bau" + i);
        coinIcon = Resources.Load<Texture2D>("Icones/moedas");
        gemIcon = Resources.Load<Texture2D>("Icones/gemas");
    }

    // Monta a lista de cima (arena mais alta) para baixo (Arena 1).
    void Build(float u)
    {
        if (Mathf.Approximately(builtFor, u)) return;
        builtFor = u;
        entries.Clear();

        float arenaH = u * 55f, rewardH = u * 15f, y = u * 3f;
        int last = Profile.ArenaTrophies.Length - 1;
        for (int a = last; a >= 0; a--)
        {
            entries.Add(new Entry
            {
                isArena = true, arena = a, trophies = Profile.ArenaTrophies[a],
                y = y, height = arenaH, anchor = y + u * 34f
            });
            y += arenaH;

            if (a == 0) break;
            for (int i = Rewards.Length - 1; i >= 0; i--)
            {
                var r = Rewards[i];
                if (r.trophies <= Profile.ArenaTrophies[a - 1] || r.trophies >= Profile.ArenaTrophies[a]) continue;
                entries.Add(new Entry
                {
                    reward = r, trophies = r.trophies, arena = a - 1,
                    y = y, height = rewardH, anchor = y + rewardH * 0.5f
                });
                y += rewardH;
            }
        }
        contentHeight = y + u * 3f;
    }

    // Converte troféus em posição vertical na trilha.
    float TrophyY(int t)
    {
        if (t >= entries[0].trophies) return entries[0].anchor;
        for (int i = 0; i < entries.Count - 1; i++)
        {
            var hi = entries[i];
            var lo = entries[i + 1];
            if (t <= hi.trophies && t >= lo.trophies)
            {
                float f = hi.trophies == lo.trophies ? 0f : (float)(t - lo.trophies) / (hi.trophies - lo.trophies);
                return Mathf.Lerp(lo.anchor, hi.anchor, f);
            }
        }
        return entries[entries.Count - 1].anchor;
    }

    public void Draw(Rect col)
    {
        float u = col.height / 100f;
        int fs = Mathf.RoundToInt(u * 2.3f);
        LoadArt();
        Build(u);

        Ui.DiamondBackground(col, col.width / 4f);

        // Cabeçalho
        var head = new Rect(col.x, col.y, col.width, u * 10f);
        Ui.Fill(head, Ui.Dark);
        Ui.Text(new Rect(head.x, head.y + u * 0.5f, head.width, u * 4.5f), "Caminho de Troféus", fs + 3, Color.white);
        var left = Profile.SeasonTimeLeft;
        Ui.Text(new Rect(head.x, head.y + u * 5f, head.width, u * 4f),
            $"Melhor marca: {Profile.BestTrophies}     Temporada acaba em {left.Days}d {left.Hours}h",
            fs - 4, new Color(0.8f, 0.9f, 1f), TextAnchor.MiddleCenter, false);

        var view = new Rect(col.x, head.yMax, col.width, col.height - u * 10f - u * 11f);

        // Arrastar com o mouse / rodinha
        Event ev = Event.current;
        float maxScroll = Mathf.Max(0f, contentHeight - view.height);
        if (view.Contains(ev.mousePosition))
        {
            if (ev.type == EventType.MouseDrag) { scroll.y -= ev.delta.y; ev.Use(); }
            else if (ev.type == EventType.ScrollWheel) { scroll.y += ev.delta.y * u * 2f; ev.Use(); }
        }
        if (center)
        {
            center = false;
            scroll.y = TrophyY(Profile.Trophies) - view.height * 0.55f;
        }
        scroll.y = Mathf.Clamp(scroll.y, 0f, maxScroll);

        var content = new Rect(0, 0, view.width - 1f, contentHeight);
        scroll = GUI.BeginScrollView(view, scroll, content, false, false, GUIStyle.none, GUIStyle.none);
        DrawContent(content.width, u, fs);
        GUI.EndScrollView();

        // Rodapé
        var foot = new Rect(col.x, col.yMax - u * 11f, col.width, u * 11f);
        Ui.Fill(foot, Ui.Dark);
        if (Ui.Button(new Rect(foot.center.x - col.width * 0.18f, foot.y + u * 2f, col.width * 0.36f, u * 7f), Ui.Orange, "OK", fs + 4))
            IsOpen = false;
    }

    void DrawContent(float width, float u, int fs)
    {
        // Barra de progresso vertical
        float top = entries[0].anchor, bottom = entries[entries.Count - 1].anchor;
        float yNow = TrophyY(Profile.Trophies);
        var bar = new Rect(u * 3f, top, u * 1.6f, bottom - top);
        Ui.Fill(new Rect(bar.x - 2, bar.y - 2, bar.width + 4, bar.height + 4), Ui.Dark);
        Ui.Fill(new Rect(bar.x, yNow, bar.width, bottom - yNow), new Color(0.3f, 0.75f, 1f));

        foreach (var e in entries)
        {
            var r = new Rect(0, e.y, width, e.height);
            if (e.isArena) DrawArena(r, e.arena, u, fs);
            else DrawReward(r, e.reward, e.arena, u, fs);
        }

        // Marcador com seus troféus
        var mark = new Rect(u * 0.4f, yNow - u * 2.2f, u * 8.5f, u * 4.4f);
        Ui.Panel(mark, Ui.Gold);
        Ui.Text(mark, Profile.Trophies.ToString(), fs, new Color(0.25f, 0.15f, 0f));
    }

    void DrawReward(Rect r, Reward rw, int arena, float u, int fs)
    {
        bool reached = Profile.BestTrophies >= rw.trophies;
        bool claimed = Profile.IsClaimed(rw.trophies);

        Ui.Text(new Rect(u * 9.5f, r.center.y - u * 2.5f, u * 6f, u * 5f), rw.trophies.ToString(), fs - 2,
            reached ? Ui.Gold : new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleLeft);

        var tile = new Rect(u * 15f, r.y + u * 3.5f, r.width - u * 16f, r.height - u * 4f);
        if (tiles[arena] != null) GUI.DrawTexture(tile, tiles[arena], ScaleMode.ScaleToFit);

        float bs = u * 10.5f;
        var box = new Rect(tile.center.x + tile.width * 0.1f, r.y + u * 0.6f, bs, bs);
        Ui.Panel(box, new Color(0.86f, 0.89f, 0.95f));
        var icon = new Rect(box.x + u, box.y + u * 0.6f, bs - u * 2f, bs - u * 3.2f);
        Texture2D tex = rw.kind == RewardKind.Coins ? coinIcon : rw.kind == RewardKind.Gems ? gemIcon : chestIcons[rw.amount];
        if (tex != null) GUI.DrawTexture(icon, tex, ScaleMode.ScaleToFit);
        string label = rw.kind == RewardKind.Chest ? Profile.ChestNames[rw.amount] : "x" + rw.amount;
        Ui.Text(new Rect(box.x, box.yMax - u * 3.2f, box.width, u * 3f), label, fs - 5, Color.white);

        if (claimed)
        {
            Ui.Fill(box, new Color(0f, 0f, 0f, 0.5f));
            Ui.Text(box, "Pego", fs - 2, new Color(0.6f, 1f, 0.6f));
        }
        else if (!reached)
        {
            Ui.Fill(box, new Color(0.05f, 0.1f, 0.2f, 0.55f));
        }
        else
        {
            var btn = new Rect(box.x - u * 12f, box.y + u * 3f, u * 11f, u * 5f);
            Color c = Color.Lerp(Ui.Orange, Color.white, Mathf.PingPong(Time.time * 0.8f, 0.3f));
            if (Ui.Button(btn, c, "Pegar!", fs - 1)) Claim(rw);
        }
    }

    void Claim(Reward rw)
    {
        Profile.MarkClaimed(rw.trophies);
        switch (rw.kind)
        {
            case RewardKind.Coins:
                Profile.Coins += rw.amount;
                Profile.Save();
                Sfx.Play("moeda");
                lobby.ShowPopup($"Caminho de Troféus\n{rw.trophies} troféus\n\n+{rw.amount} moedas");
                break;
            case RewardKind.Gems:
                Profile.Gems += rw.amount;
                Profile.Save();
                Sfx.Play("moeda");
                lobby.ShowPopup($"Caminho de Troféus\n{rw.trophies} troféus\n\n+{rw.amount} gemas");
                break;
            default:
                lobby.ShowChest(Profile.RollChest(rw.amount));
                break;
        }
    }

    void DrawArena(Rect r, int a, float u, int fs)
    {
        bool reached = Profile.HighestArena >= a;
        bool current = Profile.ArenaIndex == a;

        var img = new Rect(u * 9f, r.y + u, r.width - u * 11f, u * 29f);
        if (current) img.y += Mathf.Sin(Time.time * 1.6f) * u * 0.6f;
        if (arenaArt[a] != null)
        {
            var old = GUI.color;
            if (!reached) GUI.color = new Color(0.35f, 0.4f, 0.5f, 1f);
            GUI.DrawTexture(img, arenaArt[a], ScaleMode.ScaleToFit);
            GUI.color = old;
        }

        var ban = new Rect(u * 10f, r.y + u * 31f, r.width - u * 12f, u * 6.5f);
        Ui.Panel(ban, reached ? new Color(0.13f, 0.42f, 0.72f) : new Color(0.25f, 0.3f, 0.38f));
        Ui.Text(new Rect(ban.x + u * 1.5f, ban.y, ban.width * 0.7f, ban.height), $"Arena {a + 1}: {Profile.ArenaNames[a]}",
            fs, Color.white, TextAnchor.MiddleLeft);
        Ui.Text(new Rect(ban.x, ban.y, ban.width - u * 1.5f, ban.height), $"{Profile.ArenaTrophies[a]}",
            fs, Ui.Gold, TextAnchor.MiddleRight);

        Ui.Text(new Rect(u * 10f, r.y + u * 38.3f, r.width - u * 12f, u * 3.5f),
            reached ? "Podem cair dos baús:" : $"Desbloqueia com {Profile.ArenaTrophies[a]} troféus:", fs - 3,
            new Color(0.85f, 0.92f, 1f), TextAnchor.MiddleCenter, false);

        var unlocks = new List<CardData>();
        foreach (var c in cards) if (c.unlockArena == a && !c.starter) unlocks.Add(c);
        float cw = u * 8f, ch = u * 10.5f, gap = u * 0.8f;
        float total = unlocks.Count * cw + (unlocks.Count - 1) * gap;
        float x = u * 10f + (r.width - u * 12f - total) / 2f;
        foreach (var c in unlocks)
        {
            var cr = new Rect(x, r.y + u * 42.3f, cw, ch);
            DrawMiniCard(cr, c, u, fs);
            if (!reached) Ui.Fill(cr, new Color(0f, 0f, 0f, 0.55f));
            x += cw + gap;
        }
    }

    public static void DrawMiniCard(Rect r, CardData c, float u, int fs)
    {
        ArenaGame.DrawCardFace(r, c, Mathf.RoundToInt(r.width / 6f));
        var cost = new Rect(r.x - u * 0.6f, r.yMax - u * 3.6f, u * 3.6f, u * 3.6f);
        Ui.Disc(cost, Ui.Elixir);
        Ui.Text(cost, c.cost.ToString(), fs - 3, Color.white);
    }
}
