using System;
using System.Collections.Generic;
using UnityEngine;

// Dados do jogador salvos no PC (PlayerPrefs): nível, XP, troféus, moedas, gemas e baús.
public static class Profile
{
    public class Chest
    {
        public int type = -1;          // -1 = espaço vazio
        public bool unlocking;
        public long endTicks;

        public bool Empty => type < 0;
        public bool Ready => unlocking && DateTime.UtcNow.Ticks >= endTicks;
        public double SecondsLeft =>
            Math.Max(0, (endTicks - DateTime.UtcNow.Ticks) / (double)TimeSpan.TicksPerSecond);
    }

    // Tempos curtos para testar. Depois é só aumentar (ex.: 3h, 8h, 12h).
    public static readonly string[] ChestNames = { "Madeira", "Prata", "Ouro" };
    public static readonly int[] ChestSeconds = { 30, 120, 300 };
    public static readonly Color[] ChestColors =
    {
        new Color(0.62f, 0.42f, 0.25f), new Color(0.72f, 0.77f, 0.82f), new Color(1f, 0.80f, 0.25f)
    };
    static readonly int[] ChestCoinsMin = { 20, 60, 200 };
    static readonly int[] ChestCoinsMax = { 40, 100, 300 };
    static readonly int[] ChestGemsMax = { 0, 2, 6 };

    public static readonly int[] ArenaTrophies = { 0, 300, 600, 1000 };
    public static readonly string[] ArenaNames = { "Campo Verde", "Vale das Pedras", "Fortaleza de Gelo", "Vulcão Rugidor" };
    public static readonly Color[] ArenaGrass =
    {
        new Color(0.36f, 0.64f, 0.31f), new Color(0.52f, 0.56f, 0.40f),
        new Color(0.72f, 0.84f, 0.90f), new Color(0.42f, 0.30f, 0.26f)
    };

    public static string Name = "Kaio";
    public static int Level, Xp, Trophies, Coins, Gems;
    public static int BestTrophies;     // maior marca (libera recompensas do caminho)
    public static int HighestArena;     // maior arena alcançada (define o piso de troféus)
    public static long SeasonEndTicks;
    public const int SeasonDays = 28;
    static readonly HashSet<int> claimedRewards = new HashSet<int>();

    // Coleção: quantas cópias você tem de cada carta (0 = ainda não ganhou)
    static readonly Dictionary<string, int> cards = new Dictionary<string, int>();
    // Três decks salvos (8 chaves de carta cada); Deck é sempre o deck ativo
    static readonly List<string>[] decks = { new List<string>(), new List<string>(), new List<string>() };
    public static int ActiveDeck;
    public static List<string> Deck => decks[ActiveDeck];
    public static List<string> DeckAt(int i) => decks[i];
    public static CardData[] AllCards { get; private set; }
    public static readonly Chest[] Chests = { new Chest(), new Chest(), new Chest(), new Chest() };

    static bool loaded;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;

        Name = PlayerPrefs.GetString("nome", "Kaio");
        Level = PlayerPrefs.GetInt("nivel", 1);
        Xp = PlayerPrefs.GetInt("xp", 0);
        Trophies = PlayerPrefs.GetInt("trofeus", 0);
        Coins = PlayerPrefs.GetInt("moedas", 100);
        Gems = PlayerPrefs.GetInt("gemas", 20);
        BestTrophies = PlayerPrefs.GetInt("melhor", Trophies);
        HighestArena = PlayerPrefs.GetInt("arenaMax", 0);
        long.TryParse(PlayerPrefs.GetString("fimTemporada", "0"), out SeasonEndTicks);
        claimedRewards.Clear();
        foreach (var part in PlayerPrefs.GetString("caminho", "").Split(','))
            if (int.TryParse(part, out int t)) claimedRewards.Add(t);

        cards.Clear();
        foreach (var part in PlayerPrefs.GetString("cartas", "").Split(','))
        {
            var kv = part.Split(':');
            if (kv.Length == 2 && int.TryParse(kv[1], out int n)) cards[kv[0]] = n;
        }
        ActiveDeck = Mathf.Clamp(PlayerPrefs.GetInt("deckAtivo", 0), 0, 2);
        for (int d = 0; d < 3; d++)
        {
            decks[d].Clear();
            // compatibilidade: o deck antigo ("deck") vira o deck 1
            string saved = PlayerPrefs.GetString("deck" + d, d == 0 ? PlayerPrefs.GetString("deck", "") : "");
            foreach (var k in saved.Split(','))
                if (!string.IsNullOrEmpty(k)) decks[d].Add(k);
        }
        SeasonCrowns = PlayerPrefs.GetInt("coroasTemporada", 0);
        CrownsTotal = PlayerPrefs.GetInt("coroasTotal", 0);
        UpgradesTotal = PlayerPrefs.GetInt("evolucoes", 0);
        LoadSet("passe", passClaimed);
        LoadSet("conquistas", achClaimed);
        TowerSkin = PlayerPrefs.GetString("torreVisual", "");
        TutorialDone = PlayerPrefs.GetInt("tutorialFeito", 0) == 1;
        LoadSet("evos", evolved);
        loginIndex = PlayerPrefs.GetInt("loginIndice", 0);
        challengeWeek = PlayerPrefs.GetString("desafioSemana", "");
        ChallengeWins = PlayerPrefs.GetInt("desafioVitorias", 0);
        ChallengeLosses = PlayerPrefs.GetInt("desafioDerrotas", 0);
        challengeClaimed = PlayerPrefs.GetInt("desafioPego", 0) == 1;
        TextScale = PlayerPrefs.GetInt("textoTamanho", 1);
        ColorBlind = PlayerPrefs.GetInt("daltonico", 0) == 1;
        ReduceShake = PlayerPrefs.GetInt("semTremor", 0) == 1;
        PostFx = PlayerPrefs.GetInt("efeitosVisuais", 1) == 1;
        IntroSeen = PlayerPrefs.GetInt("introVista", 0) == 1;
        LowQuality = PlayerPrefs.GetInt("qualidadeBaixa", 0) == 1;
        ShowFps = PlayerPrefs.GetInt("mostrarFps", 0) == 1;
        cardWins.Clear();
        foreach (var part in PlayerPrefs.GetString("maestria", "").Split(','))
        {
            var kv = part.Split(':');
            if (kv.Length == 3 && int.TryParse(kv[1], out int w) && int.TryParse(kv[2], out int tier)) { cardWins[kv[0]] = w; masteryClaimed[kv[0]] = tier; }
        }
        Ui.FontScale = TextScales[Mathf.Clamp(TextScale, 0, 2)];
        loginDay = PlayerPrefs.GetString("loginDia", "");
        var cs = PlayerPrefs.GetString("campanha", "").Split(',');
        for (int i = 0; i < CampaignStars.Length; i++)
            CampaignStars[i] = i < cs.Length && int.TryParse(cs[i], out int st) ? st : 0;
        BestLeague = PlayerPrefs.GetInt("ligaMax", -1);
        LoadSet("torresCompradas", skinsOwned);
        eventDay = PlayerPrefs.GetString("eventoDia", "");
        eventWins = PlayerPrefs.GetInt("eventoVitorias", 0);

