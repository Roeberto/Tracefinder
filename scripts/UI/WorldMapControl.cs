using Godot;
using Tracefinder.Character;
using Tracefinder.World;

namespace Tracefinder.UI
{
    // Wybór świata przed rozpoczęciem przygody
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
            GetNode<Button>("%BackButton").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/CharacterCreator.tscn");
            Button start = GetNode<Button>("%StartButton");
            start.Disabled = GameSession.PendingHero == null;
            start.Pressed += OnStart;
            _canvas.CellHovered += OnHover;

            HBoxContainer legend = GetNode<HBoxContainer>("Margin/Root/Legend");
            foreach (Biome biome in System.Enum.GetValues<Biome>())
            {
                legend.AddChild(new ColorRect { Color = WorldMapCanvas.ColorFor(biome), CustomMinimumSize = new Vector2(18, 18) });
                legend.AddChild(new Label { Text = WorldMapCanvas.NameFor(biome) });
            }

            Generate();
        }

        private void OnHover(int x, int y)
        {
            Site site = _canvas.Campaign?.SiteAt(x, y);
            _info.Text = site != null ? site.Name : WorldMapCanvas.NameFor(_canvas.Map[x, y]);
        }

        private void Generate()
        {
            if (!int.TryParse(_seed.Text, out int seed))
            {
                _info.Text = "Ziarno musi być liczbą całkowitą.";
                return;
            }

            // Podgląd na przykładowej postaci: bohater dostanie sakiewkę dopiero na starcie
            Campaign preview = Campaign.New(seed, GameSession.ExampleCharacter());
            GameSession.WorldSeed = preview.Seed;
            _seed.Text = preview.Seed.ToString();
            _canvas.Map = preview.Map;
            _canvas.Campaign = preview;
            _canvas.QueueRedraw();
            _info.Text = "Złote kropki to osady, czerwone to leża, a czarna to Popielna Twierdza.";
        }

        private void OnStart()
        {
            GameSession.Campaign = Campaign.New(GameSession.WorldSeed, GameSession.PendingHero);
            GameSession.PendingHero = null;
            CampaignStorage.Save();
            GetTree().ChangeSceneToFile("res://scenes/Exploration.tscn");
        }
    }
}
