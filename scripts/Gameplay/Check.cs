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

	}
}
