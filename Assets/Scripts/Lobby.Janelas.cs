using System.Collections.Generic;
using UnityEngine;

// Janelas: ajustes, perfil, visual das torres, tutorial, prêmio diário e mapa da campanha.
public partial class Lobby
{
    // ------------------------------------------------------------ Configurações

    string nameEdit;

    void DrawSettings(Rect col, float u, int fs)
    {
        Ui.Fill(col, new Color(0f, 0f, 0f, 0.7f));
        var p = new Rect(col.x + u * 3f, col.y + col.height * 0.07f, col.width - u * 6f, col.height * 0.82f);
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

        // Acessibilidade
        float ay = p.y + u * 33f;
        Ui.Text(new Rect(p.x + u * 2f, ay, p.width * 0.4f, u * 4f), "Texto:", fs - 2, Color.white, TextAnchor.MiddleLeft);
        for (int i = 0; i < 3; i++)
        {
            var tb = new Rect(p.x + p.width * 0.3f + i * (p.width * 0.22f), ay, p.width * 0.2f, u * 4f);
            if (Ui.Button(tb, Profile.TextScale == i ? Ui.Orange : new Color(0.3f, 0.34f, 0.45f), Profile.TextScaleNames[i], fs - 5))
            {
                Profile.TextScale = i;
                Ui.FontScale = Profile.TextScales[i];
            }
        }
        var cb = new Rect(p.x + u * 2f, ay + u * 5f, p.width / 2f - u * 3f, u * 4.2f);
        if (Ui.Button(cb, Profile.ColorBlind ? Ui.Orange : new Color(0.3f, 0.34f, 0.45f), Profile.ColorBlind ? "Daltônico: SIM" : "Daltônico: NÃO", fs - 5))
            Profile.ColorBlind = !Profile.ColorBlind;
        var sb = new Rect(p.center.x + u, ay + u * 5f, p.width / 2f - u * 3f, u * 4.2f);
        if (Ui.Button(sb, Profile.ReduceShake ? Ui.Orange : new Color(0.3f, 0.34f, 0.45f), Profile.ReduceShake ? "Tremor: DESLIGADO" : "Tremor: LIGADO", fs - 5))
            Profile.ReduceShake = !Profile.ReduceShake;
        var fx = new Rect(p.x + u * 2f, ay + u * 9.6f, p.width - u * 4f, u * 4.2f);
        if (Ui.Button(fx, Profile.PostFx ? Ui.Orange : new Color(0.3f, 0.34f, 0.45f),
                Profile.PostFx ? "Efeitos visuais (brilho e cores): LIGADO" : "Efeitos visuais: DESLIGADO (mais leve)", fs - 5))
        {
            Profile.PostFx = !Profile.PostFx;
            game.ApplyPostFx();
        }

        // Desempenho: qualidade baixa e contador de quadros
        var lq = new Rect(p.x + u * 2f, ay + u * 14.2f, p.width / 2f - u * 3f, u * 4.2f);
        if (Ui.Button(lq, Profile.LowQuality ? Ui.Orange : new Color(0.3f, 0.34f, 0.45f), Profile.LowQuality ? "Qualidade: BAIXA" : "Qualidade: NORMAL", fs - 5))
        {
            Profile.LowQuality = !Profile.LowQuality;
            game.ApplyQuality();
        }
        var fp = new Rect(p.center.x + u, ay + u * 14.2f, p.width / 2f - u * 3f, u * 4.2f);
        if (Ui.Button(fp, Profile.ShowFps ? Ui.Orange : new Color(0.3f, 0.34f, 0.45f), Profile.ShowFps ? "Mostrar FPS: SIM" : "Mostrar FPS: NÃO", fs - 5))
            Profile.ShowFps = !Profile.ShowFps;

        float ry = ay + u * 20f;
        if (Ui.Button(new Rect(p.x + u * 2f, ry, p.width / 2f - u * 3f, u * 4.5f), Ui.Teal, "Rever tutorial", fs - 3))
        {
            settingsOpen = false;
            nameEdit = null;
            game.StartMatch(MatchMode.Tutorial);
        }
        if (Ui.Button(new Rect(p.center.x + u, ry, p.width / 2f - u * 3f, u * 4.5f), Ui.Teal, "Rever história", fs - 3))
        {
            settingsOpen = false;
            nameEdit = null;
            Profile.IntroSeen = false;
            introSlide = 0; introT = 0f; introTyped = false;
        }
        Ui.Text(new Rect(p.x, ry + u * 5.5f, p.width, u * 3f), "ArenaDeCartas  •  feito em Unity", fs - 5, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter, false);

        if (Ui.Button(new Rect(p.center.x - p.width * 0.25f, p.yMax - u * 9f, p.width * 0.5f, u * 7f), Ui.Orange, "Salvar", fs + 1))
        {
            string n = nameEdit.Trim();
            if (n.Length > 0) Profile.Name = n;
            Profile.Save();
            nameEdit = null;
            settingsOpen = false;
        }
    }

