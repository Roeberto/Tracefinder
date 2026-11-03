using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tracefinder.Character;
using Tracefinder.Combat;
using Tracefinder.Gameplay;
using Tracefinder.Items;
using Tracefinder.World;
using Xunit;
using Xunit.Abstractions;

namespace Tracefinder.Tests
{
    public class CampaignTests
    {
        private readonly ITestOutputHelper _output;

        public CampaignTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(Threat.Low, 4, 60)]
        [InlineData(Threat.Moderate, 4, 80)]
        [InlineData(Threat.Moderate, 1, 20)]
        [InlineData(Threat.Low, 1, 0)]
        [InlineData(Threat.Severe, 1, 30)]
        [InlineData(Threat.Extreme, 1, 40)]
        [InlineData(Threat.Extreme, 6, 240)]
        public void EncounterBudgetFollowsGmCoreTable(Threat threat, int party, int expected)
        {
            Assert.Equal(expected, Rewards.Budget(threat, party));
        }

        [Fact]
        public void TreasureComesFromGmCoreTable()
        {
            Assert.Equal(18, Rewards.TreasureGp(1, Threat.Moderate));
            Assert.Equal(270, Rewards.TreasureGp(5, Threat.Extreme));
            Assert.Equal(98000, Rewards.TreasureGp(20, Threat.Extreme));
        }

        [Theory]
        [InlineData(-1, 14, 8, 6)]
        [InlineData(1, 15, 20, 7)]
        [InlineData(5, 21, 75, 13)]
        [InlineData(12, 32, 215, 24)]
        public void ModerateMonstersUseBuildingCreaturesTable(int level, int ac, int hp, int attack)
        {
            Statblock s = MonsterFactory.Create(MonsterFactory.Bandit, level);
            Assert.Equal(ac, s.ArmorClass);
            Assert.Equal(hp, s.MaxHp);
            Assert.Equal(attack, s.Strikes[0].AttackBonus);
        }

        [Fact]
        public void EliteIsStrongerAndHasReactiveStrike()
        {
            Statblock normal = MonsterFactory.Create(MonsterFactory.Ogre, 4);
            Statblock elite = MonsterFactory.Create(MonsterFactory.Ogre, 4, true, "Wódz");
            Assert.True(elite.MaxHp > normal.MaxHp);
            Assert.True(elite.ArmorClass > normal.ArmorClass);
            Assert.True(elite.HasReactiveStrike);
            Assert.False(normal.HasReactiveStrike);
            Assert.Equal("Wódz", elite.Name);
        }

        [Theory]
        [InlineData(1, Threat.Low)]
        [InlineData(1, Threat.Extreme)]
        [InlineData(5, Threat.Moderate)]
        [InlineData(10, Threat.Severe)]
        public void BudgetBuildsAtLeastOneEnemyWithinLimits(int level, Threat threat)
        {
            for (int seed = 0; seed < 50; seed++)
            {
                List<Statblock> enemies = MonsterFactory.ForBudget(new Random(seed), level, threat, MonsterFactory.FamiliesFor(Biome.Plains));
                Assert.InRange(enemies.Count, 1, 4);
                int xp = enemies.Sum(e => Rewards.ExperienceFor(level, e.Level));
                int budget = Rewards.Budget(threat, 1);
                Assert.True(xp <= budget || enemies.Count == 1, $"{xp} przy budżecie {budget}");
            }
        }

        [Fact]
        public void EveryWorldHasReachableSites()
        {
            for (int seed = 0; seed < 150; seed++)
            {
                Campaign c = Campaign.New(seed, GameSession.ExampleCharacter());
                Assert.Equal(Campaign.TownCount, c.Sites.Count(s => s.Kind == SiteKind.Town));
                Assert.Equal(Campaign.LairCount, c.Sites.Count(s => s.Kind == SiteKind.Lair));
                Assert.Single(c.Sites, s => s.Kind == SiteKind.Fortress);
                Dictionary<(int X, int Y), int> distance = c.Distances((c.X, c.Y));
                foreach (Site site in c.Sites)
                {
                    Assert.True(distance.ContainsKey((site.X, site.Y)), $"ziarno {seed}: {site.Name} niedostępne");
                    Assert.True(c.CanWalk(site.X, site.Y));
                }
                Assert.Equal(c.Sites.Count, c.Sites.Select(s => (s.X, s.Y)).Distinct().Count());
                Assert.Same(c.StartTown, c.CurrentSite);
            }
        }

        [Fact]
        public void SameSeedGivesSameWorld()
        {
            Campaign a = Campaign.New(77, GameSession.ExampleCharacter());
            Campaign b = Campaign.New(77, GameSession.ExampleCharacter());
            Assert.Equal(a.Sites, b.Sites);
        }

        [Fact]
        public void WaterBlocksTravel()
        {
            Campaign c = Campaign.New(5, GameSession.ExampleCharacter());
            (int X, int Y)? water = null;
            for (int y = 0; y < c.Map.Height && water == null; y++)
            {
                for (int x = 0; x < c.Map.Width && water == null; x++)
                {
                    if (c.Map[x, y] == Biome.Water)
                        water = (x, y);
                }
            }
            Assert.NotNull(water);
            Assert.Equal(TravelEvent.Blocked, c.Travel(water.Value.X, water.Value.Y, out _));
            Assert.Equal(TravelEvent.Blocked, c.Travel(-1, 0, out _));
        }

