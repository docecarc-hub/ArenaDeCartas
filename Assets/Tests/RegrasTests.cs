using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Testes das regras puras do jogo (rodam em Window > General > Test Runner > EditMode).
// Não chamam Profile.Save(), então não mexem no seu progresso salvo.
public class RegrasTests
{
    static CardData[] Cartas => CardData.All();

    [Test]
    public void ChavesDasCartasSaoUnicas()
    {
        var vistas = new HashSet<string>();
        foreach (var c in Cartas) Assert.IsTrue(vistas.Add(c.key), "Chave repetida: " + c.key);
    }

    [Test]
    public void ExistemOitoCartasIniciais()
    {
        int n = 0;
        foreach (var c in Cartas) if (c.starter) n++;
        Assert.AreEqual(8, n, "O deck inicial precisa de exatamente 8 cartas");
    }

    [Test]
    public void ValoresDasCartasFazemSentido()
    {
        foreach (var c in Cartas)
        {
            Assert.That(c.cost, Is.InRange(1, 10), c.key);
            Assert.That(c.rarity, Is.InRange(0, 4), c.key);
            Assert.That(c.unlockArena, Is.InRange(0, Profile.ArenaNames.Length - 1), c.key);
            if (c.kind != CardKind.Building || c.spawnKey == null && c.key != "mur") Assert.Greater(c.damage, 0f, c.key);
            if (c.kind == CardKind.Spell) Assert.Greater(c.spellRadius, 0f, c.key);
            else
            {
                Assert.Greater(c.hp, 0f, c.key);
                Assert.Greater(c.count, 0, c.key);
                Assert.Greater(c.attackCooldown, 0f, c.key);
            }
            if (c.spawnKey != null) Assert.IsNotNull(CardData.Extra(c.spawnKey), c.key + ": tropa da construção existe");
            if (c.kind == CardKind.Building)
            {
                Assert.AreEqual(0f, c.speed, c.key + ": construção não anda");
                Assert.Greater(c.lifetime, 0f, c.key + ": construção precisa de duração");
            }
        }
    }

    [Test]
    public void CartasDeLongeAcertamVoadores()
    {
        foreach (var c in Cartas)
        {
            if (c.kind == CardKind.Spell) continue;
            bool corpoACorpo = c.projectile == null && !c.flying;
            if (corpoACorpo) Assert.IsFalse(c.hitsAir, c.key + " é corpo a corpo e não deveria acertar o ar");
        }
    }

    [Test]
    public void TodaCartaTemArte()
    {
        foreach (var c in Cartas)
        {
            Assert.IsNotNull(Resources.Load<Texture2D>("Cartas/" + c.key), "Falta retrato: Cartas/" + c.key);
            if (c.kind != CardKind.Spell)
                Assert.IsNotNull(Resources.Load<Texture2D>("Tropas/" + c.key), "Falta folha de animação: Tropas/" + c.key);
        }
    }

    [Test]
    public void MultiplicadoresDeNivel()
    {
        Assert.AreEqual(1f, Profile.LevelMult(1), 1e-4f);
        Assert.AreEqual(1.7f, Profile.LevelMult(8), 1e-4f);
        Assert.AreEqual(1f, Profile.TowerMult(1), 1e-4f);
        Assert.Less(Profile.LevelMult(2), Profile.LevelMult(3));
    }

    [Test]
    public void ArenasSobemComOsTrofeus()
    {
        for (int i = 1; i < Profile.ArenaNames.Length; i++)
            Assert.Greater(Profile.ArenaTrophies[i], Profile.ArenaTrophies[i - 1]);
    }

    [Test]
    public void SorteioPorRaridadeRespeitaOsPesos()
    {
        var lista = new List<CardData>
        {
            new CardData { key = "c", rarity = 0 },
            new CardData { key = "l", rarity = 3 }
        };
        Random.InitState(42);
        int lendarias = 0;
        for (int i = 0; i < 2000; i++) if (Profile.PickByRarity(lista).rarity == 3) lendarias++;
        // peso 3 contra 60: perto de 4,8%
        Assert.That(lendarias, Is.InRange(40, 170));
    }

    [Test]
    public void ModoSoDeLongeFiltraCorpoACorpo()
    {
        foreach (var c in Cartas)
            if (c.kind == CardKind.Troop && c.range <= 1.5f) Assert.IsFalse(ArenaGame.IsRangedCard(c), c.key);
    }

    [Test]
    public void ChefesDaCampanhaUsamCartasQueExistem()
    {
        Assert.AreEqual(10, ArenaGame.Bosses.Length);
        var cartas = Cartas;
        foreach (var b in ArenaGame.Bosses)
        {
            Assert.IsNotNull(System.Array.Find(cartas, c => c.key == b.model), "Chefe sem modelo: " + b.model);
            Assert.IsNotNull(System.Array.Find(cartas, c => c.key == b.minion), "Chefe sem lacaio: " + b.minion);
        }
    }

    [Test]
    public void PremioDiarioTemSeteDias()
    {
        Assert.AreEqual(7, Profile.LoginRewards.GetLength(0));
        Assert.AreEqual(2, Profile.LoginRewards[6, 0], "O 7º dia deve ser um baú");
    }

    [Test]
    public void MetasDeMaestriaCrescem()
    {
        for (int i = 1; i < Profile.MasteryGoals.Length; i++)
            Assert.Greater(Profile.MasteryGoals[i], Profile.MasteryGoals[i - 1]);
        Assert.AreEqual(Profile.MasteryGoals.Length, Profile.MasteryRewards.Length);
    }

    [Test]
    public void PremioDoDesafioMelhoraComVitorias()
    {
        Assert.AreNotEqual(Profile.ChallengeRewardText(2), Profile.ChallengeRewardText(12));
        StringAssert.Contains("Ouro", Profile.ChallengeRewardText(12));
    }

    [Test]
    public void CampeoesTemHabilidadeEUmPorRaridade()
    {
        foreach (var c in Cartas)
        {
            if (!c.champion) continue;
            Assert.AreEqual(4, c.rarity, c.key + " deveria ser da raridade Campeão");
            Assert.IsFalse(string.IsNullOrEmpty(c.abilityName), c.key);
            Assert.Greater(c.abilityCooldown, 0f, c.key);
        }
    }

    [Test]
    public void ModosTemNomeEDescricao()
    {
        int n = System.Enum.GetValues(typeof(MatchMode)).Length;
        Assert.AreEqual(n, ArenaGame.ModeNames.Length);
        Assert.AreEqual(n, ArenaGame.ModeDescriptions.Length);
    }
}