    // ------------------------------------------------------------ Perfil: estatísticas e conquistas

    void DrawProfile(Rect col, float u, int fs)
    {
        Ui.Fill(col, new Color(0f, 0f, 0f, 0.7f));
        var p = new Rect(col.x + u * 2f, col.y + col.height * 0.08f, col.width - u * 4f, col.height * 0.84f);
        Ui.Panel(p, new Color(0.13f, 0.22f, 0.36f));
        Ui.Text(new Rect(p.x, p.y + u, p.width, u * 5f), Profile.Name, fs + 4, Color.white);
        int games = Profile.WinsTotal + Profile.LossesTotal;
        string stats = $"Nível {Profile.Level}    Recorde: {Profile.BestTrophies} troféus\n" +
                       $"Vitórias: {Profile.WinsTotal}   Derrotas: {Profile.LossesTotal}" +
                       (games > 0 ? $"   ({100 * Profile.WinsTotal / games}%)" : "") +
                       $"\nCoroas: {Profile.CrownsTotal}   Cartas: {Profile.OwnedCount()}/{game.Deck.Length}" +
                       (Profile.LeagueIndex >= 0 ? $"\n{Profile.LeagueNames[Profile.LeagueIndex]}" : "");
        Ui.Text(new Rect(p.x + u * 2f, p.y + u * 6f, p.width - u * 4f, u * 10f), stats, fs - 2, new Color(0.85f, 0.92f, 1f));
        Ui.Text(new Rect(p.x + u * 2f, p.y + u * 16.5f, p.width, u * 4f), "Conquistas", fs + 1, Ui.Gold, TextAnchor.MiddleLeft);

        var list = Profile.Achievements;
        float rowH = u * 7.5f;
        var view = new Rect(p.x + u, p.y + u * 21f, p.width - u * 2f, p.height - u * 31f);
        var content = new Rect(0, 0, view.width - u, list.Length * rowH);
        profileScroll = GUI.BeginScrollView(view, profileScroll, content, false, false, GUIStyle.none, GUIStyle.none);
        for (int i = 0; i < list.Length; i++)
        {
            var a = list[i];
            var r = new Rect(u, i * rowH, content.width - u, rowH - u * 0.8f);
            bool done = Profile.AchievementDone(a), got = Profile.AchievementClaimed(a);
            Ui.Panel(r, got ? new Color(0.2f, 0.24f, 0.3f) : done ? new Color(0.2f, 0.45f, 0.3f) : new Color(0.16f, 0.19f, 0.28f));
            Ui.Text(new Rect(r.x + u * 1.5f, r.y + u * 0.3f, r.width * 0.62f, u * 3.2f), a.name, fs - 2, Color.white, TextAnchor.MiddleLeft);
            Ui.Text(new Rect(r.x + u * 1.5f, r.y + u * 3.3f, r.width * 0.62f, u * 3f),
                $"{a.desc}  ({Mathf.Min(a.progress(), a.goal)}/{a.goal})", fs - 5, new Color(0.85f, 0.9f, 1f), TextAnchor.MiddleLeft, false);
            var act = new Rect(r.xMax - r.width * 0.32f, r.y + u * 0.8f, r.width * 0.3f, r.height - u * 1.6f);
            if (got) Ui.Text(act, "Feito", fs - 3, new Color(1f, 1f, 1f, 0.5f));
            else if (done)
            {
                if (Ui.Button(act, Ui.Orange, $"+{a.gems} gemas", fs - 4)) { Profile.ClaimAchievement(a); Sfx.Play("moeda"); }
            }
            else Ui.Text(act, $"{a.gems} gemas", fs - 4, new Color(0.4f, 0.95f, 0.5f));
        }
        GUI.EndScrollView();

        if (Ui.Button(new Rect(p.center.x - p.width * 0.2f, p.yMax - u * 8.5f, p.width * 0.4f, u * 6.5f), Ui.Teal, "Fechar", fs + 1))
            profileOpen = false;
    }

