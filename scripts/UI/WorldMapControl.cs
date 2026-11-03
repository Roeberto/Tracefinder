using Godot;
using Tracefinder.World;

namespace Tracefinder.UI
{
    public partial class WorldMapControl : Control
    {
        private WorldMapCanvas _canvas;
        private LineEdit _seed;
        private Label _info;

        public override void _Ready()
        {
            _canvas = GetNode<WorldMapCanvas>("%MapCanvas");
            _seed = GetNode<LineEdit>("%SeedInput");
            _info = GetNode<Label>("%InfoLabel");
            _seed.Text = GameSession.WorldSeed.ToString();

            GetNode<Button>("%GenerateButton").Pressed += Generate;
            GetNode<Button>("%RandomButton").Pressed += () =>
            {
                _seed.Text = GD.RandRange(1, 999999).ToString();
                Generate();
            };
            GetNode<Button>("%BackButton").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
            _canvas.CellClicked += OnCellClicked;
            _canvas.CellHovered += (x, y) => _info.Text = $"{WorldMapCanvas.NameFor(_canvas.Map[x, y])} ({x}, {y})";

            HBoxContainer legend = GetNode<HBoxContainer>("Margin/Root/Legend");
            foreach (Biome biome in System.Enum.GetValues<Biome>())
            {
                legend.AddChild(new ColorRect { Color = WorldMapCanvas.ColorFor(biome), CustomMinimumSize = new Vector2(18, 18) });
                legend.AddChild(new Label { Text = WorldMapCanvas.NameFor(biome) });
            }

            Generate();
        }

        private void Generate()
        {
            if (!int.TryParse(_seed.Text, out int seed))
            {
                _info.Text = "Ziarno musi być liczbą całkowitą.";
                return;
            }
            GameSession.WorldSeed = seed;
            _canvas.Map = WorldMap.Generate(seed);
            _canvas.QueueRedraw();
            _info.Text = "Kliknij lokalizację, aby rozpocząć walkę.";
        }

        private void OnCellClicked(int x, int y)
        {
            Biome biome = _canvas.Map[x, y];
            if (!WorldMap.IsVisitable(biome))
            {
                _info.Text = "Na wodzie nie można stoczyć walki.";
                return;
            }
            GameSession.PendingLocation = LocationGenerator.Generate(_canvas.Map.Seed, x, y, biome);
            GetTree().ChangeSceneToFile("res://scenes/Encounter.tscn");
        }
    }
}
