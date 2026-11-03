using System;
using System.Collections.Generic;
using Tracefinder.Character;
using Tracefinder.Gameplay;
using Tracefinder.Items;

namespace Tracefinder.World
{
    public record MonsterFamily(string Name, string WeaponName, WeaponDamageType DamageType, WeaponGroup Group, bool Ranged, bool Undead, int SpeedFeet);

    // Własne potwory kampanii, zbudowane według tabel Building Creatures z GM Core (poziomy od -1 do 12).
    // Na niskim poziomie pojawiają się też trzy potwory z Monster Core.
    public static class MonsterFactory
    {
        public const int MinLevel = -1;
        public const int MaxLevel = 12;

        private record Row(int ModerateAc, int HighAc, int ModerateHp, int HighHp, int ModeratePerception, int HighSave, int ModerateSave, int LowSave,
            int ModerateAttack, int HighAttack, int ModerateDice, Dice.DiceSize ModerateDie, int ModerateFlat, int HighDice, Dice.DiceSize HighDie, int HighFlat);

        private static readonly Row[] Rows =
        {
            new(14, 15, 8, 9, 5, 8, 5, 2, 6, 8, 1, Dice.DiceSize.D4, 0, 1, Dice.DiceSize.D4, 1),
            new(15, 16, 15, 18, 6, 9, 6, 3, 6, 8, 1, Dice.DiceSize.D4, 2, 1, Dice.DiceSize.D6, 2),
            new(15, 16, 20, 25, 7, 10, 7, 4, 7, 9, 1, Dice.DiceSize.D6, 2, 1, Dice.DiceSize.D6, 3),
            new(17, 18, 30, 38, 8, 11, 8, 5, 9, 11, 1, Dice.DiceSize.D8, 4, 1, Dice.DiceSize.D10, 4),
            new(18, 19, 45, 56, 9, 12, 9, 6, 10, 12, 1, Dice.DiceSize.D8, 6, 1, Dice.DiceSize.D10, 6),
            new(20, 21, 60, 75, 11, 14, 11, 8, 12, 14, 2, Dice.DiceSize.D6, 5, 2, Dice.DiceSize.D8, 5),
            new(21, 22, 75, 94, 12, 15, 12, 9, 13, 15, 2, Dice.DiceSize.D6, 6, 2, Dice.DiceSize.D8, 7),
            new(23, 24, 95, 119, 14, 17, 14, 11, 15, 17, 2, Dice.DiceSize.D6, 8, 2, Dice.DiceSize.D8, 9),
            new(24, 25, 115, 144, 15, 18, 15, 12, 16, 18, 2, Dice.DiceSize.D8, 8, 2, Dice.DiceSize.D10, 9),
            new(26, 27, 135, 169, 16, 19, 16, 13, 18, 20, 2, Dice.DiceSize.D8, 9, 2, Dice.DiceSize.D10, 11),
            new(27, 28, 155, 194, 18, 21, 18, 15, 19, 21, 2, Dice.DiceSize.D8, 11, 2, Dice.DiceSize.D10, 13),
            new(29, 30, 175, 219, 19, 22, 19, 16, 21, 23, 2, Dice.DiceSize.D10, 11, 2, Dice.DiceSize.D12, 13),
            new(30, 31, 195, 244, 21, 24, 21, 18, 22, 24, 2, Dice.DiceSize.D10, 12, 2, Dice.DiceSize.D12, 15),
            new(32, 33, 215, 269, 22, 25, 22, 19, 24, 26, 3, Dice.DiceSize.D8, 12, 3, Dice.DiceSize.D10, 14)
        };

