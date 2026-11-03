using System.Collections.Generic;
using System.Linq;
using Tracefinder.Character;
using Tracefinder.Combat;
using Tracefinder.Gameplay;
using Tracefinder.Items;
using Xunit;

namespace Tracefinder.Tests
{
    public class CombatTests
    {
        private static PlayerCombatant Hero(GridPos pos, Equipment.Weapon weapon = null, int str = 18, int dex = 14)
        {
            CharacterCore core = new()
            {
                Name = "Amber", Strength = str, Dexterity = dex, Constitution = 16, Wisdom = 12,
                SelectedKeyAbility = AbilityScore.Strength,
                EquippedWeapon = new CarriedWeapon(weapon ?? WeaponList.Longsword)
            };
            return new PlayerCombatant(core, ClassList.Fighter, pos);
        }

        private static MonsterCombatant Goblin(int x, int y) => new(StatblockList.GoblinWarrior, new GridPos(x, y));

        private static BattleGrid Open(int w = 8, int h = 8) => new(w, h);

        private static Encounter Started(BattleGrid grid, Combatant current, params Combatant[] others)
        {
            List<Combatant> all = new() { current };
            all.AddRange(others);
            Encounter e = new(grid, all);
            e.Start();
            int guard = 0;
            while (e.Current != current && guard++ < 50)
                e.EndTurn();
            Assert.Same(current, e.Current);
            return e;
        }

        [Theory]
        [InlineData(1, 0, 5)]
        [InlineData(1, 1, 5)]
        [InlineData(2, 2, 15)]
        [InlineData(3, 3, 20)]
        [InlineData(4, 1, 20)]
        public void DiagonalsAlternateFiveAndTen(int x, int y, int expected)
        {
            Assert.Equal(expected, BattleGrid.DistanceFeet(new GridPos(0, 0), new GridPos(x, y)));
        }

        [Fact]
        public void ParseReadsTerrain()
        {
            BattleGrid g = BattleGrid.Parse(".;", "~#", "o.");
            Assert.Equal(TerrainType.Difficult, g[new GridPos(1, 0)]);
            Assert.Equal(TerrainType.Water, g[new GridPos(0, 1)]);
            Assert.Equal(TerrainType.Wall, g[new GridPos(1, 1)]);
            Assert.Equal(TerrainType.Pillar, g[new GridPos(0, 2)]);
            Assert.True(g.IsPassable(new GridPos(1, 0)));
            Assert.False(g.IsPassable(new GridPos(0, 1)));
            Assert.False(g.IsPassable(new GridPos(9, 9)));
        }

        [Fact]
        public void WallBlocksSightButWaterDoesNot()
        {
            BattleGrid g = BattleGrid.Parse(".#.", "...", ".~.");
            Assert.False(g.HasLineOfSight(new GridPos(0, 0), new GridPos(2, 0)));
            Assert.True(g.HasLineOfSight(new GridPos(0, 1), new GridPos(2, 1)));
            Assert.True(g.HasLineOfSight(new GridPos(0, 2), new GridPos(2, 2)));
        }

        private static Dictionary<GridPos, int> Reach(BattleGrid g, GridPos start, int budget) =>
            Movement.Reachable(g, start, budget, _ => false, _ => false);

        [Fact]
        public void MovementBudgetCountsAlternatingDiagonals()
        {
            Dictionary<GridPos, int> r = Reach(Open(), new GridPos(0, 0), 15);
            Assert.Equal(5, r[new GridPos(1, 1)]);
            Assert.Equal(15, r[new GridPos(2, 2)]);
            Assert.False(r.ContainsKey(new GridPos(3, 3)));
            Assert.Equal(15, r[new GridPos(3, 0)]);
            Assert.False(r.ContainsKey(new GridPos(4, 0)));
        }

        [Fact]
        public void DifficultTerrainCostsDouble()
        {
            BattleGrid g = BattleGrid.Parse(".;.", "###");
            Dictionary<GridPos, int> r = Reach(g, new GridPos(0, 0), 15);
            Assert.Equal(10, r[new GridPos(1, 0)]);
            Assert.Equal(15, r[new GridPos(2, 0)]);
        }

        [Fact]
        public void WallsAreRoutedAroundAndCornersCannotBeCut()
        {
            BattleGrid g = BattleGrid.Parse(".#.", "...", "...");
            Dictionary<GridPos, int> r = Reach(g, new GridPos(0, 0), 30);
            Assert.False(r.ContainsKey(new GridPos(1, 0)));
            // Bezpośrednia przekątna z (0,0) na (1,1) ścięłaby róg ściany, więc idzie się dłużej
            Assert.Equal(10, r[new GridPos(1, 1)]);
        }

