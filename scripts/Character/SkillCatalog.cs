using System;


namespace Tracefinder.Character
{
    public class SkillCatalog
    {
        public static AbilityScore KeyAbilityFor(SkillName skill)
        {
            switch (skill)
            {
                case SkillName.Athletics:
                    return AbilityScore.Strength;

                case SkillName.Acrobatics:
                case SkillName.Stealth:
                case SkillName.Thievery:
                    return AbilityScore.Dexterity;

                // case SkillName.
                //    return AbilityScore.Constitution;

                case SkillName.Arcana:
                case SkillName.Crafting:
                case SkillName.Occultism:
                case SkillName.Society:
                    return AbilityScore.Intelligence;

                case SkillName.Medicine:
                case SkillName.Nature:
                case SkillName.Religion:
                case SkillName.Survival:
                    return AbilityScore.Wisdom;

                case SkillName.Deception:
                case SkillName.Diplomacy:
                case SkillName.Intimidation:
                case SkillName.Performance:
                    return AbilityScore.Charisma;
                   
                default:
                    throw new ArgumentException($"Unknown skill: {skill}");
            }
        }
    }
}