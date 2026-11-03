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

    // Feat to na razie sam wpis na arkuszu, jego efekty nie są wpięte w obliczenia
    public record Feat(string Name, int Level, FeatCategory Category, string Source, string Summary);

    // Nazwy i opisy z pamięci modelu: strona z featami nie dała się odczytać, więc to NIEZWERYFIKOWANE
    public static class FeatList
    {
        public static readonly IReadOnlyList<Feat> All =
        [
            new("Double Slice", 1, FeatCategory.Class, "Fighter", "Atak dwiema broniami naraz w jednej akcji."),
            new("Power Attack", 1, FeatCategory.Class, "Fighter", "Dwuakcyjny atak z dodatkową kością obrażeń."),
            new("Reactive Shield", 1, FeatCategory.Class, "Fighter", "Podniesienie tarczy jako reakcja na atak."),
            new("Nimble Dodge", 1, FeatCategory.Class, "Rogue", "Reakcja dająca premię do AC przeciw atakowi."),
            new("Trap Finder", 1, FeatCategory.Class, "Rogue", "Premia do wykrywania i unikania pułapek."),
            new("Toughness", 1, FeatCategory.General, "General", "Dodatkowe HP równe poziomowi postaci."),
            new("Fleet", 1, FeatCategory.General, "General", "Szybkość większa o 5 stóp."),
            new("Incredible Initiative", 1, FeatCategory.General, "General", "Premia +2 do inicjatywy."),
            new("Assurance", 1, FeatCategory.Skill, "Skill", "Stały wynik testu wybranej umiejętności."),
            new("Battle Medicine", 1, FeatCategory.Skill, "Skill", "Leczenie sojusznika jedną akcją w walce."),
            new("Natural Ambition", 1, FeatCategory.Ancestry, "Human", "Dodatkowy feat klasowy pierwszego poziomu."),
            new("General Training", 1, FeatCategory.Ancestry, "Human", "Dodatkowy feat ogólny pierwszego poziomu.")
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