    // ------------------------------------------------------------ Visual das torres

    void DrawSkins(Rect col, float u, int fs)
    {
        Ui.Fill(col, new Color(0f, 0f, 0f, 0.7f));
        var p = new Rect(col.x + u * 2f, col.y + col.height * 0.1f, col.width - u * 4f, col.height * 0.8f);
        Ui.Panel(p, new Color(0.2f, 0.16f, 0.32f));
        Ui.Text(new Rect(p.x, p.y + u, p.width, u * 5f), "Visual das Torres", fs + 4, Color.white);
        Ui.Text(new Rect(p.x, p.y + u * 5.5f, p.width, u * 3f), "Só muda a aparência. Aparece na batalha e na tela de VS.", fs - 5,
            new Color(0.85f, 0.85f, 1f), TextAnchor.MiddleCenter, false);
        float cw = (p.width - u * 6f) / 2f, ch = (p.height - u * 22f) / 2f;
        for (int i = 0; i < Profile.SkinIds.Length; i++)
        {
            string id = Profile.SkinIds[i];
            var r = new Rect(p.x + u * 2f + (i % 2) * (cw + u * 2f), p.y + u * 10f + (i / 2) * (ch + u * 1.5f), cw, ch);
            bool owned = Profile.OwnsSkin(id), using_ = Profile.TowerSkin == id;
            Ui.Panel(r, using_ ? new Color(0.25f, 0.5f, 0.35f) : new Color(0.14f, 0.16f, 0.26f));
            var tex = Resources.Load<Texture2D>("Batalha/rei_azul" + (id == "" ? "" : "_" + id));
            if (tex != null) GUI.DrawTexture(new Rect(r.x + u, r.y + u, r.width - u * 2f, r.height - u * 10f), tex, ScaleMode.ScaleToFit);
            Ui.Text(new Rect(r.x, r.yMax - u * 9f, r.width, u * 3.5f), Profile.SkinNames[i], fs, Color.white);
            var b = new Rect(r.x + u, r.yMax - u * 5.2f, r.width - u * 2f, u * 4.5f);
            if (using_) Ui.Text(b, "Em uso", fs - 2, Ui.Gold);
            else if (owned)
            {
                if (Ui.Button(b, Ui.Teal, "Usar", fs - 2)) { Profile.TowerSkin = id; Profile.Save(); }
            }
            else if (Ui.Button(b, Profile.Gems >= Profile.SkinPrices[i] ? new Color(0.2f, 0.65f, 0.35f) : new Color(0.4f, 0.4f, 0.45f),
                         $"{Profile.SkinPrices[i]} gemas", fs - 3))
            {
                if (Profile.BuySkin(i)) Sfx.Play("moeda");
                else { Sfx.Play("erro"); game.Toast("Gemas insuficientes"); }
            }
        }
        if (Ui.Button(new Rect(p.center.x - p.width * 0.2f, p.yMax - u * 8.5f, p.width * 0.4f, u * 6.5f), Ui.Orange, "Fechar", fs + 1))
            skinsOpen = false;
    }

    // ------------------------------------------------------------ Convite do tutorial (primeira vez)

