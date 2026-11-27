using System.Collections.Generic;
using UnityEngine;

// Tela inicial: perfil, recursos, loja, cartas, batalha, missões, baús e configurações.
public class Lobby
{
    readonly ArenaGame game;
    readonly TrophyRoad road;
    int tab = 2;
    string popup;
    bool seasonChecked;
    ChestOpening chestAnim;
    string cardInfo;            // carta aberta no painel de detalhes
    bool settingsOpen;

    static readonly string[] Tabs = { "Loja", "Cartas", "Batalha", "Clã", "Missões" };

    public Lobby(ArenaGame game)
    {
        this.game = game;
        road = new TrophyRoad(this, game.Deck);
    }

    public void ShowPopup(string text) => popup = text;

    public void ShowChest(Profile.ChestResult result) => chestAnim = new ChestOpening(result);

    bool Modal => popup != null || chestAnim != null || cardInfo != null || settingsOpen;

    public void Draw(Rect col)
    {
        float u = col.height / 100f;
        int fs = Mathf.RoundToInt(u * 2.3f);
        Sfx.Music("musica_lobby");

        if (!seasonChecked)
        {
            seasonChecked = true;
            string season = Profile.CheckSeason();
            if (season != null) popup = season;
        }

        if (road.IsOpen)
        {
            GUI.enabled = !Modal;
            road.Draw(col);
            GUI.enabled = true;
            DrawModals(col, u, fs);
            return;
        }

        Ui.DiamondBackground(col, col.width / 4f);

        GUI.enabled = !Modal;
        DrawTopBar(col, u, fs);

        var body = new Rect(col.x, col.y + u * 12f, col.width, col.height - u * 22f);
        switch (tab)
        {
            case 0: DrawShop(body, u, fs); break;
            case 1: DrawCards(body, u, fs); break;
            case 2: DrawBattleTab(body, u, fs); break;
            case 4: DrawMissions(body, u, fs); break;
            default: Ui.Text(body, $"{Tabs[tab]}\n\nEm breve!", fs * 2, Color.white); break;
        }

        DrawNav(col, u, fs);
        GUI.enabled = true;
        DrawModals(col, u, fs);
    }

    void DrawModals(Rect col, float u, int fs)
    {
        if (chestAnim != null)
        {
            if (!chestAnim.Draw(col)) chestAnim = null;
        }
        else if (cardInfo != null) DrawCardInfo(col, u, fs);
        else if (settingsOpen) DrawSettings(col, u, fs);
        else if (popup != null) DrawPopup(col, u, fs);
    }

    // ------------------------------------------------------------ Barra de cima

    void DrawTopBar(Rect col, float u, int fs)
    {
        Ui.Fill(new Rect(col.x, col.y, col.width, u * 7f), Ui.Dark);

        var disc = new Rect(col.x + u, col.y + u, u * 5f, u * 5f);
        Ui.Disc(new Rect(disc.x - 2, disc.y - 2, disc.width + 4, disc.height + 4), Ui.Shade(Ui.Teal, 0.5f));
        Ui.Disc(disc, Ui.Teal);
        Ui.Text(disc, Profile.Level.ToString(), fs + 3, Color.white);

        Ui.Text(new Rect(disc.xMax + u, col.y + u * 0.7f, col.width * 0.35f, u * 2.8f),
            Profile.Name, fs, Color.white, TextAnchor.MiddleLeft);
        var xp = new Rect(disc.xMax + u, col.y + u * 3.8f, col.width * 0.28f, u * 1.8f);
        Ui.Bar(xp, (float)Profile.Xp / Profile.XpNeeded, new Color(0.3f, 0.7f, 1f));
        Ui.Text(xp, $"{Profile.Xp}/{Profile.XpNeeded}", fs - 5, Color.white);

        DrawResource(new Rect(col.xMax - col.width * 0.47f, col.y + u * 1.6f, col.width * 0.2f, u * 3.6f),
            Profile.Coins, Ui.Gold, u, fs);
        DrawResource(new Rect(col.xMax - col.width * 0.22f, col.y + u * 1.6f, col.width * 0.19f, u * 3.6f),
            Profile.Gems, new Color(0.3f, 0.9f, 0.45f), u, fs);

        var tr = new Rect(col.x + u, col.y + u * 8f, col.width * 0.34f, u * 3.4f);
        Ui.Panel(tr, new Color(0.14f, 0.16f, 0.24f));
        Ui.Disc(new Rect(tr.x + u * 0.5f, tr.y + u * 0.4f, u * 2.6f, u * 2.6f), Ui.Gold);
        Ui.Text(new Rect(tr.x + u * 3.5f, tr.y, tr.width - u * 3.5f, tr.height),
            $"{Profile.Trophies} troféus", fs - 1, Ui.Gold, TextAnchor.MiddleLeft);

        var gear = new Rect(col.xMax - u * 12f, col.y + u * 8f, u * 11f, u * 3.4f);
        if (Ui.Button(gear, new Color(0.2f, 0.24f, 0.34f), "Ajustes", fs - 3)) settingsOpen = true;
    }

