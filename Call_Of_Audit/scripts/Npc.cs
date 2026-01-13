using Godot;
using System;

/// <summary>
/// Comportement d'un PNJ (NPC).
/// Gère la promenade aléatoire, l'animation, l'interaction avec le joueur,
/// les dialogues conditionnels et une jauge de peur.
/// </summary>
public partial class Npc : CharacterBody2D
{
	// =======================
	// Types de PNJ
	// =======================

	/// <summary>
	/// Types possibles de PNJ pour différencier leurs dialogues.
	/// </summary>
	public enum NpcType
	{
		Rmouque,
		Ayadi,
		Lebot,
		Roy,
		Directeur,
		Haristoy
	}

	/// <summary>
	/// Type de ce PNJ (configurable dans l'inspecteur).
	/// </summary>
	[Export] public NpcType Type;


	// =======================
	// Dialogues
	// =======================

	/// <summary>
	/// Timeline forcée pour ce PNJ (prioritaire si non vide).
	/// </summary>
	[Export] public string TimelineName = "";


	/// <summary>
	/// Pour savoir si il a déjà parlé au PNJ.
	/// </summary>
	[Export] public string NpcName = "Garde"; // Pour identifier le PNJ
    public bool HasSpoken { get; private set; } = false;

	/// <summary>
	/// Timeline si coopération faible.
	/// </summary>
	[Export] public string DialogueCooperationFaible = "timeline_me_fiance";

	/// <summary>
	/// Timeline si coopération élevée.
	/// </summary>
	[Export] public string DialogueCooperationElevee = "timeline_cooperation";

	/// <summary>
	/// Timelines spécifiques par type de PNJ.
	/// </summary>
	[Export] public string VillagerTimeline;
	[Export] public string MerchantTimeline;
	[Export] public string RmouqueTimeline = "Ilias_responsable_informatique";
	[Export] public string AyadiTimeline = "monsieurAyadi";

	// =======================
	// Déplacement
	// =======================

	/// <summary>
	/// Vitesse de déplacement du PNJ.
	/// </summary>
	[Export] public float Speed = 10f;

	/// <summary>
	/// Intervalle entre deux changements de direction.
	/// </summary>
	[Export] public float ActionInterval = 2f;

	// =======================
	// Internes
	// =======================

	private Vector2 _direction = Vector2.Zero;
	private Random _random = new();
	private AnimatedSprite2D _anim;
	private Timer _timer;

	private Vector2 _lastPosition;
	private float _stuckTime;

	// =======================
	// Interaction
	// =======================

	private bool _playerNear = false;
	private Node2D _playerRef = null; // référence au joueur quand il est à portée
	private Node _dialogueManager;

	// =======================
	// Initialisation
	// =======================

	/// <summary>
	/// Initialise le PNJ : animation, timer,dialogue et zone de chat.
	/// </summary>
	public override void _Ready()
	{
		_anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_timer = GetNode<Timer>("Timer");

		_timer.WaitTime = ActionInterval;
		_timer.Timeout += OnTimerTimeout;
		_timer.Start();

		ChooseNewDirection();
		_lastPosition = GlobalPosition;

		// ---- DialogueManager ----
		_dialogueManager = GetTree().CurrentScene.GetNode<Node>("Node2D");

		// ---- Zone de chat ----
		var chatArea = GetNode<Area2D>("chat_detection_area");
		chatArea.BodyEntered += OnBodyEntered;
		chatArea.BodyExited += OnBodyExited;
	}
	
	// =======================
	// Boucle physique
	// =======================

	/// <summary>
	/// Applique le déplacement et vérifie si le PNJ est bloqué.
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		Velocity = _direction * Speed;
		MoveAndSlide();