        [Fact]
        public void FortressOpensAfterThreeLairsAndWinsTheGame()
        {
            Campaign c = Campaign.New(5, GameSession.ExampleCharacter());
            Assert.False(c.FortressOpen);
            foreach (Site lair in c.Sites.Where(s => s.Kind == SiteKind.Lair))
                c.ResolveVictory(c.SitePlan(lair), c.Hero.Level);
            Assert.True(c.FortressOpen);
            Assert.False(c.Completed);

            Site fortress = c.Sites.Single(s => s.Kind == SiteKind.Fortress);
            VictoryReward reward = c.ResolveVictory(c.SitePlan(fortress), c.Hero.Level);
            Assert.True(reward.CampaignComplete);
            Assert.True(c.Completed);
        }

        [Fact]
        public void VictoryPaysTreasureAndHeroPointOnLevelUp()
        {
            Campaign c = Campaign.New(5, GameSession.ExampleCharacter());
            int silver = c.Hero.Silver;
            int points = c.Hero.Health.HeroPoints;
            EncounterPlan plan = new(new List<Statblock>(), Threat.Moderate, null);
            c.Hero.AddExperience(1000, c.HeroClass);
            VictoryReward reward = c.ResolveVictory(plan, 1);
            Assert.Equal(18 * 10 * Campaign.TreasureMultiplier, reward.Silver);
            Assert.True(c.Hero.Silver >= silver + reward.Silver);
            Assert.Equal(1, reward.LevelsGained);
            Assert.Equal(points + 1, c.Hero.Health.HeroPoints);
        }

        [Fact]
        public void HeroicRecoveryReturnsHeroToTownOncePerPoints()
        {
            Campaign c = Campaign.New(5, GameSession.ExampleCharacter());
            Assert.Equal(1, c.Hero.Health.HeroPoints);
            c.Hero.Health.TakeDamage(1000);
            Assert.True(c.HeroicRecovery());
            Assert.Equal(SiteKind.Town, c.CurrentSite.Kind);
            Assert.Equal(1, c.Hero.Health.CurrentHp);
            Assert.Equal(1, c.Hero.Health.Wounded);
            Assert.False(c.HeroicRecovery());
        }

        [Fact]
        public void ShopSpendsMoneyAndRespectsLevels()
        {
            CharacterCore c = GameSession.ExampleCharacter();
            c.Silver = 300;
            Armor chain = ShopCatalog.Armors.First(a => a.Name == "Chain Shirt");
            Assert.True(Shop.BuyArmor(c, chain));
            Assert.Same(chain, c.EquippedArmor);
            Assert.Equal(250, c.Silver);
            Assert.False(Shop.BuyArmor(c, chain));

            Assert.False(Shop.BuyArmor(c, new Armor("Test", ArmorCategory.Heavy, 6, 0, 1, 5)));
            Assert.True(Shop.BuyShield(c));
            Assert.False(Shop.BuyShield(c));
            Assert.True(Shop.BuyPotion(c, ShopCatalog.Potions[0]));
            Assert.Equal(1, c.Potions[ShopCatalog.Potions[0].Name]);
            Assert.False(Shop.BuyPotion(c, ShopCatalog.Potions[2]));
        }

        [Fact]
        public void RunesNeedLevelMoneyAndOrder()
        {
            CharacterCore c = GameSession.ExampleCharacter();
            c.Silver = 2000;
            Assert.False(Shop.BuyStrikingRune(c, c.EquippedWeapon));
            Assert.True(Shop.BuyPotencyRune(c, c.EquippedWeapon));
            Assert.Equal(1, c.EquippedWeapon.PotencyRune);
            Assert.False(Shop.BuyStrikingRune(c, c.EquippedWeapon));
            c.Level = 3;
            Assert.True(Shop.BuyStrikingRune(c, c.EquippedWeapon));
            Assert.False(Shop.BuyPotencyRune(c, c.EquippedWeapon));
            Assert.Equal(1000, c.Silver);
        }

        [Fact]
        public void BrokeHeroCannotBuy()
        {
            CharacterCore c = GameSession.ExampleCharacter();
            c.Silver = 1;
            Assert.False(Shop.BuyShield(c));
            Assert.False(c.HasShield);
            Assert.Equal(1, c.Silver);
        }

