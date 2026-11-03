using System;
using Tracefinder.Character;
using Tracefinder.Gameplay;
using Tracefinder.Items;
using Xunit;

namespace Tracefinder.Tests
{
    public class RulesTests
    {
        [Theory]
        [InlineData(25, 15, DegreeOfSuccess.CriticalSuccess)]
        [InlineData(24, 15, DegreeOfSuccess.Success)]
        [InlineData(15, 15, DegreeOfSuccess.Success)]
        [InlineData(14, 15, DegreeOfSuccess.Failure)]
        [InlineData(6, 15, DegreeOfSuccess.Failure)]
        [InlineData(5, 15, DegreeOfSuccess.CriticalFailure)]
        public void DetermineDegree(int total, int dc, DegreeOfSuccess expected)
        {
            Assert.Equal(expected, Check.Determine(total, dc));
        }

        [Theory]
        [InlineData(DegreeOfSuccess.Success, 1, DegreeOfSuccess.CriticalSuccess)]
        [InlineData(DegreeOfSuccess.CriticalSuccess, 1, DegreeOfSuccess.CriticalSuccess)]
        [InlineData(DegreeOfSuccess.Failure, -1, DegreeOfSuccess.CriticalFailure)]
        [InlineData(DegreeOfSuccess.CriticalFailure, -1, DegreeOfSuccess.CriticalFailure)]
        public void ShiftClampsToEnum(DegreeOfSuccess degree, int steps, DegreeOfSuccess expected)
        {
            Assert.Equal(expected, Check.Shift(degree, steps));
        }

        [Fact]
        public void RollAppliesNaturalTwentyAndOneToDegree()
        {
            Dice.SetSeed(1);
            for (int i = 0; i < 20000; i++)
            {
                Check.Result r = Check.Roll(0, 20);
                Assert.Equal(r.Natural, r.Total);
                if (r.Natural == 20)
                    Assert.Equal(DegreeOfSuccess.CriticalSuccess, r.Degree);
                else if (r.Natural == 1)
                    Assert.Equal(DegreeOfSuccess.CriticalFailure, r.Degree);
                else
                    Assert.Equal(Check.Determine(r.Total, 20), r.Degree);
            }
        }

        [Fact]
        public void SameSeedGivesSameRolls()
        {
            Dice.SetSeed(42);
            int a = Dice.Roll(Dice.DiceSize.D100, 5);
            Dice.SetSeed(42);
            Assert.Equal(a, Dice.Roll(Dice.DiceSize.D100, 5));
        }

        [Theory]
        [InlineData(0, false, 0)]
        [InlineData(1, false, -5)]
        [InlineData(2, false, -10)]
        [InlineData(5, false, -10)]
        [InlineData(1, true, -4)]
        [InlineData(2, true, -8)]
        public void MultipleAttackPenalty(int attacksMade, bool agile, int expected)
        {
            Assert.Equal(expected, AttackRules.MultipleAttackPenalty(attacksMade, agile));
        }

        [Fact]
        public void DaggerIsAgileForMultipleAttackPenalty()
        {
            Assert.Equal(-4, AttackRules.MultipleAttackPenalty(1, WeaponList.Dagger));
            Assert.Equal(-5, AttackRules.MultipleAttackPenalty(1, WeaponList.Longsword));
        }

        [Theory]
        [InlineData(5, 0)]
        [InlineData(60, 0)]
        [InlineData(65, -2)]
        [InlineData(120, -2)]
        [InlineData(125, -4)]
        public void RangePenaltyGrowsPerIncrement(int distance, int expected)
        {
            Assert.Equal(expected, AttackRules.RangePenalty(60, distance));
        }

        [Fact]
        public void RangeLimitIsSixIncrements()
        {
            Assert.True(AttackRules.IsInRange(60, 360));
            Assert.False(AttackRules.IsInRange(60, 365));
            Assert.False(AttackRules.IsInRange(null, 5));
        }

