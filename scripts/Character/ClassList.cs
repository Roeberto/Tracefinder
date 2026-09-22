using System;
using System.Collections.Generic;

namespace Tracefinder.Character
{
    public static class ClassList
    {
        public static readonly ClassStats Fighter = new()
        {
            Name = "Fighter",
            KeyAbilityOptions = [AbilityScore.Strength, AbilityScore.Dexterity],
            HitPointsPerLevel = 10,
            TrainedSkills = [SkillName.Athletics],
            BonusSkillCount = 3,
            PerceptionProficiency = ProficiencyRank.Expert,
            FortitudeProficiency = ProficiencyRank.Expert,
            ReflexProficiency = ProficiencyRank.Expert,
            WillProficiency = ProficiencyRank.Trained,
            ClassDcProficiency = ProficiencyRank.Trained,
            UnarmoredProficiency = ProficiencyRank.Trained,
            Level1Features = ["Attack of Opportunity", "Shield Block"]
        };

        public static readonly ClassStats Wizard = new()
        {
            Name = "Wizard",
            KeyAbilityOptions = [AbilityScore.Intelligence],
            HitPointsPerLevel = 6,
            TrainedSkills = [SkillName.Arcana],
            BonusSkillCount = 2,
            PerceptionProficiency = ProficiencyRank.Trained,
            FortitudeProficiency = ProficiencyRank.Trained,
            ReflexProficiency = ProficiencyRank.Trained,
            WillProficiency = ProficiencyRank.Expert,
            ClassDcProficiency = ProficiencyRank.Trained,
            UnarmoredProficiency = ProficiencyRank.Trained,
            Level1Features = ["Arcane Spellcasting", "Arcane School", "Spellbook"]
        };

        public static readonly ClassStats Cleric = new()
        {
            Name = "Cleric",
            KeyAbilityOptions = [AbilityScore.Wisdom],
            HitPointsPerLevel = 8,
            TrainedSkills = [SkillName.Religion],
            BonusSkillCount = 2,
            PerceptionProficiency = ProficiencyRank.Trained,
            FortitudeProficiency = ProficiencyRank.Trained,
            ReflexProficiency = ProficiencyRank.Trained,
            WillProficiency = ProficiencyRank.Expert,
            ClassDcProficiency = ProficiencyRank.Trained,
            UnarmoredProficiency = ProficiencyRank.Trained,
            Level1Features = ["Divine Spellcasting", "Divine Font", "Doctrine"]
        };

        public static readonly ClassStats Rogue = new()
        {
            Name = "Rogue",
            KeyAbilityOptions = [AbilityScore.Dexterity],
            HitPointsPerLevel = 8,
            TrainedSkills = [SkillName.Stealth],
            BonusSkillCount = 7,
            PerceptionProficiency = ProficiencyRank.Expert,
            FortitudeProficiency = ProficiencyRank.Trained,
            ReflexProficiency = ProficiencyRank.Expert,
            WillProficiency = ProficiencyRank.Expert,
            ClassDcProficiency = ProficiencyRank.Trained,
            UnarmoredProficiency = ProficiencyRank.Trained,
            Level1Features = ["Rogue's Racket (Thief)", "Sneak Attack", "Surprise Attack"]
        };

        public static readonly IReadOnlyList<ClassStats> All =
        [
            Fighter, Wizard, Cleric, Rogue
        ];
    }
}
