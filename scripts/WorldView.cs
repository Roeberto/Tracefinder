using Godot;

namespace Tracefinder
{

public partial class WorldView : Node2D
{
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_cancel"))
			GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
	}
}

}
