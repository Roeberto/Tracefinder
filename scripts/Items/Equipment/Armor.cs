using System.Collections.Generic;

namespace Tracefinder.Items
{
    public enum ArmorCategory
    {
        Unarmored,
        Light,
        Medium,
        Heavy
    }

    public record Armor(string Name, ArmorCategory Category, int AcBonus, int DexCap, int PriceSp, int ItemLevel);

    public record Potion(string Name, int Dice, Gameplay.Dice.DiceSize Die, int Flat, int PriceSp, int ItemLevel);

    // Wartości i ceny odczytane z danych systemu PF2e dla Foundry VTT (pakiet equipment). Ceny w srebrnikach, 1 gp = 10 sp.
    public static class ShopCatalog
    {
        public const int SilverPerGold = 10;
        public const int StartingSilver = 150;
        public const int ShieldPriceSp = 20;
        public const int ShieldAcBonus = 2;
        public const int ShieldHardness = 5;
        public const int ShieldMaxHp = 20;
        public const int ShieldBrokenThreshold = 10;
        public const int PotencyRunePriceSp = 350;
        public const int PotencyRuneLevel = 2;
        public const int StrikingRunePriceSp = 650;
        public const int StrikingRuneLevel = 4;

        public static readonly IReadOnlyList<Armor> Armors =
        [
            new("Padded Armor", ArmorCategory.Light, 1, 3, 2, 0),
            new("Leather Armor", ArmorCategory.Light, 1, 4, 20, 0),
            new("Studded Leather", ArmorCategory.Light, 2, 3, 30, 0),
            new("Chain Shirt", ArmorCategory.Light, 2, 3, 50, 0),
            new("Scale Mail", ArmorCategory.Medium, 3, 2, 40, 0),
            new("Breastplate", ArmorCategory.Medium, 4, 1, 80, 0),
            new("Half Plate", ArmorCategory.Heavy, 5, 1, 180, 1),
            new("Full Plate", ArmorCategory.Heavy, 6, 0, 300, 2)
        ];

        public static readonly IReadOnlyList<Potion> Potions =
        [
            new("Minor Healing Potion", 1, Gameplay.Dice.DiceSize.D8, 0, 40, 1),
            new("Lesser Healing Potion", 2, Gameplay.Dice.DiceSize.D8, 5, 120, 3),
            new("Moderate Healing Potion", 3, Gameplay.Dice.DiceSize.D8, 10, 500, 6)
        ];

        public static int WeaponPriceSp(Equipment.Weapon weapon)
        {
            return weapon.Name switch
            {
                "Dagger" => 2,
                "Longsword" => 10,
                "Rapier" => 20,
                "Warhammer" => 10,
                "Shortbow" => 30,
                "Longbow" => 60,
                _ => 10
            };
        }
    }
}
