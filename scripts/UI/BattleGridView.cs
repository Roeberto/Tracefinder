using System;
using System.Collections.Generic;
using Godot;
using Tracefinder.Combat;

namespace Tracefinder.UI
{
    public partial class BattleGridView : Control
    {
        private static readonly Color Floor = new(0.24f, 0.25f, 0.26f);
        private static readonly Color Difficult = new(0.36f, 0.31f, 0.19f);
        private static readonly Color Water = new(0.14f, 0.28f, 0.44f);
        private static readonly Color Wall = new(0.07f, 0.07f, 0.08f);
        private static readonly Color Pillar = new(0.5f, 0.5f, 0.52f);
        private static readonly Color GridLine = new(0.11f, 0.12f, 0.13f);
        private static readonly Color PartyColor = new(0.28f, 0.6f, 0.86f);
        private static readonly Color EnemyColor = new(0.78f, 0.2f, 0.22f);
        private static readonly Color Highlight = new(0.95f, 0.8f, 0.3f, 0.32f);

        public event Action<GridPos> CellClicked;
        public Encounter Encounter { get; set; }
        public HashSet<GridPos> Highlighted { get; } = new();

        private float CellSize => Encounter == null
            ? 0
            : Mathf.Min(Size.X / Encounter.Grid.Width, Size.Y / Encounter.Grid.Height);

        private Vector2 Origin => (Size - new Vector2(Encounter.Grid.Width, Encounter.Grid.Height) * CellSize) / 2;

        public override void _Ready()
        {
            Resized += QueueRedraw;
        }

        public override void _Draw()
        {
            if (Encounter == null)
                return;

            float cell = CellSize;
            Vector2 origin = Origin;
            BattleGrid grid = Encounter.Grid;

            foreach (GridPos pos in grid.AllCells())
            {
                Rect2 rect = new(origin + new Vector2(pos.X, pos.Y) * cell, new Vector2(cell, cell));
                TerrainType terrain = grid[pos];
                DrawRect(rect, ColorFor(terrain));
                if (terrain == TerrainType.Pillar)
                    DrawCircle(rect.GetCenter(), cell * 0.32f, Pillar);
                if (Highlighted.Contains(pos))
                    DrawRect(rect, Highlight);
                DrawRect(rect, GridLine, false, 1);
            }

            Font font = ThemeDB.FallbackFont;
            foreach (Combatant unit in Encounter.Combatants)
                DrawUnit(unit, cell, origin, font);
        }

        private void DrawUnit(Combatant unit, float cell, Vector2 origin, Font font)
        {
            Rect2 rect = new(origin + new Vector2(unit.Position.X, unit.Position.Y) * cell, new Vector2(cell, cell));
            Vector2 center = rect.GetCenter();
            float radius = cell * 0.38f;

            if (unit.IsOut)
            {
                DrawCircle(center, radius * 0.7f, new Color(0.3f, 0.3f, 0.32f));
                return;
            }

            if (unit == Encounter.Current)
                DrawArc(center, radius + 3, 0, Mathf.Tau, 32, new Color(1, 1, 1), 3);

            DrawCircle(center, radius, unit.Side == Tracefinder.Combat.Side.Party ? PartyColor : EnemyColor);

            int fontSize = Mathf.Max((int)(cell * 0.42f), 8);
            string letter = unit.Name.Length > 0 ? unit.Name.Substring(0, 1) : "?";
            DrawString(font, new Vector2(rect.Position.X, center.Y + fontSize * 0.35f), letter, HorizontalAlignment.Center, cell, fontSize, new Color(1, 1, 1));

            float fraction = (float)unit.Health.CurrentHp / Mathf.Max(unit.Health.MaxHp, 1);
            Rect2 barBack = new(rect.Position + new Vector2(cell * 0.1f, cell * 0.86f), new Vector2(cell * 0.8f, cell * 0.08f));
            DrawRect(barBack, new Color(0, 0, 0));
            DrawRect(new Rect2(barBack.Position, new Vector2(barBack.Size.X * fraction, barBack.Size.Y)), new Color(0.35f, 0.8f, 0.4f));
        }

        private static Color ColorFor(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Difficult:
                    return Difficult;
                case TerrainType.Water:
                    return Water;
                case TerrainType.Wall:
                    return Wall;
                default:
                    return Floor;
            }
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (Encounter == null || @event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
                return;

            float cell = CellSize;
            if (cell <= 0)
                return;

            Vector2 local = (click.Position - Origin) / cell;
            GridPos pos = new(Mathf.FloorToInt(local.X), Mathf.FloorToInt(local.Y));
            if (Encounter.Grid.InBounds(pos))
                CellClicked?.Invoke(pos);
        }
    }
}