        [Fact]
        public void CampaignSurvivesSaveAndLoad()
        {
            string dir = Path.Combine(Path.GetTempPath(), "tracefinder-tests-" + Guid.NewGuid());
            try
            {
                CampaignRepository repo = new(Path.Combine(dir, "campaign.json"));
                Assert.False(repo.Exists);
                Assert.Null(repo.Load());

                CharacterCore hero = GameSession.ExampleCharacter();
                Campaign c = Campaign.New(5, hero);
                hero.Level = 5;
                hero.InitializeHealth(c.HeroClass);
                hero.Health.TakeDamage(7);
                hero.Silver = 123;
                hero.Feats.Add(FeatList.Fleet);
                hero.Background = BackgroundList.All[0];
                hero.Potions["Minor Healing Potion"] = 2;
                Shop.BuyShield(hero);
                hero.ShieldHp = 15;
                hero.Silver = 500;
                Shop.BuyArmor(hero, ShopCatalog.Armors[3]);
                hero.EquippedWeapon = hero.Inventory[1];
                hero.EquippedWeapon.PotencyRune = 1;
                hero.SpellSlotsUsed[1] = 2;
                Site lair = c.Sites.First(s => s.Kind == SiteKind.Lair);
                c.ResolveVictory(c.SitePlan(lair), hero.Level);
                repo.Save(c);

                Campaign loaded = repo.Load();
                Assert.NotNull(loaded);
                Assert.Equal(c.Seed, loaded.Seed);
                Assert.Equal(c.Sites, loaded.Sites);
                Assert.Equal((c.X, c.Y), (loaded.X, loaded.Y));
                Assert.True(loaded.IsCleared(loaded.Sites.First(s => s.Name == lair.Name)));
                Assert.Equal(c.FightsWon, loaded.FightsWon);

                CharacterCore h = loaded.Hero;
                Assert.Equal(hero.Health.CurrentHp, h.Health.CurrentHp);
                Assert.Equal(hero.Silver, h.Silver);
                Assert.Equal(5, h.Level);
                Assert.True(h.HasFeat(FeatList.Fleet));
                Assert.Same(BackgroundList.All[0], h.Background);
                Assert.Equal(2, h.Potions["Minor Healing Potion"]);
                Assert.True(h.HasShield);
                Assert.Equal(15, h.ShieldHp);
                Assert.Same(ShopCatalog.Armors[3], h.EquippedArmor);
                Assert.Single(h.OwnedArmors);
                Assert.Equal(hero.Inventory.Count, h.Inventory.Count);
                Assert.Equal("Dagger", h.EquippedWeapon.Name);
                Assert.Equal(1, h.EquippedWeapon.PotencyRune);
                Assert.Equal(2, h.SpellSlotsUsed[1]);
            }
            finally
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void CorruptedSaveLoadsAsNull()
        {
            string dir = Path.Combine(Path.GetTempPath(), "tracefinder-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            try
            {
                string path = Path.Combine(dir, "campaign.json");
                File.WriteAllText(path, "{ to nie jest json");
                Assert.Null(new CampaignRepository(path).Load());
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void EncounterXpUsesCampaignMultiplier()
        {
            Dice.SetSeed(5);
            Campaign c = Campaign.New(5, GameSession.ExampleCharacter());
            EncounterPlan plan = new(new List<Statblock> { MonsterFactory.Create(MonsterFactory.Bandit, -1) }, Threat.Low, null);
            Encounter e = c.BuildEncounter(plan);
            Assert.Equal(Campaign.ExperienceMultiplier, e.ExperienceMultiplier);
            e.Start();
            for (int i = 0; i < 3000 && e.State == EncounterState.InProgress; i++)
                EnemyAi.PlayTurn(e);
            Assert.Equal(Side.Party, e.Winner);
            Assert.Equal(20 * Campaign.ExperienceMultiplier, e.ExperienceAwarded);
        }

        // ---------- cała gra zagrana przez bota: dowód, że da się ją ukończyć każdą klasą ----------

        [Theory]
        [InlineData("Fighter", 1)]
        [InlineData("Fighter", 5)]
        [InlineData("Rogue", 2)]
        [InlineData("Rogue", 6)]
        [InlineData("Wizard", 3)]
        [InlineData("Wizard", 7)]
        [InlineData("Cleric", 4)]
        [InlineData("Cleric", 8)]
        public void BotFinishesWholeCampaign(string className, int seed)
        {
            CampaignBot bot = new(className, seed);
            bot.Play();
            _output.WriteLine(bot.Summary());
            Assert.True(bot.Campaign.Completed, bot.Summary());
        }

        [Fact]
        public void CampingEventuallySucceedsAtTheSameSpot()
        {
            Campaign c = Campaign.New(5, GameSession.ExampleCharacter());
            c.Roller = new Random(1);
            c.Hero.Health.TakeDamage(5);
            bool rested = false;
            for (int i = 0; i < 40 && !rested; i++)
                rested = c.Camp() == null;
            Assert.True(rested);
            Assert.Equal(c.Hero.Health.MaxHp, c.Hero.Health.CurrentHp);
        }

        [Fact]
        public void TreatWoundsHasCooldownAndNeedsMedicine()
        {
            Dice.SetSeed(9);
            CharacterCore hero = GameSession.ExampleCharacter();
            Campaign c = Campaign.New(5, hero);
            hero.Health.TakeDamage(5);
            Assert.False(c.CanTreatWounds);
            hero.Background = BackgroundList.All.First(b => b.TrainedSkill == SkillName.Medicine);
            Assert.True(c.CanTreatWounds);
            c.TreatWounds();
            hero.Health.TakeDamage(1);
            Assert.False(c.CanTreatWounds);
        }
    }
}
