using System;
using Tracefinder.Items;

namespace Tracefinder.Gameplay
{
	public static class Damage
	{
		// strikingDice: dodatkowe kości z runy Striking (0 brak, 1 striking, 2 greater, 3 major)
		public static int Roll(Equipment.Weapon weapon, int bonus, DegreeOfSuccess degree, int strikingDice = 0)
		{
			if (degree != DegreeOfSuccess.Success && degree != DegreeOfSuccess.CriticalSuccess)
				return 0;

			bool critical = degree == DegreeOfSuccess.CriticalSuccess;
			Dice.DiceSize die = weapon.DamageDice;
			int extraAfterDoubling = 0;

			if (critical)
			{
				foreach (WeaponTrait trait in weapon.TraitList)
				{
					if (trait is FatalTrait fatal)
					{
						die = fatal.Die;
						extraAfterDoubling += Dice.Roll(fatal.Die, 1);
					}
					else if (trait is DeadlyTrait deadly)
					{
						extraAfterDoubling += Dice.Roll(deadly.Die, Math.Max(strikingDice, 1));
					}
				}
			}

			int damage = Math.Max(Dice.Roll(die, weapon.DiceNumber + strikingDice) + bonus, 1);

			// Kości z Deadly i Fatal dochodzą po podwojeniu, więc nie są podwajane
			return critical ? damage * 2 + extraAfterDoubling : damage;
		}
	}
}
