using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



/// <summary>
/// Noeud global pour conserver des variables partagées entre scènes.
/// Utilise des champs statiques pour les valeurs à portée globale et des champs
/// d'instance pour l'état modifiable lié à ce noeud.
/// </summary>
public partial class Global : Node
{
	// --- Singleton d'accès global ---
	public static Global Instance { get; private set; }

	// --- Variables déjà existantes ---
	public static string SpawnPointToUse = "";

	/// <summary>
	/// Flag global indiquant si le joueur a déjà parlé à un PNJ important.
	/// </summary>
	public static bool HasTalkedToNpc = false;

	// Nom de la timeline de fin associée au Directeur (clé Dialogic)
	public static string DirectorEndTimeline = "timeline_director_end";

	/// <summary>
	/// Volume général du jeu (0.0 = muet, 1.0 = 100%).
	/// Valeur par défaut : 0.5 (50%).
	/// </summary>
	public static float MasterVolume = 0.5f;

	/// <summary>
	/// Jauge de coopération courante (valeur locale au noeud, non statique).
	/// </summary>
	public float CurrentCooperation = 50f;

	/// <summary>
	/// Valeur maximale possible pour la coopération.
	/// </summary>
	public float MaxCooperation = 100f;

	// --- Gestion globale de la PEUR ---
	// Valeur initiale placée à 40 (40%) plutôt que 0 pour refléter
	// l'état attendu au lancement du jeu.
	public float CurrentFear = 40f;
	public float MaxFear = 100f;

	// Evite de déclencher plusieurs fois l'écran de fin
	private bool _lostTriggered = false;

	// Ajoutez ceci avec vos autres variables
	// Expose un état en lecture publique pour autoriser le lancement de la timeline de fin
	public bool CanLaunchEnd { get; private set; } = false;

	private FearBarUI _fearBar;

	// Signal émis quand le joueur a rencontré/parlé à un PNJ
	[Signal]
	public delegate void MetNpcEventHandler(Node npc);

	// Ensemble des PNJ déjà contactés (identifiés par leur NodePath)
	private HashSet<string> _talkedNpcs = new();



	public override void _EnterTree()
	{
		// Enregistrer l'instance globale (autoload dans Godot)
		Instance = this;
	}

	public override void _Ready()
	{
		GD.Print("[GLOBAL] _Ready");

		// Si aucun GameManager n'existe dans l'arbre, en créer un ici pour
		// s'assurer que les handlers C# (ex. connexion aux signaux Dialogic)
		// sont bien initialisés.
		if (GetNodeOrNull("GameManager") == null)
		{
			var gm = new GameManager();
			gm.Name = "GameManager";
			AddChild(gm);
			GD.Print("[GLOBAL] GameManager instancié et ajouté à l'arbre.");
		}
	}

	// --- API publique pour modifier la peur depuis n'importe où ---
	public void SetFear(float fear)
	{
		CurrentFear = Mathf.Clamp(fear, 0f, MaxFear);
		UpdateFearBar();

		// Si la peur atteint ou dépasse le max, afficher l'écran de fin une seule fois
		if (!_lostTriggered && CurrentFear >= MaxFear)
		{
			_lostTriggered = true;
			GetTree().ChangeSceneToFile("res://scenes/loose_screen.tscn");
		}
	}

	public void AddFear(float amount)
	{
		SetFear(CurrentFear + amount);
	}

	private void UpdateFearBar()
	{
		if (_fearBar != null)
		{
			_fearBar.UpdateValue(CurrentFear, MaxFear);
		}
	}



	// Appelée par la scène de jeu quand la FearBar est prête
	public void RegisterFearBarUI(FearBarUI bar)
	{
		_fearBar = bar;
		UpdateFearBar();
	}

	/// <summary>
	/// Enregistre qu'un PNJ a été parlé. Affiche un message quand tous les PNJ
	/// du groupe "npc" présents dans l'arbre sont contactés.
	/// </summary>
	public void RegisterNpcTalk(Node npc)
{
    // 1. Sécurités : On vérifie que le PNJ est valide
    if (npc == null) return;
    if (!npc.IsInGroup("npc")) return; // On ignore les objets qui ne sont pas des PNJ

    // 2. Vérification doublon : Est-ce qu'on lui a déjà parlé ?
    string key = npc.GetPath().ToString();
    if (_talkedNpcs.Contains(key)) return;

    // 3. Enregistrement : On l'ajoute à la liste des "vus"
    _talkedNpcs.Add(key);
    
    // 4. Calcul de progression
    int total = GetTree().GetNodesInGroup("npc").Count;
    GD.Print($"[Global] Parlé à PNJ: {npc.Name} ({_talkedNpcs.Count}/{total})");

    // 5. Condition de victoire : A-t-on parlé à tout le monde ?
		if (total > 0 && _talkedNpcs.Count >= total)
    {
        GD.Print("[Global] SUCCÈS : Tous les PNJ ont été rencontrés !");
        GD.Print("[Global] -> Appuyez sur 'N' pour lancer le dialogue final.");

        // --- MODIFICATION CLÉ ---
        // On déverrouille l'autorisation d'utiliser la touche N
			CanLaunchEnd = true;
        
        // (Optionnel) On garde votre ancienne variable pour compatibilité si besoin
        HasTalkedToNpc = true;
    }
}

