using System;
using System.Collections.Generic;

using Tracefinder.Gameplay;

namespace Tracefinder.Items
{

    public static class Equipment
    {
        public class Weapon
        {
            public string Name;
            public WeaponCategory Category;
            public WeaponGroup Group;
            public WeaponKind Kind;
            public Dice.DiceSize DamageDice;
            public WeaponDamageType DamageType;
            public int DiceNumber;
            public WeaponHandedness Handedness;
            public int HandsToCarry => Handedness == WeaponHandedness.TwoHanded ? 2 : 1;
            public int HandsToAttack => Handedness == WeaponHandedness.OneHanded ? 1 : 2;
            public List<WeaponTrait> TraitList = new();
            public int? RangeIncrement; // null = broń wręcz bez cechy Thrown (nie używa mechaniki range increment)
            public int ActionsToReload; // ile akcji zajmuje przeładowanie
            public List<WeaponDamageType> PossibleDamageTypes()
            {
                List<WeaponDamageType> types = new();
                types.Add(DamageType);

                foreach (WeaponTrait trait in TraitList)
                {
                    if (trait is VersatileTrait versatile)
                    {
                        types.Add(versatile.Damage);
                    }

                    if (trait is ModularTrait modular)
                    {
                        foreach (WeaponDamageType type in modular.DamageTypes)
                        {
                            if (!types.Contains(type))
                                types.Add(type);
                        }
                    }
                }

                return types;
            }
            
            public bool HasTrait<T>() where T:WeaponTrait
            {
                foreach (WeaponTrait trait in TraitList)
                {
                    if (trait is T)
                        return true;
                }
                return false;
            }
            
            public bool CanDeal(WeaponDamageType type)
            {
                return PossibleDamageTypes().Contains(type);
            }
        }
    }

}


