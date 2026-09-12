using Godot;
using System;
using System.Reflection.Metadata;


public partial class MainMenu : Node2D
{

	[Export] private Button _PlayButton;
	[Export] private Button _SettingsButton;
	[Export] private Button _QuitButton;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		_PlayButton.Pressed += OnPlayButtonPressed;
		_QuitButton.Pressed += OnQuitButtonPressed;
		
	}

	private void OnPlayButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/WorldView.tscn");
	}
	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}



	public override void _Process(double delta)
	{
	}




}