        [Fact]
        public void LongbowHasVolleyPenaltyUpClose()
        {
            Assert.Equal(-2, AttackRules.VolleyPenalty(WeaponList.Longbow, 30));
            Assert.Equal(0, AttackRules.VolleyPenalty(WeaponList.Longbow, 35));
            Assert.Equal(0, AttackRules.VolleyPenalty(WeaponList.Shortbow, 5));
        }

        [Fact]
        public void ThrownDaggerUsesThrownRange()
        {
            Assert.Equal(10, AttackRules.RangeIncrementFor(WeaponList.Dagger, thrown: true));
        }

        private static (int min, int max) Range(Func<int> roll)
        {
            int min = int.MaxValue, max = int.MinValue;
            for (int i = 0; i < 100000; i++)
            {
                int r = roll();
                min = Math.Min(min, r);
                max = Math.Max(max, r);
            }
            return (min, max);
        }

        [Fact]
        public void DamageRangesPerDegree()
        {
            Dice.SetSeed(7);
            Assert.Equal((5, 12), Range(() => Damage.Roll(WeaponList.Longsword, 4, DegreeOfSuccess.Success)));
            Assert.Equal((10, 24), Range(() => Damage.Roll(WeaponList.Longsword, 4, DegreeOfSuccess.CriticalSuccess)));
            Assert.Equal((0, 0), Range(() => Damage.Roll(WeaponList.Longsword, 4, DegreeOfSuccess.Failure)));
            Assert.Equal((0, 0), Range(() => Damage.Roll(WeaponList.Longsword, 4, DegreeOfSuccess.CriticalFailure)));
        }

        [Fact]
        public void DeadlyDieIsAddedAfterDoubling()
        {
            Dice.SetSeed(8);
            Assert.Equal((11, 28), Range(() => Damage.Roll(WeaponList.Rapier, 4, DegreeOfSuccess.CriticalSuccess)));
            Assert.Equal((3, 22), Range(() => Damage.Roll(WeaponList.Shortbow, 0, DegreeOfSuccess.CriticalSuccess)));
        }

        [Fact]
        public void DamageIsAtLeastOneOnAHit()
        {
            Dice.SetSeed(9);
            Assert.Equal((1, 3), Range(() => Damage.Roll(WeaponList.Dagger, -1, DegreeOfSuccess.Success)));
        }

        [Fact]
        public void StrikingRuneAddsDiceAndGreaterStrikingAddsDeadlyDice()
        {
            Dice.SetSeed(10);
            Assert.Equal((2, 16), Range(() => Damage.Roll(WeaponList.Longsword, 0, DegreeOfSuccess.Success, 1)));
            // 3k6 razy 2 oraz 2k8 z Deadly przy runie greater striking
            Assert.Equal((8, 52), Range(() => Damage.Roll(WeaponList.Rapier, 0, DegreeOfSuccess.CriticalSuccess, 2)));
        }

        [Fact]
        public void FatalChangesDieAndAddsOneMoreOnCrit()
        {
            Equipment.Weapon pick = new()
            {
                Name = "Pick", DamageDice = Dice.DiceSize.D6, DiceNumber = 1,
                TraitList = [Trait.Fatal(Dice.DiceSize.D10)]
            };
            Dice.SetSeed(11);
            Assert.Equal((3, 30), Range(() => Damage.Roll(pick, 0, DegreeOfSuccess.CriticalSuccess)));
            Assert.Equal((1, 6), Range(() => Damage.Roll(pick, 0, DegreeOfSuccess.Success)));
        }

        [Fact]
        public void ActionTrackerCountsAttacksAndResets()
        {
            ActionTracker a = new();
            a.RegisterAttack();
            a.UseMainAction(3);
            Assert.Equal(1, a.AttacksThisTurn);
            Assert.False(a.CanUseMainAction(1));
            a.ResetTurn();
            Assert.Equal(0, a.AttacksThisTurn);
            Assert.True(a.CanUseMainAction(3));
        }

