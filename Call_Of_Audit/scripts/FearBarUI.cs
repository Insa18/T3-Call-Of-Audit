using Godot;

public partial class FearBarUI : Control
{
	private ProgressBar _bar;

	public override void _Ready()
	{
		_bar = GetNode<ProgressBar>("ProgressBar");

		// S'enregistrer auprès du singleton Global
		if (Global.Instance != null)
		{
			Global.Instance.RegisterFearBarUI(this);
		}

		// Initialiser l'affichage avec la valeur actuelle
		if (Global.Instance != null)
		{
			UpdateValue(Global.Instance.CurrentFear, Global.Instance.MaxFear);
		}
	}

	public void UpdateValue(float current, float max)
	{
		if (_bar == null || max <= 0f)
			return;

		_bar.MaxValue = max;
		_bar.Value = current;
	}
}
