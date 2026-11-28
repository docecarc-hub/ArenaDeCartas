using System.Collections.Generic;
using UnityEngine;

// Tela inicial: perfil, recursos, loja, cartas, batalha, missões, baús e configurações.
public partial class Lobby
{
    readonly ArenaGame game;
    readonly TrophyRoad road;
    int tab = 2;
    string popup;
    bool seasonChecked;
    ChestOpening chestAnim;
    string cardInfo;            // carta aberta no painel de detalhes
    bool settingsOpen;
    bool profileOpen, skinsOpen, tutorialAsked, campaignOpen, loginShown;
    int campaignPick = -1;
    string bookCard;           // carta aberta no Livro de cartas
    float bookT;

    // Avisos que deslizam no topo do lobby ("Baú pronto!" etc.)
    class Note { public string text; public Color color; public int tab; public float t; }
    readonly List<Note> notes = new List<Note>();
    readonly Dictionary<string, bool> noteState = new Dictionary<string, bool>();
    Texture2D mentorTex;
    Vector2 eventScroll, profileScroll;

    static readonly string[] Tabs = { "Loja", "Cartas", "Batalha", "Eventos", "Missões" };

    public Lobby(ArenaGame game)
    {
        this.game = game;
        road = new TrophyRoad(this, game.Deck);
    }

    public void ShowPopup(string text) => popup = text;

    public void ShowChest(Profile.ChestResult result) => chestAnim = new ChestOpening(result);

    bool Modal => popup != null || chestAnim != null || cardInfo != null || settingsOpen || profileOpen || skinsOpen || campaignOpen || bookCard != null
                 || (Profile.LoginAvailable && Profile.TutorialDone)
                 || (!Profile.TutorialDone && !tutorialAsked) || !Profile.IntroSeen;

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
            case 3: DrawEvents(body, u, fs); break;
            case 4: DrawMissions(body, u, fs); break;
            default: Ui.Text(body, $"{Tabs[tab]}\n\nEm breve!", fs * 2, Color.white); break;
        }

        DrawNav(col, u, fs);
        GUI.enabled = true;
        if (!Modal) { CheckNotes(); DrawNotes(col, u, fs); }
        DrawModals(col, u, fs);
    }

    void DrawModals(Rect col, float u, int fs)
    {
        if (chestAnim != null)
        {
            if (!chestAnim.Draw(col)) chestAnim = null;
        }
        else if (bookCard != null) DrawBook(col, u, fs);
        else if (cardInfo != null) DrawCardInfo(col, u, fs);
        else if (settingsOpen) DrawSettings(col, u, fs);
        else if (profileOpen) DrawProfile(col, u, fs);
        else if (skinsOpen) DrawSkins(col, u, fs);
        else if (popup != null) DrawPopup(col, u, fs);
        else if (campaignOpen) DrawCampaign(col, u, fs);
        else if (Profile.LoginAvailable && Profile.TutorialDone) DrawLogin(col, u, fs);
        else if (!Profile.IntroSeen) DrawIntro(col, u, fs);
        else if (!Profile.TutorialDone && !tutorialAsked) DrawTutorialInvite(col, u, fs);
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

        // Tocar no nível/nome abre o perfil (estatísticas e conquistas)
        var who = new Rect(col.x, col.y, col.width * 0.45f, u * 7f);
        if (Profile.AchievementsReady() > 0) Badge(new Rect(disc.xMax - u * 1.5f, disc.y - u * 0.5f, u * 2.6f, u * 2.6f), fs - 5);
        if (GUI.Button(who, GUIContent.none, GUIStyle.none)) { profileOpen = true; Sfx.Play("botao"); }

        var gear = new Rect(col.xMax - u * 12f, col.y + u * 8f, u * 11f, u * 3.4f);
        if (Ui.Button(gear, new Color(0.2f, 0.24f, 0.34f), "Ajustes", fs - 3)) settingsOpen = true;
    }

    static void DrawResource(Rect r, int value, Color icon, float u, int fs)
    {
        Ui.Panel(r, new Color(0.14f, 0.16f, 0.24f));
        Ui.Disc(new Rect(r.x - u * 1.2f, r.y, r.height, r.height), icon);
        Ui.Text(new Rect(r.x + u, r.y, r.width - u, r.height), value.ToString(), fs - 1, Color.white);
    }

    // ------------------------------------------------------------ Navegação e popup

    static void Badge(Rect r, int fs)
    {
        // pulsa para chamar atenção
        float s = 1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.time * 4f));
        var rr = new Rect(r.center.x - r.width * s / 2f, r.center.y - r.height * s / 2f, r.width * s, r.height * s);
        Ui.Disc(new Rect(rr.x - 2, rr.y - 2, rr.width + 4, rr.height + 4), new Color(0.35f, 0f, 0f));
        Ui.Disc(rr, new Color(0.95f, 0.2f, 0.2f));
        Ui.Text(rr, "!", fs + 2, Color.white);
    }

    // Selo da liga: escudo com faixa e estrelas
    public static void DrawLeagueBadge(Rect r, int lg, float u, int fs)
    {
        Color c = Profile.LeagueColors[lg];
        Ui.Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height * 0.8f + 4), new Color(0.1f, 0.08f, 0.15f));
        Ui.Fill(new Rect(r.x, r.y, r.width, r.height * 0.8f), c);
        Ui.Fill(new Rect(r.x, r.y, r.width, r.height * 0.3f), new Color(1f, 1f, 1f, 0.25f));
        var m = GUI.matrix;
        var tip = new Vector2(r.center.x, r.y + r.height * 0.8f);
        GUIUtility.RotateAroundPivot(45f, tip);
        float s = r.width * 0.7f;
        Ui.Fill(new Rect(tip.x - s / 2f, tip.y - s / 2f, s, s), c);
        GUI.matrix = m;
        for (int i = 0; i <= lg; i++)
            Ui.Text(new Rect(r.x + (i - lg / 2f) * u * 2.4f, r.y + r.height * 0.18f, r.width, u * 4f), "*", fs + 4, Color.white);
        Ui.Fill(new Rect(r.x - u, r.y + r.height * 0.5f, r.width + u * 2f, u * 3f), Ui.Shade(c, 0.6f));
        Ui.Text(new Rect(r.x - u, r.y + r.height * 0.5f, r.width + u * 2f, u * 3f), Profile.LeagueNames[lg].Replace("Liga ", ""), fs - 4, Color.white);
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
            !Profile.OfferBought(0), AnyUpgradeReady(), false,
            Profile.PassTiersReady() > 0 || Profile.EventRewardLeft() == Profile.EventRewardsPerDay, Profile.AnyMissionToClaim
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
