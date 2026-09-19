using System;

namespace Tracefinder.Gameplay
{
    public class ActionTracker
    {
        public const int maxMainAction = 3;
        public const int maxReaction = 1;

        public int mainActions { get; private set; } = maxMainAction;
        public int reaction { get; private set; } = maxReaction;

        public void useMainAction(int actionCost)
        {
            mainActions = Math.Max(mainActions - actionCost, 0);
        }

        public void restoreMainAction(int actionCost)
        {
            mainActions = Math.Min(mainActions + actionCost, maxMainAction);
        }

        public void UseReaction()
        {
            reaction = Math.Max(reaction - 1, 0);
        }

        public void RestoreReaction()
        {
            reaction = Math.Min(reaction + 1, maxReaction);
        }

        public void ResetTurn()
        {
            MainActions = maxMainAction;
            Reaction = maxReaction;
        }

    }
}