    static void DrawResource(Rect r, int value, Color icon, float u, int fs)
    {
        Ui.Panel(r, new Color(0.14f, 0.16f, 0.24f));
        Ui.Disc(new Rect(r.x - u * 1.2f, r.y, r.height, r.height), icon);
        Ui.Text(new Rect(r.x + u, r.y, r.width - u, r.height), value.ToString(), fs - 1, Color.white);
    }

    // ------------------------------------------------------------ Aba Batalha

    void DrawBattleTab(Rect body, float u, int fs)
    {
        int a = Profile.ArenaIndex;
        Ui.Text(new Rect(body.x, body.y, body.width, u * 4f), $"Arena {a + 1}: {Profile.ArenaNames[a]}", fs + 2, Color.white);
        if (a + 1 < Profile.ArenaTrophies.Length)
            Ui.Text(new Rect(body.x, body.y + u * 3.6f, body.width, u * 3f),
                $"Próxima arena com {Profile.ArenaTrophies[a + 1]} troféus", fs - 3, new Color(0.75f, 0.85f, 0.95f), TextAnchor.MiddleCenter, false);

        var preview = new Rect(body.x + u, body.y + u * 7f, body.width - u * 2f, u * 30f);
        DrawArenaPreview(preview, a, u);
        Ui.Text(new Rect(preview.x, preview.yMax - u * 3f, preview.width, u * 3f), "Toque na arena para ver o Caminho de Troféus",
            fs - 5, new Color(0.8f, 0.9f, 1f), TextAnchor.MiddleCenter, false);
        if (TrophyRoad.AnyClaimable()) Badge(new Rect(preview.xMax - u * 9f, preview.y + u * 2f, u * 5f, u * 5f), fs);
        if (GUI.Button(preview, GUIContent.none, GUIStyle.none)) road.Open();

        float bw = body.width * 0.4f;
        var battle = new Rect(body.center.x - bw - u, preview.yMax + u * 2f, bw, u * 9f);
        if (Ui.Button(battle, Ui.Orange, "Batalhar", fs + 6)) game.StartMatch();

        var party = new Rect(body.center.x + u, battle.y, bw, u * 9f);
        if (Ui.Button(party, Ui.Teal, "Amigos", fs + 6)) game.Toast("Batalha com amigos: em breve!");

        // Baú grátis e baú de coroas
        float hw = (body.width - u * 3f) / 2f;
        var free = new Rect(body.x + u, battle.yMax + u * 2f, hw, u * 6.5f);
        if (Profile.FreeChestReady)
        {
            var c = Color.Lerp(new Color(0.2f, 0.6f, 0.3f), new Color(0.4f, 0.85f, 0.45f), Mathf.PingPong(Time.time, 0.6f));
            if (Ui.Button(free, c, "Baú grátis!  ABRIR", fs - 2)) ShowChest(Profile.ClaimFreeChest());
        }
        else
        {
            var left = Profile.FreeChestLeft;
            Ui.Panel(free, new Color(0.16f, 0.22f, 0.3f));
            Ui.Text(free, $"Baú grátis em\n{left.Hours}h {left.Minutes:00}m", fs - 4, new Color(0.85f, 0.9f, 1f));
        }

        var crown = new Rect(free.xMax + u, free.y, hw, u * 6.5f);
        DrawCrownChest(crown, u, fs);

        float cw = (body.width - u * 5f) / 4f;
        for (int i = 0; i < 4; i++)
        {
            var r = new Rect(body.x + u + i * (cw + u), free.yMax + u * 1.5f, cw, u * 13f);
            DrawChest(r, Profile.Chests[i], fs);
        }
    }

    void DrawCrownChest(Rect r, float u, int fs)
    {
        if (Profile.CrownChestReady)
        {
            var c = Color.Lerp(Ui.Gold, Color.white, Mathf.PingPong(Time.time, 0.5f));
            if (Ui.Button(r, Ui.Shade(c, 0.85f), "Baú de coroas!  ABRIR", fs - 3)) ShowChest(Profile.ClaimCrownChest());
            return;
        }
        Ui.Panel(r, new Color(0.16f, 0.22f, 0.3f));
        if (Profile.CrownChestClaimed)
        {
            Ui.Text(r, "Baú de coroas\nVolte amanhã!", fs - 4, new Color(0.85f, 0.9f, 1f));
            return;
        }
        Ui.Text(new Rect(r.x, r.y + u * 0.3f, r.width, r.height * 0.5f), "Baú de coroas", fs - 4, Ui.Gold);
        var bar = new Rect(r.x + u, r.y + r.height * 0.55f, r.width - u * 2f, u * 2.2f);
        Ui.Bar(bar, (float)Profile.CrownsToday / Profile.CrownChestGoal, Ui.Gold);
        Ui.Text(bar, $"{Profile.CrownsToday}/{Profile.CrownChestGoal}", fs - 5, Color.white);
    }

    static readonly Texture2D[] arenaArt = new Texture2D[4];

