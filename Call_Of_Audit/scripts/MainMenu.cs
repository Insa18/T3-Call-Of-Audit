using Godot;

/// <summary>
/// Contrôleur du menu principal (UI).
/// Récupère et connecte les boutons du menu, et fournit les callbacks pour
/// lancer la scène de jeu, ouvrir les paramètres et quitter l'application.
/// </summary>
public partial class MainMenu : Control
{
	// Références aux boutons du menu (doivent exister dans l'arbre de scène)
	private Button _buttonPlay;
	private Button _buttonSettings;
	private Button _buttonQuit;

	// Conteneur de l'UI des paramètres (doit exister en enfant si souhaité)
	private Control _settingsMenu;
	private Button _buttonSettingsBack;
	private HSlider _masterVolumeSlider;

	/// <summary>
	/// Initialisation : met le noeud en mode Always (pour fonctionner même si le
	/// tree est en pause), récupère les boutons et les connecte aux callbacks.
	/// </summary>
	public override void _Ready()
	{
		// Le menu doit toujours fonctionner
		ProcessMode = Node.ProcessModeEnum.Always;

		// Récupération des boutons (chemins EXACTS)
		_buttonPlay     = GetNode<Button>("ButtonPlay");
		_buttonSettings = GetNode<Button>("ButtonSettings");
		_buttonQuit     = GetNode<Button>("ButtonQuit");

		// Connexion des boutons
		_buttonPlay.Pressed     += OnPlayPressed;
		_buttonSettings.Pressed += OnSettingsPressed;
		_buttonQuit.Pressed     += OnQuitPressed;

		// Récupérer l'élément UI des paramètres s'il existe (nom attendu : "SettingsMenu")
		_settingsMenu = GetNodeOrNull<Control>("SettingsMenu");
		if (_settingsMenu != null)
		{
			// Par défaut cacher le panneau des settings
			_settingsMenu.Visible = false;

			// Rechercher un bouton de retour dans le panneau des settings (nom commun : ButtonBack ou BackButton)
			_buttonSettingsBack = _settingsMenu.GetNodeOrNull<Button>("ButtonBack") ?? _settingsMenu.GetNodeOrNull<Button>("BackButton");
			if (_buttonSettingsBack != null)
			{
				_buttonSettingsBack.Pressed += OnSettingsBackPressed;
			}

			// Chercher un HSlider (contrôle du volume principal) dans le panneau Settings
			_masterVolumeSlider = FindFirstHSlider(_settingsMenu);
			if (_masterVolumeSlider != null)
			{
				// Initialiser la valeur du slider (0-100) à partir du volume global
				_masterVolumeSlider.Value = Global.MasterVolume * 100.0;
				_masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
				// Appliquer immédiatement le volume courant
				ApplyMasterVolume(Global.MasterVolume);
			}
		}

		// Souris visible dans les menus
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	/// <summary>
	/// Démarre la scène principale du jeu (change scene vers l'accueil/room).
	/// </summary>
	private void OnPlayPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/rooms/accueil.tscn");
	}

	/// <summary>
	/// Placeholder pour ouvrir les paramètres. À implémenter si nécessaire.
	/// </summary>
	private void OnSettingsPressed()
	{
		// Toggle visibility du panneau de paramètres si présent
		if (_settingsMenu != null)
		{
			_settingsMenu.Visible = !_settingsMenu.Visible;
			if (_settingsMenu.Visible)
			{
				// S'assurer que la souris est visible pour interagir avec l'UI
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
			return;
		}

		// Si le conteneur des paramètres n'existe pas, logguer pour informer le développeur
		GD.PrintErr("SettingsMenu introuvable : créez un Control enfant nommé 'SettingsMenu' pour que le bouton Settings fonctionne.");
	}

	/// <summary>
	/// Handler pour le bouton "Back" du panneau Settings : cache le panneau
	/// et restaure l'état du menu principal.
	/// </summary>
	private void OnSettingsBackPressed()
	{
		if (_settingsMenu == null)
			return;

		_settingsMenu.Visible = false;
		// Optionnel : vous pouvez recapturer la souris si le jeu doit reprendre le focus
		// Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	/// <summary>
	/// Recherche récursive du premier HSlider dans le Control fourni.
	/// Retourne null si aucun HSlider n'est trouvé.
	/// </summary>
	private HSlider FindFirstHSlider(Control root)
	{
		foreach (Node child in root.GetChildren())
		{
			if (child is HSlider hs)
				return hs;
			if (child is Control c)
			{
				var found = FindFirstHSlider(c);
				if (found != null)
					return found;
			}
		}
		return null;
	}

	/// <summary>
	/// Callback pour la ValueChanged du slider (valeur 0-100).
	/// Convertit en 0..1 et applique sur le bus Master.
	/// </summary>
	private void OnMasterVolumeChanged(double value)
	{
		float linear = (float)value / 100.0f;
		Global.MasterVolume = linear;
		ApplyMasterVolume(linear);
	}

	/// <summary>
	/// Applique le volume linéaire (0..1) au bus Master en dB.
	/// Gère le cas muet en forçant une valeur très faible (-80 dB).
	/// </summary>
	private void ApplyMasterVolume(float linear)
	{
		int bus = AudioServer.GetBusIndex("Master");
		if (bus < 0)
		{
			GD.PrintErr("Bus 'Master' introuvable — impossible d'appliquer le volume.");
			return;
		}

		float db;
		if (linear <= 0f)
			db = -80f; // muet
		else
			db = Mathf.LinearToDb(linear);

		AudioServer.SetBusVolumeDb(bus, db);
	}

	/// <summary>
	/// Quitte l'application (utilise SceneTree.Quit()).
	/// </summary>
	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
