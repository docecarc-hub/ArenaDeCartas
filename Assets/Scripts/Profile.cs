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
    // Deck atual (8 chaves de carta)
    public static readonly List<string> Deck = new List<string>();
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
        Deck.Clear();
        foreach (var k in PlayerPrefs.GetString("deck", "").Split(','))
            if (!string.IsNullOrEmpty(k)) Deck.Add(k);

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
        PlayerPrefs.SetString("deck", string.Join(",", Deck));
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

        Deck.RemoveAll(k => !Owns(k) || Find(k) == null);
        for (int i = Deck.Count - 1; i >= 0; i--)
            if (Deck.IndexOf(Deck[i]) != i) Deck.RemoveAt(i);
        foreach (var c in all)
        {
            if (Deck.Count >= 8) break;
            if (Owns(c.key) && !Deck.Contains(c.key)) Deck.Add(c.key);
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
        return msg;
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
        int prize = 10 + HighestArena * 10;
        Gems += prize;
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
                    ? missing[UnityEngine.Random.Range(0, missing.Count)]
                    : pool[UnityEngine.Random.Range(0, pool.Count)];
                int n = UnityEngine.Random.Range(1, 3 + t * 2);
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
    public static int CopiesNeeded(string key) => CardLevel(key) >= MaxCardLevel ? 0 : UpgradeCopies[CardLevel(key) - 1];
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
        cards[key] = CardCount(key) - UpgradeCopies[lvl - 1];
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
}