        [Fact]
        public void EnemiesBlockPassageAlliesOnlyBlockStopping()
        {
            BattleGrid g = BattleGrid.Parse("...", "###");
            GridPos middle = new(1, 0);
            Assert.False(Movement.Reachable(g, new GridPos(0, 0), 30, p => p == middle, p => p == middle).ContainsKey(new GridPos(2, 0)));

            Dictionary<GridPos, int> ally = Movement.Reachable(g, new GridPos(0, 0), 30, _ => false, p => p == middle);
            Assert.True(ally.ContainsKey(new GridPos(2, 0)));
            Assert.False(ally.ContainsKey(middle));
        }

        [Fact]
        public void StepRefusesDifficultTerrainAndOccupiedCells()
        {
            BattleGrid g = BattleGrid.Parse(".;", "..");
            List<GridPos> steps = Movement.StepTargets(g, new GridPos(0, 0), p => p == new GridPos(0, 1));
            Assert.Equal(new[] { new GridPos(1, 1) }, steps);
        }

        [Fact]
        public void InitiativeSortsDescendingAndEnemiesWinTies()
        {
            Dice.SetSeed(3);
            PlayerCombatant hero = Hero(new GridPos(0, 0));
            MonsterCombatant goblin = Goblin(5, 5);
            Encounter e = new(Open(), new Combatant[] { hero, goblin });
            e.Start();
            int[] rolls = e.Combatants.Select(c => c.Initiative).ToArray();
            Assert.True(rolls[0] >= rolls[1]);
            Assert.Equal(EncounterState.InProgress, e.State);
            Assert.Equal(1, e.Round);
            Assert.Same(e.Combatants[0], e.Current);
        }

        [Fact]
        public void ThreeActionsPerTurnThenRefused()
        {
            Dice.SetSeed(4);
            PlayerCombatant hero = Hero(new GridPos(0, 0));
            Encounter e = Started(Open(), hero, Goblin(7, 7));
            Assert.True(e.Stride(new GridPos(1, 0)).Success);
            Assert.True(e.Stride(new GridPos(2, 0)).Success);
            Assert.True(e.Step(new GridPos(3, 0)).Success);
            Assert.False(e.Stride(new GridPos(4, 0)).Success);
            Assert.Equal(new GridPos(3, 0), hero.Position);
        }

        [Fact]
        public void StrideRefusesOutOfBudgetCells()
        {
            Dice.SetSeed(5);
            PlayerCombatant hero = Hero(new GridPos(0, 0));
            Encounter e = Started(Open(), hero, Goblin(7, 7));
            Assert.False(e.Stride(new GridPos(6, 0)).Success);
            Assert.True(e.Stride(new GridPos(5, 0)).Success);
            Assert.Equal(new GridPos(5, 0), hero.Position);
        }

        [Fact]
        public void TurnsAdvanceAndRoundsCount()
        {
            Dice.SetSeed(6);
            Encounter e = new(Open(), new Combatant[] { Hero(new GridPos(0, 0)), Goblin(7, 7) });
            e.Start();
            Combatant first = e.Current;
            e.EndTurn();
            Assert.NotSame(first, e.Current);
            e.EndTurn();
            Assert.Same(first, e.Current);
            Assert.Equal(2, e.Round);
        }

        [Fact]
        public void EndTurnResetsActionsAndAttacks()
        {
            Dice.SetSeed(7);
            PlayerCombatant hero = Hero(new GridPos(0, 0));
            MonsterCombatant goblin = Goblin(1, 0);
            goblin.Health.SetMaxHp(1000);
            goblin.Health.SetToMaxHp();
            Encounter e = Started(Open(), hero, goblin);
            e.Strike(goblin, hero.Strikes[0]);
            Assert.Equal(1, hero.Actions.AttacksThisTurn);
            e.EndTurn();
            e.EndTurn();
            Assert.Same(hero, e.Current);
            Assert.Equal(0, hero.Actions.AttacksThisTurn);
            Assert.Equal(3, hero.Actions.MainActions);
        }

        [Fact]
        public void StrikeRefusesOutOfReachAndFriendlyTargets()
        {
            Dice.SetSeed(8);
            PlayerCombatant hero = Hero(new GridPos(0, 0));
            MonsterCombatant goblin = Goblin(5, 0);
            PlayerCombatant ally = Hero(new GridPos(1, 0));
            Encounter e = Started(Open(), hero, goblin, ally);
            Assert.False(e.Strike(goblin, hero.Strikes[0]).Success);
            Assert.False(e.Strike(ally, hero.Strikes[0]).Success);
            Assert.Equal(3, hero.Actions.MainActions);
        }

