using System.Collections.Generic;

namespace Tracefinder.Character
{
    public enum FeatCategory
    {
        Ancestry,
        Class,
        Skill,
        General
    }

    // Efekty działają w walce i na arkuszu. Vicious Swing, Toughness, Fleet, Incredible Initiative i Battle Medicine
    // odczytane z danych systemu PF2e dla Foundry VTT, pozostałe z pamięci modelu: NIEZWERYFIKOWANE.
    public record Feat(string Name, int Level, FeatCategory Category, string Source, string Summary);

    public static class FeatList
    {
        public static readonly Feat ViciousSwing = new("Vicious Swing", 1, FeatCategory.Class, "Fighter", "Atak wręcz za dwie akcje z dodatkową kością obrażeń. Liczy się jak dwa ataki do MAP.");
        public static readonly Feat ReactiveShield = new("Reactive Shield", 1, FeatCategory.Class, "Fighter", "Reakcja: podnosisz tarczę, gdy dzięki temu atak by chybił.");
        public static readonly Feat NimbleDodge = new("Nimble Dodge", 1, FeatCategory.Class, "Rogue", "Reakcja: +2 do AC, gdy dzięki temu atak by chybił.");
        public static readonly Feat ReachSpell = new("Reach Spell", 1, FeatCategory.Class, "Wizard", "Zaklęcia mają zasięg większy o 30 stóp.");
        public static readonly Feat HealingHands = new("Healing Hands", 1, FeatCategory.Class, "Cleric", "Heal używa k10 zamiast k8.");
        public static readonly Feat Toughness = new("Toughness", 1, FeatCategory.General, "General", "Dodatkowe HP równe poziomowi postaci.");
        public static readonly Feat Fleet = new("Fleet", 1, FeatCategory.General, "General", "Szybkość większa o 5 stóp.");
        public static readonly Feat IncredibleInitiative = new("Incredible Initiative", 1, FeatCategory.General, "General", "Premia +2 do inicjatywy.");
        public static readonly Feat BattleMedicine = new("Battle Medicine", 1, FeatCategory.Skill, "Skill", "Raz na walkę opatrujesz się jedną akcją. Wymaga wyszkolenia w Medycynie.");
        public static readonly Feat NaturalAmbition = new("Natural Ambition", 1, FeatCategory.Ancestry, "Human", "Dodatkowy feat klasowy.");
        public static readonly Feat GeneralTraining = new("General Training", 1, FeatCategory.Ancestry, "Human", "Dodatkowy feat ogólny.");

        public static readonly IReadOnlyList<Feat> All =
        [
            ViciousSwing, ReactiveShield, NimbleDodge, ReachSpell, HealingHands,
            Toughness, Fleet, IncredibleInitiative, BattleMedicine, NaturalAmbition, GeneralTraining
        ];
    }

    // Harmonogramy z Player Core, sprawdzone w Archives of Nethys
    public static class Progression
    {
        public const int ExperiencePerLevel = 1000;
        public const int MaxLevel = 20;

        public static bool GrantsAncestryFeat(int level)
        {
            return level == 1 || level == 5 || level == 9 || level == 13 || level == 17;
        }

        public static bool GrantsGeneralFeat(int level)
        {
            return level == 3 || level == 7 || level == 11 || level == 15 || level == 19;
        }

        public static bool GrantsClassFeat(int level, ClassStats classStats)
        {
            return level % 2 == 0 || (level == 1 && classStats.ClassFeatAtFirstLevel);
        }

        public static bool GrantsSkillFeat(int level, ClassStats classStats)
        {
            return classStats.SkillFeatEveryLevel || level % 2 == 0;
        }

        public static bool GrantsSkillIncrease(int level, ClassStats classStats)
        {
            if (level < 2)
                return false;
            return classStats.SkillIncreaseEveryLevel || level % 2 == 1;
        }

        public static bool GrantsAttributeBoosts(int level)
        {
            return level == 5 || level == 10 || level == 15 || level == 20;
        }

        public static int FeatSlots(FeatCategory category, int level, ClassStats classStats)
        {
            int slots = 0;
            for (int l = 1; l <= level; l++)
            {
                bool grants = category switch
                {
                    FeatCategory.Ancestry => GrantsAncestryFeat(l),
                    FeatCategory.Class => GrantsClassFeat(l, classStats),
                    FeatCategory.Skill => GrantsSkillFeat(l, classStats),
                    _ => GrantsGeneralFeat(l)
                };
                if (grants)
                    slots++;
            }
            return slots;
        }
    }
}
