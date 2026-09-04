using Godot;

public partial class PlayerControl : CharacterBody2D
{
	[Export] public float Speed = 200f;

	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		// pobieramy referencję do dziecka o nazwie "Sprite2D"
		// (u Ciebie w drzewie sceny nazywa się tak mimo że to AnimatedSprite2D)
		_sprite = GetNode<AnimatedSprite2D>("Sprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		// kierunek ruchu na podstawie wciśniętych klawiszy, znormalizowany
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		Velocity = direction * Speed;
		MoveAndSlide();

		UpdateAnimation(direction);
	}

	private void UpdateAnimation(Vector2 direction)
	{
		if (direction == Vector2.Zero)
		{
			_sprite.Stop();
			return;
		}

		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			// ruch bardziej poziomy niż pionowy
			if (direction.X > 0)
				_sprite.Play("walk_right");
			else
				_sprite.Play("walk_left");
		}
		else
		{
			// ruch bardziej pionowy niż poziomy
			if (direction.Y > 0)
				_sprite.Play("walk_down");
			else
				_sprite.Play("walk_up");
		}
	}
}
