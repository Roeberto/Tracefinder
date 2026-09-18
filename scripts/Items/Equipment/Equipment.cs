using System;
using System.Collections.Generic;

using Tracefinder.WeaponStats;
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
        public Dice.DiceSize DamageDice;
        public WeaponDamageType DamageType;
        public int DiceNumber;
        public WeaponHandedness Handedness; 
        public List<WeaponTrait> TraitList;
        public int? RangeIncrement; // null = broń wręcz bez cechy Thrown (nie używa mechaniki range increment)
        public Dice.DiceSize? DeadlyDie; // kość dokładana przy krytyku z cechy Deadly, dla null = broń nie ma Deadly
        public int? VolleyRange; // dystans w stopach, poniżej którego Volley daje -2, dla null = broń nie ma Volley
        public int ActionsToReload; // ile akcji zajmuje przeładowanie
    }
}

}


