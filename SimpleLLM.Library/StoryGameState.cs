namespace SimpleLLM.Library.TextAdventure
{
	/// <summary>
	/// Stato runtime della sessione di gioco.
	/// </summary>
	public sealed class StoryGameState
	{
		public required string CurrentSceneId { get; set; }
		public HashSet<string> Flags { get; } = new(StringComparer.OrdinalIgnoreCase);
		public HashSet<string> Inventory { get; } = new(StringComparer.OrdinalIgnoreCase);
	}
}