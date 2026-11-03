using System.Linq;
using Godot;
using Tracefinder.Character;
using Tracefinder.Gameplay;
using Tracefinder.World;

namespace Tracefinder.UI
{
    public partial class ExplorationView : Node2D
    {
        private const float CellSize = ExplorationMap.CellSize;

        private Campaign _campaign;
        private CharacterBody2D _player;
        private Label _message;
        private Vector2 _lastValid;
        private bool _leaving;

        public override void _Ready()
        {
            _campaign = GameSession.Campaign;
            if (_campaign == null)
            {
                Leave("res://scenes/MainMenu.tscn");
                return;
            }
            if (_campaign.Completed)
            {
                GameSession.EndingVictory = true;
                Leave("res://scenes/Ending.tscn");
                return;
            }

            AddWasdKeys();
            _player = GetNode<CharacterBody2D>("Player");
            _message = GetNode<Label>("%MessageLabel");
            ExplorationMap map = GetNode<ExplorationMap>("MapLayer");
            map.Campaign = _campaign;
            map.QueueRedraw();

            _player.Position = CenterOf(_campaign.X, _campaign.Y);
            _lastValid = _player.Position;
            Camera2D camera = _player.GetNode<Camera2D>("Camera2D");
            // Zapas na górze i dole, żeby pasek i panel miejsca nie zasłaniały bohatera na krawędzi świata
            camera.LimitLeft = 0;
            camera.LimitTop = -(int)CellSize;
            camera.LimitRight = (int)(_campaign.Map.Width * CellSize);
            camera.LimitBottom = (int)((_campaign.Map.Height + 1.5f) * CellSize);

            WorldMapCanvas mini = GetNode<WorldMapCanvas>("%MiniMap");
            mini.Map = _campaign.Map;
            mini.Campaign = _campaign;

            GetNode<Button>("%SheetButton").Pressed += () => Leave("res://scenes/CharacterSheet.tscn");
            GetNode<Button>("%LevelUpButton").Pressed += () => Leave("res://scenes/LevelUp.tscn");
            GetNode<Button>("%MenuButton").Pressed += () =>
            {
                CampaignStorage.Save();
                Leave("res://scenes/MainMenu.tscn");
            };
            GetNode<Button>("%CampButton").Pressed += OnCamp;
            GetNode<Button>("%TreatButton").Pressed += OnTreat;
            GetNode<Button>("%SiteActionButton").Pressed += OnSiteAction;
            GetNode<Button>("%ShopButton").Pressed += () => Leave("res://scenes/Shop.tscn");

            Refresh();
        }

        // Arkusz, walka i sklep to osobne sceny, więc zmiana sceny musi zaczekać do końca bieżącej klatki
        private void Leave(string scene)
        {
            _leaving = true;
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, scene);
        }

        private static void AddWasdKeys()
        {
            (string action, Key key)[] keys = { ("ui_left", Key.A), ("ui_right", Key.D), ("ui_up", Key.W), ("ui_down", Key.S) };
            foreach ((string action, Key key) in keys)
            {
                bool present = InputMap.ActionGetEvents(action).OfType<InputEventKey>().Any(e => e.PhysicalKeycode == key);
                if (!present)
                    InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
            }
        }

        private static Vector2 CenterOf(int x, int y)
        {
            return new Vector2((x + 0.5f) * CellSize, (y + 0.5f) * CellSize);
        }

        private bool Walkable(Vector2 position)
        {
            return _campaign.CanWalk(Mathf.FloorToInt(position.X / CellSize), Mathf.FloorToInt(position.Y / CellSize));
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_leaving || _campaign == null)
                return;

            _player.Position = KeepOnLand(_player.Position);
            _lastValid = _player.Position;

            int x = Mathf.FloorToInt(_player.Position.X / CellSize);
            int y = Mathf.FloorToInt(_player.Position.Y / CellSize);
            if (x == _campaign.X && y == _campaign.Y)
                return;

