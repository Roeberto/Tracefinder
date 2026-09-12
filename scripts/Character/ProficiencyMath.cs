using System;
using System.Reflection.Metadata.Ecma335;


namespace Tracefinder.Character
{
	public static class ProficiencyMath
	{
		public static int Bonus(ProficiencyRank rank, int level)
		{
			
			switch (rank)
			{
				case ProficiencyRank.Untrained:
					return 0;
				case ProficiencyRank.Trained:
					return level +2;
				case ProficiencyRank.Expert:
					return level + 4;
				case ProficiencyRank.Master:
					return level + 6;
				case ProficiencyRank.Legendary:
					return level + 8;
				default: 
					return 0;
			}
		}
		
		public static int AbilityModifier (int score) // Zwraca modyfikator np. +1 do craftingu zależnie od wartości int
		{
			int modifier = (int)Math.Floor( (score - 10) / 2.0 );
			return modifier;
		}
	}
}
