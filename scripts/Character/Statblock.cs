using System.Collections.Generic;
using Tracefinder.Gameplay;
using Tracefinder.Items;

namespace Tracefinder.Character
{
    public record MonsterStrike(string Name, int AttackBonus, Equipment.Weapon Weapon, int DamageBonus);

    public class Statblock
    {
        public string Name { get; init; } = "";
        public int Level { get; init; }
        public int MaxHp { get; init; }
        public int ArmorClass { get; init; }
        public int Perception { get; init; }
        public int Fortitude { get; init; }
        public int Reflex { get; init; }
        public int Will { get; init; }
        public int SpeedFeet { get; init; } = 25;
        public IReadOnlyList<MonsterStrike> Strikes { get; init; } = [];
    }

    // Wartości z pamięci modelu: strony Monster Core nie dały się odczytać, więc to NIEZWERYFIKOWANE
    public static class StatblockList
    {
        private static readonly Equipment.Weapon Dogslicer = new()
        {
            Name = "Dogslicer",
            Category = WeaponCategory.Martial,
            DamageDice = Dice.DiceSize.D6,
            DamageType = WeaponDamageType.Slashing,
            DiceNumber = 1,
            Group = WeaponGroup.Sword,
            Kind = WeaponKind.Melee,
            Handedness = WeaponHandedness.OneHanded,
            TraitList = [Trait.Agile, Trait.Backstabber, Trait.Finesse]
        };

        private static readonly Equipment.Weapon Spear = new()
        {
            Name = "Spear",
            Category = WeaponCategory.Simple,
            DamageDice = Dice.DiceSize.D6,
            DamageType = WeaponDamageType.Piercing,
            DiceNumber = 1,
            Group = WeaponGroup.Spear,
            Kind = WeaponKind.Melee,
            Handedness = WeaponHandedness.OneHanded,
            TraitList = [Trait.Thrown(20)],
            RangeIncrement = 20
        };

        private static readonly Equipment.Weapon Scimitar = new()
        {
            Name = "Scimitar",
            Category = WeaponCategory.Martial,
            DamageDice = Dice.DiceSize.D6,
            DamageType = WeaponDamageType.Slashing,
            DiceNumber = 1,
            Group = WeaponGroup.Sword,
            Kind = WeaponKind.Melee,
            Handedness = WeaponHandedness.OneHanded,
            TraitList = [Trait.Forceful, Trait.Sweep]
        };

        public static readonly Statblock GoblinWarrior = new()
        {
            Name = "Goblin Warrior",
            Level = -1,
            MaxHp = 6,
            ArmorClass = 16,
            Perception = 2,
            Fortitude = 5,
            Reflex = 7,
            Will = 3,
            Strikes =
            [
                new("Dogslicer", 8, Dogslicer, 0),
                new("Shortbow", 6, WeaponList.Shortbow, 0)
            ]
        };

        public static readonly Statblock KoboldWarrior = new()
        {
            Name = "Kobold Warrior",
            Level = -1,
            MaxHp = 8,
            ArmorClass = 16,
            Perception = 3,
            Fortitude = 4,
            Reflex = 7,
            Will = 5,
            Strikes =
            [
                new("Spear", 8, Spear, 0)
            ]
        };

        public static readonly Statblock SkeletonGuard = new()
        {
            Name = "Skeleton Guard",
            Level = -1,
            MaxHp = 4,
            ArmorClass = 16,
            Perception = 2,
            Fortitude = 2,
            Reflex = 8,
            Will = 2,
            Strikes =
            [
                new("Scimitar", 6, Scimitar, 2),
                new("Shortbow", 6, WeaponList.Shortbow, 0)
            ]
        };

        public static readonly IReadOnlyList<Statblock> All =
        [
            GoblinWarrior, KoboldWarrior, SkeletonGuard
        ];
    }
}
