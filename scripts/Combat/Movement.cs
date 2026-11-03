using System;
using System.Collections.Generic;

namespace Tracefinder.Combat
{
    public static class Movement
    {
        private static readonly (int X, int Y)[] Directions =
        [
            (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)
        ];

        // Pola osiągalne za budżet stóp i ich koszt. Przez isBlocked (wrogowie) nie da się przejść,
        // a na isOccupied (każda inna jednostka) nie da się stanąć.
        public static Dictionary<GridPos, int> Reachable(
            BattleGrid grid,
            GridPos start,
            int budgetFeet,
            Func<GridPos, bool> isBlocked,
            Func<GridPos, bool> isOccupied)
        {
            Dictionary<(GridPos, bool), int> best = new() { [(start, false)] = 0 };
            PriorityQueue<(GridPos Pos, bool OddDiagonal, int Cost), int> queue = new();
            queue.Enqueue((start, false, 0), 0);
            Dictionary<GridPos, int> result = new();

            while (queue.Count > 0)
            {
                (GridPos pos, bool oddDiagonal, int cost) = queue.Dequeue();
                if (best[(pos, oddDiagonal)] < cost)
                    continue;

                if (pos != start && !isOccupied(pos) && (!result.TryGetValue(pos, out int known) || cost < known))
                    result[pos] = cost;

                foreach ((int dx, int dy) in Directions)
                {
                    GridPos next = new(pos.X + dx, pos.Y + dy);
                    if (!CanEnter(grid, pos, next) || isBlocked(next))
                        continue;

                    bool diagonal = dx != 0 && dy != 0;
                    int stepCost = diagonal && oddDiagonal ? 10 : BattleGrid.FeetPerSquare;
                    if (grid[next] == TerrainType.Difficult)
                        stepCost += BattleGrid.FeetPerSquare;

                    int nextCost = cost + stepCost;
                    if (nextCost > budgetFeet)
                        continue;

                    bool nextOdd = diagonal ? !oddDiagonal : oddDiagonal;
                    if (!best.TryGetValue((next, nextOdd), out int previous) || nextCost < previous)
                    {
                        best[(next, nextOdd)] = nextCost;
                        queue.Enqueue((next, nextOdd, nextCost), nextCost);
                    }
                }
            }
            return result;
        }

        // Step to jedno pole, nie wolno nim wejść na trudny teren
        public static List<GridPos> StepTargets(BattleGrid grid, GridPos start, Func<GridPos, bool> isOccupied)
        {
            List<GridPos> targets = new();
            foreach ((int dx, int dy) in Directions)
            {
                GridPos next = new(start.X + dx, start.Y + dy);
                if (CanEnter(grid, start, next) && grid[next] != TerrainType.Difficult && !isOccupied(next))
                    targets.Add(next);
            }
            return targets;
        }

        // Nie da się ściąć rogu ściany ani filara po przekątnej
        private static bool CanEnter(BattleGrid grid, GridPos from, GridPos to)
        {
            if (!grid.IsPassable(to))
                return false;
            if (from.X == to.X || from.Y == to.Y)
                return true;
            return !IsSolid(grid, new GridPos(from.X, to.Y)) && !IsSolid(grid, new GridPos(to.X, from.Y));
        }

        private static bool IsSolid(BattleGrid grid, GridPos pos)
        {
            return grid.InBounds(pos) && grid.BlocksSight(pos);
        }
    }
}
