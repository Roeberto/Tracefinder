using System;
using Godot;
using Tracefinder.World;

namespace Tracefinder.UI
{
    public partial class WorldMapCanvas : Control
    {
        public event Action<int, int> CellClicked;
        public event Action<int, int> CellHovered;
        public WorldMap Map { get; set; }
        public Campaign Campaign { get; set; }

        public static Color ColorFor(Biome biome)
        {
            switch (biome)
            {
                case Biome.Water:
                    return new Color(0.13f, 0.27f, 0.44f);
                case Biome.Plains:
                    return new Color(0.46f, 0.55f, 0.28f);
                case Biome.Forest:
                    return new Color(0.16f, 0.36f, 0.2f);
                case Biome.Mountains:
                    return new Color(0.45f, 0.43f, 0.42f);
                default:
                    return new Color(0.82f, 0.62f, 0.25f);
            }
        }

        public static string NameFor(Biome biome)
        {
            switch (biome)
            {
                case Biome.Water:
                    return "Woda";
                case Biome.Plains:
                    return "Równina";
                case Biome.Forest:
                    return "Las";
                case Biome.Mountains:
                    return "Góry";
                default:
                    return "Ruiny";
            }
        }

        private float CellSize => Map == null ? 0 : Mathf.Min(Size.X / Map.Width, Size.Y / Map.Height);

        private Vector2 Origin => (Size - new Vector2(Map.Width, Map.Height) * CellSize) / 2;

        public override void _Ready()
        {
            Resized += QueueRedraw;
        }

        public override void _Draw()
        {
            if (Map == null)
                return;

            float cell = CellSize;
            Vector2 origin = Origin;
            for (int y = 0; y < Map.Height; y++)
            {
                for (int x = 0; x < Map.Width; x++)
                    DrawRect(new Rect2(origin + new Vector2(x, y) * cell, new Vector2(cell, cell)), ColorFor(Map[x, y]));
            }

            if (Campaign != null)
                DrawMarkers(cell, origin);
        }

        private void DrawMarkers(float cell, Vector2 origin)
        {
            float radius = Mathf.Max(cell * 0.75f, 3);
            foreach (Site site in Campaign.Sites)
            {
                Vector2 center = origin + new Vector2(site.X + 0.5f, site.Y + 0.5f) * cell;
                Color color = site.Kind switch
                {
                    SiteKind.Town => new Color(1f, 0.85f, 0.35f),
                    SiteKind.Fortress => new Color(0.05f, 0.05f, 0.07f),
                    _ => Campaign.IsCleared(site) ? new Color(0.55f, 0.55f, 0.55f) : new Color(0.9f, 0.15f, 0.2f)
                };
                DrawCircle(center, radius + 1, new Color(1, 1, 1));
                DrawCircle(center, radius, color);
            }

            Vector2 hero = origin + new Vector2(Campaign.X + 0.5f, Campaign.Y + 0.5f) * cell;
            DrawArc(hero, radius + 3, 0, Mathf.Tau, 24, new Color(1, 1, 1), 2);
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (Map == null || CellSize <= 0 || @event is not InputEventMouse mouse)
                return;

            Vector2 local = (mouse.Position - Origin) / CellSize;
            int x = Mathf.FloorToInt(local.X);
            int y = Mathf.FloorToInt(local.Y);
            if (x < 0 || y < 0 || x >= Map.Width || y >= Map.Height)
                return;

            if (@event is InputEventMouseMotion)
                CellHovered?.Invoke(x, y);
            else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                CellClicked?.Invoke(x, y);
        }
    }
}
