using System;

namespace Tracefinder.Gameplay
{
	public static class Check
	{
		public static DegreeOfSuccess Determine(int total, int dc)
		{
			if (total >= dc + 10)
				return DegreeOfSuccess.CriticalSuccess;
			if (total >= dc)
				return DegreeOfSuccess.Success;
			if (total <= dc - 10)
				return DegreeOfSuccess.CriticalFailure;
			return DegreeOfSuccess.Failure;
		}


		public static DegreeOfSuccess Shift(DegreeOfSuccess degree, int steps)
		{
			return (DegreeOfSuccess)(Math.Clamp(((int)degree + steps),0,3));
		}
		
		public record Result(int Natural, int Total, int Dc, DegreeOfSuccess Degree);
		
		public static Result Roll(int bonus, int dc)
		{
			int natural = Dice.Roll(Dice.DiceSize.D20, 1);
			int total = natural + bonus;
			DegreeOfSuccess degree = Determine(total,dc);
			if (natural == 20)
				degree = Shift(degree, +1);
			else if (natural == 1)
				degree = Shift(degree, -1);
			return new Result(natural,total,dc,degree);
		}

	}
}