        public static readonly MonsterFamily Bandit = new("Bandyta", "Szabla", WeaponDamageType.Slashing, WeaponGroup.Sword, false, false, 25);
        public static readonly MonsterFamily Poacher = new("Kłusownik", "Łuk", WeaponDamageType.Piercing, WeaponGroup.Bow, true, false, 25);
        public static readonly MonsterFamily Wolf = new("Wilk", "Kły", WeaponDamageType.Piercing, WeaponGroup.Brawling, false, false, 35);
        public static readonly MonsterFamily Spider = new("Olbrzymi pająk", "Żuwaczki", WeaponDamageType.Piercing, WeaponGroup.Brawling, false, false, 30);
        public static readonly MonsterFamily Orc = new("Ork", "Topór", WeaponDamageType.Slashing, WeaponGroup.Axe, false, false, 25);
        public static readonly MonsterFamily Ogre = new("Ogr", "Maczuga", WeaponDamageType.Bludgeoning, WeaponGroup.Club, false, false, 25);
        public static readonly MonsterFamily Ghoul = new("Ghul", "Pazury", WeaponDamageType.Slashing, WeaponGroup.Brawling, false, true, 30);
        public static readonly MonsterFamily Wraith = new("Popielny strażnik", "Czarny miecz", WeaponDamageType.Slashing, WeaponGroup.Sword, false, true, 25);
        public static readonly MonsterFamily AshArcher = new("Popielny łucznik", "Kościany łuk", WeaponDamageType.Piercing, WeaponGroup.Bow, true, true, 25);

        public static IReadOnlyList<MonsterFamily> FamiliesFor(Biome biome)
        {
            switch (biome)
            {
                case Biome.Forest:
                    return [Wolf, Spider, Poacher];
                case Biome.Mountains:
                    return [Orc, Ogre, Wolf];
                default:
                    return [Bandit, Poacher, Wolf];
            }
        }

        public static Statblock Create(MonsterFamily family, int level, bool elite = false, string name = null)
        {
            int clamped = Math.Clamp(level, MinLevel, MaxLevel);
            Row row = Rows[clamped - MinLevel];
            Equipment.Weapon weapon = new()
            {
                Name = family.WeaponName,
                Category = WeaponCategory.Martial,
                DamageDice = elite ? row.HighDie : row.ModerateDie,
                DiceNumber = elite ? row.HighDice : row.ModerateDice,
                DamageType = family.DamageType,
                Group = family.Group,
                Kind = family.Ranged ? WeaponKind.Ranged : WeaponKind.Melee,
                Handedness = WeaponHandedness.OneHanded,
                RangeIncrement = family.Ranged ? 60 : null
            };

            return new Statblock
            {
                Name = name ?? family.Name,
                Level = clamped,
                MaxHp = elite ? row.HighHp : row.ModerateHp,
                ArmorClass = elite ? row.HighAc : row.ModerateAc,
                Perception = row.ModeratePerception,
                Fortitude = row.HighSave,
                Reflex = row.ModerateSave,
                Will = row.LowSave,
                SpeedFeet = family.SpeedFeet,
                IsUndead = family.Undead,
                HasReactiveStrike = elite,
                Strikes =
                [
                    new(family.WeaponName, elite ? row.HighAttack : row.ModerateAttack, weapon, elite ? row.HighFlat : row.ModerateFlat)
                ]
            };
        }

        // Skład starcia według budżetu XP dla jednej postaci. Gdy budżet nie starcza na nikogo, staje najsłabszy możliwy przeciwnik.
        public static List<Statblock> ForBudget(Random random, int partyLevel, Threat threat, IReadOnlyList<MonsterFamily> families)
        {
            const int maxEnemies = 4;
            const int widestGap = 4;
            int budget = Rewards.Budget(threat, 1);
            int lowest = Math.Max(partyLevel - widestGap, MinLevel);
            List<Statblock> enemies = new();

            while (enemies.Count < maxEnemies)
            {
                List<int> levels = new();
                for (int level = lowest; level <= partyLevel + 2; level++)
                {
                    if (Rewards.ExperienceFor(partyLevel, level) <= budget)
                        levels.Add(level);
                }
                if (levels.Count == 0)
                    break;

                int chosen = levels[random.Next(levels.Count)];
                budget -= Rewards.ExperienceFor(partyLevel, chosen);
                enemies.Add(Pick(random, chosen, families));
            }

            if (enemies.Count == 0)
                enemies.Add(Pick(random, lowest, families));
            return enemies;
        }

        private static Statblock Pick(Random random, int level, IReadOnlyList<MonsterFamily> families)
        {
            // Na poziomie -1 czasem trafia się prawdziwy potwór z Monster Core
            if (level == MinLevel && random.Next(2) == 0)
                return StatblockList.All[random.Next(StatblockList.All.Count)];
            return Create(families[random.Next(families.Count)], level);
        }
    }
}