        [Fact]
        public void MultipleAttackPenaltyAppliesInLog()
        {
            Dice.SetSeed(9);
            PlayerCombatant hero = Hero(new GridPos(0, 0));
            MonsterCombatant goblin = Goblin(1, 0);
            goblin.Health.SetMaxHp(1000);
            goblin.Health.SetToMaxHp();
            Encounter e = Started(Open(), hero, goblin);
            string first = e.Strike(goblin, hero.Strikes[0]).Message;
            string second = e.Strike(goblin, hero.Strikes[0]).Message;
            string third = e.Strike(goblin, hero.Strikes[0]).Message;
            Assert.Contains("+ 9 =", first);
            Assert.Contains("+ 4 =", second);
            Assert.Contains("- 1 =", third);
        }

        [Fact]
        public void ThrownDaggerReachesPastMeleeAndRangedNeedsSight()
        {
            Dice.SetSeed(10);
            PlayerCombatant hero = Hero(new GridPos(0, 0), WeaponList.Dagger);
            MonsterCombatant goblin = Goblin(3, 0);
            goblin.Health.SetMaxHp(1000);
            goblin.Health.SetToMaxHp();
            Encounter e = Started(Open(), hero, goblin);
            (string refusal, bool thrown, int? increment) = e.StrikeReach(hero, goblin, hero.Strikes[0]);
            Assert.Null(refusal);
            Assert.True(thrown);
            Assert.Equal(10, increment);

            BattleGrid walled = BattleGrid.Parse(".#.", "###");
            PlayerCombatant archer = Hero(new GridPos(0, 0), WeaponList.Shortbow);
            MonsterCombatant target = Goblin(2, 0);
            Encounter blocked = Started(walled, archer, target);
            Assert.False(blocked.Strike(target, archer.Strikes[0]).Success);
        }

        [Fact]
        public void AmmunitionRunsOut()
        {
            Dice.SetSeed(11);
            PlayerCombatant archer = Hero(new GridPos(0, 0), WeaponList.Shortbow);
            archer.Strikes[0].Carried.Ammunition = 1;
            MonsterCombatant goblin = Goblin(5, 0);
            goblin.Health.SetMaxHp(1000);
            goblin.Health.SetToMaxHp();
            Encounter e = Started(Open(), archer, goblin);
            Assert.True(e.Strike(goblin, archer.Strikes[0]).Success);
            Assert.False(e.Strike(goblin, archer.Strikes[0]).Success);
        }

        [Fact]
        public void ReloadCostsActionsAndAllowsFiringAgain()
        {
            Equipment.Weapon crossbow = new()
            {
                Name = "Crossbow", Kind = WeaponKind.Ranged, DamageDice = Dice.DiceSize.D8, DiceNumber = 1,
                Category = WeaponCategory.Simple, RangeIncrement = 120, ActionsToReload = 1
            };
            Dice.SetSeed(12);
            PlayerCombatant archer = Hero(new GridPos(0, 0), crossbow);
            MonsterCombatant goblin = Goblin(5, 0);
            goblin.Health.SetMaxHp(1000);
            goblin.Health.SetToMaxHp();
            Encounter e = Started(Open(), archer, goblin);
            CarriedWeapon carried = archer.Strikes[0].Carried;
            Assert.False(e.Reload(new CarriedWeapon(WeaponList.Longsword)).Success);
            Assert.True(e.Strike(goblin, archer.Strikes[0]).Success);
            Assert.False(e.Strike(goblin, archer.Strikes[0]).Success);
            Assert.True(e.Reload(carried).Success);
            Assert.True(e.Strike(goblin, archer.Strikes[0]).Success);
        }

        [Fact]
        public void VersatileChoosesDamageTypeWithoutAction()
        {
            Dice.SetSeed(13);
            PlayerCombatant hero = Hero(new GridPos(0, 0));
            MonsterCombatant goblin = Goblin(1, 0);
            goblin.Health.SetMaxHp(1000);
            goblin.Health.SetToMaxHp();
            Encounter e = Started(Open(), hero, goblin);
            Assert.False(e.Strike(goblin, hero.Strikes[0], WeaponDamageType.Bludgeoning).Success);
            Assert.Equal(3, hero.Actions.MainActions);
            Assert.True(e.Strike(goblin, hero.Strikes[0], WeaponDamageType.Piercing).Success);
        }

