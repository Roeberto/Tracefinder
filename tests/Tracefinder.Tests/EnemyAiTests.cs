using System.Collections.Generic;
using System.Linq;
using Tracefinder.Character;
using Tracefinder.Combat;
using Tracefinder.Gameplay;
using Tracefinder.Items;
using Tracefinder.World;
using Xunit;

namespace Tracefinder.Tests
{
    public class EnemyAiTests
    {
        private static PlayerCombatant Hero(GridPos pos)
        {
            CharacterCore core = GameSession.ExampleCharacter();
            core.Health.SetMaxHp(1000);
            core.Health.SetToMaxHp();
            return new PlayerCombatant(core, ClassList.Fighter, pos);
        }

        private static Encounter StartedOn(BattleGrid grid, Combatant current, params Combatant[] others)
        {
            List<Combatant> all = new() { current };
            all.AddRange(others);
            Encounter e = new(grid, all);
            e.Start();
            for (int i = 0; i < 50 && e.Current != current; i++)
                e.EndTurn();
            Assert.Same(current, e.Current);
            return e;
        }

        // Kobold bez procy, żeby sprawdzić samą walkę wręcz i bronią rzucaną
        private static readonly Statblock Spearman = new()
        {
            Name = "Kobold Warrior",
            Level = -1,
            MaxHp = 7,
            ArmorClass = 16,
            Strikes = [StatblockList.KoboldWarrior.Strikes[0]]
        };

        [Fact]
        public void MeleeMonsterWalksUpAndAttacks()
        {
            Dice.SetSeed(1);
            MonsterCombatant kobold = new(Spearman, new GridPos(0, 0));
            PlayerCombatant hero = Hero(new GridPos(7, 0));
            Encounter e = StartedOn(new BattleGrid(9, 3), kobold, hero);

            EnemyAi.PlayTurn(e);

            Assert.NotSame(kobold, e.Current);
            Assert.Equal(new GridPos(6, 0), kobold.Position);
            Assert.Contains(e.Log, line => line.Contains("Kobold Warrior atakuje"));
        }

        [Fact]
        public void MonsterDoesNotThrowItsOnlyMeleeWeaponFromAfar()
        {
            Dice.SetSeed(2);
            MonsterCombatant kobold = new(Spearman, new GridPos(0, 0));
            PlayerCombatant hero = Hero(new GridPos(20, 0));
            Encounter e = StartedOn(new BattleGrid(21, 1), kobold, hero);

            Assert.True(EnemyAi.TakeAction(e));

            Assert.NotEqual(new GridPos(0, 0), kobold.Position);
            Assert.DoesNotContain(e.Log, line => line.Contains("atakuje"));
        }

        [Fact]
        public void ArcherShootsFromRangeWithoutMoving()
        {
            Dice.SetSeed(3);
            MonsterCombatant goblin = new(StatblockList.GoblinWarrior, new GridPos(0, 0));
            PlayerCombatant hero = Hero(new GridPos(10, 0));
            Encounter e = StartedOn(new BattleGrid(11, 1), goblin, hero);

            Assert.True(EnemyAi.TakeAction(e));

            Assert.Equal(new GridPos(0, 0), goblin.Position);
            Assert.Contains(e.Log, line => line.Contains("(Shortbow)"));
        }

        [Fact]
        public void AdjacentMonsterPrefersMeleeOverBow()
        {
            Dice.SetSeed(4);
            MonsterCombatant goblin = new(StatblockList.GoblinWarrior, new GridPos(0, 0));
            PlayerCombatant hero = Hero(new GridPos(1, 0));
            Encounter e = StartedOn(new BattleGrid(3, 1), goblin, hero);

            EnemyAi.TakeAction(e);

            Assert.Contains(e.Log, line => line.Contains("(Dogslicer)"));
        }

        [Fact]
        public void MonsterPathsAroundWallInsteadOfGettingStuck()
        {
            Dice.SetSeed(5);
            BattleGrid grid = BattleGrid.Parse(
                "...#...",
                "...#...",
                "...#...",
                ".......");
            MonsterCombatant kobold = new(StatblockList.KoboldWarrior, new GridPos(2, 0));
            PlayerCombatant hero = Hero(new GridPos(4, 0));
            Encounter e = StartedOn(grid, kobold, hero);

            EnemyAi.TakeAction(e);

            // Prosto do celu jest ściana, więc dobra droga prowadzi w dół, do przejścia
            Assert.True(kobold.Position.Y > 0, $"pozycja: {kobold.Position}");
        }

