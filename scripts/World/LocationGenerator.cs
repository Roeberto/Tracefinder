using System;
using System.Collections.Generic;
using Tracefinder.Combat;

namespace Tracefinder.World
{
    public record Location(BattleGrid Grid, Biome Biome, int Seed, IReadOnlyList<GridPos> PartySpawns, IReadOnlyList<GridPos> EnemySpawns);

    public static class LocationGenerator
    {
        public const int DefaultWidth = 24;
        public const int DefaultHeight = 16;
        private const int SpawnZoneColumns = 3;
        private const int SpawnSlots = 4;

        public static Location Generate(int worldSeed, int worldX, int worldY, Biome biome, int width = DefaultWidth, int height = DefaultHeight)
        {
            int seed = WorldMap.LocationSeed(worldSeed, worldX, worldY);
            Random random = new(seed);
            BattleGrid grid = new(width, height);

            switch (biome)
            {
                case Biome.Plains:
                    Scatter(grid, random, TerrainType.Difficult, 6, 3);
                    Sprinkle(grid, random, TerrainType.Pillar, 4);
                    break;
                case Biome.Forest:
                    Scatter(grid, random, TerrainType.Difficult, 10, 3);
                    Sprinkle(grid, random, TerrainType.Pillar, width * height / 9);
                    break;
                case Biome.Mountains:
                    Scatter(grid, random, TerrainType.Wall, 6, 3);
                    Scatter(grid, random, TerrainType.Difficult, 8, 2);
                    break;
                case Biome.Settlement:
                    Buildings(grid, random, 5);
                    Sprinkle(grid, random, TerrainType.Difficult, 8);
                    break;
                case Biome.Water:
                    Scatter(grid, random, TerrainType.Water, 7, 4);
                    break;
                default:
                    throw new ArgumentException($"Unknown biome: {biome}");
            }

            ClearSpawnZones(grid);
            ConnectSpawnZones(grid, random);
            return new Location(grid, biome, seed, Spawns(grid, 1), Spawns(grid, width - 2));
        }

        private static void Scatter(BattleGrid grid, Random random, TerrainType terrain, int patches, int radius)
        {
            for (int i = 0; i < patches; i++)
            {
                int cx = random.Next(grid.Width);
                int cy = random.Next(grid.Height);
                int r = random.Next(1, radius + 1);
                for (int y = cy - r; y <= cy + r; y++)
                {
                    for (int x = cx - r; x <= cx + r; x++)
                    {
                        GridPos pos = new(x, y);
                        if (grid.InBounds(pos) && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                            grid[pos] = terrain;
                    }
                }
            }
        }

        private static void Sprinkle(BattleGrid grid, Random random, TerrainType terrain, int count)
        {
            for (int i = 0; i < count; i++)
                grid[new GridPos(random.Next(grid.Width), random.Next(grid.Height))] = terrain;
        }

        private static void Buildings(BattleGrid grid, Random random, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int w = random.Next(4, 7);
                int h = random.Next(3, 6);
                int left = random.Next(SpawnZoneColumns, Math.Max(SpawnZoneColumns + 1, grid.Width - SpawnZoneColumns - w));
                int top = random.Next(0, Math.Max(1, grid.Height - h));
                for (int y = top; y < top + h && y < grid.Height; y++)
                {
                    for (int x = left; x < left + w && x < grid.Width; x++)
                    {
                        bool edge = x == left || x == left + w - 1 || y == top || y == top + h - 1;
                        if (edge)
                            grid[new GridPos(x, y)] = TerrainType.Wall;
                    }
                }
                // Wejście do budynku
                int doorX = left + w / 2;
                int doorY = random.Next(2) == 0 ? top : top + h - 1;
                GridPos door = new(doorX, doorY);
                if (grid.InBounds(door))
                    grid[door] = TerrainType.Floor;
            }
        }

        private static void ClearSpawnZones(BattleGrid grid)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                for (int i = 0; i < SpawnZoneColumns; i++)
                {
                    grid[new GridPos(i, y)] = TerrainType.Floor;
                    grid[new GridPos(grid.Width - 1 - i, y)] = TerrainType.Floor;
                }
            }
        }

        // Gdy teren odciął strony od siebie, przebija przejście przez całą szerokość mapy
        private static void ConnectSpawnZones(BattleGrid grid, Random random)
        {
            if (Reaches(grid, new GridPos(0, 0), new GridPos(grid.Width - 1, 0)))
                return;

            int row = random.Next(grid.Height);
            for (int x = 0; x < grid.Width; x++)
            {
                GridPos pos = new(x, row);
                if (!grid.IsPassable(pos))
                    grid[pos] = TerrainType.Floor;
            }
        }

        public static bool Reaches(BattleGrid grid, GridPos from, GridPos to)
        {
            return from == to || Movement.Reachable(grid, from, int.MaxValue / 2, _ => false, _ => false).ContainsKey(to);
        }

        private static List<GridPos> Spawns(BattleGrid grid, int column)
        {
            List<GridPos> spawns = new();
            for (int i = 0; i < SpawnSlots; i++)
                spawns.Add(new GridPos(column, (i + 1) * grid.Height / (SpawnSlots + 1)));
            return spawns;
        }
    }
}
