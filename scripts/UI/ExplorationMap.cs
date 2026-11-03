using Godot;
using Tracefinder.World;

namespace Tracefinder.UI
{
    // Świat rysowany kafelkami z arkusza. Rysuje się raz, a na nowo tylko po zmianie stanu miejsc.
    public partial class ExplorationMap : Node2D
    {
        public const float CellSize = 64f;
        private const int TilePitch = 17;
        private const int TileSize = 16;

        private static readonly Vector2I Grass = new(10, 26);
        private static readonly Vector2I WaterTile = new(16, 26);
        private static readonly Vector2I Stone = new(4, 26);
        private static readonly Vector2I Dirt = new(1, 26);
        private static readonly Vector2I Sand = new(7, 26);
        private static readonly Vector2I[] Trees = { new(15, 9), new(18, 9), new(14, 9), new(15, 9) };
        private static readonly Color ForestShade = new(0.55f, 0.72f, 0.55f);
        private static readonly Color MountainShade = new(0.68f, 0.68f, 0.72f);
        private static readonly Vector2I[] Rocks = { new(54, 21), new(55, 21), new(56, 21), new(54, 20) };
        private static readonly Vector2I Campfire = new(14, 8);
        private static readonly Vector2I BurntCamp = new(13, 8);
        private static readonly Vector2I TentLeftTop = new(48, 10);
        private static readonly Vector2I StatueTop = new(50, 10);

        private Texture2D _sheet;

        public Campaign Campaign { get; set; }

        public override void _Ready()
        {
            _sheet = GD.Load<Texture2D>("res://assets/Floor/roguelikeSheet_transparent.png");
            TextureFilter = TextureFilterEnum.Nearest;
        }

        private void Tile(Vector2I atlas, Rect2 target, Color? shade = null)
        {
            Rect2 region = new(atlas.X * TilePitch, atlas.Y * TilePitch, TileSize, TileSize);
            DrawTextureRectRegion(_sheet, target, region, shade ?? new Color(1, 1, 1));
        }

        public override void _Draw()
        {
            if (Campaign == null)
                return;

            WorldMap map = Campaign.Map;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Rect2 cell = new(x * CellSize, y * CellSize, CellSize, CellSize);
                    Biome biome = map[x, y];
                    Tile(GroundFor(biome), cell, biome == Biome.Forest ? ForestShade : biome == Biome.Mountains ? MountainShade : null);
                    int variant = (x * 7 + y * 13) % 4;
                    if (biome == Biome.Forest)
                        Tile(Trees[variant], cell);
                    else if (biome == Biome.Mountains && (x + y) % 2 == 0)
                        Tile(Rocks[variant], cell);
                }
            }

            foreach (Site site in Campaign.Sites)
                DrawSite(site);
        }

        private static Vector2I GroundFor(Biome biome)
        {
            switch (biome)
            {
                case Biome.Water:
                    return WaterTile;
                case Biome.Mountains:
                    return Stone;
                case Biome.Settlement:
                    return Sand;
                default:
                    return Grass;
            }
        }

        private void DrawSite(Site site)
        {
            Rect2 cell = new(site.X * CellSize, site.Y * CellSize, CellSize, CellSize);
            Tile(Dirt, cell);

            switch (site.Kind)
            {
                case SiteKind.Town:
                    float half = CellSize / 2;
                    Tile(TentLeftTop, new Rect2(cell.Position, new Vector2(half, half)));
                    Tile(TentLeftTop + new Vector2I(1, 0), new Rect2(cell.Position + new Vector2(half, 0), new Vector2(half, half)));
                    Tile(TentLeftTop + new Vector2I(0, 1), new Rect2(cell.Position + new Vector2(0, half), new Vector2(half, half)));
                    Tile(TentLeftTop + new Vector2I(1, 1), new Rect2(cell.Position + new Vector2(half, half), new Vector2(half, half)));
                    break;
                case SiteKind.Lair:
                    Tile(Campaign.IsCleared(site) ? BurntCamp : Campfire, cell);
                    break;
                default:
                    float part = CellSize / 2;
                    Tile(StatueTop, new Rect2(cell.Position + new Vector2(part / 2, 0), new Vector2(part, part)));
                    Tile(StatueTop + new Vector2I(0, 1), new Rect2(cell.Position + new Vector2(part / 2, part), new Vector2(part, part)));
                    break;
            }

            Font font = ThemeDB.FallbackFont;
            const int fontSize = 12;
            // Nazwa nie może wyjść poza świat przy krawędzi mapy
            float textLeft = Mathf.Clamp(cell.Position.X - CellSize, 0, Campaign.Map.Width * CellSize - CellSize * 3);
            Vector2 textPosition = new(textLeft, cell.Position.Y - 4);
            DrawString(font, textPosition + new Vector2(1, 1), site.Name, HorizontalAlignment.Center, CellSize * 3, fontSize, new Color(0, 0, 0));
            DrawString(font, textPosition, site.Name, HorizontalAlignment.Center, CellSize * 3, fontSize, new Color(1, 1, 1));
        }
    }
}
