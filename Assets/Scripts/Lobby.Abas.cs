using System.Collections.Generic;
using UnityEngine;

// Abas Batalha, Loja, Missões e Eventos.
public partial class Lobby
{
    // ------------------------------------------------------------ Aba Batalha

    void DrawBattleTab(Rect body, float u, int fs)
    {
        int a = Profile.ArenaIndex;
        Ui.Text(new Rect(body.x, body.y, body.width, u * 4f), $"Arena {a + 1}: {Profile.ArenaNames[a]}", fs + 2, Color.white);
        int lg = Profile.LeagueIndex;
        int goal = Profile.NextGoal;
        string next = goal < 0 ? "Você está no topo! Defenda sua Liga Lenda."
            : a + 1 < Profile.ArenaTrophies.Length ? $"Próxima arena com {goal} troféus"
            : $"{(lg < 0 ? "Primeira liga" : "Próxima liga")} com {goal} troféus";
        Ui.Text(new Rect(body.x, body.y + u * 3.6f, body.width, u * 3f), next, fs - 3, new Color(0.75f, 0.85f, 0.95f), TextAnchor.MiddleCenter, false);
        if (lg >= 0) DrawLeagueBadge(new Rect(body.xMax - u * 12f, body.y + u * 7.5f, u * 11f, u * 12f), lg, u, fs);

        var preview = new Rect(body.x + u, body.y + u * 7f, body.width - u * 2f, u * 30f);
        DrawClouds(preview, u);
        DrawArenaPreview(preview, a, u);
        DrawWalkers(preview, u);
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
        if (Ui.Button(new Rect(body.xMax - u * 14f, body.y, u * 13f, u * 4.5f), new Color(0.45f, 0.3f, 0.7f), "Torres", fs - 2))
            skinsOpen = true;
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

    // ------------------------------------------------------------ Aba Eventos: treino, evento do dia e passe

    void DrawEvents(Rect body, float u, int fs)
    {
        float y = body.y + u;
        float w = body.width - u * 4f;
        float x = body.x + u * 2f;

        // Campanha
        var cp = new Rect(x, y, w, u * 9f);
        Ui.Panel(cp, new Color(0.55f, 0.18f, 0.25f));
        Ui.Text(new Rect(cp.x + u * 2f, cp.y + u * 0.8f, cp.width * 0.6f, u * 4f), "Campanha dos Chefes", fs + 1, Ui.Gold, TextAnchor.MiddleLeft);
        Ui.Text(new Rect(cp.x + u * 2f, cp.y + u * 4.6f, cp.width * 0.6f, u * 3.5f),
            $"Fase {Profile.CampaignUnlocked + 1}/10   ·   {Profile.CampaignTotalStars}/30 estrelas", fs - 4, Color.white, TextAnchor.MiddleLeft, false);
        if (Ui.Button(new Rect(cp.xMax - cp.width * 0.33f, cp.y + u * 1.5f, cp.width * 0.3f, u * 6f), Ui.Orange, "Mapa", fs))
        {
            campaignOpen = true;
            campaignPick = -1;
        }
        y = cp.yMax + u * 1.5f;

        // Desafio semanal
        var dz = new Rect(x, y, w, u * 10f);
        Ui.Panel(dz, new Color(0.15f, 0.42f, 0.38f));
        Ui.Text(new Rect(dz.x + u * 2f, dz.y + u * 0.6f, dz.width * 0.6f, u * 4f), "Desafio Semanal", fs + 1, Ui.Gold, TextAnchor.MiddleLeft);
        int cw = Profile.ChallengeWinsNow, cl = Profile.ChallengeLossesNow;
        for (int i = 0; i < Profile.ChallengeMaxWins; i++)      // 12 bolinhas de vitória
            Ui.Disc(new Rect(dz.x + u * 2f + i * u * 2.6f, dz.y + u * 4.8f, u * 2.1f, u * 2.1f), i < cw ? Ui.Gold : new Color(0f, 0f, 0f, 0.35f));
        for (int i = 0; i < Profile.ChallengeMaxLosses; i++)    // 3 corações de vida
            Ui.Disc(new Rect(dz.x + u * 2f + i * u * 2.6f, dz.y + u * 7.3f, u * 2.1f, u * 2.1f), i < Profile.ChallengeMaxLosses - cl ? new Color(1f, 0.35f, 0.4f) : new Color(0f, 0f, 0f, 0.35f));
        Ui.Text(new Rect(dz.x + u * 10f, dz.y + u * 7f, dz.width * 0.5f, u * 2.6f), "Prêmio: " + Profile.ChallengeRewardText(cw), fs - 6,
            Color.white, TextAnchor.MiddleLeft, false);
        var db = new Rect(dz.xMax - dz.width * 0.33f, dz.y + u * 1.5f, dz.width * 0.3f, u * 5f);
        if (!Profile.ChallengeOver)
        {
            if (Ui.Button(db, Ui.Orange, "Jogar", fs)) game.StartMatch(MatchMode.Desafio);
        }
        else if (!Profile.ChallengeClaimed)
        {
            if (Ui.Button(db, new Color(0.2f, 0.65f, 0.3f), "Pegar", fs))
            {
                string txt = Profile.ChallengeRewardText(cw);
                var res = Profile.ClaimChallenge();
                Sfx.Play("moeda");
                if (res != null) ShowChest(res);
                popup = $"Desafio semanal encerrado com {cw} vitórias!\n\n{txt}";
            }
        }
        else Ui.Text(db, "Volta segunda", fs - 4, Color.white);
        y = dz.yMax + u * 1.5f;

        // Treino
        var tr = new Rect(x, y, w, u * 11f);
        Ui.Panel(tr, new Color(0.18f, 0.32f, 0.5f));
        Ui.Text(new Rect(tr.x + u * 2f, tr.y + u, tr.width * 0.6f, u * 4f), "Treino", fs + 2, Color.white, TextAnchor.MiddleLeft);
        Ui.Text(new Rect(tr.x + u * 2f, tr.y + u * 5f, tr.width * 0.6f, u * 5f), ArenaGame.ModeDescriptions[1], fs - 4,
            new Color(0.85f, 0.92f, 1f), TextAnchor.UpperLeft, false);
        if (Ui.Button(new Rect(tr.xMax - tr.width * 0.33f, tr.y + u * 2.5f, tr.width * 0.3f, u * 6f), Ui.Teal, "Treinar", fs))
            game.StartMatch(MatchMode.Treino);
        y = tr.yMax + u * 1.5f;

        // Evento do dia
        var mode = Profile.TodayEvent();
        var ev = new Rect(x, y, w, u * 14f);
        Ui.Panel(ev, new Color(0.45f, 0.2f, 0.55f));
        Ui.Text(new Rect(ev.x + u * 2f, ev.y + u, ev.width, u * 3f), "EVENTO DO DIA", fs - 3, Ui.Gold, TextAnchor.MiddleLeft);
        Ui.Text(new Rect(ev.x + u * 2f, ev.y + u * 3.8f, ev.width * 0.6f, u * 4f), ArenaGame.ModeNames[(int)mode], fs + 2, Color.white, TextAnchor.MiddleLeft);
        Ui.Text(new Rect(ev.x + u * 2f, ev.y + u * 7.8f, ev.width * 0.6f, u * 5.5f),
            ArenaGame.ModeDescriptions[(int)mode] + $"\nVitória: +60 moedas ({Profile.EventRewardLeft()}/{Profile.EventRewardsPerDay} hoje)",
            fs - 4, new Color(0.95f, 0.88f, 1f), TextAnchor.UpperLeft, false);
        if (Ui.Button(new Rect(ev.xMax - ev.width * 0.33f, ev.y + u * 4f, ev.width * 0.3f, u * 6f), Ui.Orange, "Jogar", fs))
            game.StartMatch(mode);
        y = ev.yMax + u * 1.5f;

        // Passe da temporada (grátis): coroas da temporada liberam prêmios
        Ui.Text(new Rect(x, y, w, u * 3.5f), "Passe da Temporada", fs + 1, Color.white, TextAnchor.MiddleLeft);
        Ui.Text(new Rect(x, y, w, u * 3.5f), $"{Profile.SeasonCrowns} coroas", fs - 2, Ui.Gold, TextAnchor.MiddleRight);
        y += u * 4f;
        var view = new Rect(body.x, y, body.width, body.yMax - y);
        float rowH = u * 6.2f;
        var content = new Rect(0, 0, body.width - u * 2f, Profile.PassTiers * rowH);
        eventScroll = GUI.BeginScrollView(view, eventScroll, content, false, false, GUIStyle.none, GUIStyle.none);
        for (int i = 0; i < Profile.PassTiers; i++)
        {
            var r = new Rect(u * 2f, i * rowH, content.width - u * 2f, rowH - u * 0.8f);
            bool open = Profile.PassUnlocked(i), got = Profile.PassClaimed(i);
            Ui.Panel(r, got ? new Color(0.2f, 0.25f, 0.3f) : open ? new Color(0.2f, 0.45f, 0.3f) : new Color(0.15f, 0.18f, 0.26f));
            var num = new Rect(r.x + u, r.y + u * 0.6f, r.height - u * 1.2f, r.height - u * 1.2f);
            Ui.Disc(num, open ? Ui.Gold : new Color(0.4f, 0.4f, 0.45f));
            Ui.Text(num, (i + 1).ToString(), fs - 2, Color.white);
            Ui.Text(new Rect(num.xMax + u * 1.5f, r.y, r.width * 0.5f, r.height), Profile.PassRewardText(i), fs - 2, Color.white, TextAnchor.MiddleLeft);
            var act = new Rect(r.xMax - r.width * 0.3f, r.y + u * 0.6f, r.width * 0.28f, r.height - u * 1.2f);
            if (got) Ui.Text(act, "Pego", fs - 3, new Color(1f, 1f, 1f, 0.5f));
            else if (!open) Ui.Text(act, $"{(i + 1) * Profile.CrownsPerTier} coroas", fs - 4, new Color(1f, 1f, 1f, 0.6f));
            else if (Ui.Button(act, Ui.Orange, "Pegar", fs - 3))
            {
                var res = Profile.ClaimPass(i);
                if (res != null) ShowChest(res);
                else { Sfx.Play("moeda"); popup = "Prêmio do passe:\n\n" + Profile.PassRewardText(i); }
            }
        }
        GUI.EndScrollView();
    }
}
