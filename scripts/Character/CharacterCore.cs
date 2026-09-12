using System;
using System.Collections.Generic;

namespace Tracefinder.Character
{
    public class CharacterCore
    {
        public int Level { get; set; } = 1;
        public string Name { get; set; } = "";
        public int Strength { get; set; } = 10;
        public int Dexterity { get; set; } = 10;
        public int Constitution { get; set; } = 10;
        public int Intelligence { get; set; } = 10;
        public int Wisdom { get; set; } = 10;
        public int Charisma { get; set; } = 10;
        public string ClassID { get; set; } = "";
        public AbilityScore SelectedKeyAbility { get; set; }
        public List<SkillName> ExtraTrainedSkills = new();


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

    }


}