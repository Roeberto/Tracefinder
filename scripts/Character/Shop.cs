using System.Collections.Generic;
using System.Linq;
using Tracefinder.Items;

namespace Tracefinder.Character
{
    // Zakupy w osadzie. Sprzedawca ma towar najwyżej o poziom wyższy niż poziom bohatera.
    public static class Shop
    {
        public static bool IsAvailable(int itemLevel, CharacterCore core)
        {
            return itemLevel <= core.Level + 1;
        }

        private static bool Pay(CharacterCore core, int priceSp)
        {
            if (core.Silver < priceSp)
                return false;
            core.Silver -= priceSp;
            return true;
        }

        public static bool BuyArmor(CharacterCore core, Armor armor)
        {
            if (core.OwnedArmors.Contains(armor) || !IsAvailable(armor.ItemLevel, core) || !Pay(core, armor.PriceSp))
                return false;

            core.OwnedArmors.Add(armor);
            core.EquippedArmor = armor;
            return true;
        }

        public static bool BuyWeapon(CharacterCore core, Equipment.Weapon weapon)
        {
            if (core.Inventory.Any(w => w.Template == weapon) || !Pay(core, ShopCatalog.WeaponPriceSp(weapon)))
                return false;

            CarriedWeapon carried = new(weapon);
            core.Inventory.Add(carried);
            core.EquippedWeapon ??= carried;
            return true;
        }

        public static bool BuyShield(CharacterCore core)
        {
            if (core.HasShield || !Pay(core, ShopCatalog.ShieldPriceSp))
                return false;

            core.HasShield = true;
            core.ShieldHp = ShopCatalog.ShieldMaxHp;
            return true;
        }

        public static bool BuyPotion(CharacterCore core, Potion potion)
        {
            if (!IsAvailable(potion.ItemLevel, core) || !Pay(core, potion.PriceSp))
                return false;

            core.Potions[potion.Name] = core.Potions.GetValueOrDefault(potion.Name) + 1;
            return true;
        }

        public static bool BuyPotencyRune(CharacterCore core, CarriedWeapon weapon)
        {
            if (weapon == null || weapon.PotencyRune > 0 || !IsAvailable(ShopCatalog.PotencyRuneLevel, core) || !Pay(core, ShopCatalog.PotencyRunePriceSp))
                return false;

            weapon.PotencyRune = 1;
            return true;
        }

        // Runa striking wymaga wcześniej runy potency
        public static bool BuyStrikingRune(CharacterCore core, CarriedWeapon weapon)
        {
            if (weapon == null || weapon.PotencyRune == 0 || weapon.StrikingRune > 0
                || !IsAvailable(ShopCatalog.StrikingRuneLevel, core) || !Pay(core, ShopCatalog.StrikingRunePriceSp))
                return false;

            weapon.StrikingRune = 1;
            return true;
        }
    }
}
