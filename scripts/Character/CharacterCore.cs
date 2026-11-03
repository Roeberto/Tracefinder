using System;
using System.Collections.Generic;

using Tracefinder.Items;

namespace Tracefinder.Character
{
    public class CharacterCore
    {
        public const int DefaultSpeedFeet = 25;

        public int Level { get; set; } = 1; // poziom postaci
        public string Name { get; set; } = ""; // nazwa postaci
        public int Strength { get; set; } = 10;
        public int Dexterity { get; set; } = 10;
        public int Constitution { get; set; } = 10;
        public int Intelligence { get; set; } = 10;
        public int Wisdom { get; set; } = 10;
        public int Charisma { get; set; } = 10;
        public HealthTracker Health { get; set; } = new();
        public string ClassID { get; set; } = ""; // nazwa klasy postaci z ClassList
        public AbilityScore SelectedKeyAbility { get; set; }
        public List<SkillName> ExtraTrainedSkills = new(); //czy postać ma jeszcze jakieś dodatkowe wytrenowane skille
        public Dictionary<SkillName, ProficiencyRank> SkillIncreases = new(); // rangi podniesione przy awansach, ponad Trained
        public int Experience { get; set; } = 0;
        public Ancestry Ancestry { get; set; }
        public List<Feat> Feats = new();
        public List<CarriedWeapon> Inventory = new();
        public CarriedWeapon EquippedWeapon { get; set; }

        public int SpeedFeet => Ancestry?.SpeedFeet ?? DefaultSpeedFeet;

        // Połączenie zmiennej postaci z clasą AbilityScore
        public int GetScore(AbilityScore ability)
        {
            switch(ability)
            {
                case AbilityScore.Strength:
                    return Strength;
                case AbilityScore.Dexterity:
                    return Dexterity;
                case AbilityScore.Constitution:
                    return Constitution;
                case AbilityScore.Intelligence:
                    return Intelligence;
                case AbilityScore.Wisdom:
                    return Wisdom;
                case AbilityScore.Charisma:
                    return Charisma;
                default:
                    throw new ArgumentException($"Unknown ability: {ability}");
            }

        }

        // Wylicz jaki jest dodatek do AbilityScore zależnie od lvl i proficiency
        public int AbilityModifier(AbilityScore ability)
        {
            return ProficiencyMath.AbilityModifier(GetScore(ability));
        }

        public int ComputeMaxHp(ClassStats classStats)
        {
            int ancestryHp = Ancestry?.HitPoints ?? 0;
            return ancestryHp + (AbilityModifier(AbilityScore.Constitution) + classStats.HitPointsPerLevel) * Level;
        }

        public int ComputeMaxAc(ClassStats classStats)
        {
            return 10 + AbilityModifier(AbilityScore.Dexterity) + Bonus(classStats, ProficiencyStat.Unarmored);
        }

        public int ComputeClassDc(ClassStats classStats)
        {
            return 10 + AbilityModifier(SelectedKeyAbility) + Bonus(classStats, ProficiencyStat.ClassDc);
        }

        public int ComputeFortitude(ClassStats classStats)
        {
            return Bonus(classStats, ProficiencyStat.Fortitude) + AbilityModifier(AbilityScore.Constitution);
        }

        public int ComputeReflex(ClassStats classStats)
        {
            return Bonus(classStats, ProficiencyStat.Reflex) + AbilityModifier(AbilityScore.Dexterity);
        }

        public int ComputeWill(ClassStats classStats)
        {
            return Bonus(classStats, ProficiencyStat.Will) + AbilityModifier(AbilityScore.Wisdom);
        }

        public int ComputePerception(ClassStats classStats)
        {
            return Bonus(classStats, ProficiencyStat.Perception) + AbilityModifier(AbilityScore.Wisdom);
        }

        private int Bonus(ClassStats classStats, ProficiencyStat stat)
        {
            return ProficiencyMath.Bonus(classStats.RankAt(stat, Level), Level);
        }

        // sprawdza czy skill w liście classstats.trainedskills lub extratrainedskills zawiera skill wskazany w metodzie i zwraca zmienną typu proficiencyrank
        public ProficiencyRank GetSkillRank(SkillName skill ,ClassStats classStats)
		{
			bool trained = classStats.TrainedSkills.Contains(skill) || ExtraTrainedSkills.Contains(skill);
    		ProficiencyRank rank = trained ? ProficiencyRank.Trained : ProficiencyRank.Untrained;
    		ProficiencyRank increased = SkillIncreases.GetValueOrDefault(skill);
    		return increased > rank ? increased : rank;
		}

        public int ComputeSkillBonus(SkillName skill, ClassStats classStats)
        {
            ProficiencyRank rank = GetSkillRank(skill, classStats);
            int bonus = ProficiencyMath.Bonus(rank, Level);
            int bonusFromAbilityScore = AbilityModifier(SkillCatalog.KeyAbilityFor(skill));
            return bonus + bonusFromAbilityScore;
        }

        public ProficiencyRank GetWeaponRank(Equipment.Weapon weapon, ClassStats classStats)
        {
            return classStats.WeaponRankAt(weapon.Category, Level);
        }

        // Rzut bronią jest atakiem dystansowym, więc idzie ze ZRĘ nawet przy broni wręcz
        public AbilityScore AttackAbility(Equipment.Weapon weapon, bool thrown = false)
        {
            if (weapon.Kind == WeaponKind.Ranged || thrown)
                return AbilityScore.Dexterity;

            if (weapon.HasTrait<FinesseTrait>() && Dexterity > Strength)
                return AbilityScore.Dexterity;

            return AbilityScore.Strength;
        }

        public int ComputeAttackBonus(Equipment.Weapon weapon, ClassStats classStats, bool thrown = false)
        {
            int proficiencyBonus = ProficiencyMath.Bonus(GetWeaponRank(weapon, classStats), Level);
            return proficiencyBonus + AbilityModifier(AttackAbility(weapon, thrown));

        }

        public int ComputeAttackBonus(CarriedWeapon weapon, ClassStats classStats, bool thrown = false)
        {
            return ComputeAttackBonus(weapon.Template, classStats, thrown) + weapon.PotencyRune;
        }

        // Wręcz i rzut dodają SIŁ. Propulsive daje połowę dodatniej SIŁ, a ujemną w całości.
        public int ComputeDamageBonus(Equipment.Weapon weapon, bool thrown = false)
        {
            int strength = AbilityModifier(AbilityScore.Strength);

            if (weapon.Kind == WeaponKind.Melee || thrown)
                return strength;

            if (weapon.HasTrait<PropulsiveTrait>())
                return strength > 0 ? strength / 2 : strength;

            return 0;
        }

        // Dodaje doświadczenie i awansuje za każde pełne 1000 XP. Zwraca liczbę zdobytych poziomów.
        public int AddExperience(int amount, ClassStats classStats)
        {
            Experience += amount;
            int gained = 0;
            while (Experience >= Progression.ExperiencePerLevel && Level < Progression.MaxLevel)
            {
                Experience -= Progression.ExperiencePerLevel;
                Level++;
                gained++;
            }

            if (gained > 0)
            {
                int oldMax = Health.MaxHp;
                Health.SetMaxHp(ComputeMaxHp(classStats));
                Health.HealDamage(Health.MaxHp - oldMax);
            }
            return gained;
        }

        // Ustawia pełne HP. Bez tego świeża postać ma MaxHp i CurrentHp równe 0.
        public void InitializeHealth(ClassStats classStats)
        {
            Health.SetMaxHp(ComputeMaxHp(classStats));
            Health.SetToMaxHp();
        }



    }


}
