using Godot;
using System;

public partial class EcranDeFin : Control
{
    // On crée une "case" pour glisser notre label depuis l'éditeur
    [Export] public RichTextLabel ReportLabel;

    public override void _Ready()
    {
        GD.Print("Lancement de l'écran de fin...");

        // 1. Libérer la souris (Au cas où elle était cachée pendant le jeu)
        Input.MouseMode = Input.MouseModeEnum.Visible;

        // 2. Récupérer le texte depuis Global
        if (Global.Instance != null)
        {
            string rapport = Global.Instance.GenerateEndGameReport();
            
            // 3. Afficher le texte
            if (ReportLabel != null)
            {
                ReportLabel.Text = rapport;
            }
            else
            {
                GD.PrintErr("Erreur : Le ReportLabel n'est pas assigné dans l'inspecteur !");
            }
        }
        if (Global.Instance != null)
    {
        string rapport = Global.Instance.GenerateEndGameReport();
        
        // --- AJOUTEZ CETTE LIGNE ---
        GD.Print("CONTENU DU RAPPORT : " + rapport); 
        // ---------------------------

        if (ReportLabel != null)
        {
            ReportLabel.Text = rapport;
        }
    }
    }

    // Cette fonction sera reliée au bouton
    public void _on_quit_button_pressed()
    {
        GetTree().Quit();
    }
}