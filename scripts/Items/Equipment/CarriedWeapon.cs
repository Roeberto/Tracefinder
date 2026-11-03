namespace Tracefinder.Items
{
    // Konkretny egzemplarz broni. Equipment.Weapon to wspólny wzorzec i nie zmienia się w trakcie gry.
    public class CarriedWeapon
    {
        public Equipment.Weapon Template { get; }
        public int PotencyRune { get; set; }
        public int StrikingRune { get; set; }
        public int? Ammunition { get; set; }
        public bool IsLoaded { get; private set; } = true;
        public WeaponDamageType CurrentDamageType { get; private set; }

        public CarriedWeapon(Equipment.Weapon template)
        {
            Template = template;
            CurrentDamageType = template.DamageType;
        }

        public string Name => Template.Name;

        // Versatile wybiera się przy każdym ataku, a Modular przełącza akcją Interact
        public bool CanStrikeWith(WeaponDamageType type)
        {
            if (type == CurrentDamageType)
                return true;

            foreach (WeaponTrait trait in Template.TraitList)
            {
                if (trait is VersatileTrait versatile && versatile.Damage == type)
                    return true;
            }
            return false;
        }

        public bool SwitchModular(WeaponDamageType type)
        {
            foreach (WeaponTrait trait in Template.TraitList)
            {
                if (trait is ModularTrait modular && System.Array.IndexOf(modular.DamageTypes, type) >= 0)
                {
                    CurrentDamageType = type;
                    return true;
                }
            }
            return false;
        }

        public bool CanFire => IsLoaded && (!Ammunition.HasValue || Ammunition.Value > 0);

        public void Fire()
        {
            if (Ammunition.HasValue)
                Ammunition--;
            if (Template.ActionsToReload > 0)
                IsLoaded = false;
        }

        // Rzucona broń trzeba dobyć ponownie, tak jak przeładowanie kosztuje to akcję
        public void MarkThrown()
        {
            IsLoaded = false;
        }

        public int ActionsToReady => System.Math.Max(Template.ActionsToReload, 1);

        public void Reload()
        {
            IsLoaded = true;
        }
    }
}
