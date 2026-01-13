using Godot;
using System;

/// <summary>
/// Écran de défaite (Loose/ Game Over).
/// Gère les boutons permettant de recommencer la partie ou de quitter le jeu.
/// </summary>
public partial class LooseScreen : Control
{
	private Button _buttonRestart;
	private Button _buttonQuit;

	/// <summary>
	/// Récupère les boutons enfants (attendus noms : ButtonRestart, ButtonQuit)
	/// et connecte leurs callbacks. Si un bouton est absent, on loggue une erreur.
	/// </summary>
	public override void _Ready()
	{
		_buttonRestart = GetNodeOrNull<Button>("ButtonRestart");
		_buttonQuit = GetNodeOrNull<Button>("ButtonQuit");

		if (_buttonRestart != null)																															
			_buttonRestart.Pressed += OnRestartPressed;
		else
			GD.PrintErr("LooseScreen: ButtonRestart introuvable (attendu comme enfant).");

		if (_buttonQuit != null)
			_buttonQuit.Pressed += OnQuitPressed;
		else
			GD.PrintErr("LooseScreen: ButtonQuit introuvable (attendu comme enfant).");
	}

	/// <summary>
	/// Callback appelé quand l'utilisateur clique sur Recommencer.
	/// Par défaut on renvoie à la scène d'accueil principale.
	/// </summary>
	private void OnRestartPressed()
	{
		// Remplacez la route par celle de la scène à lancer pour recommencer le jeu
		GetTree().ChangeSceneToFile("res://scenes/rooms/accueil.tscn");
	}

	/// <summary>
	/// Callback appelé quand l'utilisateur clique sur Quitter : quitte l'application.
	/// </summary>
	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