        [Fact]
        public void MonsterWithNoRouteEndsTurnInsteadOfHanging()
        {
            Dice.SetSeed(6);
            BattleGrid grid = BattleGrid.Parse(
                "..#..",
                "..#..");
            MonsterCombatant kobold = new(StatblockList.KoboldWarrior, new GridPos(0, 0));
            PlayerCombatant hero = Hero(new GridPos(4, 0));
            Encounter e = StartedOn(grid, kobold, hero);

            Assert.False(EnemyAi.TakeAction(e));
            Assert.Equal(new GridPos(0, 0), kobold.Position);

            EnemyAi.PlayTurn(e);
            Assert.NotSame(kobold, e.Current);
        }

        [Theory]
        [InlineData(1, -1, 20)]
        [InlineData(1, 1, 40)]
        [InlineData(1, 2, 60)]
        [InlineData(1, 5, 160)]
        [InlineData(1, 9, 160)]
        [InlineData(5, 1, 10)]
        [InlineData(6, 1, 0)]
        public void CreatureExperienceFollowsGmCoreTable(int partyLevel, int creatureLevel, int expected)
        {
            Assert.Equal(expected, Rewards.ExperienceFor(partyLevel, creatureLevel));
        }

        private static Encounter FinishedFight(CharacterCore core, params Statblock[] statblocks)
        {
            List<Combatant> all = new() { new PlayerCombatant(core, ClassList.Fighter, new GridPos(0, 0)) };
            for (int i = 0; i < statblocks.Length; i++)
                all.Add(new MonsterCombatant(statblocks[i], new GridPos(i + 1, 0)));
            Encounter e = new(new BattleGrid(8, 3), all);
            e.Start();
            foreach (Combatant monster in all.Skip(1))
                monster.Health.TakeDamage(1000);
            // Zwycięstwo jest wykrywane przy przejściu do kolejnej tury
            e.EndTurn();
            Assert.Equal(Side.Party, e.Winner);
            return e;
        }

        [Fact]
        public void WinningGrantsExperienceOnce()
        {
            Dice.SetSeed(30);
            CharacterCore core = GameSession.ExampleCharacter();
            Encounter e = FinishedFight(core, StatblockList.GoblinWarrior, StatblockList.KoboldWarrior);
            Assert.Equal(40, e.ExperienceAwarded);
            Assert.Equal(40, core.Experience);
            e.EndTurn();
            Assert.Equal(40, core.Experience);
        }

        [Fact]
        public void EnoughExperienceLevelsUpAndIsLogged()
        {
            Dice.SetSeed(31);
            CharacterCore core = GameSession.ExampleCharacter();
            core.Experience = 990;
            Encounter e = FinishedFight(core, StatblockList.GoblinWarrior);
            Assert.Equal(2, core.Level);
            Assert.Equal(10, core.Experience);
            Assert.Contains(e.Log, line => line.Contains("awansuje na poziom 2"));
        }

        [Fact]
        public void LosingGrantsNoExperience()
        {
            Dice.SetSeed(32);
            CharacterCore core = GameSession.ExampleCharacter();
            MonsterCombatant goblin = new(StatblockList.GoblinWarrior, new GridPos(1, 0));
            PlayerCombatant hero = new(core, ClassList.Fighter, new GridPos(0, 0));
            Encounter e = new(new BattleGrid(4, 1), new Combatant[] { hero, goblin });
            e.Start();
            hero.Health.TakeDamage(1000);
            e.EndTurn();
            Assert.Equal(Side.Enemies, e.Winner);
            Assert.Equal(0, core.Experience);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void GeneratedFightsPlayedByAiAlwaysFinish(int seed)
        {
            Dice.SetSeed(seed);
            foreach (Biome biome in new[] { Biome.Plains, Biome.Forest, Biome.Mountains, Biome.Settlement })
            {
                Location location = LocationGenerator.Generate(seed, seed, seed, biome);
                Encounter e = GameSession.CreateEncounter(location, GameSession.ExampleCharacter(), ClassList.Fighter);
                e.Start();
                for (int i = 0; i < 2000 && e.State == EncounterState.InProgress; i++)
                    EnemyAi.PlayTurn(e);
                Assert.Equal(EncounterState.Finished, e.State);
            }
        }
    }
}