		UpdateAnimation();
		CheckIfStuck((float)delta);
	}

	/// <summary>
	/// Choisit une nouvelle direction à intervalle régulier.
	/// </summary>
	private void OnTimerTimeout() => ChooseNewDirection();

	/// <summary>
	/// Choisit une direction aléatoire ou reste immobile.
	/// </summary>
	private void ChooseNewDirection()
	{
		if (_random.Next(100) < 50)
		{
			_direction = Vector2.Zero;
			return;
		}

		Vector2[] dirs = { Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right };
		_direction = dirs[_random.Next(dirs.Length)];

		_stuckTime = 0f;
		_lastPosition = GlobalPosition;
	}

	/// <summary>
	/// Met à jour l'animation selon la direction.
	/// </summary>
	private void UpdateAnimation()
	{
		if (_direction == Vector2.Zero)
		{
			_anim.Play("idle");
			return;
		}

		if (_direction == Vector2.Up) _anim.Play("walk_n");
		else if (_direction == Vector2.Down) _anim.Play("walk_s");
		else if (_direction == Vector2.Left) _anim.Play("walk_w");
		else if (_direction == Vector2.Right) _anim.Play("walk_e");
	}

	/// <summary>
	/// Détecte si le PNJ est bloqué et force un changement de direction.
	/// </summary>
	private void CheckIfStuck(float delta)
	{
		if (_direction == Vector2.Zero)
			return;

		float moved = GlobalPosition.DistanceTo(_lastPosition);

		if (moved < 1f)
			_stuckTime += delta;
		else
		{
			_stuckTime = 0f;
			_lastPosition = GlobalPosition;
		}

		if (_stuckTime > 0.5f)
		{
			ChooseNewDirection();
		}
	}

	// =======================
	// Dialogue
	// =======================

	/// <summary>
	/// Retourne la timeline à jouer selon le type du PNJ.
	/// </summary>
	public string GetDialogueTimeline()
	{
		// Si le PNJ est le Directeur et que le joueur a parlé à tous les PNJ,
		// renvoyer la timeline de fin définie dans Global (prioritaire).
		if (Type == NpcType.Directeur && Global.HasTalkedToNpc)
		{
			return Global.DirectorEndTimeline;
		}

		// Si une timeline forcée est définie dans l'inspecteur, l'utiliser
		if (!string.IsNullOrEmpty(TimelineName))
			return TimelineName;

		return Type switch
		{
			NpcType.Rmouque => RmouqueTimeline,
			NpcType.Ayadi => AyadiTimeline,
			NpcType.Lebot => AyadiTimeline,
			NpcType.Directeur => AyadiTimeline,
			NpcType.Haristoy => AyadiTimeline,
			NpcType.Roy => AyadiTimeline,
			_ => ""
		};
	}

	/// <summary>
	/// Met en pause le déplacement du PNJ.
	/// </summary>
	public void PauseMovement()
	{
		_timer.Stop();
		_direction = Vector2.Zero;
		UpdateAnimation();
	}

	/// <summary>
	/// Reprend le déplacement du PNJ.
	/// </summary>
	public void ResumeMovement()
	{
		_timer.Start();
	}

	// =======================
	// Interaction joueur
	// =======================

	private void OnBodyEntered(Node body)
	{
		if (body.IsInGroup("player"))
		{
			_playerNear = true;
			_playerRef = body as Node2D;
		}
	}

	private void OnBodyExited(Node body)
	{
		if (body.IsInGroup("player"))
		{
			_playerNear = false;
			if (body == _playerRef) _playerRef = null;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_playerNear && @event.IsActionPressed("interact"))
			Interact();
	}

	/// <summary>
	/// Interaction principale avec le joueur.
	/// </summary>
	public void Interact()
	{
		// Pause le PNJ et demande au Player (si présent) de se mettre en état "dialogue"
		PauseMovement();
		_playerRef?.Call("SetInDialogue", true);

		string timeline = GetDialogueTimeline();
		if (string.IsNullOrEmpty(timeline))
		{
			GD.PushError("Aucune timeline définie pour ce PNJ");
			ResumeMovement();
			_playerRef?.Call("SetInDialogue", false);
			return;
		}

		// Enregistrer qu'on a engagé un dialogue avec ce PNJ (si souhaité)
		if (Global.Instance != null)
		{
			Global.Instance.RegisterNpcTalk(this);
		}

		_dialogueManager.Call("StartDialogue", timeline);
	}

}
