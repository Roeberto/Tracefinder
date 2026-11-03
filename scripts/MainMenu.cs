using Godot;
using Tracefinder.UI;
using Tracefinder.World;

namespace Tracefinder
{

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		// Niezapisany stan z poprzedniej sceny znika, a Kontynuuj zawsze czyta plik
		GameSession.Campaign = null;
		GameSession.Character = null;
		GameSession.PendingHero = null;
		GameSession.PendingPlan = null;

		Button continueButton = GetNode<Button>("%ContinueButton");
		continueButton.Disabled = !CampaignStorage.HasSave;
		continueButton.Pressed += OnContinue;
		GetNode<Button>("%NewGameButton").Pressed += OnNewGame;
		GetNode<Button>("%QuitButton").Pressed += () => GetTree().Quit();
	}

	private void OnContinue()
	{
		if (!CampaignStorage.Load())
		{
			GetNode<Label>("%MenuMessage").Text = "Nie udało się odczytać zapisu gry.";
			return;
		}
		GetTree().ChangeSceneToFile("res://scenes/Exploration.tscn");
	}

	private void OnNewGame()
	{
		if (!CampaignStorage.HasSave)
		{
			GetTree().ChangeSceneToFile("res://scenes/CharacterCreator.tscn");
			return;
		}

		ConfirmationDialog dialog = new()
		{
			Name = "NewGameDialog",
			Title = "Nowa gra",
			DialogText = "Nowa przygoda po rozpoczęciu zastąpi obecny zapis gry. Kontynuować?",
			OkButtonText = "Tak",
			CancelButtonText = "Nie"
		};
		dialog.Confirmed += () => GetTree().ChangeSceneToFile("res://scenes/CharacterCreator.tscn");
		AddChild(dialog);
		dialog.PopupCentered();
	}
}

}
