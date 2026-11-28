using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Regras usadas pelas tropas: alvos, caminho pelas pontes, morte e fim da partida.
public partial class ArenaGame
{
    // ---------------------------------------------------------------- Regras usadas pelas tropas

    public Entity FindTarget(Entity a)
    {
        Entity best = null;
        float bestD = float.MaxValue;
        float reach = a.isTower ? a.range : a.sight;
        foreach (var e in entities)
        {
            if (e == null || e.team == a.team || !e.Alive) continue;
            if (a.targetsTowersOnly && !e.isTower && !e.isBuilding) continue;
            if (e.flying && !a.hitsAir) continue;
            float d = Vector2.Distance(a.transform.position, e.transform.position) - e.radius - a.radius;
            if (d <= reach && d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    public Entity NearestEnemyTower(Entity a)
    {
        Entity best = null;
        float bestD = float.MaxValue;
        foreach (var e in entities)
        {
            if (e == null || !(e.isTower || (e.isBuilding && a.targetsTowersOnly)) || e.team == a.team || !e.Alive) continue;
            float d = Vector2.Distance(a.transform.position, e.transform.position);
            if (e.isBuilding && d > 7f) continue;   // construção só atrai quem passa perto
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    // Faz as tropas atravessarem o rio só pelas pontes.
    public Vector2 PathPoint(Vector2 from, Vector2 to)
    {
        if (Mathf.Sign(from.y) != Mathf.Sign(to.y))
        {
            float lx = from.x < 0f ? -LaneX : LaneX;
            if (Mathf.Abs(from.y) > 0.95f) return new Vector2(lx, Mathf.Sign(from.y) * 0.9f);
            return new Vector2(lx, Mathf.Sign(to.y) * 1.0f);
        }
        return to;
    }

    public bool PrincessLost(int team) => crowns[1 - team] > 0;

    public void OnEntityDied(Entity e)
    {
        entities.Remove(e);
        if (!e.isTower)
        {
            StartCoroutine(Puff(e.transform.position, Mathf.Max(0.8f, e.radius * 2f)));
            Sfx.Play("puf", 0.6f);
            return;
        }
        Sfx.Play("torre");
        if (Random.value < 0.5f) botEmoteDelay = 0.8f;
        Vibrate();

        // Torre destruída vira ruína
        StartCoroutine(Puff(e.transform.position, 2.5f));
        shake = 0.35f;
        var ruin = new GameObject("Ruina");
        ruin.transform.SetParent(matchRoot);
        ruin.transform.position = e.transform.position + Vector3.down * (e.isKing ? 0.9f : 0.6f);
        var rsr = ruin.AddComponent<SpriteRenderer>();
        rsr.sprite = Art("Batalha/ruina", new Vector2(0.5f, 0.2f));
        rsr.sortingOrder = 1000 - Mathf.RoundToInt(e.transform.position.y * 20f);
        if (rsr.sprite != null) ruin.transform.localScale = Vector3.one * ((e.isKing ? 2.4f : 1.7f) / rsr.sprite.bounds.size.x);

        int other = 1 - e.team;
        if (!e.isKing) princessDead[e.team, e.transform.position.x < 0f ? 0 : 1] = true;
        crowns[other] = e.isKing ? 3 : crowns[other] + 1;
        crownFlies.Add(new CrownFly { from = e.transform.position + Vector3.up * 1.5f, team = other, start = Time.time });
        if (other == 0) Profile.Track("torres");
        // Na prorrogação, a primeira torre derrubada decide
        if (e.isKing) StartCoroutine(KingFinale(e.transform.position, other));
        else if (overtime) EndMatch(other);
    }

    void EndMatch(int winner)
    {
        if (!Running) return;
        Running = false;
        selected = -1;
        deployZone.enabled = false;
        resultText = winner == 0 ? "VITÓRIA!" : winner == 1 ? "DERROTA" : "EMPATE";
        resWinner = winner;
        resTrophies = resCoins = resXp = 0;
        resChest = -1;
        resChestFull = false;
        resultT = 0f;
        crownsShown = 0;
        if (ghost != null) ghost.SetActive(false);
        foreach (var p in pocketZones) if (p != null) p.enabled = false;
        Sfx.Play(winner == 0 ? "vitoria" : "derrota");

        // Missões, baú de coroas e estatísticas
        Profile.Track("batalhas");
        if (winner == 0) { Profile.Track("vencer"); Profile.WinsTotal++; }
        // Maestria: conta a vitória para cada carta do deck (fora treino, tutorial e eventos casuais)
        if (winner == 0 && (Mode == MatchMode.Normal || Mode == MatchMode.Desafio || Mode == MatchMode.Campanha)) Profile.AddDeckWin();
        else if (winner == 1) Profile.LossesTotal++;
        Profile.Track("coroas", crowns[0]);
        Profile.AddCrowns(crowns[0]);

        int trophies = 0, coins = 0, xp;
        if (winner == 0) { SpawnConfetti(); botEmoteDelay = 1.2f; }
        if (Mode != MatchMode.Normal)
        {
            // Treino não vale nada além de experiência; eventos dão moedas sem mexer nos troféus
            bool evento = Mode != MatchMode.Treino && Mode != MatchMode.Tutorial && Mode != MatchMode.Campanha && Mode != MatchMode.Desafio;
            if (Mode == MatchMode.Desafio && winner != -1)
            {
                Profile.ChallengeResult(winner == 0);
                campaignMsg = $"Desafio semanal: {Profile.ChallengeWinsNow} vitórias, {Profile.ChallengeLossesNow}/3 derrotas"
                              + (Profile.ChallengeOver ? "\nDesafio encerrado! Pegue o prêmio na aba Eventos." : "");
            }
            if (Mode == MatchMode.Campanha && winner == 0)
            {
                int stars = crowns[0] >= 3 ? 3 : crowns[0] >= 2 ? 2 : 1;
                int old = Profile.CampaignStars[CampaignPhase];
                if (stars > old) Profile.CampaignStars[CampaignPhase] = stars;
                campaignMsg = $"Fase {CampaignPhase + 1}: {new string('*', stars)}{new string('-', 3 - stars)}  ({stars} estrela{(stars > 1 ? "s" : "")})";
                if (old == 0)
                {
                    campaignMsg += "\nPrêmio da primeira vitória: " + Profile.CampaignFirstReward(CampaignPhase);
                    var cr = Profile.GiveCampaignReward(CampaignPhase);
                    if (cr != null) pendingChest = cr;
                }
            }
            else campaignMsg = Mode == MatchMode.Campanha ? $"O chefe venceu. Tente a fase {CampaignPhase + 1} de novo!" : null;
            if (Mode == MatchMode.Tutorial)
            {
                bool first = !Profile.TutorialDone;
                Profile.TutorialDone = true;
                if (first && winner == 0) { coins += 100; Profile.Gems += 10; }
            }
            xp = winner == 0 ? 10 : 3;
            if (evento && winner == 0) coins = Profile.EventRewardLeft() > 0 ? 60 : 10;
            if (evento && winner == 0) Profile.EventWin();
            Profile.Coins += coins;
            resCoins = coins;
            resXp = xp;
            bool lvUp = Profile.AddXp(xp);
            Profile.CheckAchievements();
            Profile.Save();
            rewardText = (Mode == MatchMode.Tutorial ? "Tutorial concluído! Presente do Capitão: +10 gemas"
                          : campaignMsg ?? $"{ModeNames[(int)Mode]}: não vale troféus")
                         + (evento ? $"\nPrêmios de evento hoje: {Profile.EventRewardLeft()} restantes" : "")
                         + (lvUp ? $"\nSUBIU PARA O NÍVEL {Profile.Level}!" : "");
            return;
        }
        if (winner == 0)
        {
            trophies = 30;
            coins = 20;
            xp = 20;
            int type = Profile.RandomChestType();
            if (Profile.TryAddChest(type)) resChest = type;
            else resChestFull = true;
        }
        else if (winner == 1)
        {
            // Nunca cai abaixo do mínimo da arena que você já alcançou
            trophies = -Mathf.Min(15, Profile.Trophies - Profile.ArenaFloor);
            xp = 5;
        }
        else xp = 10;

        Profile.Trophies += trophies;
        Profile.Coins += coins;
        bool levelUp = Profile.AddXp(xp);
        string arenaMsg = Profile.UpdateProgress(deck);
        int passTiers = Profile.PassTiersReady();
        Profile.CheckAchievements();
        Profile.Save();

        resTrophies = trophies;
        resCoins = coins;
        resXp = xp;
        rewardText = (levelUp ? $"SUBIU PARA O NÍVEL {Profile.Level}! (+5 gemas)\n" : "")
                     + (arenaMsg != null ? arenaMsg.Split('\n')[0] + "\n" : "")
                     + (resChestFull ? "Espaços de baú cheios: abra algum no lobby.\n" : "")
                     + $"Baú de coroas: {Profile.CrownsToday}/{Profile.CrownChestGoal}"
                     + (passTiers > 0 ? $"   ·   Passe: {passTiers} prêmio(s)!" : "");
    }
}
