using System;
using System.Collections.Generic;
using Tracefinder.Items;

namespace Tracefinder.Character
{
    // Dane z Player Core, sprawdzone w Archives of Nethys.
    // Uproszczenia: mistrzostwo Wojownika na poziomie 5 obejmuje wszystkie grupy broni zamiast jednej wybranej,
    // Wojownik ma na stałe Athletics, a Zbir cechę kluczową Dexterity. Pancerze i zaklęcia nie są modelowane.
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
            WeaponProficiencies = new()
            {
                [WeaponCategory.Simple] = ProficiencyRank.Expert,
                [WeaponCategory.Martial] = ProficiencyRank.Expert,
                [WeaponCategory.Advanced] = ProficiencyRank.Trained,
                [WeaponCategory.Unarmed] = ProficiencyRank.Expert,
            },
            RankUps =
            [
                new(3, ProficiencyStat.Will, ProficiencyRank.Expert),
                new(5, ProficiencyStat.SimpleWeapons, ProficiencyRank.Master),
                new(5, ProficiencyStat.MartialWeapons, ProficiencyRank.Master),
                new(5, ProficiencyStat.UnarmedAttacks, ProficiencyRank.Master),
                new(5, ProficiencyStat.AdvancedWeapons, ProficiencyRank.Expert),
                new(7, ProficiencyStat.Perception, ProficiencyRank.Master),
                new(9, ProficiencyStat.Fortitude, ProficiencyRank.Master),
                new(11, ProficiencyStat.Unarmored, ProficiencyRank.Expert),
                new(11, ProficiencyStat.LightArmor, ProficiencyRank.Expert),
                new(11, ProficiencyStat.MediumArmor, ProficiencyRank.Expert),
                new(11, ProficiencyStat.HeavyArmor, ProficiencyRank.Expert),
                new(11, ProficiencyStat.ClassDc, ProficiencyRank.Expert),
                new(15, ProficiencyStat.Reflex, ProficiencyRank.Master),
                new(17, ProficiencyStat.Unarmored, ProficiencyRank.Master),
                new(17, ProficiencyStat.LightArmor, ProficiencyRank.Master),
                new(17, ProficiencyStat.MediumArmor, ProficiencyRank.Master),
                new(17, ProficiencyStat.HeavyArmor, ProficiencyRank.Master),
                new(19, ProficiencyStat.SimpleWeapons, ProficiencyRank.Legendary),
                new(19, ProficiencyStat.MartialWeapons, ProficiencyRank.Legendary),
                new(19, ProficiencyStat.UnarmedAttacks, ProficiencyRank.Legendary),
                new(19, ProficiencyStat.AdvancedWeapons, ProficiencyRank.Master),
                new(19, ProficiencyStat.ClassDc, ProficiencyRank.Master),
            ],
            ClassFeatAtFirstLevel = true,
            LightArmorProficiency = ProficiencyRank.Trained,
            MediumArmorProficiency = ProficiencyRank.Trained,
            HeavyArmorProficiency = ProficiencyRank.Trained,
            Level1Features = ["Reactive Strike", "Shield Block"]
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
            WeaponProficiencies = new()
            {
                [WeaponCategory.Simple] = ProficiencyRank.Trained,
                [WeaponCategory.Martial] = ProficiencyRank.Untrained,
                [WeaponCategory.Advanced] = ProficiencyRank.Untrained,
                [WeaponCategory.Unarmed] = ProficiencyRank.Trained,
            },
            RankUps =
            [
                new(5, ProficiencyStat.Reflex, ProficiencyRank.Expert),
                new(7, ProficiencyStat.Spellcasting, ProficiencyRank.Expert),
                new(9, ProficiencyStat.Fortitude, ProficiencyRank.Expert),
                new(11, ProficiencyStat.Perception, ProficiencyRank.Expert),
                new(15, ProficiencyStat.Spellcasting, ProficiencyRank.Master),
                new(19, ProficiencyStat.Spellcasting, ProficiencyRank.Legendary),
                new(11, ProficiencyStat.SimpleWeapons, ProficiencyRank.Expert),
                new(11, ProficiencyStat.UnarmedAttacks, ProficiencyRank.Expert),
                new(13, ProficiencyStat.Unarmored, ProficiencyRank.Expert),
                new(17, ProficiencyStat.Will, ProficiencyRank.Master),
            ],
            SpellcastingProficiency = ProficiencyRank.Trained,
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
            WeaponProficiencies = new()
            {
                [WeaponCategory.Simple] = ProficiencyRank.Trained,
                [WeaponCategory.Martial] = ProficiencyRank.Untrained,
                [WeaponCategory.Advanced] = ProficiencyRank.Untrained,
                [WeaponCategory.Unarmed] = ProficiencyRank.Trained,
            },
            RankUps =
            [
                new(5, ProficiencyStat.Perception, ProficiencyRank.Expert),
                // Rzucanie zaklęć Kapłana z pamięci modelu, tabela klasy tego nie podaje: NIEZWERYFIKOWANE
                new(7, ProficiencyStat.Spellcasting, ProficiencyRank.Expert),
                new(15, ProficiencyStat.Spellcasting, ProficiencyRank.Master),
                new(19, ProficiencyStat.Spellcasting, ProficiencyRank.Legendary),
                new(9, ProficiencyStat.Will, ProficiencyRank.Master),
                new(11, ProficiencyStat.Reflex, ProficiencyRank.Expert),
                new(13, ProficiencyStat.Unarmored, ProficiencyRank.Expert),
            ],
            SpellcastingProficiency = ProficiencyRank.Trained,
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
            WeaponProficiencies = new()
            {
                [WeaponCategory.Simple] = ProficiencyRank.Trained,
                [WeaponCategory.Martial] = ProficiencyRank.Trained,
                [WeaponCategory.Advanced] = ProficiencyRank.Untrained,
                [WeaponCategory.Unarmed] = ProficiencyRank.Trained,
            },
            RankUps =
            [
                new(5, ProficiencyStat.SimpleWeapons, ProficiencyRank.Expert),
                new(5, ProficiencyStat.MartialWeapons, ProficiencyRank.Expert),
                new(5, ProficiencyStat.UnarmedAttacks, ProficiencyRank.Expert),
                new(7, ProficiencyStat.Reflex, ProficiencyRank.Master),
                new(7, ProficiencyStat.Perception, ProficiencyRank.Master),
                new(9, ProficiencyStat.Fortitude, ProficiencyRank.Expert),
                new(11, ProficiencyStat.ClassDc, ProficiencyRank.Expert),
                new(13, ProficiencyStat.Reflex, ProficiencyRank.Legendary),
                new(13, ProficiencyStat.Perception, ProficiencyRank.Legendary),
                new(13, ProficiencyStat.Unarmored, ProficiencyRank.Expert),
                new(13, ProficiencyStat.LightArmor, ProficiencyRank.Expert),
                new(13, ProficiencyStat.SimpleWeapons, ProficiencyRank.Master),
                new(13, ProficiencyStat.MartialWeapons, ProficiencyRank.Master),
                new(13, ProficiencyStat.UnarmedAttacks, ProficiencyRank.Master),
                new(17, ProficiencyStat.Will, ProficiencyRank.Master),
                new(19, ProficiencyStat.Unarmored, ProficiencyRank.Master),
                new(19, ProficiencyStat.LightArmor, ProficiencyRank.Master),
                new(19, ProficiencyStat.ClassDc, ProficiencyRank.Master),
            ],
            LightArmorProficiency = ProficiencyRank.Trained,
            ClassFeatAtFirstLevel = true,
            SkillFeatEveryLevel = true,
            SkillIncreaseEveryLevel = true,
            Level1Features = ["Rogue's Racket (Thief)", "Sneak Attack", "Surprise Attack"]
        };

        public static readonly IReadOnlyList<ClassStats> All =
        [
            Fighter, Wizard, Cleric, Rogue
        ];

        public static ClassStats ByName(string name)
        {
            foreach (ClassStats classStats in All)
            {
                if (classStats.Name == name)
                    return classStats;
            }
            return null;
        }
    }
}
