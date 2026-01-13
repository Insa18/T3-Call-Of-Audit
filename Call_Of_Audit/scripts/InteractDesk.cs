using Godot;
using System;

/// <summary>
/// Zone d'interaction générique. Attacher à une `Area2D` qui possède une
/// collision shape. Quand le joueur entre dans la zone et appuie sur la
/// touche `interact` (E), démarre la timeline Dialogic configurée.
/// </summary>
public partial class InteractDesk : Area2D
{
	public string TimelineName = "res://addons/dialogic/characters/Accueil de lIUT.dtl";

	private Node2D _playerRef = null;
	private Node _dialogueManager = null;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		// On cherche le node qui contient le script dialogic.gd dans la scène
		_dialogueManager = GetTree().CurrentScene.GetNodeOrNull<Node>("Node2D");
		if (_dialogueManager == null)
			GD.Print("InteractDesk: Dialogue manager Node2D introuvable (ok si vous utilisez /root/Dialogic)");
	}

	private void OnBodyEntered(Node body)
	{
		if (body.IsInGroup("player"))
		{
			_playerRef = body as Node2D;
		}
	}

	private void OnBodyExited(Node body)
	{
		if (body == _playerRef)
			_playerRef = null;
	}

	public override void _Process(double delta)
	{
		if (_playerRef == null)
			return;

		if (Input.IsActionJustPressed("interact"))
		{
			StartDialogue();
		}
	}

	private void StartDialogue()
	{
		if (string.IsNullOrEmpty(TimelineName))
			return;

		// Mettre le joueur en état dialogue (il existe une méthode SetInDialogue)
		_playerRef?.Call("SetInDialogue", true);

		if (_dialogueManager != null)
		{
			_dialogueManager.Call("StartDialogue", TimelineName);
			return;
		}

		// Fallback : utiliser l'autoload Dialogic si présent
		var rootDialogic = GetNodeOrNull("/root/Dialogic");
		if (rootDialogic != null)
		{
			rootDialogic.Call("start", TimelineName);
			return;
		}

		GD.PrintErr("InteractDesk: impossible de démarrer le dialogue — gestionnaire introuvable.");
	}
}
