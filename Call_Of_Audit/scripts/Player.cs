using Godot;
using System;

/// <summary>
/// Player controller class.
/// Gère le déplacement, l'animation et l'interaction avec les NPC (dialogues).
/// Hérite de <see cref="CharacterBody2D"/> pour utiliser la physique 2D de Godot.
/// </summary>
public partial class Player : CharacterBody2D
{
	/// <summary>
	/// Vitesse de déplacement de base du joueur.
	/// </summary>
	[Export] public float Speed = 75f;

	/// <summary>
	/// Multiplicateur appliqué à la vitesse lorsque le joueur sprinte.
	/// </summary>
	[Export] public float SprintMultiplier = 2.0f;

	/// <summary>
	/// Valeur maximale de coopération du joueur.
	/// </summary>
	[Export] public float MaxCooperation = 100f;

	/// <summary>
	/// Valeur actuelle de coopération du joueur.
	/// </summary>
	public float CurrentCooperation = 50f;

	/// <summary>
	/// Sprite animé du joueur.
	/// </summary>
	private AnimatedSprite2D _anim;

	/// <summary>
	/// Indique si le joueur peut parler à un PNJ.
	/// </summary>
	private bool _canTalk = false;

	/// <summary>
	/// PNJ actuellement ciblé pour une interaction.
	/// </summary>
	private Node2D _npcTarget = null;

	/// <summary>
	/// Indique si un dialogue est en cours.
	/// </summary>
	private bool _isInDialogue = false;

	/// <summary>
	/// Référence au gestionnaire de dialogue (Dialogic).
	/// </summary>
	private Node _dialogueManager;

	/// <summary>
	/// Initialisation du joueur :
	/// - récupération du sprite
	/// - connexion des zones de détection
	/// - connexion au DialogueManager
	/// </summary>
	public override void _Ready()
	{
		_anim = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_anim == null)
		{
			GD.PushError("AnimatedSprite2D introuvable dans Player !");
			return;
		}

		// Zone de détection des PNJ
		var chatArea = GetNodeOrNull<Area2D>("chat_detection_area");
		if (chatArea != null)
		{
			chatArea.BodyEntered += OnChatBodyEntered;
			chatArea.BodyExited += OnChatBodyExited;
		}

		// DialogueManager (Dialogic)
		_dialogueManager = GetNodeOrNull("/root/Dialogic") ?? GetNodeOrNull<Node>("Node2D");
		if (_dialogueManager != null)
		{
			_dialogueManager.Connect("timeline_ended", Callable.From(OnDialogueEnded));
		}
	}

	/// <summary>
	/// Boucle physique principale.
	/// Gère le déplacement, le sprint, les animations et l'interaction avec les PNJ.
	/// </summary>
	/// <param name="delta">Temps écoulé depuis la dernière frame physique.</param>
	public override void _PhysicsProcess(double delta)
	{
		if (_anim == null)
			return;

		// Bloque le joueur pendant un dialogue
		if (_isInDialogue)
		{
			Velocity = Vector2.Zero;
			UpdateAnimation(Vector2.Zero);
			return;
		}

		Vector2 direction = Vector2.Zero;

		if (Input.IsActionPressed("walk_n")) direction += Vector2.Up;
		if (Input.IsActionPressed("walk_s")) direction += Vector2.Down;
		if (Input.IsActionPressed("walk_w")) direction += Vector2.Left;
		if (Input.IsActionPressed("walk_e")) direction += Vector2.Right;

		if (direction != Vector2.Zero)
			direction = direction.Normalized();

		float currentSpeed = Speed;
		if (Input.IsActionPressed("sprint"))
			currentSpeed *= SprintMultiplier;

		Velocity = direction * currentSpeed;
		MoveAndSlide();

		UpdateAnimation(direction);

		// Interaction PNJ
		if (_canTalk && Input.IsActionJustPressed("interact"))
		{
			if (_npcTarget is Npc npc)
			{
				string timeline = npc.GetDialogueTimeline();
				if (!string.IsNullOrEmpty(timeline))
					StartDialogue(timeline, npc);
			}
		}
	}

	/// <summary>
	/// Met à jour l'animation du joueur selon sa direction.
	/// </summary>
	/// <param name="direction">Direction normalisée du mouvement.</param>
	private void UpdateAnimation(Vector2 direction)
	{
		if (direction == Vector2.Zero)
		{
			_anim.Play("idle");
			return;
		}

		if (direction.X > 0 && direction.Y < 0) _anim.Play("walk_ne");
		else if (direction.X < 0 && direction.Y < 0) _anim.Play("walk_nw");
		else if (direction.X > 0 && direction.Y > 0) _anim.Play("walk_se");
		else if (direction.X < 0 && direction.Y > 0) _anim.Play("walk_sw");
		else if (direction.Y < 0) _anim.Play("walk_n");
		else if (direction.Y > 0) _anim.Play("walk_s");
		else if (direction.X < 0) _anim.Play("walk_w");
		else if (direction.X > 0) _anim.Play("walk_e");
	}

	/// <summary>
	/// Lance un dialogue avec un PNJ.
	/// </summary>
	/// <param name="timelineName">Nom de la timeline Dialogic.</param>
	/// <param name="npc">PNJ concerné.</param>
	private void StartDialogue(string timelineName, Npc npc)
	{
		_isInDialogue = true;
		npc.PauseMovement();
		// Register that we talked to this NPC (only if it's in the targeted group)
		if (Global.Instance != null)
		{
			Global.Instance.RegisterNpcTalk(npc);
		}

		_dialogueManager.Call("start", timelineName);
	}

	/// <summary>
	/// Appelé lorsque le dialogue est terminé.
	/// </summary>
	private void OnDialogueEnded()
	{
		_isInDialogue = false;

		if (_npcTarget is Npc npc)
			npc.ResumeMovement();

		// Émettre un signal global indiquant qu'on a rencontré/parlé à ce PNJ
		if (_npcTarget is Npc npc2 && Global.Instance != null)
		{
			Global.Instance.EmitSignal("met_npc", npc2);
		}
	}

	
	/// <summary>
	/// Permet à d'autres scripts (ex: Npc) d'indiquer que le joueur est
	/// entré/sorti d'un dialogue. Quand <paramref name="inDialogue"/> est vrai,
	/// le joueur est immobilisé et l'animation mise en idle.
	/// </summary>
	/// <param name="inDialogue">true pour bloquer le joueur, false pour le débloquer.</param>
	public void SetInDialogue(bool inDialogue)
	{
		_isInDialogue = inDialogue;
		if (inDialogue)
		{
			// S'assurer que le joueur ne se déplace plus
			Velocity = Vector2.Zero;
			UpdateAnimation(Vector2.Zero);
		}
	}
	/// <summary>
	/// Détecte l'entrée d'un PNJ dans la zone d'interaction.
	/// </summary>
	private void OnChatBodyEntered(Node2D body)
	{
		if (body.IsInGroup("npc"))
		{
			_canTalk = true;
			_npcTarget = body;
		}
	}

	/// <summary>
	/// Détecte la sortie du PNJ de la zone d'interaction.
	/// </summary>
	private void OnChatBodyExited(Node2D body)
	{
		if (body == _npcTarget)
		{
			_canTalk = false;
			_npcTarget = null;
		}
	}
}
