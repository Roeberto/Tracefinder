using System;

namespace Tracefinder.Gameplay
{
    public class ActionTracker
    {
        public const int MaxMainAction = 3;
        public const int MaxReaction = 1;

        public int MainActions { get; private set; } = MaxMainAction;
        public int Reaction { get; private set; } = MaxReaction;

        public void UseMainAction(int actionCost)
        {
            MainActions = Math.Max(MainActions - actionCost, 0);
        }

        public void RestoreMainAction(int actionCost)
        {
            MainActions = Math.Min(MainActions + actionCost, MaxMainAction);
        }

        public void UseReaction()
        {
            Reaction = Math.Max(Reaction - 1, 0);
        }

        public void RestoreReaction()
        {
            Reaction = Math.Min(Reaction + 1, MaxReaction);
        }

        public void ResetTurn()
        {
            MainActions = MaxMainAction;
            Reaction = MaxReaction;
        }

    }
}