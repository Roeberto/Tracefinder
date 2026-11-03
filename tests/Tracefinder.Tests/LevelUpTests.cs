using System.IO;
using System.Linq;
using Tracefinder.Character;
using Tracefinder.Items;
using Tracefinder.World;
using Xunit;

namespace Tracefinder.Tests
{
    public class LevelUpTests
    {
        private static CharacterCore Fighter(int level = 1)
        {
            CharacterCore core = GameSession.ExampleCharacter();
            core.Level = level;
            core.InitializeHealth(ClassList.Fighter);
            return core;
        }

        [Fact]
        public void FeatSlotsShrinkWhenFeatIsTaken()
        {
            CharacterCore c = Fighter();
            Assert.Equal(1, LevelUp.AvailableFeatSlots(c, ClassList.Fighter, FeatCategory.Class));
            Feat feat = LevelUp.AvailableFeats(c, ClassList.Fighter, FeatCategory.Class)[0];
            Assert.True(LevelUp.TakeFeat(c, ClassList.Fighter, feat));
            Assert.Equal(0, LevelUp.AvailableFeatSlots(c, ClassList.Fighter, FeatCategory.Class));
            Assert.DoesNotContain(feat, LevelUp.AvailableFeats(c, ClassList.Fighter, FeatCategory.Class));
        }

        [Fact]
        public void CannotTakeFeatWithoutSlotOrFromOtherClass()
        {
            CharacterCore c = Fighter();
            Feat rogueFeat = FeatList.All.First(f => f.Source == "Rogue");
            Assert.False(LevelUp.TakeFeat(c, ClassList.Fighter, rogueFeat));

            Feat general = FeatList.All.First(f => f.Category == FeatCategory.General);
            Assert.False(LevelUp.TakeFeat(c, ClassList.Fighter, general));
            c.Level = 3;
            Assert.True(LevelUp.TakeFeat(c, ClassList.Fighter, general));
        }

        [Fact]
        public void AncestryFeatsFollowCharacterAncestry()
        {
            CharacterCore c = Fighter();
            Assert.All(LevelUp.AvailableFeats(c, ClassList.Fighter, FeatCategory.Ancestry), f => Assert.Equal("Human", f.Source));
            c.Ancestry = AncestryList.Dwarf;
            Assert.Empty(LevelUp.AvailableFeats(c, ClassList.Fighter, FeatCategory.Ancestry));
            c.Ancestry = null;
            Assert.Empty(LevelUp.AvailableFeats(c, ClassList.Fighter, FeatCategory.Ancestry));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 0)]
        [InlineData(3, 1)]
        [InlineData(5, 2)]
        public void FighterSkillIncreases(int level, int expected)
        {
            Assert.Equal(expected, LevelUp.AvailableSkillIncreases(Fighter(level), ClassList.Fighter));
        }

        [Fact]
        public void SkillIncreaseRaisesOneRankAndSpendsIt()
        {
            CharacterCore c = Fighter(3);
            Assert.True(LevelUp.IncreaseSkill(c, ClassList.Fighter, SkillName.Stealth));
            Assert.Equal(ProficiencyRank.Trained, c.GetSkillRank(SkillName.Stealth, ClassList.Fighter));
            Assert.Equal(0, LevelUp.AvailableSkillIncreases(c, ClassList.Fighter));
            Assert.False(LevelUp.IncreaseSkill(c, ClassList.Fighter, SkillName.Stealth));
        }

        [Fact]
        public void SkillRankIsCappedByLevel()
        {
            CharacterCore c = Fighter(5);
            Assert.True(LevelUp.IncreaseSkill(c, ClassList.Fighter, SkillName.Athletics));
            Assert.Equal(ProficiencyRank.Expert, c.GetSkillRank(SkillName.Athletics, ClassList.Fighter));
            Assert.False(LevelUp.CanIncreaseSkill(c, ClassList.Fighter, SkillName.Athletics));

            c.Level = 7;
            Assert.True(LevelUp.IncreaseSkill(c, ClassList.Fighter, SkillName.Athletics));
            Assert.Equal(ProficiencyRank.Master, c.GetSkillRank(SkillName.Athletics, ClassList.Fighter));
            Assert.Equal(ProficiencyRank.Legendary, LevelUp.HighestSkillRank(15));
        }

