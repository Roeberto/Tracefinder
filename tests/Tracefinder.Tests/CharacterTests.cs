using Tracefinder.Character;
using Tracefinder.Items;
using Xunit;

namespace Tracefinder.Tests
{
    public class CharacterTests
    {
        private static CharacterCore Fighter() => new()
        {
            Strength = 18, Dexterity = 14, Constitution = 16, Intelligence = 10, Wisdom = 12, Charisma = 8,
            SelectedKeyAbility = AbilityScore.Strength
        };

        [Fact]
        public void FighterAtLevelOneMatchesHandCalculatedSheet()
        {
            CharacterCore c = Fighter();
            ClassStats s = ClassList.Fighter;
            Assert.Equal(13, c.ComputeMaxHp(s));
            Assert.Equal(15, c.ComputeMaxAc(s));
            Assert.Equal(17, c.ComputeClassDc(s));
            Assert.Equal(8, c.ComputeFortitude(s));
            Assert.Equal(7, c.ComputeReflex(s));
            Assert.Equal(4, c.ComputeWill(s));
            Assert.Equal(6, c.ComputePerception(s));
            Assert.Equal(7, c.ComputeSkillBonus(SkillName.Athletics, s));
            Assert.Equal(2, c.ComputeSkillBonus(SkillName.Stealth, s));
        }

        [Fact]
        public void AncestryAddsHitPointsAndSetsSpeed()
        {
            CharacterCore c = Fighter();
            Assert.Equal(25, c.SpeedFeet);
            c.Ancestry = AncestryList.Dwarf;
            Assert.Equal(23, c.ComputeMaxHp(ClassList.Fighter));
            Assert.Equal(20, c.SpeedFeet);
        }

        [Fact]
        public void HitPointsScaleWithLevel()
        {
            CharacterCore c = Fighter();
            c.Ancestry = AncestryList.Human;
            c.Level = 5;
            Assert.Equal(8 + (10 + 3) * 5, c.ComputeMaxHp(ClassList.Fighter));
        }

        [Fact]
        public void ClassRankUpsApplyFromTheirLevel()
        {
            Assert.Equal(ProficiencyRank.Trained, ClassList.Fighter.RankAt(ProficiencyStat.Will, 2));
            Assert.Equal(ProficiencyRank.Expert, ClassList.Fighter.RankAt(ProficiencyStat.Will, 3));
            Assert.Equal(ProficiencyRank.Legendary, ClassList.Fighter.WeaponRankAt(WeaponCategory.Martial, 19));
            Assert.Equal(ProficiencyRank.Legendary, ClassList.Rogue.RankAt(ProficiencyStat.Reflex, 13));
        }

        [Fact]
        public void RogueIsTrainedInMartialWeapons()
        {
            CharacterCore c = new() { Dexterity = 18 };
            Assert.Equal(ProficiencyRank.Trained, c.GetWeaponRank(WeaponList.Rapier, ClassList.Rogue));
            Assert.Equal(7, c.ComputeAttackBonus(WeaponList.Rapier, ClassList.Rogue));
        }

        [Fact]
        public void WizardIsUntrainedInMartialWeapons()
        {
            CharacterCore c = new() { Strength = 10 };
            Assert.Equal(0, c.ComputeAttackBonus(WeaponList.Longsword, ClassList.Wizard));
        }

        [Fact]
        public void AttackBonusAddsPotencyRune()
        {
            CharacterCore c = Fighter();
            CarriedWeapon sword = new(WeaponList.Longsword) { PotencyRune = 1 };
            Assert.Equal(10, c.ComputeAttackBonus(sword, ClassList.Fighter));
        }

        [Fact]
        public void FinesseUsesHigherOfStrengthAndDexterity()
        {
            CharacterCore c = new() { Strength = 10, Dexterity = 16 };
            Assert.Equal(AbilityScore.Dexterity, c.AttackAbility(WeaponList.Rapier));
            Assert.Equal(AbilityScore.Strength, c.AttackAbility(WeaponList.Longsword));
        }

        [Fact]
        public void ThrownAttackUsesDexterityButDamageUsesStrength()
        {
            CharacterCore c = new() { Strength = 16, Dexterity = 12 };
            Assert.Equal(AbilityScore.Dexterity, c.AttackAbility(WeaponList.Dagger, thrown: true));
            Assert.Equal(3, c.ComputeDamageBonus(WeaponList.Dagger, thrown: true));
        }

        [Fact]
        public void RangedWeaponAddsNoStrengthToDamage()
        {
            CharacterCore c = new() { Strength = 18 };
            Assert.Equal(0, c.ComputeDamageBonus(WeaponList.Shortbow));
        }

        [Fact]
        public void ExperienceLevelsUpAndRaisesHitPoints()
        {
            CharacterCore c = Fighter();
            c.InitializeHealth(ClassList.Fighter);
            Assert.Equal(13, c.Health.CurrentHp);

            int gained = c.AddExperience(2300, ClassList.Fighter);

            Assert.Equal(2, gained);
            Assert.Equal(3, c.Level);
            Assert.Equal(300, c.Experience);
            Assert.Equal(39, c.Health.MaxHp);
            Assert.Equal(39, c.Health.CurrentHp);
        }

        [Fact]
        public void SkillIncreaseRaisesRankAboveTrained()
        {
            CharacterCore c = new() { Strength = 10, Level = 3 };
            c.SkillIncreases[SkillName.Athletics] = ProficiencyRank.Expert;
            Assert.Equal(ProficiencyRank.Expert, c.GetSkillRank(SkillName.Athletics, ClassList.Fighter));
            Assert.Equal(7, c.ComputeSkillBonus(SkillName.Athletics, ClassList.Fighter));
        }

        [Theory]
        [InlineData(1, FeatCategory.Class, 1)]
        [InlineData(2, FeatCategory.Class, 2)]
        [InlineData(1, FeatCategory.Ancestry, 1)]
        [InlineData(5, FeatCategory.Ancestry, 2)]
        [InlineData(3, FeatCategory.General, 1)]
        [InlineData(4, FeatCategory.Skill, 2)]
        public void FighterFeatSlots(int level, FeatCategory category, int expected)
        {
            Assert.Equal(expected, Progression.FeatSlots(category, level, ClassList.Fighter));
        }

        [Fact]
        public void WizardGetsNoClassFeatAtFirstLevelAndRogueGetsSkillFeatEveryLevel()
        {
            Assert.Equal(0, Progression.FeatSlots(FeatCategory.Class, 1, ClassList.Wizard));
            Assert.Equal(3, Progression.FeatSlots(FeatCategory.Skill, 3, ClassList.Rogue));
            Assert.True(Progression.GrantsSkillIncrease(2, ClassList.Rogue));
            Assert.False(Progression.GrantsSkillIncrease(2, ClassList.Fighter));
            Assert.True(Progression.GrantsSkillIncrease(3, ClassList.Fighter));
        }

        [Fact]
        public void WeaponGroupHasAllSixteenValues()
        {
            Assert.Equal(16, System.Enum.GetValues<WeaponGroup>().Length);
        }

        [Fact]
        public void BothBowsHaveDeadlyD10()
        {
            Assert.Contains(WeaponList.Longbow.TraitList, t => t is DeadlyTrait d && d.Die == Gameplay.Dice.DiceSize.D10);
            Assert.Contains(WeaponList.Shortbow.TraitList, t => t is DeadlyTrait d && d.Die == Gameplay.Dice.DiceSize.D10);
        }
    }
}
