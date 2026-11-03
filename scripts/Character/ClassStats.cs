using System;
using System.Collections.Generic;
using Tracefinder.Items;

namespace Tracefinder.Character
{
    public enum ProficiencyStat
    {
        Perception,
        Fortitude,
        Reflex,
        Will,
        ClassDc,
        Unarmored,
        SimpleWeapons,
        MartialWeapons,
        AdvancedWeapons,
        UnarmedAttacks
    }

    // Od tego poziomu dana statystyka ma podaną rangę
    public record RankUp(int Level, ProficiencyStat Stat, ProficiencyRank Rank);

    public class ClassStats
    {
        public string Name { get; set; } = ""; // nazwa klasy postaci
        public List<AbilityScore> KeyAbilityOptions { get; set; } = new(); //
        public int HitPointsPerLevel { get; set; } // ile hp na poziom ma postać
        public List<SkillName> TrainedSkills { get; set; } = new(); // Jakie skille typu medicine albo athelics jest wytrenowana
        public int BonusSkillCount { get; set; } // ile dodatkowych skillow moze postac sobie dobrac
        public ProficiencyRank PerceptionProficiency { get; set; }
        public ProficiencyRank FortitudeProficiency { get; set; }
        public ProficiencyRank ReflexProficiency { get; set; }
        public ProficiencyRank WillProficiency { get; set; }
        public ProficiencyRank ClassDcProficiency { get; set; }
        public ProficiencyRank UnarmoredProficiency { get; set; }
        public List<string> Level1Features { get; set; } = new();
        public Dictionary<WeaponCategory, ProficiencyRank> WeaponProficiencies { get; set; } = new();
        public List<RankUp> RankUps { get; set; } = new();
        public bool ClassFeatAtFirstLevel { get; set; }
        public bool SkillFeatEveryLevel { get; set; }
        public bool SkillIncreaseEveryLevel { get; set; }

        public ProficiencyRank RankAt(ProficiencyStat stat, int level)
        {
            ProficiencyRank rank = BaseRank(stat);
            foreach (RankUp up in RankUps)
            {
                if (up.Stat == stat && up.Level <= level && up.Rank > rank)
                    rank = up.Rank;
            }
            return rank;
        }

        public ProficiencyRank WeaponRankAt(WeaponCategory category, int level)
        {
            return RankAt(StatFor(category), level);
        }

        private ProficiencyRank BaseRank(ProficiencyStat stat)
        {
            switch (stat)
            {
                case ProficiencyStat.Perception:
                    return PerceptionProficiency;
                case ProficiencyStat.Fortitude:
                    return FortitudeProficiency;
                case ProficiencyStat.Reflex:
                    return ReflexProficiency;
                case ProficiencyStat.Will:
                    return WillProficiency;
                case ProficiencyStat.ClassDc:
                    return ClassDcProficiency;
                case ProficiencyStat.Unarmored:
                    return UnarmoredProficiency;
                case ProficiencyStat.SimpleWeapons:
                    return WeaponProficiencies.GetValueOrDefault(WeaponCategory.Simple);
                case ProficiencyStat.MartialWeapons:
                    return WeaponProficiencies.GetValueOrDefault(WeaponCategory.Martial);
                case ProficiencyStat.AdvancedWeapons:
                    return WeaponProficiencies.GetValueOrDefault(WeaponCategory.Advanced);
                case ProficiencyStat.UnarmedAttacks:
                    return WeaponProficiencies.GetValueOrDefault(WeaponCategory.Unarmed);
                default:
                    throw new ArgumentException($"Unknown stat: {stat}");
            }
        }

        private static ProficiencyStat StatFor(WeaponCategory category)
        {
            switch (category)
            {
                case WeaponCategory.Simple:
                    return ProficiencyStat.SimpleWeapons;
                case WeaponCategory.Martial:
                    return ProficiencyStat.MartialWeapons;
                case WeaponCategory.Advanced:
                    return ProficiencyStat.AdvancedWeapons;
                case WeaponCategory.Unarmed:
                    return ProficiencyStat.UnarmedAttacks;
                default:
                    throw new ArgumentException($"Unknown category: {category}");
            }
        }
    }
}
