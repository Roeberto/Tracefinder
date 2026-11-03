using System;
using System.Collections.Generic;

namespace Tracefinder.Combat
{
    public enum TerrainType
    {
        Floor,
        Difficult,
        Water,
        Wall,
        Pillar
    }

    public readonly record struct GridPos(int X, int Y);

    // Zwykła struktura danych, bez zależności od sceny. Generator map albo ręczna mapa podają ją do Encounter.
    public class BattleGrid
    {
        public const int FeetPerSquare = 5;

        private readonly TerrainType[,] _cells;

        public int Width { get; }
        public int Height { get; }

        public BattleGrid(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Siatka musi mieć dodatnie wymiary.");
            Width = width;
            Height = height;
            _cells = new TerrainType[width, height];
        }

        // Legenda: . podłoga, ; trudny teren, ~ woda, # ściana, o filar
        public static BattleGrid Parse(params string[] rows)
        {
            BattleGrid grid = new(rows[0].Length, rows.Length);
            for (int y = 0; y < rows.Length; y++)
            {
                if (rows[y].Length != grid.Width)
                    throw new ArgumentException($"Wiersz {y} ma inną długość niż pierwszy.");
                for (int x = 0; x < grid.Width; x++)
                    grid._cells[x, y] = TerrainFor(rows[y][x]);
            }
            return grid;
        }

        private static TerrainType TerrainFor(char symbol)
        {
            switch (symbol)
            {
                case '.':
                    return TerrainType.Floor;
                case ';':
                    return TerrainType.Difficult;
                case '~':
                    return TerrainType.Water;
                case '#':
                    return TerrainType.Wall;
                case 'o':
                    return TerrainType.Pillar;
                default:
                    throw new ArgumentException($"Nieznany symbol terenu: {symbol}");
            }
        }

        public TerrainType this[GridPos pos]
        {
            get => _cells[pos.X, pos.Y];
            set => _cells[pos.X, pos.Y] = value;
        }

        public bool InBounds(GridPos pos)
        {
            return pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height;
        }

        public bool IsPassable(GridPos pos)
        {
            if (!InBounds(pos))
                return false;
            TerrainType terrain = this[pos];
            return terrain == TerrainType.Floor || terrain == TerrainType.Difficult;
        }

        public bool BlocksSight(GridPos pos)
        {
            if (!InBounds(pos))
                return true;
            TerrainType terrain = this[pos];
            return terrain == TerrainType.Wall || terrain == TerrainType.Pillar;
        }

        // Przekątne na przemian 5 i 10 stóp, czyli co druga liczy się podwójnie
        public static int DistanceFeet(GridPos a, GridPos b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            int longer = Math.Max(dx, dy);
            int shorter = Math.Min(dx, dy);
            return (longer + shorter / 2) * FeetPerSquare;
        }

        // Krawędzie pola startowego i docelowego nie zasłaniają, liczą się tylko pola pomiędzy
        public bool HasLineOfSight(GridPos from, GridPos to)
        {
            int x = from.X;
            int y = from.Y;
            int dx = Math.Abs(to.X - x);
            int dy = -Math.Abs(to.Y - y);
            int stepX = x < to.X ? 1 : -1;
            int stepY = y < to.Y ? 1 : -1;
            int error = dx + dy;

            while (x != to.X || y != to.Y)
            {
                int doubled = 2 * error;
                if (doubled >= dy)
                {
                    error += dy;
                    x += stepX;
                }
                if (doubled <= dx)
                {
                    error += dx;
                    y += stepY;
                }

                GridPos current = new(x, y);
                if (current != to && BlocksSight(current))
                    return false;
            }
            return true;
        }

        public IEnumerable<GridPos> AllCells()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                    yield return new GridPos(x, y);
            }
        }
    }
}