    static void DrawArenaPreview(Rect r, int arena, float u)
    {
        // Ilustração da ilha flutuante (Assets/Resources/Arenas/arenaN.png), balançando de leve
        if (arenaArt[arena] == null) arenaArt[arena] = Resources.Load<Texture2D>("Arenas/arena" + arena);
        if (arenaArt[arena] != null)
        {
            float bob = Mathf.Sin(Time.time * 1.6f) * u * 0.7f;
            GUI.DrawTexture(new Rect(r.x, r.y + bob, r.width, r.height), arenaArt[arena], ScaleMode.ScaleToFit);
            return;
        }

        // Plano B (se a imagem não existir): desenho simples
        Ui.Fill(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), Ui.Dark);
        Ui.Fill(r, Profile.ArenaGrass[arena]);

        var path = new Color(0.80f, 0.67f, 0.47f);
        float lane = r.width * 0.22f;
        Ui.Fill(new Rect(r.center.x - lane - u * 0.6f, r.y + r.height * 0.12f, u * 1.2f, r.height * 0.76f), path);
        Ui.Fill(new Rect(r.center.x + lane - u * 0.6f, r.y + r.height * 0.12f, u * 1.2f, r.height * 0.76f), path);
        Ui.Fill(new Rect(r.x, r.center.y - u * 0.8f, r.width, u * 1.6f), new Color(0.25f, 0.55f, 0.85f));