        [Fact]
        public void ModularSwitchCostsOneAction()
        {
            Equipment.Weapon modular = new()
            {
                Name = "Modular", Kind = WeaponKind.Melee, DamageType = WeaponDamageType.Bludgeoning,
                DamageDice = Dice.DiceSize.D6, DiceNumber = 1,
                TraitList = [Trait.Modular(WeaponDamageType.Bludgeoning, WeaponDamageType.Piercing)]
            };
            Dice.SetSeed(14);
            PlayerCombatant hero = Hero(new GridPos(0, 0), modular);
            Encounter e = Started(Open(), hero, Goblin(7, 7));
            Assert.True(e.SwitchModular(hero.Strikes[0].Carried, WeaponDamageType.Piercing).Success);
            Assert.Equal(2, hero.Actions.MainActions);
            Assert.Equal(WeaponDamageType.Piercing, hero.Strikes[0].Carried.CurrentDamageType);
        }

        [Fact]
        public void KillingLastEnemyFinishesEncounter()
        {
            PlayerCombatant hero = Hero(new GridPos(0, 0));
            MonsterCombatant goblin = Goblin(1, 0);
            Dice.SetSeed(15);
            Encounter e = Started(Open(), hero, goblin);
            for (int i = 0; i < 300 && e.State == EncounterState.InProgress; i++)
            {
                if (e.Current == hero)
                    e.Strike(goblin, hero.Strikes[0]);
                e.EndTurn();
            }
            Assert.Equal(EncounterState.Finished, e.State);
            Assert.Equal(Side.Party, e.Winner);
            Assert.True(goblin.Health.IsDead);
            Assert.Null(e.Current);
            Assert.False(e.Stride(new GridPos(1, 1)).Success);
        }

        [Fact]
        public void DownedPlayerMakesRecoveryChecksAndTurnsSkipHim()
        {
            Dice.SetSeed(16);
            PlayerCombatant downed = Hero(new GridPos(0, 0));
            PlayerCombatant ally = Hero(new GridPos(0, 1));
            MonsterCombatant goblin = Goblin(7, 7);
            Encounter e = new(Open(), new Combatant[] { downed, ally, goblin });
            e.Start();
            downed.Health.TakeDamage(1000);
            Assert.True(downed.IsOut);
            for (int i = 0; i < 12; i++)
            {
                Assert.NotSame(downed, e.Current);
                e.EndTurn();
                if (e.State != EncounterState.InProgress)
                    break;
            }
            Assert.Contains(e.Log, line => line.Contains("walczy o życie"));
        }

        // Prosta symulacja: każdy idzie do najbliższego wroga i atakuje
        private static void Simulate(Encounter e)
        {
            for (int guard = 0; guard < 5000 && e.State == EncounterState.InProgress; guard++)
            {
                Combatant unit = e.Current;
                for (int action = 0; action < 3 && e.State == EncounterState.InProgress && e.Current == unit; action++)
                {
                    Combatant foe = e.Combatants.Where(c => c.Side != unit.Side && !c.IsOut)
                        .OrderBy(c => BattleGrid.DistanceFeet(c.Position, unit.Position)).First();
                    StrikeOption strike = unit.Strikes.FirstOrDefault(s => e.StrikeReach(unit, foe, s).Refusal == null);
                    if (strike != null && e.Strike(foe, strike).Success)
                        continue;
                    Dictionary<GridPos, int> targets = e.StrideTargets();
                    if (targets.Count == 0)
                        break;
                    GridPos best = targets.Keys.OrderBy(p => BattleGrid.DistanceFeet(p, foe.Position)).First();
                    e.Stride(best);
                }
                if (e.State == EncounterState.InProgress)
                    e.EndTurn();
            }
        }

        [Theory]
        [InlineData(100)]
        [InlineData(200)]
        [InlineData(300)]
        [InlineData(400)]
        public void FullFightRunsToAnEnd(int seed)
        {
            Dice.SetSeed(seed);
            BattleGrid grid = BattleGrid.Parse(
                "..........",
                "..;;..#...",
                "..;;..#...",
                "....o.....",
                "..~~......",
                "..~~...;;.",
                "..........");
            List<Combatant> all = new()
            {
                Hero(new GridPos(0, 0)),
                Hero(new GridPos(0, 2), WeaponList.Shortbow, str: 12, dex: 18),
                new MonsterCombatant(StatblockList.GoblinWarrior, new GridPos(9, 0), "Goblin 1"),
                new MonsterCombatant(StatblockList.KoboldWarrior, new GridPos(9, 3), "Kobold"),
                new MonsterCombatant(StatblockList.SkeletonGuard, new GridPos(9, 6), "Szkielet")
            };
            Encounter e = new(grid, all);
            e.Start();
            Simulate(e);
            Assert.Equal(EncounterState.Finished, e.State);
            Assert.NotNull(e.Winner);
        }
    }
}