            TravelEvent happened = _campaign.Travel(x, y, out EncounterPlan ambush);
            CampaignStorage.Save();
            if (happened == TravelEvent.Ambush)
            {
                StartFight(ambush);
                return;
            }
            _message.Text = "";
            Refresh();
        }

        // Woda i krawędź świata zatrzymują bohatera, ale pozwalają ślizgać się wzdłuż brzegu
        private Vector2 KeepOnLand(Vector2 position)
        {
            if (Walkable(position))
                return position;

            Vector2 horizontal = new(position.X, _lastValid.Y);
            if (Walkable(horizontal))
                return horizontal;

            Vector2 vertical = new(_lastValid.X, position.Y);
            return Walkable(vertical) ? vertical : _lastValid;
        }

        private void StartFight(EncounterPlan plan)
        {
            CampaignStorage.Save();
            GameSession.PendingPlan = plan;
            Leave("res://scenes/Encounter.tscn");
        }

        private void OnCamp()
        {
            EncounterPlan ambush = _campaign.Camp();
            CampaignStorage.Save();
            if (ambush != null)
            {
                StartFight(ambush);
                return;
            }
            _message.Text = "Spokojna noc. Bohater odzyskał siły i zaklęcia.";
            Refresh();
        }

        private void OnTreat()
        {
            HealingResult result = _campaign.TreatWounds();
            CampaignStorage.Save();
            _message.Text = result.Healed > 0
                ? $"Opatrzenie ran: {result.Healed} HP."
                : result.Damage > 0 ? $"Opatrzenie pogorszyło rany: {result.Damage} obrażeń." : "Nie udało się opatrzyć ran.";
            Refresh();
        }

        private void OnSiteAction()
        {
            Site site = _campaign.CurrentSite;
            if (site == null)
                return;

            if (site.Kind == SiteKind.Town)
            {
                _campaign.RestInTown();
                CampaignStorage.Save();
                _message.Text = "Bohater wypoczął, a kowal naprawił tarczę.";
                Refresh();
                return;
            }
            StartFight(_campaign.SitePlan(site));
        }

        private void Refresh()
        {
            CharacterCore hero = _campaign.Hero;
            GetNode<Label>("%HeroLabel").Text = $"{hero.Name}, {hero.ClassID}, poziom {hero.Level}";
            GetNode<Label>("%StatsLabel").Text = $"HP {hero.Health.CurrentHp}/{hero.Health.MaxHp}    {UiText.Money(hero.Silver)}    Hero Points {hero.Health.HeroPoints}    XP {hero.Experience}/{Progression.ExperiencePerLevel}";
            GetNode<Label>("%ObjectiveLabel").Text = _campaign.Objective();

            int pending = LevelUp.PendingChoices(hero, _campaign.HeroClass);
            GetNode<Button>("%LevelUpButton").Text = pending > 0 ? $"Rozwój ({pending})" : "Rozwój";

            Site site = _campaign.CurrentSite;
            GetNode<Button>("%CampButton").Disabled = site?.Kind == SiteKind.Town;
            GetNode<Button>("%TreatButton").Disabled = !_campaign.CanTreatWounds;
            GetNode<WorldMapCanvas>("%MiniMap").QueueRedraw();
            GetNode<ExplorationMap>("MapLayer").QueueRedraw();
            RefreshSitePanel(site);
        }

        private void RefreshSitePanel(Site site)
        {
            PanelContainer panel = GetNode<PanelContainer>("%SitePanel");
            panel.Visible = site != null;
            if (site == null)
                return;

            GetNode<Label>("%SiteName").Text = site.Name;
            Button action = GetNode<Button>("%SiteActionButton");
            GetNode<Button>("%ShopButton").Visible = site.Kind == SiteKind.Town;

            string description = site.Description;
            action.Visible = true;
            action.Disabled = false;
            switch (site.Kind)
            {
                case SiteKind.Town:
                    action.Text = "Odpocznij";
                    break;
                case SiteKind.Lair:
                    action.Text = "Zaatakuj";
                    if (_campaign.IsCleared(site))
                    {
                        action.Visible = false;
                        description = "Leże zniszczone, klucz zdobyty.";
                    }
                    break;
                default:
                    action.Text = "Wejdź do twierdzy";
                    action.Disabled = !_campaign.FortressOpen;
                    break;
            }
            GetNode<Label>("%SiteDescription").Text = description;
        }

        // Do testów interfejsu bez okna
        public bool IsWalkable(int x, int y)
        {
            return _campaign.CanWalk(x, y);
        }

        // Pola miejsc danego rodzaju, do testów interfejsu bez okna
        public Godot.Collections.Array<Vector2I> SiteCells(string kind)
        {
            return new Godot.Collections.Array<Vector2I>(_campaign.Sites.Where(s => s.Kind.ToString() == kind).Select(s => new Vector2I(s.X, s.Y)));
        }
    }
}