    void DrawTutorialInvite(Rect col, float u, int fs)
    {
        if (mentorTex == null) mentorTex = Resources.Load<Texture2D>("Tutorial/mentor");
        Ui.Fill(col, new Color(0f, 0f, 0f, 0.72f));
        var p = new Rect(col.x + u * 3f, col.y + col.height * 0.2f, col.width - u * 6f, col.height * 0.58f);
        Ui.Panel(p, new Color(0.12f, 0.42f, 0.4f));
        float bob = Mathf.Sin(Time.time * 2f) * u * 0.6f;
        if (mentorTex != null) GUI.DrawTexture(new Rect(p.center.x - u * 11f, p.y - u * 12f + bob, u * 22f, u * 26f), mentorTex, ScaleMode.ScaleToFit);
        Ui.Text(new Rect(p.x, p.y + u * 14.5f, p.width, u * 5f), "Bem-vindo à Arena!", fs + 4, Ui.Gold);
        Ui.Text(new Rect(p.x + u * 3f, p.y + u * 20f, p.width - u * 6f, u * 12f),
            "Eu sou o Capitão Tomás. Quer uma partida rápida de treino para aprender a jogar?\nNo fim tem presente: 100 moedas e 10 gemas!",
            fs - 2, Color.white, TextAnchor.UpperCenter, false);
        var a = new Rect(p.x + u * 3f, p.yMax - u * 9f, p.width * 0.55f - u * 3f, u * 7f);
        var b = new Rect(a.xMax + u * 2f, a.y, p.xMax - a.xMax - u * 5f, a.height);
        GUI.enabled = true;
        if (Ui.Button(a, Ui.Orange, "Vamos lá!", fs + 1)) { tutorialAsked = true; game.StartMatch(MatchMode.Tutorial); }
        if (Ui.Button(b, new Color(0.3f, 0.32f, 0.4f), "Pular", fs - 1))
        {
            tutorialAsked = true;
            Profile.TutorialDone = true;
            Profile.Save();
        }
    }

    // ------------------------------------------------------------ Prêmio de login diário

    Texture2D[] chestIcons;

    void DrawLogin(Rect col, float u, int fs)
    {
        if (chestIcons == null)
        {
            chestIcons = new Texture2D[3];
            for (int i = 0; i < 3; i++) chestIcons[i] = Resources.Load<Texture2D>("Icones/bau" + i);
        }
        Ui.Fill(col, new Color(0f, 0f, 0f, 0.72f));
        var p = new Rect(col.x + u * 2f, col.y + col.height * 0.14f, col.width - u * 4f, col.height * 0.66f);
        Ui.Panel(p, new Color(0.16f, 0.24f, 0.45f));
        Ui.Text(new Rect(p.x, p.y + u, p.width, u * 5f), "Prêmio Diário", fs + 4, Ui.Gold);
        Ui.Text(new Rect(p.x, p.y + u * 5.5f, p.width, u * 3f), "Volte todo dia: o 7º dia tem Baú de Ouro!", fs - 4,
            new Color(0.85f, 0.9f, 1f), TextAnchor.MiddleCenter, false);
        int today = Profile.LoginIndex;
        float tw = (p.width - u * 6f) / 4f, th = u * 15f;
        for (int i = 0; i < 7; i++)
        {
            int row = i / 4, colI = i % 4;
            float rowW = row == 0 ? 4 : 3;
            float sx = p.x + (p.width - (rowW * tw + (rowW - 1) * u * 1.3f)) / 2f;
            var r = new Rect(sx + colI * (tw + u * 1.3f), p.y + u * 10f + row * (th + u * 1.5f), tw, th);
            bool past = i < today, now = i == today;
            float pulse = now ? 1f + 0.04f * Mathf.Sin(Time.time * 5f) : 1f;
            var rr = new Rect(r.center.x - r.width * pulse / 2f, r.center.y - r.height * pulse / 2f, r.width * pulse, r.height * pulse);
            if (now) Ui.Fill(new Rect(rr.x - 3, rr.y - 3, rr.width + 6, rr.height + 6), Ui.Gold);
            Ui.Panel(rr, past ? new Color(0.2f, 0.25f, 0.3f) : i == 6 ? new Color(0.55f, 0.35f, 0.75f) : new Color(0.22f, 0.36f, 0.6f));
            Ui.Text(new Rect(rr.x, rr.y + u * 0.3f, rr.width, u * 3f), $"Dia {i + 1}", fs - 4, Color.white);
            int k = Profile.LoginRewards[i, 0], v = Profile.LoginRewards[i, 1];
            var ic = new Rect(rr.x + u, rr.y + u * 3.3f, rr.width - u * 2f, rr.height * 0.45f);
            if (k == 2 && chestIcons[v] != null) GUI.DrawTexture(ic, chestIcons[v], ScaleMode.ScaleToFit);
            else Ui.Disc(new Rect(ic.center.x - ic.height * 0.4f, ic.y + ic.height * 0.1f, ic.height * 0.8f, ic.height * 0.8f),
                k == 0 ? Ui.Gold : new Color(0.3f, 0.9f, 0.45f));
            Ui.Text(new Rect(rr.x, rr.yMax - u * 4.5f, rr.width, u * 4f), k == 2 ? Profile.ChestNames[v] : $"+{v}", fs - 4, Color.white);
            if (past) Ui.Text(rr, "OK", fs + 4, new Color(0.4f, 1f, 0.5f));
        }
        var b = new Rect(p.center.x - p.width * 0.25f, p.yMax - u * 9f, p.width * 0.5f, u * 7f);
        if (Ui.Button(b, Ui.Orange, "Pegar prêmio", fs + 1))
        {
            string txt = Profile.LoginRewardText(today);
            var res = Profile.ClaimLogin();
            Sfx.Play("moeda");
            if (res != null) ShowChest(res); else popup = $"Prêmio do dia {today + 1}:\n\n{txt}";
        }
    }