        [Fact]
        public void CarriedWeaponModularAndVersatile()
        {
            Equipment.Weapon modular = new()
            {
                Name = "M", DamageType = WeaponDamageType.Bludgeoning,
                TraitList = [Trait.Modular(WeaponDamageType.Bludgeoning, WeaponDamageType.Piercing)]
            };
            CarriedWeapon m = new(modular);
            Assert.False(m.CanStrikeWith(WeaponDamageType.Piercing));
            Assert.True(m.SwitchModular(WeaponDamageType.Piercing));
            Assert.True(m.CanStrikeWith(WeaponDamageType.Piercing));
            Assert.False(m.SwitchModular(WeaponDamageType.Slashing));

            CarriedWeapon sword = new(WeaponList.Longsword);
            Assert.True(sword.CanStrikeWith(WeaponDamageType.Piercing));
            Assert.False(sword.CanStrikeWith(WeaponDamageType.Bludgeoning));
        }

        [Fact]
        public void CarriedWeaponAmmunitionAndReload()
        {
            Equipment.Weapon crossbow = new() { Name = "C", ActionsToReload = 1 };
            CarriedWeapon c = new(crossbow) { Ammunition = 1 };
            Assert.True(c.CanFire);
            c.Fire();
            Assert.False(c.CanFire);
            c.Reload();
            Assert.False(c.CanFire);
        }

        private static HealthTracker Hero(int hp = 10)
        {
            HealthTracker h = new();
            h.SetMaxHp(hp);
            h.SetToMaxHp();
            return h;
        }

        [Fact]
        public void FallingToZeroGivesDyingOneOrTwoOnCrit()
        {
            HealthTracker h = Hero();
            h.TakeDamage(10);
            Assert.Equal(1, h.Dying);

            HealthTracker c = Hero();
            c.TakeDamage(10, criticalHit: true);
            Assert.Equal(2, c.Dying);
        }

        [Fact]
        public void WoundedAddsToDyingOnlyWhenKnockedDown()
        {
            HealthTracker h = Hero();
            h.IncreaseWounded();
            h.TakeDamage(10);
            Assert.Equal(2, h.Dying);
            h.TakeDamage(1);
            Assert.Equal(3, h.Dying);
        }

        [Fact]
        public void DyingFourIsDeath()
        {
            HealthTracker h = Hero();
            h.TakeDamage(10, true);
            h.TakeDamage(1, true);
            Assert.True(h.IsDead);
            Assert.Equal(HealthTracker.MaxDying, h.Dying);
        }

        [Fact]
        public void HealingFromDyingWakesAndWounds()
        {
            HealthTracker h = Hero();
            h.TakeDamage(10);
            h.HealDamage(3);
            Assert.Equal(0, h.Dying);
            Assert.Equal(1, h.Wounded);
            Assert.Equal(3, h.CurrentHp);
            Assert.False(h.IsUnconscious);
        }

        [Fact]
        public void RecoveryToZeroDyingAddsWoundedButStaysUnconscious()
        {
            HealthTracker h = Hero();
            h.TakeDamage(10);
            h.ApplyRecovery(DegreeOfSuccess.Success);
            Assert.Equal(0, h.Dying);
            Assert.Equal(1, h.Wounded);
            Assert.True(h.IsUnconscious);
            Assert.False(h.IsDead);
        }

        [Fact]
        public void RecoveryFailureCanKill()
        {
            HealthTracker h = Hero();
            h.TakeDamage(10, true);
            h.ApplyRecovery(DegreeOfSuccess.CriticalFailure);
            Assert.True(h.IsDead);
        }

        [Fact]
        public void FullRestClearsEverything()
        {
            HealthTracker h = Hero();
            h.TakeDamage(10, true);
            h.IncreaseWounded();
            h.FullRest();
            Assert.Equal(10, h.CurrentHp);
            Assert.Equal(0, h.Dying);
            Assert.Equal(0, h.Wounded);
            Assert.False(h.IsDead);
        }

        [Fact]
        public void MonsterDiesAtZeroHp()
        {
            HealthTracker h = Hero(5);
            h.DiesAtZeroHp = true;
            h.TakeDamage(5);
            Assert.True(h.IsDead);
            Assert.Equal(0, h.Dying);
        }
    }
}