        levels.Clear();
        foreach (var part in PlayerPrefs.GetString("niveis", "").Split(','))
        {
            var kv = part.Split(':');
            if (kv.Length == 2 && int.TryParse(kv[1], out int n)) levels[kv[0]] = n;
        }
        long.TryParse(PlayerPrefs.GetString("bauGratis", "0"), out FreeChestTicks);
        crownDay = PlayerPrefs.GetString("coroasDia", "");
        CrownsToday = PlayerPrefs.GetInt("coroasHoje", 0);
        crownChestClaimed = PlayerPrefs.GetInt("bauCoroasPego", 0) == 1;
        missionDay = PlayerPrefs.GetString("missoesDia", "");
        var mids = PlayerPrefs.GetString("missoes", "").Split(',');
        for (int i = 0; i < 3; i++)
        {
            var mp = i < mids.Length ? mids[i].Split(':') : new string[0];
            missionIds[i] = mp.Length == 3 ? mp[0] : null;
            missionProg[i] = mp.Length == 3 && int.TryParse(mp[1], out int pr) ? pr : 0;
            missionClaimed[i] = mp.Length == 3 && mp[2] == "1";
        }
        shopDay = PlayerPrefs.GetString("lojaDia", "");
        shopBought.Clear();
        foreach (var part in PlayerPrefs.GetString("lojaComprado", "").Split(','))
            if (int.TryParse(part, out int b)) shopBought.Add(b);
        WinsTotal = PlayerPrefs.GetInt("vitorias", 0);
        LossesTotal = PlayerPrefs.GetInt("derrotas", 0);

