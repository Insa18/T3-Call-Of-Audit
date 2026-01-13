using Godot;

/// <summary>
/// UI de pause du jeu.
/// Ce <see cref="CanvasLayer"/> gère l'affichage du menu de pause, la mise en pause
/// du <see cref="SceneTree"/>, et la connexion des boutons Resume/Quit.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
    // Indique si le jeu est actuellement en pause (état local pour simplicité)
    private bool _paused = false;

    // Références aux boutons du menu (attendues dans l'arbre de scène)
    private Button _buttonResume;
    private Button _buttonQuit;

    /// <summary>
    /// Initialisation : récupération des noeuds enfants, connexion des signaux
    /// et configuration du mode de traitement pour fonctionner même en pause.
    /// </summary>
    public override void _Ready()
    {
        // Important : permet à ce noeud de continuer à recevoir _Process même quand le tree est en pause
        ProcessMode = Node.ProcessModeEnum.Always;

        // Récupération des boutons. Les chemins doivent correspondre à l'arbre de la scène
        _buttonResume = GetNode<Button>("ButtonResume");
        _buttonQuit   = GetNode<Button>("ButtonQuit");

        // Connexion aux callbacks lorsque les boutons sont pressés
        _buttonResume.Pressed += _on_button_resume_pressed;
        _buttonQuit.Pressed   += _on_button_quit_pressed;

        // Par défaut le menu est caché
        Visible = false;
    }

    /// <summary>
    /// Vérifie l'entrée d'annulation (par défaut `ui_cancel`) pour basculer entre pause/reprise.
    /// Ce traitement s'exécute toujours car ProcessMode=Always.
    /// </summary>
    /// <param name="delta">Temps écoulé depuis la dernière frame (non utilisé ici).</param>
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            if (_paused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    /// <summary>
    /// Active le menu de pause : rend le menu visible, met le SceneTree en pause
    /// et affiche le curseur de la souris pour l'UI.
    /// </summary>
    private void PauseGame()
    {
        Visible = true;
        GetTree().Paused = true; // met en pause l'ensemble des noeuds (sauf ceux en Always)
        _paused = true;

        // Affiche la souris pour permettre l'interaction avec les boutons
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    /// <summary>
    /// Désactive le menu de pause : cache l'UI, réactive le SceneTree et restaure l'état local.
    /// </summary>
    private void ResumeGame()
    {
        Visible = false;
        GetTree().Paused = false;
        _paused = false;

        // Optionnel : recapturer la souris si le jeu l'utilise (ex: FPS)
        // Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    /// <summary>
    /// Callback appelé quand le bouton Resume est pressé depuis l'UI.
    /// Appelle <see cref="ResumeGame"/> pour reprendre la partie.
    /// </summary>
    private void _on_button_resume_pressed()
    {
        ResumeGame();
    }

    /// <summary>
    /// Callback appelé quand le bouton Quit est pressé depuis l'UI.
    /// Termine l'application via <see cref="SceneTree.Quit"/>.
    /// </summary>
    private void _on_button_quit_pressed()
    {   
        GetTree().Quit();
    }
}
