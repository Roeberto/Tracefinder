using Godot;

public partial class MainMenu : Control
{
    private Button _characterCreatorButton;

    public override void _Ready()
    {
        _characterCreatorButton = GetNode<Button>("%CharacterCreatorButton");
        _characterCreatorButton.Pressed += OnCharacterCreatorButtonPressed;
    }

    private void OnCharacterCreatorButtonPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/CharacterCreator.tscn");
    }
}