    // ------------------------------------------------------------ Mapa da Campanha

    void DrawCampaign(Rect col, float u, int fs)
    {
        Ui.DiamondBackground(col, col.width / 4f);
        Ui.Fill(col, new Color(0.1f, 0.05f, 0.15f, 0.55f));
        Ui.Text(new Rect(col.x, col.y + u, col.width, u * 5f), "Campanha dos Chefes", fs + 4, Ui.Gold);
        Ui.Text(new Rect(col.x, col.y + u * 5.5f, col.width, u * 3f), $"{Profile.CampaignTotalStars}/30 estrelas", fs - 3, Color.white);
        int unlocked = Profile.CampaignUnlocked;
        float top = col.y + u * 15f, bottom = col.yMax - u * 21f;   // espaço para o título e o botão Fechar
        var pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float t = i / 9f;
            pts[i] = new Vector2(col.center.x + Mathf.Sin(t * Mathf.PI * 3f) * col.width * 0.28f, Mathf.Lerp(bottom, top, t));
        }
        // caminho pontilhado
        for (int i = 0; i < 9; i++)
            for (int d = 1; d < 6; d++)
            {
                var q = Vector2.Lerp(pts[i], pts[i + 1], d / 6f);
                Ui.Disc(new Rect(q.x - u * 0.5f, q.y - u * 0.5f, u, u), i < unlocked ? Ui.Gold : new Color(1f, 1f, 1f, 0.3f));
            }
        for (int i = 0; i < 10; i++)
        {
            bool open = i <= unlocked;
            bool cur = i == unlocked;
            float s = u * (cur ? 9f + Mathf.Sin(Time.time * 4f) * 0.6f : 8f);
            var r = new Rect(pts[i].x - s / 2f, pts[i].y - s / 2f, s, s);
            Ui.Disc(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), cur ? Ui.Gold : new Color(0.1f, 0.08f, 0.15f));
            Ui.Disc(r, open ? ArenaGame.Bosses[i].tint * new Color(0.6f, 0.45f, 0.55f) : new Color(0.3f, 0.3f, 0.35f));
            var bc = Profile.Find(ArenaGame.Bosses[i].model);
            if (open && bc != null && bc.Portrait != null)
                GUI.DrawTexture(new Rect(r.x + s * 0.1f, r.y + s * 0.05f, s * 0.8f, s * 0.8f), bc.Portrait, ScaleMode.ScaleToFit);
            else Ui.Text(r, (i + 1).ToString(), fs + 2, Color.white);
            int st = Profile.CampaignStars[i];
            for (int k = 0; k < 3; k++)
                Ui.Text(new Rect(r.center.x - u * 4.5f + k * u * 3f, r.yMax - u * 1.5f, u * 3f, u * 3f), "*", fs + 2,
                    k < st ? Ui.Gold : new Color(1f, 1f, 1f, 0.25f));
            if (open && GUI.Button(r, GUIContent.none, GUIStyle.none)) { campaignPick = i; Sfx.Play("botao"); }
        }

        // Detalhes da fase escolhida
        if (campaignPick >= 0)
        {
            var info = ArenaGame.Bosses[campaignPick];
            var p = new Rect(col.x + u * 3f, col.y + col.height * 0.3f, col.width - u * 6f, col.height * 0.4f);
            Ui.Fill(col, new Color(0f, 0f, 0f, 0.5f));
            Ui.Panel(p, new Color(0.3f, 0.12f, 0.2f));
            var bc = Profile.Find(info.model);
            if (bc != null && bc.Portrait != null)
            {
                var old = GUI.color;
                GUI.color = info.tint;
                GUI.DrawTexture(new Rect(p.x + u * 2f, p.y + u * 2f, p.width * 0.32f, p.width * 0.32f), bc.Portrait, ScaleMode.ScaleToFit);
                GUI.color = old;
            }
            float tx = p.x + p.width * 0.36f;
            Ui.Text(new Rect(tx, p.y + u * 1.5f, p.xMax - tx - u, u * 3f), $"Fase {campaignPick + 1}", fs - 3, Ui.Gold, TextAnchor.MiddleLeft);
            Ui.Text(new Rect(tx, p.y + u * 4.5f, p.xMax - tx - u, u * 4f), info.name, fs + 2, Color.white, TextAnchor.MiddleLeft);
            Ui.Text(new Rect(tx, p.y + u * 9f, p.xMax - tx - u * 2f, u * 10f), info.lore, fs - 4, new Color(0.95f, 0.85f, 0.9f), TextAnchor.UpperLeft, false);
            Ui.Text(new Rect(p.x + u * 2f, p.yMax - u * 16f, p.width - u * 4f, u * 6f),
                "Estrelas: vencer = 1, 2 coroas = 2, 3 coroas = 3\n" +
                (Profile.CampaignStars[campaignPick] == 0 ? "Primeira vitória: " + Profile.CampaignFirstReward(campaignPick) : "Fase já vencida"),
                fs - 4, Color.white, TextAnchor.UpperCenter, false);
            var a = new Rect(p.x + u * 3f, p.yMax - u * 9f, p.width * 0.5f - u * 4f, u * 7f);
            var b = new Rect(p.center.x + u, a.y, a.width, a.height);
            if (Ui.Button(a, Ui.Orange, "Lutar!", fs + 2))
            {
                game.CampaignPhase = campaignPick;
                campaignOpen = false;
                game.StartMatch(MatchMode.Campanha);
            }
            if (Ui.Button(b, new Color(0.35f, 0.32f, 0.42f), "Voltar", fs)) campaignPick = -1;
            return;
        }
        if (Ui.Button(new Rect(col.center.x - col.width * 0.2f, col.yMax - u * 10f, col.width * 0.4f, u * 7f), Ui.Teal, "Fechar", fs + 1))
            campaignOpen = false;
    }

    // ------------------------------------------------------------ História de abertura (primeira vez que abre o jogo)

    int introSlide;
    float introT;
    bool introTyped;
    readonly Texture2D[] introTex = new Texture2D[5];

    static readonly string[] IntroTexts =
    {
        "Há muito tempo, o Reino das Cinco Ilhas vivia em paz, unido pela Arena de Cartas.",
        "No Campo Verde, novos guerreiros aprendem a lutar com cartas mágicas que chamam tropas e feitiços.",
        "No Vale das Pedras, golens antigos acordaram... e só os corajosos conseguem atravessá-lo.",
        "Na Fortaleza de Gelo, lobos e feiticeiras guardam segredos congelados.",
        "E no Vulcão Rugidor, dizem que vive o Rei da Tempestade. A sua jornada começa agora!"
    };

    void DrawIntro(Rect col, float u, int fs)
    {
        if (introTex[0] == null)
        {
            introTex[0] = Resources.Load<Texture2D>("Icones/emblema");
            for (int i = 0; i < 4; i++) introTex[i + 1] = Resources.Load<Texture2D>("Arenas/arena" + i);
            if (mentorTex == null) mentorTex = Resources.Load<Texture2D>("Tutorial/mentor");
        }
        if (Event.current.type == EventType.Repaint) introT += Time.deltaTime;
        GUI.enabled = true;
        GUI.color = Color.white;
        // fundo noturno com estrelas
        for (int i = 0; i < 20; i++)
        {
            float t = i / 19f;
            Ui.Fill(new Rect(col.x, col.y + col.height * t, col.width, col.height / 20f + 1f),
                Color.Lerp(new Color(0.05f, 0.07f, 0.2f), new Color(0.18f, 0.1f, 0.25f), t));
        }
        var rnd = new System.Random(7);
        for (int i = 0; i < 40; i++)
        {
            float x = col.x + (float)rnd.NextDouble() * col.width, y = col.y + (float)rnd.NextDouble() * col.height * 0.6f;
            float tw = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.time * 1.5f + i));
            Ui.Disc(new Rect(x, y, u * 0.6f, u * 0.6f), new Color(1f, 1f, 1f, 0.6f * tw));
        }
        // imagem do slide: aproxima devagar e aparece suavemente
        float fade = Mathf.Clamp01(introT / 0.6f);
        float zoom = 1f + Mathf.Min(introT, 4f) * 0.025f;
        var img = introTex[introSlide];
        float w = col.width * 0.86f * zoom, h = col.height * 0.42f * zoom;
        if (img != null)
        {
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, fade);
            GUI.BeginGroup(col);    // a imagem nunca passa das bordas da coluna
            GUI.DrawTexture(new Rect(col.width / 2f - w / 2f, col.height * 0.1f + (col.height * 0.42f - h) / 2f, w, h), img, ScaleMode.ScaleToFit);
            GUI.EndGroup();
            GUI.color = old;
        }
        // narração do Capitão Tomás
        var box = new Rect(col.x + u * 3f, col.y + col.height * 0.6f, col.width - u * 6f, u * 18f);
        Ui.Fill(new Rect(box.x - 3, box.y - 3, box.width + 6, box.height + 6), new Color(0.6f, 0.45f, 0.2f));
        Ui.Fill(box, new Color(0.97f, 0.93f, 0.82f));
        if (mentorTex != null) GUI.DrawTexture(new Rect(box.x - u * 2f, box.y - u * 9f, u * 10f, u * 11f), mentorTex, ScaleMode.ScaleToFit);
        string text = IntroTexts[introSlide];
        int shown = introTyped ? text.Length : Mathf.Min(text.Length, Mathf.FloorToInt(introT * 40f));
        Ui.Text(new Rect(box.x + u * 2f, box.y + u * 2f, box.width - u * 4f, box.height - u * 4f), text.Substring(0, shown), fs,
            new Color(0.22f, 0.12f, 0.05f), TextAnchor.MiddleCenter, true);
        for (int i = 0; i < IntroTexts.Length; i++)
            Ui.Disc(new Rect(col.center.x - u * 6f + i * u * 2.6f, box.yMax + u * 2f, u * 1.6f, u * 1.6f),
                i == introSlide ? Ui.Gold : new Color(1f, 1f, 1f, 0.3f));
        Ui.Text(new Rect(col.x, box.yMax + u * 5f, col.width, u * 3f), "toque para continuar", fs - 4,
            new Color(1f, 1f, 1f, 0.4f + 0.4f * Mathf.Sin(Time.time * 4f)), TextAnchor.MiddleCenter, false);

        var skip = new Rect(col.xMax - u * 12f, col.y + u * 2f, u * 10f, u * 4f);
        if (Ui.Button(skip, new Color(0.3f, 0.3f, 0.4f), "Pular", fs - 3)) { Profile.IntroSeen = true; Profile.Save(); return; }
        var ev = Event.current;
        if (ev.type == EventType.MouseDown && col.Contains(ev.mousePosition))
        {
            ev.Use();
            if (shown < text.Length) introTyped = true;
            else if (introSlide < IntroTexts.Length - 1) { introSlide++; introT = 0f; introTyped = false; Sfx.Play("carta", 0.5f); }
            else { Profile.IntroSeen = true; Profile.Save(); Sfx.Play("batalha", 0.6f); }
        }
    }
}
