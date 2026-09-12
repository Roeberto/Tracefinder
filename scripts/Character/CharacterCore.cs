using System;
using System.Collections.Generic;
using System.Threading.Channels;

namespace Tracefinder.Character
{
    public class CharacterCore
    {
        public int Level { get; set; } = 1; // poziom postaci
        public string Name { get; set; } = ""; // nazwa postaci
        public int Strength { get; set; } = 10; 
        public int Dexterity { get; set; } = 10;
        public int Constitution { get; set; } = 10;
        public int Intelligence { get; set; } = 10;
        public int Wisdom { get; set; } = 10;
        public int Charisma { get; set; } = 10;
        public string ClassID { get; set; } = ""; // ID klasy postaci np. wojownik = 0
        public AbilityScore SelectedKeyAbility { get; set; } 
        public List<SkillName> ExtraTrainedSkills = new(); //czy postać ma jeszcze jakieś dodatkowe wytrenowane skille


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
            return AbilityModifier(AbilityScore.Constitution) + classStats.HitPointsPerLevel;
        }

        public int ComputeMaxAc(ClassStats classStats)
        {
            return 10 + AbilityModifier(AbilityScore.Dexterity) + ProficiencyMath.Bonus(classStats.UnarmoredProficiency, Level);
        }

        public int ComputeMaxDc(ClassStats classStats)
        {
            return 10 + AbilityModifier(SelectedKeyAbility) + ProficiencyMath.Bonus(classStats.ClassDcProficiency, Level);
        }

        public int ComputeFortitude(ClassStats classStats)
        {
            return ProficiencyMath.Bonus(classStats.FortitudeProficiency, Level) + AbilityModifier(AbilityScore.Constitution);
        }

        public int ComputeReflex(ClassStats classStats)
        {
            return ProficiencyMath.Bonus(classStats.ReflexProficiency, Level) + AbilityModifier(AbilityScore.Dexterity);
        }

        public int ComputeWill(ClassStats classStats)
        {
            return ProficiencyMath.Bonus(classStats.WillProficiency, Level) + AbilityModifier(AbilityScore.Wisdom);
        }

        public int ComputePerception(ClassStats classStats)
        {
            return ProficiencyMath.Bonus(classStats.PerceptionProficiency, Level) + AbilityModifier(AbilityScore.Wisdom);
        }

        // sprawdza czy skill w liście classstats.trainedskills lub extratrainedskills zawiera skill wskazany w metodzie i zwraca zmienną typu proficiencyrank, !!!na razie tylko trained i untrained
        public ProficiencyRank GetSkillRank(SkillName skill ,ClassStats classStats)  
		{	
			bool trained = classStats.TrainedSkills.Contains(skill) || ExtraTrainedSkills.Contains(skill);
    		return trained ? ProficiencyRank.Trained : ProficiencyRank.Untrained;
		}

        public int ComputeSkillBonus(SkillName skill, ClassStats classStats)
        {
            ProficiencyRank rank = GetSkillRank(skill, classStats);
            int bonus = ProficiencyMath.Bonus(rank, Level);
            int bonusFromAbilityScore = AbilityModifier(SkillCatalog.KeyAbilityFor(skill));
            return bonus + bonusFromAbilityScore;
        }
        





    }


}