        [Fact]
        public void AttributeBoostsAreFourDifferentPerBatch()
        {
            CharacterCore c = Fighter(4);
            Assert.Equal(0, LevelUp.AvailableAttributeBoosts(c));
            Assert.False(LevelUp.Boost(c, ClassList.Fighter, AbilityScore.Wisdom));

            c.Level = 5;
            Assert.Equal(4, LevelUp.AvailableAttributeBoosts(c));
            Assert.True(LevelUp.Boost(c, ClassList.Fighter, AbilityScore.Wisdom));
            Assert.False(LevelUp.Boost(c, ClassList.Fighter, AbilityScore.Wisdom));
            Assert.Equal(14, c.Wisdom);
            Assert.True(LevelUp.Boost(c, ClassList.Fighter, AbilityScore.Dexterity));
            Assert.True(LevelUp.Boost(c, ClassList.Fighter, AbilityScore.Intelligence));
            Assert.True(LevelUp.Boost(c, ClassList.Fighter, AbilityScore.Charisma));
            Assert.Equal(0, LevelUp.AvailableAttributeBoosts(c));

            c.Level = 10;
            Assert.True(LevelUp.Boost(c, ClassList.Fighter, AbilityScore.Wisdom));
        }

        [Fact]
        public void BoostIsPartialFromEighteen()
        {
            CharacterCore c = Fighter(5);
            Assert.Equal(18, c.Strength);
            Assert.True(LevelUp.Boost(c, ClassList.Fighter, AbilityScore.Strength));
            Assert.Equal(19, c.Strength);
            Assert.Equal(4, c.AbilityModifier(AbilityScore.Strength));
        }

        [Fact]
        public void ConstitutionBoostRaisesHitPoints()
        {
            CharacterCore c = Fighter(5);
            int before = c.Health.MaxHp;
            Assert.True(LevelUp.Boost(c, ClassList.Fighter, AbilityScore.Constitution));
            Assert.Equal(before + 5, c.Health.MaxHp);
            Assert.Equal(before + 5, c.Health.CurrentHp);
        }

        [Fact]
        public void PendingChoicesCountsOnlyWhatCanBeChosen()
        {
            CharacterCore c = Fighter();
            Assert.Equal(2, LevelUp.PendingChoices(c, ClassList.Fighter));
            c.Ancestry = AncestryList.Dwarf;
            Assert.Equal(1, LevelUp.PendingChoices(c, ClassList.Fighter));
        }

        [Fact]
        public void ChoicesSurviveSaveAndLoad()
        {
            string dir = Path.Combine(Path.GetTempPath(), "tracefinder-tests-" + System.Guid.NewGuid());
            try
            {
                CharacterCore c = Fighter(5);
                c.Name = "Awans";
                LevelUp.IncreaseSkill(c, ClassList.Fighter, SkillName.Athletics);
                LevelUp.Boost(c, ClassList.Fighter, AbilityScore.Wisdom);

                CharacterRepository repo = new(dir);
                Assert.False(repo.Exists("Awans"));
                repo.Save(c);
                Assert.True(repo.Exists("Awans"));

                CharacterCore loaded = repo.Load("Awans");
                Assert.Equal(5, loaded.Level);
                Assert.Equal(14, loaded.Wisdom);
                Assert.Equal(ProficiencyRank.Expert, loaded.GetSkillRank(SkillName.Athletics, ClassList.Fighter));
                Assert.Equal(1, LevelUp.AvailableSkillIncreases(loaded, ClassList.Fighter));
                Assert.Equal(3, LevelUp.AvailableAttributeBoosts(loaded));
                Assert.False(LevelUp.CanBoost(loaded, AbilityScore.Wisdom));
            }
            finally
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void OldFormatSaveStillLoads()
        {
            string dir = Path.Combine(Path.GetTempPath(), "tracefinder-tests-" + System.Guid.NewGuid());
            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "stara.json"), "{\"Name\":\"stara\",\"Level\":1,\"Strength\":12,\"ClassID\":\"Fighter\"}");
                CharacterCore loaded = new CharacterRepository(dir).Load("stara");
                Assert.NotNull(loaded);
                Assert.Equal(12, loaded.Strength);
                Assert.Empty(loaded.SkillIncreases);
                Assert.Empty(loaded.AttributeBoosts);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
