using System;
using System.Collections.Generic;
using Tracefinder.Character;
using Tracefinder.Combat;
using Tracefinder.Items;

namespace Tracefinder.World
{
    // Stan przekazywany między scenami: kampania, aktualna postać i zaplanowana walka
    public static class GameSession
    {
        public static CharacterCore Character { get; set; }
        public static int WorldSeed { get; set; } = 2026;
        public static Campaign Campaign { get; set; }
        public static CharacterCore PendingHero { get; set; }
        public static EncounterPlan PendingPlan { get; set; }
        public static bool EndingVictory { get; set; }

        public static ClassStats CharacterClass => ClassList.ByName(CurrentCharacter().ClassID) ?? ClassList.Fighter;

        // Bez utworzonej postaci sceny korzystają z przykładowego Wojownika
        public static CharacterCore CurrentCharacter()
        {
            if (Campaign != null)
                return Campaign.Hero;
            Character ??= ExampleCharacter();
            return Character;
        }

        public static CharacterCore ExampleCharacter()
        {
            CharacterCore core = new()
            {
                Name = "Amber",
                Strength = 18,
                Dexterity = 14,
                Constitution = 16,
                Intelligence = 10,
                Wisdom = 12,
                Charisma = 8,
                ClassID = ClassList.Fighter.Name,
                SelectedKeyAbility = AbilityScore.Strength,
                Ancestry = AncestryList.Human
            };
            core.Inventory.Add(new CarriedWeapon(WeaponList.Longsword));
            core.Inventory.Add(new CarriedWeapon(WeaponList.Dagger));
            core.Inventory.Add(new CarriedWeapon(WeaponList.Shortbow));
            core.EquippedWeapon = core.Inventory[0];
            core.InitializeHealth(ClassList.Fighter);
            return core;
        }

        // Skład przeciwników zależy od ziarna lokalizacji, więc wraca taki sam przy ponownej wizycie
        public static Encounter CreateEncounter(Location location, CharacterCore character, ClassStats classStats)
        {
            Random random = new(location.Seed);
            List<Combatant> units = new() { new PlayerCombatant(character, classStats, location.PartySpawns[1]) };

            int enemies = random.Next(2, 4);
            for (int i = 0; i < enemies && i < location.EnemySpawns.Count; i++)
            {
                Statblock statblock = StatblockList.All[random.Next(StatblockList.All.Count)];
                units.Add(new MonsterCombatant(statblock, location.EnemySpawns[i], $"{statblock.Name} {i + 1}"));
            }
            return new Encounter(location.Grid, units);
        }
    }
}
