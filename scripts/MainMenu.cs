using Godot;

namespace Tracefinder
{

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		Connect("%PlayButton", "res://scenes/WorldView.tscn");
		Connect("%WorldButton", "res://scenes/WorldMap.tscn");
		Connect("%EncounterButton", "res://scenes/Encounter.tscn");
		Connect("%CreatorButton", "res://scenes/CharacterCreator.tscn");
		Connect("%SheetButton", "res://scenes/CharacterSheet.tscn");
		GetNode<Button>("%QuitButton").Pressed += () => GetTree().Quit();
	}

	private void Connect(string button, string scene)
	{
		GetNode<Button>(button).Pressed += () => GetTree().ChangeSceneToFile(scene);
	}
}

}
