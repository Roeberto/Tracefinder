using System;
using System.Collections.Generic;
using System.Linq;

namespace Tracefinder.Character
{
    // Wybory dostępne przy awansie. Liczy, co postaci się należy z poziomu, i odejmuje to, co już wydała.
    public static class LevelUp
    {
        public const int BoostsPerAttributeLevel = 4;
        private const int FullBoostScoreLimit = 18;
        private const int MasterSkillLevel = 7;
        private const int LegendarySkillLevel = 15;

        public static int AvailableFeatSlots(CharacterCore core, ClassStats classStats, FeatCategory category)
        {
            int taken = core.Feats.Count(f => f.Category == category);
            int bonus = 0;
            if (category == FeatCategory.Class && core.HasFeat(FeatList.NaturalAmbition))
                bonus = 1;
            if (category == FeatCategory.General && core.HasFeat(FeatList.GeneralTraining))
                bonus = 1;
            return Math.Max(Progression.FeatSlots(category, core.Level, classStats) + bonus - taken, 0);
        }

        public static List<Feat> AvailableFeats(CharacterCore core, ClassStats classStats, FeatCategory category)
        {
            string source = category switch
            {
                FeatCategory.Ancestry => core.Ancestry?.Name,
                FeatCategory.Class => classStats.Name,
                FeatCategory.Skill => "Skill",
                _ => "General"
            };

            return FeatList.All
                .Where(f => f.Category == category && f.Source == source && f.Level <= core.Level)
                .Where(f => core.Feats.All(owned => owned.Name != f.Name))
                .ToList();
        }

        public static bool TakeFeat(CharacterCore core, ClassStats classStats, Feat feat)
        {
            if (AvailableFeatSlots(core, classStats, feat.Category) <= 0)
                return false;
            if (!AvailableFeats(core, classStats, feat.Category).Contains(feat))
                return false;

            core.Feats.Add(feat);
            return true;
        }

        public static int AvailableSkillIncreases(CharacterCore core, ClassStats classStats)
        {
            int granted = 0;
            for (int level = 1; level <= core.Level; level++)
            {
                if (Progression.GrantsSkillIncrease(level, classStats))
                    granted++;
            }
            return Math.Max(granted - core.SkillIncreasesUsed, 0);
        }

        public static ProficiencyRank HighestSkillRank(int level)
        {
            if (level >= LegendarySkillLevel)
                return ProficiencyRank.Legendary;
            if (level >= MasterSkillLevel)
                return ProficiencyRank.Master;
            return ProficiencyRank.Expert;
        }

        public static bool CanIncreaseSkill(CharacterCore core, ClassStats classStats, SkillName skill)
        {
            return AvailableSkillIncreases(core, classStats) > 0
                && core.GetSkillRank(skill, classStats) < HighestSkillRank(core.Level);
        }

        public static bool IncreaseSkill(CharacterCore core, ClassStats classStats, SkillName skill)
        {
            if (!CanIncreaseSkill(core, classStats, skill))
                return false;

            core.SkillIncreases[skill] = core.GetSkillRank(skill, classStats) + 1;
            core.SkillIncreasesUsed++;
            return true;
        }

        public static int AvailableAttributeBoosts(CharacterCore core)
        {
            int granted = 0;
            for (int level = 1; level <= core.Level; level++)
            {
                if (Progression.GrantsAttributeBoosts(level))
                    granted += BoostsPerAttributeLevel;
            }
            return Math.Max(granted - core.AttributeBoosts.Count, 0);
        }

        // W jednej paczce czterech przyrostów każda cecha może być podniesiona tylko raz
        public static bool CanBoost(CharacterCore core, AbilityScore ability)
        {
            if (AvailableAttributeBoosts(core) <= 0)
                return false;

            int inCurrentBatch = core.AttributeBoosts.Count % BoostsPerAttributeLevel;
            return !core.AttributeBoosts.TakeLast(inCurrentBatch).Contains(ability);
        }

        public static bool Boost(CharacterCore core, ClassStats classStats, AbilityScore ability)
        {
            if (!CanBoost(core, ability))
                return false;

            int score = core.GetScore(ability);
            core.SetScore(ability, score + (score >= FullBoostScoreLimit ? 1 : 2));
            core.AttributeBoosts.Add(ability);
            core.RefreshHealth(classStats);
            return true;
        }

        public static int PendingChoices(CharacterCore core, ClassStats classStats)
        {
            int total = AvailableSkillIncreases(core, classStats) + AvailableAttributeBoosts(core);
            foreach (FeatCategory category in Enum.GetValues<FeatCategory>())
            {
                // Pusty slot liczy się tylko wtedy, gdy jest z czego wybierać
                total += Math.Min(AvailableFeatSlots(core, classStats, category), AvailableFeats(core, classStats, category).Count);
            }
            return total;
        }
    }
}