        Color red = new Color(0.85f, 0.3f, 0.3f), blue = new Color(0.25f, 0.5f, 1f);
        float t = u * 3f;
        Ui.Panel(new Rect(r.center.x - lane - t / 2, r.y + r.height * 0.2f, t, t), red);
        Ui.Panel(new Rect(r.center.x + lane - t / 2, r.y + r.height * 0.2f, t, t), red);
        Ui.Panel(new Rect(r.center.x - t * 0.7f, r.y + u, t * 1.4f, t * 1.2f), Ui.Shade(red, 0.75f));
        Ui.Panel(new Rect(r.center.x - lane - t / 2, r.yMax - r.height * 0.2f - t, t, t), blue);
        Ui.Panel(new Rect(r.center.x + lane - t / 2, r.yMax - r.height * 0.2f - t, t, t), blue);
        Ui.Panel(new Rect(r.center.x - t * 0.7f, r.yMax - u - t * 1.2f, t * 1.4f, t * 1.2f), Ui.Shade(blue, 0.75f));
    }

    void DrawChest(Rect r, Profile.Chest c, int fs)
    {
        if (c.Empty)
        {
            Ui.Fill(r, new Color(0f, 0f, 0f, 0.3f));
            Ui.Text(r, "Espaço\nde baú", fs - 4, new Color(1f, 1f, 1f, 0.45f));
            return;
        }

        string name = Profile.ChestNames[c.type];
        Color color = Ui.Shade(Profile.ChestColors[c.type], 0.85f);
        string text;
        if (!c.unlocking) text = $"{name}\n\nToque para abrir";
        else if (c.Ready)
        {
            text = $"{name}\n\nABRIR!";
            color = Color.Lerp(color, Color.white, Mathf.PingPong(Time.time, 0.5f));
        }
        else
        {
            int s = Mathf.CeilToInt((float)c.SecondsLeft);
            text = $"{name}\n\n{s / 60}:{s % 60:00}";
        }

        if (!Ui.Button(r, color, text, fs - 4)) return;

        if (!c.unlocking)
        {
            if (Profile.AnyUnlocking) game.Toast("Já tem um baú sendo aberto");
            else Profile.StartUnlock(c);
        }
        else if (c.Ready) ShowChest(Profile.Open(c));
        else game.Toast("Esse baú ainda não está pronto");
    }

    // ------------------------------------------------------------ Aba Loja

    static Texture2D coinTex, gemTex;
    static readonly Texture2D[] chestTex = new Texture2D[3];

    void DrawShop(Rect body, float u, int fs)
    {
        if (coinTex == null)
        {
            coinTex = Resources.Load<Texture2D>("Icones/moedas");
            gemTex = Resources.Load<Texture2D>("Icones/gemas");
            for (int i = 0; i < 3; i++) chestTex[i] = Resources.Load<Texture2D>("Icones/bau" + i);
        }

        Ui.Text(new Rect(body.x, body.y, body.width, u * 4f), "Ofertas do dia", fs + 2, Color.white);
        Ui.Text(new Rect(body.x, body.y + u * 3.5f, body.width, u * 3f), "Renovam à meia-noite", fs - 4,
            new Color(0.75f, 0.85f, 0.95f), TextAnchor.MiddleCenter, false);

        var offers = Profile.TodayOffers();
        float cw = (body.width - u * 4f) / 3f, ch = cw * 1.45f;
        for (int i = 0; i < offers.Count; i++)
        {
            var o = offers[i];
            var r = new Rect(body.x + u + (i % 3) * (cw + u), body.y + u * 7f + (i / 3) * (ch + u), cw, ch);
            var art = new Rect(r.x, r.y, r.width, r.height - u * 6f);
            bool bought = Profile.OfferBought(i);

            if (o.key == null)
            {
                Ui.Panel(art, new Color(0.25f, 0.45f, 0.3f));
                if (coinTex != null) GUI.DrawTexture(new Rect(art.x + u, art.y + u, art.width - u * 2f, art.height - u * 5f), coinTex, ScaleMode.ScaleToFit);
                Ui.Text(new Rect(art.x, art.yMax - u * 4f, art.width, u * 3.5f), $"{o.giftCoins} moedas", fs - 3, Color.white);
            }
            else
            {
                var c = Profile.Find(o.key);
                ArenaGame.DrawCardFace(art, c, Mathf.RoundToInt(cw / 7f));
                var cnt = new Rect(art.xMax - u * 6f, art.y + u * 0.5f, u * 5.5f, u * 3f);
                Ui.Fill(cnt, new Color(0f, 0f, 0f, 0.6f));
                Ui.Text(cnt, "x" + o.copies, fs - 2, Color.white);
            }

            var btn = new Rect(r.x, r.yMax - u * 5.5f, r.width, u * 5.5f);
            if (bought)
            {
                Ui.Fill(art, new Color(0f, 0f, 0f, 0.55f));
                Ui.Panel(btn, new Color(0.3f, 0.3f, 0.35f));
                Ui.Text(btn, "Comprado", fs - 3, Color.white);
                continue;
            }
            string label = o.key == null ? "GRÁTIS" : $"{o.price} moedas";
            Color bc = o.key == null ? new Color(0.2f, 0.65f, 0.3f) : (Profile.Coins >= o.price ? new Color(0.85f, 0.6f, 0.1f) : new Color(0.4f, 0.4f, 0.45f));
            if (Ui.Button(btn, bc, label, fs - 3))
            {
                string msg = Profile.BuyOffer(i);
                if (msg == "") { Sfx.Play("erro"); game.Toast("Moedas insuficientes"); }
                else if (msg != null) { Sfx.Play("moeda"); popup = msg; }
            }
        }

        // Baús por gemas
        float y = body.y + u * 7f + 2f * (ch + u) + u;
        Ui.Text(new Rect(body.x, y, body.width, u * 4f), "Baús", fs + 1, Color.white);
        float bw = (body.width - u * 3f) / 2f;
        for (int t = 1; t <= 2; t++)
        {
            var r = new Rect(body.x + u + (t - 1) * (bw + u), y + u * 4.5f, bw, body.yMax - y - u * 5.5f);
            Ui.Panel(r, Ui.Shade(Profile.ChestColors[t], 0.55f));
            if (chestTex[t] != null) GUI.DrawTexture(new Rect(r.x + u, r.y + u * 0.5f, r.width * 0.45f, r.height - u), chestTex[t], ScaleMode.ScaleToFit);
            Ui.Text(new Rect(r.x + r.width * 0.45f, r.y + u * 0.5f, r.width * 0.55f, r.height * 0.45f), $"Baú de\n{Profile.ChestNames[t]}", fs - 3, Color.white);
            var b = new Rect(r.x + r.width * 0.48f, r.yMax - u * 5.5f, r.width * 0.5f, u * 4.8f);
            int price = Profile.ChestGemPrice[t];
            if (Ui.Button(b, Profile.Gems >= price ? new Color(0.2f, 0.65f, 0.35f) : new Color(0.4f, 0.4f, 0.45f), $"{price} gemas", fs - 4))
            {
                if (Profile.Gems < price) { Sfx.Play("erro"); game.Toast("Gemas insuficientes"); }
                else
                {
                    Profile.Gems -= price;
                    ShowChest(Profile.RollChest(t));
                }
            }
        }
    }

    // ------------------------------------------------------------ Aba Missões

    void DrawMissions(Rect body, float u, int fs)
    {
        Ui.Text(new Rect(body.x, body.y, body.width, u * 4f), "Missões diárias", fs + 2, Color.white);
        Ui.Text(new Rect(body.x, body.y + u * 3.5f, body.width, u * 3f), "Novas missões todo dia", fs - 4,
            new Color(0.75f, 0.85f, 0.95f), TextAnchor.MiddleCenter, false);

        for (int i = 0; i < 3; i++)
        {
            var m = Profile.Mission(i);
            int prog = Profile.MissionProgress(i);
            bool done = Profile.MissionDone(i), claimed = Profile.MissionClaimed(i);
            var r = new Rect(body.x + u, body.y + u * 7.5f + i * u * 13f, body.width - u * 2f, u * 12f);
            Ui.Panel(r, claimed ? new Color(0.18f, 0.24f, 0.3f) : new Color(0.14f, 0.3f, 0.45f));
            Ui.Text(new Rect(r.x + u * 1.5f, r.y + u, r.width * 0.65f, u * 3.5f), m.text, fs - 1, Color.white, TextAnchor.MiddleLeft);
            string reward = (m.coins > 0 ? $"{m.coins} moedas  " : "") + (m.gems > 0 ? $"{m.gems} gemas" : "");
            Ui.Text(new Rect(r.x + u * 1.5f, r.y + u * 4.3f, r.width * 0.65f, u * 3f), "Prêmio: " + reward, fs - 4, Ui.Gold, TextAnchor.MiddleLeft, false);
            var bar = new Rect(r.x + u * 1.5f, r.y + u * 8f, r.width * 0.6f, u * 2.4f);
            Ui.Bar(bar, (float)prog / m.target, new Color(0.35f, 0.8f, 0.45f));
            Ui.Text(bar, $"{prog}/{m.target}", fs - 5, Color.white);

            var btn = new Rect(r.xMax - r.width * 0.3f, r.y + u * 3f, r.width * 0.27f, u * 6f);
            if (claimed) Ui.Text(btn, "Concluída", fs - 3, new Color(0.6f, 1f, 0.6f));
            else if (done)
            {
                var c = Color.Lerp(Ui.Orange, Color.white, Mathf.PingPong(Time.time * 0.8f, 0.3f));
                if (Ui.Button(btn, c, "Pegar!", fs - 1))
                {
                    Sfx.Play("moeda");
                    popup = Profile.ClaimMission(i);
                }
            }
            else
            {
                Ui.Panel(btn, new Color(0.25f, 0.3f, 0.38f));
                Ui.Text(btn, "Em andamento", fs - 5, new Color(1f, 1f, 1f, 0.7f));
            }
        }

        var crown = new Rect(body.x + u, body.y + u * 47f, body.width - u * 2f, u * 8f);
        DrawCrownChest(crown, u, fs);

        var stats = new Rect(body.x + u, crown.yMax + u * 2f, body.width - u * 2f, u * 14f);
        Ui.Panel(stats, new Color(0.12f, 0.18f, 0.28f));
        int total = Profile.WinsTotal + Profile.LossesTotal;
        string rate = total > 0 ? $"{Mathf.RoundToInt(100f * Profile.WinsTotal / total)}%" : "-";
        Ui.Text(stats, $"Seu perfil\n\nVitórias: {Profile.WinsTotal}    Derrotas: {Profile.LossesTotal}    Aproveitamento: {rate}\nMelhor marca: {Profile.BestTrophies} troféus",
            fs - 3, Color.white);
    }

    // ------------------------------------------------------------ Aba Cartas (montar deck e evoluir)

    string picked;          // carta da coleção escolhida para entrar no deck
    Vector2 cardScroll;

    void DrawCards(Rect body, float u, int fs)
    {
        float avg = 0f;
        foreach (var k in Profile.Deck) avg += Profile.Find(k).cost;
        avg /= Mathf.Max(1, Profile.Deck.Count);
        Ui.Text(new Rect(body.x + u, body.y, body.width * 0.6f, u * 4f), "Seu deck", fs + 2, Color.white, TextAnchor.MiddleLeft);
        Ui.Text(new Rect(body.x, body.y, body.width - u, u * 4f), $"Custo médio: {avg:0.0}", fs - 2, Ui.Elixir, TextAnchor.MiddleRight);

        float cw = (body.width - u * 5f) / 4f, ch = cw * 1.22f, tileH = ch + u * 2.6f;
        for (int i = 0; i < Profile.Deck.Count; i++)
        {
            var c = Profile.Find(Profile.Deck[i]);
            var r = new Rect(body.x + u + (i % 4) * (cw + u), body.y + u * 4.5f + (i / 4) * (tileH + u * 0.6f), cw, ch);
            if (picked != null)
            {
                float pulse = Mathf.PingPong(Time.time * 2f, 1f);
                Ui.Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Color.Lerp(Ui.Orange, Color.yellow, pulse));
            }
            DrawCollectionCard(r, c, u, fs, true);
            if (GUI.Button(new Rect(r.x, r.y, r.width, tileH), GUIContent.none, GUIStyle.none))
            {
                if (picked != null)
                {
                    Profile.Deck[i] = picked;
                    picked = null;
                    Profile.Save();
                    Sfx.Play("carta");
                }
                else cardInfo = c.key;
            }
        }

        float listTop = body.y + u * 5f + 2f * (tileH + u * 0.6f);
        string hint = picked != null ? $"Toque numa carta do deck para trocar por {Profile.Find(picked).name}"
                                     : "Toque numa carta para ver detalhes, evoluir ou usar no deck";
        Ui.Text(new Rect(body.x, listTop, body.width, u * 3.5f), hint, fs - 4, picked != null ? Color.yellow : new Color(0.8f, 0.9f, 1f),
            TextAnchor.MiddleCenter, false);

        var view = new Rect(body.x, listTop + u * 4f, body.width, body.yMax - listTop - u * 4f);
        var owned = new List<CardData>();
        var findable = new List<CardData>();
        var locked = new List<CardData>();
        foreach (var c in game.Deck)
        {
            if (Profile.Deck.Contains(c.key)) continue;
            if (Profile.Owns(c.key)) owned.Add(c);
            else if (Profile.CanDrop(c)) findable.Add(c);
            else locked.Add(c);
        }

        float rowH = tileH + u * 1.2f;
        float contentH = 0f;
        foreach (var list in new[] { owned, findable, locked })
            if (list.Count > 0) contentH += u * 4.5f + Mathf.Ceil(list.Count / 4f) * rowH;
        contentH = Mathf.Max(contentH, view.height);

        Event ev = Event.current;
        if (view.Contains(ev.mousePosition) && GUI.enabled)
        {
            if (ev.type == EventType.MouseDrag) { cardScroll.y -= ev.delta.y; ev.Use(); }
            else if (ev.type == EventType.ScrollWheel) { cardScroll.y += ev.delta.y * u * 2f; ev.Use(); }
        }
        cardScroll.y = Mathf.Clamp(cardScroll.y, 0f, Mathf.Max(0f, contentH - view.height));
        cardScroll = GUI.BeginScrollView(view, cardScroll, new Rect(0, 0, view.width - 1f, contentH), false, false, GUIStyle.none, GUIStyle.none);

        float y = 0f;
        y = DrawSection("Coleção", owned, y, view.width, cw, ch, tileH, rowH, u, fs, 0);
        y = DrawSection("Encontre em baús", findable, y, view.width, cw, ch, tileH, rowH, u, fs, 1);
        DrawSection("Bloqueadas", locked, y, view.width, cw, ch, tileH, rowH, u, fs, 2);
        GUI.EndScrollView();
    }

    // state: 0 = sua, 1 = desbloqueada mas não encontrada, 2 = bloqueada
    float DrawSection(string title, List<CardData> list, float y, float width,
        float cw, float ch, float tileH, float rowH, float u, int fs, int state)
    {
        if (list.Count == 0) return y;
        Ui.Text(new Rect(u, y, width, u * 4f), title, fs, Color.white, TextAnchor.MiddleLeft);
        y += u * 4.5f;
        for (int i = 0; i < list.Count; i++)
        {
            var c = list[i];
            var r = new Rect(u + (i % 4) * (cw + u), y + (i / 4) * rowH, cw, ch);
            if (state == 0 && picked == c.key)
                Ui.Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Color.yellow);
            DrawCollectionCard(r, c, u, fs, state == 0);
            if (state == 1)
            {
                Ui.Fill(r, new Color(0.05f, 0.08f, 0.15f, 0.55f));
                Ui.Text(r, "?", fs * 2, new Color(1f, 1f, 1f, 0.8f));
            }
            else if (state == 2)
            {
                Ui.Fill(r, new Color(0f, 0f, 0f, 0.7f));
                Ui.Text(r, $"Arena {c.unlockArena + 1}", fs - 2, Color.white);
            }
            if (GUI.Button(new Rect(r.x, r.y, r.width, tileH), GUIContent.none, GUIStyle.none))
            {
                if (state == 0)
                {
                    if (picked == c.key) picked = null;
                    else cardInfo = c.key;
                }
                else if (state == 1) game.Toast($"{c.name}: pode cair de qualquer baú!");
                else game.Toast($"{c.name}: chegue na {Profile.ArenaNames[c.unlockArena]} para desbloquear");
            }
        }
        return y + Mathf.Ceil(list.Count / 4f) * rowH;
    }

    static void DrawCollectionCard(Rect r, CardData c, float u, int fs, bool owned)
    {
        ArenaGame.DrawCardFace(r, c, Mathf.RoundToInt(r.width / 6.5f));
        var cost = new Rect(r.x - u * 0.5f, r.y - u * 0.5f, u * 4f, u * 4f);
        Ui.Disc(cost, Ui.Elixir);
        Ui.Text(cost, c.cost.ToString(), fs - 1, Color.white);
        if (!owned) return;

        var lvl = new Rect(r.xMax - u * 6f, r.y + u * 0.3f, u * 5.6f, u * 2.6f);
        Ui.Fill(lvl, new Color(0f, 0f, 0f, 0.6f));
        Ui.Text(lvl, "Nv " + Profile.CardLevel(c.key), fs - 5, Color.white);

        // barra de cópias para evoluir
        var bar = new Rect(r.x, r.yMax + u * 0.4f, r.width, u * 2.2f);
        if (Profile.CardLevel(c.key) >= Profile.MaxCardLevel)
        {
            Ui.Bar(bar, 1f, Ui.Gold);
            Ui.Text(bar, "MÁX", fs - 6, Color.white);
            return;
        }
        bool ready = Profile.HasCopiesToUpgrade(c.key);
        Color fill = ready ? Color.Lerp(new Color(0.3f, 0.85f, 0.35f), new Color(0.6f, 1f, 0.6f), Mathf.PingPong(Time.time, 0.5f))
                           : new Color(0.3f, 0.6f, 1f);
        Ui.Bar(bar, (float)Profile.CardCount(c.key) / Profile.CopiesNeeded(c.key), fill);
        Ui.Text(bar, ready ? "EVOLUIR!" : $"{Profile.CardCount(c.key)}/{Profile.CopiesNeeded(c.key)}", fs - 6, Color.white);
    }

    // Painel de detalhes da carta: atributos, evolução e "usar no deck"
    void DrawCardInfo(Rect col, float u, int fs)
    {
        var c = Profile.Find(cardInfo);
        Ui.Fill(col, new Color(0f, 0f, 0f, 0.7f));
        var p = new Rect(col.x + u * 2f, col.y + col.height * 0.16f, col.width - u * 4f, col.height * 0.64f);
        Ui.Panel(p, new Color(0.13f, 0.25f, 0.4f));

        var art = new Rect(p.x + u * 2f, p.y + u * 2f, p.width * 0.36f, p.width * 0.36f * 1.25f);
        ArenaGame.DrawCardFace(art, c, fs - 3);
        int lvl = Profile.CardLevel(c.key);
        float m = Profile.LevelMult(lvl), mNext = Profile.LevelMult(lvl + 1);
        bool canNext = lvl < Profile.MaxCardLevel;

        var info = new Rect(art.xMax + u * 2f, art.y, p.xMax - art.xMax - u * 4f, art.height);
        Ui.Text(new Rect(info.x, info.y, info.width, u * 4.5f), c.name, fs + 3, Color.white, TextAnchor.MiddleLeft);
        Ui.Text(new Rect(info.x, info.y + u * 4.5f, info.width, u * 3.5f), $"Nível {lvl}" + (canNext ? "" : " (máximo)"), fs, Ui.Gold, TextAnchor.MiddleLeft);
        Ui.Text(new Rect(info.x, info.y + u * 8.5f, info.width, info.height - u * 8.5f), c.description, fs - 3,
            new Color(0.85f, 0.92f, 1f), TextAnchor.UpperLeft, false);

        string Stat(string name, float baseValue)
        {
            int now = Mathf.RoundToInt(baseValue * m);
            return canNext ? $"{name}: {now}  (+{Mathf.RoundToInt(baseValue * mNext) - now})" : $"{name}: {now}";
        }
        string stats = c.kind == CardKind.Spell
            ? $"{Stat("Dano", c.damage)}\nRaio: {c.spellRadius}\nCusto: {c.cost} de elixir"
            : $"{Stat("Vida", c.hp)}\n{Stat("Dano", c.damage)}\nAlcance: {(c.range > 1.5f ? c.range.ToString("0.#") : "corpo a corpo")}" +
              $"\nQuantidade: x{c.count}   Custo: {c.cost} de elixir" +
              (c.splash > 0 ? "\nDano em área" : "") + (c.targetsTowersOnly ? "\nAtaca só torres" : "");
        var st = new Rect(p.x + u * 2f, art.yMax + u * 1.5f, p.width - u * 4f, u * 16f);
        Ui.Fill(st, new Color(0f, 0f, 0f, 0.25f));
        Ui.Text(new Rect(st.x + u, st.y + u * 0.5f, st.width - u * 2f, st.height - u), stats, fs - 2, Color.white, TextAnchor.UpperLeft, false);

        var bar = new Rect(p.x + u * 2f, st.yMax + u * 1.2f, p.width - u * 4f, u * 3f);
        if (canNext)
        {
            Ui.Bar(bar, (float)Profile.CardCount(c.key) / Profile.CopiesNeeded(c.key), new Color(0.3f, 0.75f, 0.4f));
            Ui.Text(bar, $"Cópias: {Profile.CardCount(c.key)}/{Profile.CopiesNeeded(c.key)}", fs - 3, Color.white);
        }

        float bw = (p.width - u * 8f) / 3f;
        var by = p.yMax - u * 9f;
        if (canNext)
        {
            bool ok = Profile.CanUpgrade(c.key);
            var b = new Rect(p.x + u * 2f, by, bw, u * 7f);
            if (Ui.Button(b, ok ? new Color(0.2f, 0.65f, 0.3f) : new Color(0.4f, 0.4f, 0.45f), $"Evoluir\n{Profile.CoinsNeeded(c.key)} moedas", fs - 4))
            {
                if (Profile.Upgrade(c.key))
                {
                    Sfx.Play("evoluir");
                    Profile.Track("evoluir");
                    Profile.Save();
                    game.Toast($"{c.name} subiu para o nível {Profile.CardLevel(c.key)}!");
                }
                else
                {
                    Sfx.Play("erro");
                    game.Toast(Profile.HasCopiesToUpgrade(c.key) ? "Moedas insuficientes" : "Faltam cópias (ganhe em baús e na loja)");
                }
            }
        }
        if (!Profile.Deck.Contains(c.key))
        {
            var b = new Rect(p.x + u * 4f + bw, by, bw, u * 7f);
            if (Ui.Button(b, Ui.Orange, "Usar", fs))
            {
                picked = c.key;
                cardInfo = null;
                return;
            }
        }
        var close = new Rect(p.xMax - u * 2f - bw, by, bw, u * 7f);
        if (Ui.Button(close, new Color(0.35f, 0.38f, 0.45f), "Fechar", fs)) cardInfo = null;
    }

    // ------------------------------------------------------------ Configurações

    string nameEdit;

    void DrawSettings(Rect col, float u, int fs)
    {
        Ui.Fill(col, new Color(0f, 0f, 0f, 0.7f));
        var p = new Rect(col.x + u * 3f, col.y + col.height * 0.22f, col.width - u * 6f, col.height * 0.52f);
        Ui.Panel(p, new Color(0.13f, 0.25f, 0.4f));
        Ui.Text(new Rect(p.x, p.y + u, p.width, u * 5f), "Ajustes", fs + 3, Color.white);

        if (nameEdit == null) nameEdit = Profile.Name;
        Ui.Text(new Rect(p.x + u * 2f, p.y + u * 7f, p.width, u * 3f), "Seu nome", fs - 2, Color.white, TextAnchor.MiddleLeft);
        var style = new GUIStyle(GUI.skin.textField) { fontSize = fs, alignment = TextAnchor.MiddleLeft };
        nameEdit = GUI.TextField(new Rect(p.x + u * 2f, p.y + u * 10.5f, p.width - u * 4f, u * 5f), nameEdit, 16, style);

        Ui.Text(new Rect(p.x + u * 2f, p.y + u * 17.5f, p.width, u * 3f), $"Música: {Mathf.RoundToInt(Sfx.MusicVolume * 100)}%", fs - 2, Color.white, TextAnchor.MiddleLeft);
        float mv = GUI.HorizontalSlider(new Rect(p.x + u * 2f, p.y + u * 21f, p.width - u * 4f, u * 3f), Sfx.MusicVolume, 0f, 1f);
        Ui.Text(new Rect(p.x + u * 2f, p.y + u * 25f, p.width, u * 3f), $"Efeitos: {Mathf.RoundToInt(Sfx.SfxVolume * 100)}%", fs - 2, Color.white, TextAnchor.MiddleLeft);
        float sv = GUI.HorizontalSlider(new Rect(p.x + u * 2f, p.y + u * 28.5f, p.width - u * 4f, u * 3f), Sfx.SfxVolume, 0f, 1f);
        if (!Mathf.Approximately(mv, Sfx.MusicVolume) || !Mathf.Approximately(sv, Sfx.SfxVolume)) Sfx.SetVolumes(mv, sv);

        Ui.Text(new Rect(p.x, p.y + u * 33f, p.width, u * 4f), "ArenaDeCartas  •  feito em Unity", fs - 5, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter, false);

        if (Ui.Button(new Rect(p.center.x - p.width * 0.25f, p.yMax - u * 9f, p.width * 0.5f, u * 7f), Ui.Orange, "Salvar", fs + 1))
        {
            string n = nameEdit.Trim();
            if (n.Length > 0) Profile.Name = n;
            Profile.Save();
            nameEdit = null;
            settingsOpen = false;
        }
    }

    // ------------------------------------------------------------ Navegação e popup

    static void Badge(Rect r, int fs)
    {
        Ui.Disc(r, new Color(0.9f, 0.15f, 0.15f));
        Ui.Text(r, "!", fs + 2, Color.white);
    }

    bool AnyUpgradeReady()
    {
        foreach (var c in game.Deck) if (Profile.Owns(c.key) && Profile.HasCopiesToUpgrade(c.key)) return true;
        return false;
    }

    void DrawNav(Rect col, float u, int fs)
    {
        var nav = new Rect(col.x, col.yMax - u * 10f, col.width, u * 10f);
        Ui.Fill(nav, Ui.Dark);
        float tw = col.width / Tabs.Length;
        bool[] badge =
        {
            !Profile.OfferBought(0), AnyUpgradeReady(), false, false, Profile.AnyMissionToClaim
        };
        for (int i = 0; i < Tabs.Length; i++)
        {
            var t = new Rect(col.x + i * tw, nav.y, tw, nav.height);
            bool sel = i == tab;
            if (sel) Ui.Panel(new Rect(t.x + 2, t.y + 2, t.width - 4, t.height - 4), Ui.Teal);
            Ui.Text(t, Tabs[i], sel ? fs : fs - 3, sel ? Color.white : new Color(1f, 1f, 1f, 0.6f));
            if (badge[i]) Badge(new Rect(t.xMax - u * 4f, t.y + u * 0.6f, u * 3f, u * 3f), fs - 4);
            if (GUI.Button(t, GUIContent.none, GUIStyle.none) && tab != i)
            {
                tab = i;
                picked = null;
                Sfx.Play("botao");
            }
        }
    }

    void DrawPopup(Rect col, float u, int fs)
    {
        Ui.Fill(col, new Color(0f, 0f, 0f, 0.6f));
        var p = new Rect(col.x + col.width * 0.08f, col.y + col.height * 0.24f, col.width * 0.84f, col.height * 0.48f);
        Ui.Panel(p, Ui.Shade(Ui.Teal, 0.8f));
        Ui.Text(new Rect(p.x + u, p.y + u * 2f, p.width - u * 2f, p.height - u * 12f), popup, fs, Color.white);
        if (Ui.Button(new Rect(p.center.x - col.width * 0.15f, p.yMax - u * 9f, col.width * 0.3f, u * 7f), Ui.Orange, "OK", fs + 2))
            popup = null;
    }
}
