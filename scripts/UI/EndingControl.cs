using Godot;
using Tracefinder.Character;
using Tracefinder.World;

namespace Tracefinder.UI
{
    public partial class EndingControl : Control
    {
        public override void _Ready()
        {
            Campaign campaign = GameSession.Campaign;
            bool victory = GameSession.EndingVictory;
            GetNode<Label>("%TitleLabel").Text = victory ? "Zwycięstwo" : "Bohater upadł";

            CharacterCore hero = campaign?.Hero;
            bool canRecover = !victory && hero != null && hero.Health.HeroPoints > 0;
            GetNode<Label>("%StoryLabel").Text = victory
                ? "Władca Popiołów nie żyje, a Popielna Twierdza pustoszeje. Drogi znów są bezpieczne, a o bohaterze będą śpiewać w każdej osadzie."
                : canRecover
                    ? "Bohater leży bez ducha. Wszystkie Hero Pointy mogą jeszcze odwrócić los: obudzi się ranny w najbliższej osadzie."
                    : "Nie ma już Hero Pointów, które mogłyby odwrócić los. Pozostaje wrócić do chwili sprzed tej walki.";

            if (hero != null)
                GetNode<Label>("%StatsLabel").Text = $"{hero.Name}, {hero.ClassID}, poziom {hero.Level}\nWygrane walki: {campaign.FightsWon}\nKroki: {campaign.StepsTaken}\nLeża: {campaign.LairsCleared} z {Campaign.LairCount}\nSakiewka: {UiText.Money(hero.Silver)}";

            Button recover = GetNode<Button>("%RecoverButton");
            recover.Visible = canRecover;
            recover.Pressed += OnRecover;

            Button load = GetNode<Button>("%LoadButton");
            load.Visible = !victory && CampaignStorage.HasSave;
            load.Pressed += OnLoad;

            GetNode<Button>("%MenuButton").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        }

        private void OnRecover()
        {
            GameSession.Campaign.HeroicRecovery();
            CampaignStorage.Save();
            GetTree().ChangeSceneToFile("res://scenes/Exploration.tscn");
        }

        private void OnLoad()
        {
            if (CampaignStorage.Load())
                GetTree().ChangeSceneToFile("res://scenes/Exploration.tscn");
        }
    }
}