	/// <summary>
	/// Retourne le rapport de fin de partie. Délègue à GameManager si présent.
	/// </summary>
	public string GenerateEndGameReport()
	{
		if (GameManager.Instance != null)
		{
			return GameManager.Instance.GenerateEndGameReport();
		}
		return "Aucun rapport disponible (GameManager non initialisé).";
	}


}


public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;

		// ÉTAPE 1 : Trouver le nœud Dialogic
		// Dialogic est un "Autoload" créé par le plugin, il est à la racine.
		var dialogic = GetNode("/root/Dialogic");

		// ÉTAPE 2 : Se connecter ("S'abonner")
		// On dit : "Quand Dialogic lance 'signal_event', appelle ma fonction 'OnDialogicSignal'"
		dialogic.Connect("signal_event", Callable.From<string>(OnDialogicSignal));
	}

	private void OnDialogicSignal(string argument)
	{
		// Normaliser et logger le signal reçu pour éviter les problèmes de
		// casse ou d'espaces (ex: "lebot_witcher ").
		string raw = argument ?? string.Empty;
		string key = raw.Trim().ToLowerInvariant();
		GD.Print($"[GameManager] Dialogic signal reçu: '{raw}' -> normalisé: '{key}'");

		// 1. Si c'est un signal de PNJ connu, on le stocke
		if (_definitionsRapport.ContainsKey(key))
		{
			if (!_signauxRecus.Contains(key))
			{
				_signauxRecus.Add(key);
				GD.Print($"[GameManager] Signal enregistré: {key}");
			}
		}
		// 2. Si c'est le signal DE FIN, on change de scène !
		else if (key == "show_end_report")
		{
			GetTree().ChangeSceneToFile("res://scenes/EcranDeFin.tscn");
		}
		else
		{
			GD.Print($"[GameManager] Signal inconnu (non présent dans _definitionsRapport): '{key}'");
		}
	}

	// Ce dictionnaire sert de "Base de données" pour vos textes
	private Dictionary<string, string> _definitionsRapport = new Dictionary<string, string>()
{
	{
	"ayadi_cool",
	"Ayadi : S'est montré visiblement tendu au début de l'audit, mais votre attitude posée "
	+ "et votre manière de cadrer l'échange ont contribué à le rassurer. "
	+ "Il a pu répondre plus librement une fois en confiance. "
	+ "Il estime que votre posture d'auditeur était professionnelle et respectueuse."
	+ "Il vous a bien détaillé les sécurités mises en place, ce qui a facilité l’audit."
	},

	{
	"ayadi_mauvais",
	"Ayadi : A ressenti l'entretien comme une mise en accusation. "
	+ "Le ton et la manière de poser les questions ont renforcé son stress. "
	+ "Il a eu le sentiment d'être jugé plutôt qu'accompagné, "
	+ "ce qui a nui à la qualité de l'échange."
	+ "Il n'a pas fourni les informations complètes sur les sécurités mises en place, ce qui a compliqué l’audit."
	},


	{
	"bryan_cool",
	"Bryan : Abordait l'audit avec méfiance, craignant des conséquences sur son travail. "
	+ "Votre assurance et votre clarté lui ont permis de se détendre progressivement. "
	+ "Il a perçu l'échange comme exigeant mais juste."
	+ "Il vous a bien donné les informations concernant les achats de l’exercice précédent et elle sont conformes aux attentes."
	},

	{
	"bryan_mauvais",
	"Bryan : S'est senti rapidement sur la défensive. "
	+ "Il a eu l'impression que chaque réponse pouvait se retourner contre lui. "
	+ "Cette pression a limité sa coopération et rendu l'entretien tendu."
	+ "Il a omis de fournir certaines informations sur les achats de l’exercice précédent, ce qui a compliqué l’audit."
	},


	{
	"roy_cool",
	"Roy : A abordé l'audit avec une inquiétude manifeste. "
	+ "Votre écoute et votre ton bienveillant ont créé un climat de confiance. "
	+ "Elle s'est sentie suffisamment à l'aise pour évoquer des points sensibles."
	+ "Elle vous a transmis toutes les informations demandées des fiches de paye de manière claire et complète."
	},

	{
	"roy_mauvais",
	"Roy : A vécu l'entretien comme inconfortable. "
	+ "Le manque de chaleur dans l'échange a accentué sa peur des répercussions. "
	+ "Elle est restée sur la réserve et ne vous a pas transmis les informations nécessaires."
	},

	{
	"ilias_cool",
	"Ilias : Redoutait un audit purement technique et sanctionnant. "
	+ "Votre manière de poser les questions a réduit cette crainte. "
	+ "Il a pu expliquer ses choix sans se sentir attaqué."
	+ "Vous a informé de la réparation d'un faille de sécurité récente, montrant sa volonté de transparence."
	},

	{
	"ilias_mauvais",
	"Ilias : A perçu l'audit comme un test de compétence permanent. "
	+ "La pression ressentie a renforcé son stress et limité ses explications. "
	+ "Il craint que l'échange ne reflète pas fidèlement son travail réel."
	+ "Il a omis de mentionner une faille de sécurité récente, ce qui a nui à la confiance établie."
	},

	{
	"lebot_cool",
	"Lebot : A abordé l'audit avec sérieux et une certaine appréhension initiale. "
	+ "Votre posture professionnelle et structurée lui a permis de répondre "
	+ "aux questions sans se sentir mis sous pression. "
	+ "L'entretien s'est déroulé dans un cadre formel mais maîtrisé."
	},

	{
	"lebot_witcher",
	"Lebot : La mention de l'univers de The Witcher a instauré une véritable connivence. "
	+ "Se sentant compris et mis à l'aise, il a exprimé ses réponses "
	+ "avec assurance et spontanéité. "
	+ "L'audit s'est transformé en échange de confiance constructive."
	},
	
	{
	"lebot_sda",
	"Lebot : La référence au Seigneur des Anneaux a immédiatement créé un lien négatif. "
	+ "Il a semblé se refermer, adoptant une posture plus défensive. "
	+ "L'audit a été marqué par une certaine distance, "
	+ "rendant l'échange moins fluide et plus formel."
	}
};

	// Cette liste servira de mémoire pour savoir quels signaux le joueur a déclenchés
	private List<string> _signauxRecus = new List<string>();


	public string GenerateEndGameReport()
	{
		StringBuilder rapport = new StringBuilder();
		rapport.AppendLine("[center][b]RAPPORT D'INTERACTIONS[/b][/center]\n");

		int total = _signauxRecus.Count;
		int positives = _signauxRecus.Count(s => s.Contains("_cool") || s.Contains("_witcher") || s.Contains("_sda"));
		int negatives = _signauxRecus.Count(s => s.Contains("_mauvais"));

		rapport.AppendLine($"[b]Synthèse :[/b]\n- Interactions enregistrées : {total}\n- Interactions positives : {positives}\n- Interactions négatives : {negatives}\n");

		if (Global.Instance != null)
		{
			rapport.AppendLine($"[b]Niveau de peur final :[/b] {Global.Instance.CurrentFear:0}/{Global.Instance.MaxFear:0}\n");
		}

		// Regrouper par interlocuteur (préfixe avant le premier '_')
		var parInterlocuteur = new Dictionary<string, List<(string key, string texte)>>();
		foreach (var signal in _signauxRecus)
		{
			if (_definitionsRapport.TryGetValue(signal, out string texte))
			{
				string baseName = signal;
				int idx = signal.IndexOf('_');
				if (idx > 0) baseName = signal.Substring(0, idx);
				if (!parInterlocuteur.ContainsKey(baseName)) parInterlocuteur[baseName] = new List<(string, string)>();
				parInterlocuteur[baseName].Add((signal, texte));
			}
		}

		if (parInterlocuteur.Count == 0)
		{
			rapport.AppendLine("Aucune donnée disponible. Le candidat est resté muet.");
			return rapport.ToString();
		}

		rapport.AppendLine("[b]Détails par interlocuteur :[/b]\n");
		foreach (var kv in parInterlocuteur.OrderBy(k => k.Key))
		{
			string npc = kv.Key;
			rapport.AppendLine($"[b]{char.ToUpper(npc[0]) + npc.Substring(1)}[/b]");
			foreach (var item in kv.Value)
			{
				string sentiment = "Neutre";
				if (item.key.Contains("_cool") || item.key.Contains("_witcher")) sentiment = "Positif";
				else if (item.key.Contains("_mauvais")) sentiment = "Négatif";
				rapport.AppendLine($"[i]{sentiment}[/i] : {item.texte}\n");
			}
			rapport.AppendLine("---\n");
		}

		// Conclusion finale ajoutée par demande utilisateur
		rapport.AppendLine("\nMerci d'avoir joué à notre jeu! Notre but était de faire comprendre l'importance de bonnes relations humaines pour le bon déroulement d'un audit.");
		rapport.AppendLine("Si vous ne deviez retenir qu'une seule phrase, ce serait: Mettre en confiance les audités pour obtenir des informations fiables  et aider l'organisme audité est primordial.");

		return rapport.ToString();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Si on appuie sur la touche N
		if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.N)
		{
			// On vérifie si on a le droit de lancer la fin
			if (Global.Instance != null && Global.Instance.CanLaunchEnd)
			{
				var dialogic = GetNodeOrNull("/root/Dialogic");

				// Sécurité : On vérifie que Dialogic existe et qu'on ne parle pas déjà
				if (dialogic != null)
				{
					var currentTimeline = dialogic.Get("current_timeline");
					if (currentTimeline.Obj == null) // Si personne ne parle déjà
					{
						GD.Print("[Global] Lancement de la timeline finale via 'N' !");
						dialogic.Call("start", "timeline_director_end");
					}
				}
			}
			else
			{
				GD.Print("[Global] Touche N ignorée : Vous n'avez pas encore parlé à tout le monde.");
			}
		}
	}

}