        for (int i = 0; i < Chests.Length; i++)
        {
            var parts = PlayerPrefs.GetString("bau" + i, "").Split('|');
            if (parts.Length == 3 && int.TryParse(parts[0], out int t) && long.TryParse(parts[2], out long end))
            {
                Chests[i].type = t;
                Chests[i].unlocking = parts[1] == "1";
                Chests[i].endTicks = end;
            }
        }
    }

    public static void Save()
    {
        PlayerPrefs.SetString("nome", Name);
        PlayerPrefs.SetInt("nivel", Level);
        PlayerPrefs.SetInt("xp", Xp);
        PlayerPrefs.SetInt("trofeus", Trophies);
        PlayerPrefs.SetInt("moedas", Coins);
        PlayerPrefs.SetInt("gemas", Gems);
        PlayerPrefs.SetInt("melhor", BestTrophies);
        PlayerPrefs.SetInt("arenaMax", HighestArena);
        PlayerPrefs.SetString("fimTemporada", SeasonEndTicks.ToString());
        PlayerPrefs.SetString("caminho", string.Join(",", claimedRewards));
        var parts = new List<string>();
        foreach (var kv in cards) parts.Add(kv.Key + ":" + kv.Value);
        PlayerPrefs.SetString("cartas", string.Join(",", parts));
        PlayerPrefs.SetInt("deckAtivo", ActiveDeck);
        for (int d = 0; d < 3; d++) PlayerPrefs.SetString("deck" + d, string.Join(",", decks[d]));
        PlayerPrefs.SetInt("coroasTemporada", SeasonCrowns);
        PlayerPrefs.SetInt("coroasTotal", CrownsTotal);
        PlayerPrefs.SetInt("evolucoes", UpgradesTotal);
        PlayerPrefs.SetString("passe", string.Join(",", passClaimed));
        PlayerPrefs.SetString("conquistas", string.Join(",", achClaimed));
        PlayerPrefs.SetString("torreVisual", TowerSkin ?? "");
        PlayerPrefs.SetInt("tutorialFeito", TutorialDone ? 1 : 0);
        PlayerPrefs.SetString("evos", string.Join(",", evolved));
        PlayerPrefs.SetInt("loginIndice", loginIndex);
        PlayerPrefs.SetString("desafioSemana", challengeWeek ?? "");
        PlayerPrefs.SetInt("desafioVitorias", ChallengeWins);
        PlayerPrefs.SetInt("desafioDerrotas", ChallengeLosses);
        PlayerPrefs.SetInt("desafioPego", challengeClaimed ? 1 : 0);
        PlayerPrefs.SetInt("textoTamanho", TextScale);
        PlayerPrefs.SetInt("daltonico", ColorBlind ? 1 : 0);
        PlayerPrefs.SetInt("semTremor", ReduceShake ? 1 : 0);
        PlayerPrefs.SetInt("efeitosVisuais", PostFx ? 1 : 0);
        PlayerPrefs.SetInt("introVista", IntroSeen ? 1 : 0);
        PlayerPrefs.SetInt("qualidadeBaixa", LowQuality ? 1 : 0);
        PlayerPrefs.SetInt("mostrarFps", ShowFps ? 1 : 0);
        var mm = new List<string>();
        foreach (var kv in cardWins) mm.Add($"{kv.Key}:{kv.Value}:{(masteryClaimed.TryGetValue(kv.Key, out int t) ? t : 0)}");
        PlayerPrefs.SetString("maestria", string.Join(",", mm));
        PlayerPrefs.SetString("loginDia", loginDay ?? "");
        PlayerPrefs.SetString("campanha", string.Join(",", CampaignStars));
        PlayerPrefs.SetInt("ligaMax", BestLeague);
        PlayerPrefs.SetString("torresCompradas", string.Join(",", skinsOwned));
        PlayerPrefs.SetString("eventoDia", eventDay ?? "");
        PlayerPrefs.SetInt("eventoVitorias", eventWins);
        var lv = new List<string>();
        foreach (var kv in levels) lv.Add(kv.Key + ":" + kv.Value);
        PlayerPrefs.SetString("niveis", string.Join(",", lv));
        PlayerPrefs.SetString("bauGratis", FreeChestTicks.ToString());
        PlayerPrefs.SetString("coroasDia", crownDay ?? "");
        PlayerPrefs.SetInt("coroasHoje", CrownsToday);
        PlayerPrefs.SetInt("bauCoroasPego", crownChestClaimed ? 1 : 0);
        PlayerPrefs.SetString("missoesDia", missionDay ?? "");
        var ms = new List<string>();
        for (int i = 0; i < 3; i++) ms.Add($"{missionIds[i]}:{missionProg[i]}:{(missionClaimed[i] ? 1 : 0)}");
        PlayerPrefs.SetString("missoes", string.Join(",", ms));
        PlayerPrefs.SetString("lojaDia", shopDay ?? "");
        PlayerPrefs.SetString("lojaComprado", string.Join(",", shopBought));
        PlayerPrefs.SetInt("vitorias", WinsTotal);
        PlayerPrefs.SetInt("derrotas", LossesTotal);
        for (int i = 0; i < Chests.Length; i++)
        {
            var c = Chests[i];
            PlayerPrefs.SetString("bau" + i, $"{c.type}|{(c.unlocking ? 1 : 0)}|{c.endTicks}");
        }
        PlayerPrefs.Save();
    }

    // Garante coleção inicial (8 cartas iniciais) e um deck válido de 8 cartas que você possui.
    public static void EnsureCollection(CardData[] all)
    {
        AllCards = all;
        foreach (var c in all)
            if (c.starter && !Owns(c.key)) cards[c.key] = 0;

        foreach (var deck in decks)
        {
            deck.RemoveAll(k => !Owns(k) || Find(k) == null);
            for (int i = deck.Count - 1; i >= 0; i--)
                if (deck.IndexOf(deck[i]) != i) deck.RemoveAt(i);
            foreach (var c in all)
            {
                if (deck.Count >= 8) break;
                if (Owns(c.key) && !deck.Contains(c.key)) deck.Add(c.key);
            }
        }
        Save();
    }

    public static CardData Find(string key)
    {
        if (AllCards == null) return null;
        foreach (var c in AllCards) if (c.key == key) return c;
        return null;
    }

    // cards[key] existe = você tem a carta; o valor são as cópias extras (usadas para evoluir)
    public static int CardCount(string key) => cards.TryGetValue(key, out int n) ? n : 0;
    public static bool Owns(string key) => cards.ContainsKey(key);

    public static bool CanDrop(CardData c) => c.unlockArena <= HighestArena;

    public static int XpNeeded => 50 + Level * 50;

    // Retorna true se subiu de nível.
    public static bool AddXp(int amount)
    {
        bool up = false;
        Xp += amount;
        while (Xp >= XpNeeded)
        {
            Xp -= XpNeeded;
            Level++;
            Gems += 5;
            up = true;
        }
        return up;
    }

    public static int ArenaIndex
    {
        get
        {
            int a = 0;
            for (int i = 0; i < ArenaTrophies.Length; i++)
                if (Trophies >= ArenaTrophies[i]) a = i;
            return a;
        }
    }

    // Troféus mínimos: depois de chegar numa arena, você não cai abaixo do início dela.
    public static int ArenaFloor => ArenaTrophies[HighestArena];

    // Chamar depois de mudar os troféus. Retorna mensagem se entrou numa arena nova.
    public static string UpdateProgress(CardData[] allCards)
    {
        if (Trophies < ArenaFloor) Trophies = ArenaFloor;
        if (Trophies > BestTrophies) BestTrophies = Trophies;

        string msg = null;
        int arena = ArenaIndex;
        if (arena > HighestArena)
        {
            HighestArena = arena;
            msg = $"NOVA ARENA: {ArenaNames[arena]}!\nAgora podem cair dos baús:";
            foreach (var c in allCards)
                if (c.unlockArena == arena && !c.starter) msg += $"\n- {c.name}";
        }
        int league = LeagueIndex;
        if (league > BestLeague)
        {
            BestLeague = league;
            int prize = 20 * (league + 1);
            Gems += prize;
            msg = (msg != null ? msg + "\n\n" : "") + $"NOVA LIGA: {LeagueNames[league]}! +{prize} gemas";
        }
        return msg;
    }

    // ------------------------------------------------------------ Ligas (depois da última arena)

    public static readonly int[] LeagueTrophies = { 1300, 1700, 2200, 3000 };
    public static readonly string[] LeagueNames = { "Liga Bronze", "Liga Prata", "Liga Ouro", "Liga Lenda" };
    public static readonly Color[] LeagueColors =
    {
        new Color(0.80f, 0.50f, 0.25f), new Color(0.75f, 0.80f, 0.88f), new Color(1.00f, 0.80f, 0.25f), new Color(0.70f, 0.35f, 1.00f)
    };
    public static int BestLeague = -1;
    public static int LeagueIndex
    {
        get
        {
            int l = -1;
            for (int i = 0; i < LeagueTrophies.Length; i++) if (Trophies >= LeagueTrophies[i]) l = i;
            return l;
        }
    }
    // Próxima meta: próxima arena ou próxima liga
    public static int NextGoal
    {
        get
        {
            foreach (var t in ArenaTrophies) if (t > Trophies) return t;
            foreach (var t in LeagueTrophies) if (t > Trophies) return t;
            return -1;
        }
    }

    public static bool IsClaimed(int trophies) => claimedRewards.Contains(trophies);

    public static void MarkClaimed(int trophies)
    {
        claimedRewards.Add(trophies);
        Save();
    }

    // Temporada: ao terminar, você mantém a arena, mas perde metade do progresso dentro dela.
    public static string CheckSeason()
    {
        long now = DateTime.UtcNow.Ticks;
        if (SeasonEndTicks == 0)
        {
            SeasonEndTicks = DateTime.UtcNow.AddDays(SeasonDays).Ticks;
            Save();
            return null;
        }
        if (now < SeasonEndTicks) return null;

        int before = Trophies;
        Trophies = ArenaFloor + (Trophies - ArenaFloor) / 2;
        int prize = 10 + HighestArena * 10 + (LeagueIndex + 1) * 25;
        Gems += prize;
        SeasonCrowns = 0;
        passClaimed.Clear();
        SeasonEndTicks = DateTime.UtcNow.AddDays(SeasonDays).Ticks;
        Save();
        return $"Nova temporada!\n\nTroféus: {before} -> {Trophies}\nVocê continua na {ArenaNames[HighestArena]}\n\nPrêmio da temporada: +{prize} gemas";
    }

    public static TimeSpan SeasonTimeLeft =>
        TimeSpan.FromTicks(Math.Max(0, SeasonEndTicks - DateTime.UtcNow.Ticks));

    public static int RandomChestType()
    {
        float r = UnityEngine.Random.value;
        return r < 0.6f ? 0 : r < 0.9f ? 1 : 2;
    }

    public static bool TryAddChest(int type)
    {
        foreach (var c in Chests)
        {
            if (!c.Empty) continue;
            c.type = type;
            c.unlocking = false;
            c.endTicks = 0;
            return true;
        }
        return false;
    }

    public static bool AnyUnlocking
    {
        get
        {
            foreach (var c in Chests)
                if (!c.Empty && c.unlocking && !c.Ready) return true;
            return false;
        }
    }

    public static void StartUnlock(Chest c)
    {
        c.unlocking = true;
        c.endTicks = DateTime.UtcNow.AddSeconds(ChestSeconds[c.type]).Ticks;
        Save();
    }

    public static ChestResult Open(Chest c)
    {
        int t = c.type;
        c.type = -1;
        c.unlocking = false;
        c.endTicks = 0;
        return RollChest(t);
    }

    static readonly int[] ChestCardDrops = { 2, 4, 7 };

    public struct CardDrop
    {
        public string key;
        public int count;
        public bool isNew;
    }

    public class ChestResult
    {
        public int type, coins, gems;
        public readonly List<CardDrop> cards = new List<CardDrop>();
    }

    // Sorteia e entrega o conteúdo de um baú (moedas, gemas e cartas desbloqueadas pela sua arena)
    public static ChestResult RollChest(int t)
    {
        var res = new ChestResult { type = t };
        res.coins = UnityEngine.Random.Range(ChestCoinsMin[t], ChestCoinsMax[t] + 1);
        res.gems = UnityEngine.Random.Range(0, ChestGemsMax[t] + 1);
        Coins += res.coins;
        Gems += res.gems;

        if (AllCards != null)
        {
            var pool = new List<CardData>();
            var missing = new List<CardData>();
            foreach (var c in AllCards)
            {
                if (!CanDrop(c)) continue;
                pool.Add(c);
                if (!Owns(c.key)) missing.Add(c);
            }
            var got = new Dictionary<string, int>();
            var fresh = new HashSet<string>();
            for (int i = 0; i < ChestCardDrops[t] && pool.Count > 0; i++)
            {
                // boa chance de vir uma carta que você ainda não tem
                CardData pick = missing.Count > 0 && UnityEngine.Random.value < 0.35f
                    ? PickByRarity(missing)
                    : PickByRarity(pool);
                int n = Mathf.Max(1, UnityEngine.Random.Range(1, 3 + t * 2) / RarityCopyDiv[pick.rarity]);
                if (!Owns(pick.key))
                {
                    fresh.Add(pick.key);
                    cards[pick.key] = n - 1;      // a primeira cópia é a própria carta
                }
                else cards[pick.key] = CardCount(pick.key) + n;
                got[pick.key] = (got.TryGetValue(pick.key, out int g) ? g : 0) + n;
                missing.Remove(pick);
            }
            foreach (var kv in got)
                res.cards.Add(new CardDrop { key = kv.Key, count = kv.Value, isNew = fresh.Contains(kv.Key) });
            // cartas novas aparecem por último (suspense!)
            res.cards.Sort((a, b) => a.isNew.CompareTo(b.isNew));
        }
        Save();
        return res;
    }

    // Chance de cada raridade cair (Comum, Rara, Épica, Lendária) e divisor de cópias
    public static readonly int[] RarityWeight = { 60, 25, 12, 3, 2 };
    public static readonly int[] RarityCopyDiv = { 1, 2, 4, 10, 20 };

    public static CardData PickByRarity(List<CardData> list)
    {
        int total = 0;
        foreach (var c in list) total += RarityWeight[c.rarity];
        int r = UnityEngine.Random.Range(0, total);
        foreach (var c in list)
        {
            r -= RarityWeight[c.rarity];
            if (r < 0) return c;
        }
        return list[list.Count - 1];
    }

    public static string Summary(ChestResult r)
    {
        string text = $"Baú de {ChestNames[r.type]}\n\n+{r.coins} moedas" + (r.gems > 0 ? $"   +{r.gems} gemas" : "");
        foreach (var d in r.cards)
            text += d.isNew ? $"\nNOVA CARTA: {Find(d.key).name}!" : $"\n{Find(d.key).name} x{d.count}";
        return text;
    }

    public static string GiveChestContents(int t) => Summary(RollChest(t));

    // ------------------------------------------------------------ Níveis das cartas

    static readonly Dictionary<string, int> levels = new Dictionary<string, int>();
    public const int MaxCardLevel = 8;
    static readonly int[] UpgradeCopies = { 2, 4, 10, 20, 50, 100, 200 };
    static readonly int[] UpgradeCoins = { 20, 50, 150, 400, 1000, 2000, 4000 };
    static readonly int[] UpgradeXp = { 4, 5, 6, 10, 25, 50, 100 };

    public static int CardLevel(string key) => levels.TryGetValue(key, out int l) ? l : 1;
    public static int CopiesNeeded(string key) =>
        CardLevel(key) >= MaxCardLevel ? 0 : Math.Max(1, UpgradeCopies[CardLevel(key) - 1] / RarityCopyDiv[RarityOf(key)]);
    static int RarityOf(string key) { var c = Find(key); return c != null ? c.rarity : 0; }
    public static int CoinsNeeded(string key) => CardLevel(key) >= MaxCardLevel ? 0 : UpgradeCoins[CardLevel(key) - 1];
    public static bool HasCopiesToUpgrade(string key) =>
        Owns(key) && CardLevel(key) < MaxCardLevel && CardCount(key) >= CopiesNeeded(key);
    public static bool CanUpgrade(string key) => HasCopiesToUpgrade(key) && Coins >= CoinsNeeded(key);

    // +10% de vida e dano por nível
    public static float LevelMult(float level) => 1f + 0.1f * (level - 1f);

    public static bool Upgrade(string key)
    {
        if (!CanUpgrade(key)) return false;
        int lvl = CardLevel(key);
        cards[key] = CardCount(key) - CopiesNeeded(key);
        UpgradesTotal++;
        Coins -= UpgradeCoins[lvl - 1];
        levels[key] = lvl + 1;
        AddXp(UpgradeXp[lvl - 1]);
        Save();
        return true;
    }

    public static float DeckAverageLevel()
    {
        if (Deck.Count == 0) return 1f;
        float sum = 0f;
        foreach (var k in Deck) sum += CardLevel(k);
        return sum / Deck.Count;
    }

    // Torres ficam mais fortes com o nível de rei (+8% por nível)
    public static float TowerMult(int level) => 1f + 0.08f * (level - 1);

    // ------------------------------------------------------------ Baú grátis (4 horas) e baú de coroas (10 por dia)

    public static long FreeChestTicks;
    public const int FreeChestHours = 4;
    public static bool FreeChestReady => DateTime.UtcNow.Ticks >= FreeChestTicks;
    public static TimeSpan FreeChestLeft => TimeSpan.FromTicks(Math.Max(0, FreeChestTicks - DateTime.UtcNow.Ticks));

    public static ChestResult ClaimFreeChest()
    {
        FreeChestTicks = DateTime.UtcNow.AddHours(FreeChestHours).Ticks;
        return RollChest(UnityEngine.Random.value < 0.2f ? 1 : 0);
    }

    static string crownDay;
    static bool crownChestClaimed;
    public static int CrownsToday { get; private set; }
    public const int CrownChestGoal = 10;

    public static void AddCrowns(int n)
    {
        RollDay();
        CrownsToday = Mathf.Min(CrownChestGoal, CrownsToday + n);
        SeasonCrowns += n;
        CrownsTotal += n;
    }

    public static bool CrownChestReady { get { RollDay(); return CrownsToday >= CrownChestGoal && !crownChestClaimed; } }
    public static bool CrownChestClaimed { get { RollDay(); return crownChestClaimed; } }

    public static ChestResult ClaimCrownChest()
    {
        crownChestClaimed = true;
        return RollChest(2);
    }

    // ------------------------------------------------------------ Missões diárias

    public class MissionDef
    {
        public string id, text;
        public int target, coins, gems;
    }

    public static readonly MissionDef[] MissionPool =
    {
        new MissionDef { id = "vencer", text = "Vença 2 batalhas", target = 2, coins = 60 },
        new MissionDef { id = "coroas", text = "Ganhe 4 coroas", target = 4, gems = 5 },
        new MissionDef { id = "cartas", text = "Jogue 20 cartas", target = 20, coins = 40 },
        new MissionDef { id = "feitico", text = "Use 5 feitiços", target = 5, coins = 40 },
        new MissionDef { id = "torres", text = "Derrube 3 torres", target = 3, coins = 60, gems = 2 },
        new MissionDef { id = "dano", text = "Cause 5000 de dano em torres", target = 5000, coins = 50 },
        new MissionDef { id = "batalhas", text = "Jogue 3 batalhas", target = 3, coins = 30 },
        new MissionDef { id = "evoluir", text = "Evolua 1 carta", target = 1, gems = 3 },
    };

    static string missionDay;
    static readonly string[] missionIds = new string[3];
    static readonly int[] missionProg = new int[3];
    static readonly bool[] missionClaimed = new bool[3];

    static string Today => DateTime.Now.ToString("yyyyMMdd");

    // Na virada do dia: novas missões e zera as coroas do dia
    static void RollDay()
    {
        string today = Today;
        if (crownDay != today)
        {
            crownDay = today;
            CrownsToday = 0;
            crownChestClaimed = false;
        }
        if (missionDay == today) return;
        missionDay = today;
        var rnd = new System.Random(today.GetHashCode());
        var idx = new List<int>();
        for (int i = 0; i < MissionPool.Length; i++) idx.Add(i);
        for (int i = 0; i < 3; i++)
        {
            int j = rnd.Next(idx.Count);
            missionIds[i] = MissionPool[idx[j]].id;
            idx.RemoveAt(j);
            missionProg[i] = 0;
            missionClaimed[i] = false;
        }
        Save();
    }

    static MissionDef FindMission(string id)
    {
        foreach (var m in MissionPool) if (m.id == id) return m;
        return MissionPool[0];
    }

    public static MissionDef Mission(int i) { RollDay(); return FindMission(missionIds[i]); }
    public static int MissionProgress(int i) { RollDay(); return missionProg[i]; }
    public static bool MissionClaimed(int i) { RollDay(); return missionClaimed[i]; }
    public static bool MissionDone(int i) => MissionProgress(i) >= Mission(i).target;

    public static bool AnyMissionToClaim
    {
        get
        {
            for (int i = 0; i < 3; i++) if (MissionDone(i) && !MissionClaimed(i)) return true;
            return false;
        }
    }

    // Avança missões que contam este tipo de ação
    public static void Track(string id, int amount = 1)
    {
        RollDay();
        for (int i = 0; i < 3; i++)
            if (missionIds[i] == id && !missionClaimed[i])
                missionProg[i] = Mathf.Min(FindMission(id).target, missionProg[i] + amount);
    }

    public static string ClaimMission(int i)
    {
        if (!MissionDone(i) || MissionClaimed(i)) return null;
        var m = Mission(i);
        missionClaimed[i] = true;
        Coins += m.coins;
        Gems += m.gems;
        Save();
        return $"Missão concluída!\n{m.text}\n\n" + (m.coins > 0 ? $"+{m.coins} moedas  " : "") + (m.gems > 0 ? $"+{m.gems} gemas" : "");
    }

    // ------------------------------------------------------------ Loja (ofertas mudam todo dia)

    public class Offer
    {
        public string key;      // null = presente grátis
        public int copies, price, giftCoins;
    }

    static string shopDay;
    static readonly HashSet<int> shopBought = new HashSet<int>();
    public static readonly int[] ChestGemPrice = { 0, 30, 80 };

    public static List<Offer> TodayOffers()
    {
        if (shopDay != Today)
        {
            shopDay = Today;
            shopBought.Clear();
        }
        var list = new List<Offer> { new Offer { key = null, giftCoins = 30 } };
        if (AllCards == null) return list;
        var pool = new List<CardData>();
        foreach (var c in AllCards) if (CanDrop(c) && Owns(c.key)) pool.Add(c);
        var rnd = new System.Random((Today + "loja").GetHashCode());
        for (int i = 0; i < 5 && pool.Count > 0; i++)
        {
            var c = pool[rnd.Next(pool.Count)];
            pool.Remove(c);
            int copies = new[] { 2, 3, 5, 8 }[rnd.Next(4)];
            list.Add(new Offer { key = c.key, copies = copies, price = copies * (c.cost >= 5 ? 18 : 10) });
        }
        return list;
    }

    public static bool OfferBought(int i) => shopDay == Today && shopBought.Contains(i);

    public static string BuyOffer(int i)
    {
        var offers = TodayOffers();
        if (i >= offers.Count || OfferBought(i)) return null;
        var o = offers[i];
        if (o.key == null)
        {
            Coins += o.giftCoins;
            shopBought.Add(i);
            Save();
            return $"Presente diário!\n\n+{o.giftCoins} moedas";
        }
        if (Coins < o.price) return "";
        Coins -= o.price;
        cards[o.key] = CardCount(o.key) + o.copies;
        shopBought.Add(i);
        Save();
        return $"Você comprou\n{Find(o.key).name} x{o.copies}";
    }

    // ------------------------------------------------------------ Estatísticas

    public static int WinsTotal, LossesTotal;

    // ------------------------------------------------------------ Passe da temporada (grátis, por coroas)

    public static int SeasonCrowns, CrownsTotal, UpgradesTotal;
    static readonly HashSet<int> passClaimed = new HashSet<int>();
    public const int PassTiers = 10, CrownsPerTier = 6;
    // tipo: 0 moedas, 1 baú (valor = tipo do baú), 2 gemas
    public static readonly int[,] PassRewards =
    {
        { 0, 50 }, { 1, 0 }, { 2, 10 }, { 0, 100 }, { 1, 1 },
        { 2, 20 }, { 0, 200 }, { 1, 1 }, { 2, 40 }, { 1, 2 }
    };
    public static bool PassUnlocked(int tier) => SeasonCrowns >= (tier + 1) * CrownsPerTier;
    public static bool PassClaimed(int tier) => passClaimed.Contains(tier);
    public static int PassTiersReady()
    {
        int n = 0;
        for (int i = 0; i < PassTiers; i++) if (PassUnlocked(i) && !PassClaimed(i)) n++;
        return n;
    }
    public static string PassRewardText(int tier)
    {
        int kind = PassRewards[tier, 0], v = PassRewards[tier, 1];
        return kind == 0 ? $"{v} moedas" : kind == 2 ? $"{v} gemas" : $"Baú de {ChestNames[v]}";
    }
    // Retorna o baú sorteado (ou null se o prêmio foi moedas/gemas)
    public static ChestResult ClaimPass(int tier)
    {
        if (!PassUnlocked(tier) || PassClaimed(tier)) return null;
        passClaimed.Add(tier);
        int kind = PassRewards[tier, 0], v = PassRewards[tier, 1];
        ChestResult res = null;
        if (kind == 0) Coins += v;
        else if (kind == 2) Gems += v;
        else res = RollChest(v);
        Save();
        return res;
    }

    // ------------------------------------------------------------ Conquistas (gemas)

    public class Achievement
    {
        public string id, name, desc;
        public int goal, gems;
        public Func<int> progress;
    }

    public static readonly Achievement[] Achievements =
    {
        new Achievement { id = "v10", name = "Primeiros Passos", desc = "Vença 10 batalhas", goal = 10, gems = 10, progress = () => WinsTotal },
        new Achievement { id = "v50", name = "Veterano", desc = "Vença 50 batalhas", goal = 50, gems = 30, progress = () => WinsTotal },
        new Achievement { id = "c30", name = "Caçador de Coroas", desc = "Ganhe 30 coroas", goal = 30, gems = 15, progress = () => CrownsTotal },
        new Achievement { id = "t300", name = "Subindo!", desc = "Chegue a 300 troféus", goal = 300, gems = 20, progress = () => BestTrophies },
        new Achievement { id = "t1000", name = "Lenda do Vulcão", desc = "Chegue a 1000 troféus", goal = 1000, gems = 50, progress = () => BestTrophies },
        new Achievement { id = "col12", name = "Colecionador", desc = "Tenha 12 cartas diferentes", goal = 12, gems = 15, progress = OwnedCount },
        new Achievement { id = "evo10", name = "Treinador", desc = "Evolua cartas 10 vezes", goal = 10, gems = 20, progress = () => UpgradesTotal },
        new Achievement { id = "rei5", name = "Rei em Ascensão", desc = "Chegue ao nível 5", goal = 5, gems = 25, progress = () => Level },
    };
    static readonly HashSet<string> achClaimed = new HashSet<string>();

    public static int OwnedCount()
    {
        int n = 0;
        foreach (var kv in cards) n++;
        return n;
    }
    public static bool AchievementDone(Achievement a) => a.progress() >= a.goal;
    public static bool AchievementClaimed(Achievement a) => achClaimed.Contains(a.id);
    public static int AchievementsReady()
    {
        int n = 0;
        foreach (var a in Achievements) if (AchievementDone(a) && !AchievementClaimed(a)) n++;
        return n;
    }
    public static void CheckAchievements() { }   // progresso é calculado na hora; mantido para chamadas futuras
    public static bool ClaimAchievement(Achievement a)
    {
        if (!AchievementDone(a) || AchievementClaimed(a)) return false;
        achClaimed.Add(a.id);
        Gems += a.gems;
        Save();
        return true;
    }

    // ------------------------------------------------------------ Eventos (3 prêmios por dia)

    static string eventDay;
    static int eventWins;
    public const int EventRewardsPerDay = 3;
    static void RollEventDay()
    {
        string today = DateTime.UtcNow.ToString("yyyyMMdd");
        if (eventDay != today) { eventDay = today; eventWins = 0; }
    }
    public static int EventRewardLeft() { RollEventDay(); return Math.Max(0, EventRewardsPerDay - eventWins); }
    public static void EventWin() { RollEventDay(); eventWins++; }

    // Evento do dia: gira entre os três modos especiais
    public static MatchMode TodayEvent()
    {
        int d = (int)(DateTime.UtcNow.Date - new DateTime(2026, 1, 1)).TotalDays;
        MatchMode[] list = { MatchMode.ElixirTriplo, MatchMode.DeckAleatorio, MatchMode.SoDeLonge };
        return list[((d % 3) + 3) % 3];
    }

    static void LoadSet(string key, HashSet<int> set)
    {
        set.Clear();
        foreach (var p in PlayerPrefs.GetString(key, "").Split(','))
            if (int.TryParse(p, out int v)) set.Add(v);
    }
    static void LoadSet(string key, HashSet<string> set)
    {
        set.Clear();
        foreach (var p in PlayerPrefs.GetString(key, "").Split(','))
            if (!string.IsNullOrEmpty(p)) set.Add(p);
    }

    // ------------------------------------------------------------ Visual das torres (cosmético)

    public static readonly string[] SkinIds = { "", "gelo", "vulcao", "ouro" };
    public static readonly string[] SkinNames = { "Pedra", "Gelo", "Vulcão", "Ouro" };
    public static readonly int[] SkinPrices = { 0, 50, 80, 150 };
    public static string TowerSkin = "";
    public static bool TutorialDone;
    static readonly HashSet<string> skinsOwned = new HashSet<string>();
    public static bool OwnsSkin(string id) => string.IsNullOrEmpty(id) || skinsOwned.Contains(id);
    public static bool BuySkin(int i)
    {
        if (OwnsSkin(SkinIds[i]) || Gems < SkinPrices[i]) return false;
        Gems -= SkinPrices[i];
        skinsOwned.Add(SkinIds[i]);
        TowerSkin = SkinIds[i];
        Save();
        return true;
    }

    // ------------------------------------------------------------ Despertar EVO (evolução especial)

    public static readonly string[] EvoCards = { "gue", "arq", "esc", "bom" };
    public static readonly Dictionary<string, string> EvoBonus = new Dictionary<string, string>
    {
        { "gue", "+30% de vida e +15% de dano" },
        { "arq", "atiram 65% mais rápido" },
        { "esc", "+50% de vida" },
        { "bom", "explosão 40% maior e +10% de dano" },
    };
    public const int EvoMinLevel = 3, EvoCoins = 300, EvoGems = 20;
    static readonly HashSet<string> evolved = new HashSet<string>();
    public static bool HasEvo(string key) => Array.IndexOf(EvoCards, key) >= 0;
    public static bool Evolved(string key) => evolved.Contains(key);
    public static bool CanAwaken(string key) =>
        HasEvo(key) && Owns(key) && !Evolved(key) && CardLevel(key) >= EvoMinLevel && Coins >= EvoCoins && Gems >= EvoGems;
    public static bool Awaken(string key)
    {
        if (!CanAwaken(key)) return false;
        Coins -= EvoCoins;
        Gems -= EvoGems;
        evolved.Add(key);
        Save();
        return true;
    }
    public static bool ChampionInDeck(List<string> deck, int ignoreIndex = -1)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            if (i == ignoreIndex) continue;
            var c = Find(deck[i]);
            if (c != null && c.champion) return true;
        }
        return false;
    }

    // ------------------------------------------------------------ Prêmio de login diário (7 dias)

    static int loginIndex;
    static string loginDay;
    // tipo: 0 moedas, 1 gemas, 2 baú (valor = tipo do baú)
    public static readonly int[,] LoginRewards = { { 0, 50 }, { 1, 5 }, { 2, 0 }, { 0, 100 }, { 1, 10 }, { 2, 1 }, { 2, 2 } };
    public static int LoginIndex => loginIndex % 7;
    public static bool LoginAvailable => loginDay != DateTime.Now.ToString("yyyyMMdd");
    public static string LoginRewardText(int i)
    {
        int k = LoginRewards[i, 0], v = LoginRewards[i, 1];
        return k == 0 ? $"{v} moedas" : k == 1 ? $"{v} gemas" : $"Baú de {ChestNames[v]}";
    }
    public static ChestResult ClaimLogin()
    {
        if (!LoginAvailable) return null;
        int i = LoginIndex, k = LoginRewards[i, 0], v = LoginRewards[i, 1];
        loginDay = DateTime.Now.ToString("yyyyMMdd");
        loginIndex++;
        ChestResult res = null;
        if (k == 0) Coins += v; else if (k == 1) Gems += v; else res = RollChest(v);
        Save();
        return res;
    }

    // ------------------------------------------------------------ Campanha (10 fases com chefes)

    public static readonly int[] CampaignStars = new int[10];
    public static int CampaignUnlocked
    {
        get
        {
            int i = 0;
            while (i < CampaignStars.Length && CampaignStars[i] > 0) i++;
            return Mathf.Min(i, CampaignStars.Length - 1);
        }
    }
    public static int CampaignTotalStars { get { int s = 0; foreach (var x in CampaignStars) s += x; return s; } }
    // Prêmio da primeira vitória em cada fase
    public static string CampaignFirstReward(int phase) =>
        phase % 3 == 2 ? "Baú de Ouro" : phase % 2 == 0 ? $"{80 + phase * 20} moedas" : $"{10 + phase * 2} gemas";
    public static ChestResult GiveCampaignReward(int phase)
    {
        if (phase % 3 == 2) return RollChest(2);
        if (phase % 2 == 0) Coins += 80 + phase * 20; else Gems += 10 + phase * 2;
        Save();
        return null;
    }

    // ------------------------------------------------------------ Desafio semanal (até 12 vitórias, 3 derrotas eliminam)

    static string challengeWeek;
    static bool challengeClaimed;
    public static int ChallengeWins, ChallengeLosses;
    public const int ChallengeMaxWins = 12, ChallengeMaxLosses = 3;
    static string WeekId()
    {
        var d = DateTime.Now.Date;
        int monday = ((int)d.DayOfWeek + 6) % 7;
        return d.AddDays(-monday).ToString("yyyyMMdd");
    }
    static void RollWeek()
    {
        string w = WeekId();
        if (challengeWeek == w) return;
        challengeWeek = w;
        ChallengeWins = ChallengeLosses = 0;
        challengeClaimed = false;
    }
    public static bool ChallengeOver { get { RollWeek(); return ChallengeWins >= ChallengeMaxWins || ChallengeLosses >= ChallengeMaxLosses; } }
    public static bool ChallengeClaimed { get { RollWeek(); return challengeClaimed; } }
    public static int ChallengeWinsNow { get { RollWeek(); return ChallengeWins; } }
    public static int ChallengeLossesNow { get { RollWeek(); return ChallengeLosses; } }
    public static void ChallengeResult(bool win)
    {
        RollWeek();
        if (ChallengeOver) return;
        if (win) ChallengeWins++; else ChallengeLosses++;
        Save();
    }
    // Prêmio cresce a cada vitória
    public static string ChallengeRewardText(int wins) =>
        wins >= 12 ? "Baú de Ouro + 100 gemas" : wins >= 9 ? "Baú de Ouro + 30 gemas" : wins >= 6 ? "Baú de Prata + 15 gemas"
        : wins >= 3 ? $"{wins * 40} moedas + 5 gemas" : $"{wins * 30} moedas";
    public static ChestResult ClaimChallenge()
    {
        RollWeek();
        if (!ChallengeOver || challengeClaimed) return null;
        challengeClaimed = true;
        int w = ChallengeWins;
        ChestResult res = null;
        if (w >= 12) { Gems += 100; res = RollChest(2); }
        else if (w >= 9) { Gems += 30; res = RollChest(2); }
        else if (w >= 6) { Gems += 15; res = RollChest(1); }
        else if (w >= 3) { Coins += w * 40; Gems += 5; }
        else Coins += w * 30;
        Save();
        return res;
    }

    // ------------------------------------------------------------ Acessibilidade

    public static int TextScale = 1;                       // 0 pequeno, 1 normal, 2 grande
    public static readonly float[] TextScales = { 0.85f, 1f, 1.2f };
    public static readonly string[] TextScaleNames = { "Pequeno", "Normal", "Grande" };
    public static bool ColorBlind;                         // inimigo em laranja em vez de vermelho
    public static bool ReduceShake;                        // sem tremor de tela

    public static bool PostFx = true;      // brilho, vinheta e cores (pode desligar em PCs fracos)
    public static bool IntroSeen;          // já viu a história de abertura
    public static bool LowQuality;         // qualidade baixa: 30 quadros/s e menos partículas
    public static bool ShowFps;            // mostra os quadros por segundo no canto da tela

    // ------------------------------------------------------------ Maestria de cartas (vitórias com a carta no deck)

    static readonly Dictionary<string, int> cardWins = new Dictionary<string, int>();
    static readonly Dictionary<string, int> masteryClaimed = new Dictionary<string, int>();
    public static readonly int[] MasteryGoals = { 5, 15, 40 };
    public static readonly string[] MasteryRewards = { "50 moedas", "150 moedas", "20 gemas" };
    public static int CardWins(string key) => cardWins.TryGetValue(key, out int w) ? w : 0;
    public static int MasteryClaimedTier(string key) => masteryClaimed.TryGetValue(key, out int t) ? t : 0;
    public static int MasteryReachedTier(string key)
    {
        int w = CardWins(key), t = 0;
        while (t < MasteryGoals.Length && w >= MasteryGoals[t]) t++;
        return t;
    }
    public static bool MasteryReady(string key) => MasteryReachedTier(key) > MasteryClaimedTier(key);
    public static bool AnyMasteryReady() { foreach (var kv in cardWins) if (MasteryReady(kv.Key)) return true; return false; }
    public static void AddDeckWin()
    {
        foreach (var k in Deck) cardWins[k] = CardWins(k) + 1;
    }
    public static string ClaimMastery(string key)
    {
        if (!MasteryReady(key)) return null;
        int t = MasteryClaimedTier(key);
        if (t == 0) Coins += 50; else if (t == 1) Coins += 150; else Gems += 20;
        masteryClaimed[key] = t + 1;
        Save();
        return MasteryRewards[t];
    }
}
