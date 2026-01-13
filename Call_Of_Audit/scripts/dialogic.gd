extends Node2D # ou juste Node, Node2D si tu veux qu’il soit dans le monde

# Démarre le dialogue Dialogic
func StartDialogue(timeline_name: String):
	print("StartDialogue called with: ", timeline_name)
	if timeline_name != "":
		Dialogic.start(timeline_name)


# --- APPELÉE DEPUIS DIALOGIC ---
func change_fear(amount: float) -> void:
	# amount peut être positif (augmenter la peur) ou négatif (la réduire)
	Global.AddFear(amount)
