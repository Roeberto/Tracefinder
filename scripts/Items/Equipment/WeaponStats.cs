using System;

namespace Tracefinder.WeaponStats
{
    public enum WeaponGroup
    {
        Bow,
        Hammer,
        Knife,
        Sword
    }

    public enum WeaponTrait
    {
        Agile,
        Deadly,
        Disarm,
        Finesse,
        Thrown,
        Versatile
    }

    public enum WeaponCategory
    {
        Simple,
        Martial,
        Advanced,
        Unarmed
    }

    public enum WeaponDamageType
    {
        Bludgeoning,
        Piercing,
        Slashing
    }

    public enum WeaponHandedness
    {
        OneHanded,
        TwoHanded
    }
}