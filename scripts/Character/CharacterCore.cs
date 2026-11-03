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
        public int SkillIncreasesUsed { get; set; } = 0;
        public List<AbilityScore> AttributeBoosts = new(); // wydane przyrosty cech, w kolejności wyboru
        public int Experience { get; set; } = 0;
        public Ancestry Ancestry { get; set; }
        public List<Feat> Feats = new();
        public List<CarriedWeapon> Inventory = new();
        public CarriedWeapon EquippedWeapon { get; set; }
        public Armor EquippedArmor { get; set; }
        public List<Armor> OwnedArmors = new();
        public bool HasShield { get; set; }
        public int ShieldHp { get; set; } = ShopCatalog.ShieldMaxHp;
        public Dictionary<string, int> Potions = new(); // nazwa mikstury i ile sztuk
        public int Silver { get; set; }
        public Background Background { get; set; }
        public Dictionary<int, int> SpellSlotsUsed = new(); // ranga i ile slotów zużyto
        public int FontSlotsUsed { get; set; }

        public int SpeedFeet => (Ancestry?.SpeedFeet ?? DefaultSpeedFeet) + (HasFeat(FeatList.Fleet) ? 5 : 0);

        // Tarcza poniżej progu jest uszkodzona i nie daje premii, dopóki kowal w osadzie jej nie naprawi
        public bool ShieldUsable => HasShield && ShieldHp > ShopCatalog.ShieldBrokenThreshold;

        public bool HasFeat(Feat feat)
        {
            return Feats.Exists(f => f.Name == feat.Name);
        }

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

        public void SetScore(AbilityScore ability, int value)
        {
            switch (ability)
            {
                case AbilityScore.Strength:
                    Strength = value;
                    break;
                case AbilityScore.Dexterity:
                    Dexterity = value;
                    break;
                case AbilityScore.Constitution:
                    Constitution = value;
                    break;
                case AbilityScore.Intelligence:
                    Intelligence = value;
                    break;
                case AbilityScore.Wisdom:
                    Wisdom = value;
                    break;
                case AbilityScore.Charisma:
                    Charisma = value;
                    break;
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
            int toughness = HasFeat(FeatList.Toughness) ? Level : 0;
            return ancestryHp + (AbilityModifier(AbilityScore.Constitution) + classStats.HitPointsPerLevel) * Level + toughness;
        }

        public int ComputeMaxAc(ClassStats classStats)
        {
            int dexterity = AbilityModifier(AbilityScore.Dexterity);
            if (EquippedArmor == null)
                return 10 + dexterity + Bonus(classStats, ProficiencyStat.Unarmored);

            int capped = Math.Min(dexterity, EquippedArmor.DexCap);
            ProficiencyRank rank = classStats.ArmorRankAt(EquippedArmor.Category, Level);
            return 10 + capped + ProficiencyMath.Bonus(rank, Level) + EquippedArmor.AcBonus;
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

        public int ComputeInitiative(ClassStats classStats)
        {
            return ComputePerception(classStats) + (HasFeat(FeatList.IncredibleInitiative) ? 2 : 0);
        }

        public int ComputeSpellAttack(ClassStats classStats)
        {
            return Bonus(classStats, ProficiencyStat.Spellcasting) + AbilityModifier(SelectedKeyAbility);
        }

        public int ComputeSpellDc(ClassStats classStats)
        {
            return 10 + ComputeSpellAttack(classStats);
        }

        public int SneakAttackDice(ClassStats classStats)
        {
            if (classStats.Name != ClassList.Rogue.Name)
                return 0;
            if (Level >= 17)
                return 4;
            if (Level >= 11)
                return 3;
            return Level >= 5 ? 2 : 1;
        }

        public int FreeSlots(int rank)
        {
            return Math.Max(SpellSlots.SlotsAt(Level, rank) - SpellSlotsUsed.GetValueOrDefault(rank), 0);
        }

        public int FreeFontSlots(ClassStats classStats)
        {
            if (classStats.Name != ClassList.Cleric.Name)
                return 0;
            return Math.Max(SpellSlots.FontSlots(Level) - FontSlotsUsed, 0);
        }

        // Najwyższa wolna ranga dla zaklęcia. Zero oznacza, że nie ma czym rzucić.
        public int CastingRank(Spell spell, ClassStats classStats)
        {
            List<int> ranks = CastingRanks(spell, classStats);
            return ranks.Count > 0 ? ranks[0] : 0;
        }

        // Rangi, na których można teraz rzucić zaklęcie, od najwyższej
        public List<int> CastingRanks(Spell spell, ClassStats classStats)
        {
            List<int> ranks = new();
            if (spell.IsCantrip)
            {
                ranks.Add(SpellSlots.CantripRank(Level));
                return ranks;
            }
            int highest = SpellSlots.HighestRank(Level);
            for (int rank = highest; rank >= spell.BaseRank; rank--)
            {
                bool font = rank == highest && spell.Name == SpellList.Heal.Name && FreeFontSlots(classStats) > 0;
                if (font || FreeSlots(rank) > 0)
                    ranks.Add(rank);
            }
            return ranks;
        }

        public void ConsumeSlot(Spell spell, int rank, ClassStats classStats)
        {
            if (spell.IsCantrip)
                return;
            if (rank == SpellSlots.HighestRank(Level) && spell.Name == SpellList.Heal.Name && FreeFontSlots(classStats) > 0)
            {
                FontSlotsUsed++;
                return;
            }
            SpellSlotsUsed[rank] = SpellSlotsUsed.GetValueOrDefault(rank) + 1;
        }

        // Pełny odpoczynek: HP, stany i sloty zaklęć
        public void Rest()
        {
            Health.FullRest();
            SpellSlotsUsed.Clear();
            FontSlotsUsed = 0;
        }

        private int Bonus(ClassStats classStats, ProficiencyStat stat)
        {
            return ProficiencyMath.Bonus(classStats.RankAt(stat, Level), Level);
        }

        // sprawdza czy skill w liście classstats.trainedskills lub extratrainedskills zawiera skill wskazany w metodzie i zwraca zmienną typu proficiencyrank
        public ProficiencyRank GetSkillRank(SkillName skill ,ClassStats classStats)
		{
			bool trained = classStats.TrainedSkills.Contains(skill) || ExtraTrainedSkills.Contains(skill) || Background?.TrainedSkill == skill;
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
                RefreshHealth(classStats);
            return gained;
        }

        // Po zmianie poziomu albo Kondycji: nowe maksimum, a różnica dochodzi do aktualnego HP
        public void RefreshHealth(ClassStats classStats)
        {
            int oldMax = Health.MaxHp;
            Health.SetMaxHp(ComputeMaxHp(classStats));
            Health.HealDamage(Health.MaxHp - oldMax);
        }

        // Ustawia pełne HP. Bez tego świeża postać ma MaxHp i CurrentHp równe 0.
        public void InitializeHealth(ClassStats classStats)
        {
            Health.SetMaxHp(ComputeMaxHp(classStats));
            Health.SetToMaxHp();
        }



    }


}
