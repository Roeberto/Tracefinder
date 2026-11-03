using System.Collections.Generic;

namespace Tracefinder.Character
{
    public enum CreatureSize
    {
        Small,
        Medium
    }

    public enum Vision
    {
        Normal,
        LowLight,
        Darkvision
    }

    public record Ancestry(
        string Name,
        int HitPoints,
        CreatureSize Size,
        int SpeedFeet,
        IReadOnlyList<AbilityScore> Boosts,
        int FreeBoosts,
        AbilityScore? Flaw,
        Vision Vision);

    // Dane z Player Core, sprawdzone w Archives of Nethys
    public static class AncestryList
    {
        public static readonly Ancestry Dwarf = new("Dwarf", 10, CreatureSize.Medium, 20, [AbilityScore.Constitution, AbilityScore.Wisdom], 1, AbilityScore.Charisma, Vision.Darkvision);
        public static readonly Ancestry Elf = new("Elf", 6, CreatureSize.Medium, 30, [AbilityScore.Dexterity, AbilityScore.Intelligence], 1, AbilityScore.Constitution, Vision.LowLight);
        public static readonly Ancestry Gnome = new("Gnome", 8, CreatureSize.Small, 25, [AbilityScore.Constitution, AbilityScore.Charisma], 1, AbilityScore.Strength, Vision.LowLight);
        public static readonly Ancestry Goblin = new("Goblin", 6, CreatureSize.Small, 25, [AbilityScore.Dexterity, AbilityScore.Charisma], 1, AbilityScore.Wisdom, Vision.Darkvision);
        public static readonly Ancestry Halfling = new("Halfling", 6, CreatureSize.Small, 25, [AbilityScore.Dexterity, AbilityScore.Wisdom], 1, AbilityScore.Strength, Vision.Normal);
        public static readonly Ancestry Human = new("Human", 8, CreatureSize.Medium, 25, [], 2, null, Vision.Normal);
        public static readonly Ancestry Leshy = new("Leshy", 8, CreatureSize.Small, 25, [AbilityScore.Constitution, AbilityScore.Wisdom], 1, AbilityScore.Intelligence, Vision.LowLight);
        public static readonly Ancestry Orc = new("Orc", 10, CreatureSize.Medium, 25, [], 2, null, Vision.Darkvision);

        public static readonly IReadOnlyList<Ancestry> All =
        [
            Dwarf, Elf, Gnome, Goblin, Halfling, Human, Leshy, Orc
        ];
    }
}
