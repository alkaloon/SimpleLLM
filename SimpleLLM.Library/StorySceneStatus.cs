namespace SimpleLLM.Library.TextAdventure
{
	/// <summary>
	/// Stato sintetico della scena corrente per la UI testuale.
	/// </summary>
	public sealed record StorySceneStatus
	{
		public required string SceneTitle { get; init; }
		public required string SceneDescription { get; init; }
		public required string Objective { get; init; }
		public required string InventorySummary { get; init; }
		public required string FlagSummary { get; init; }
		public required IReadOnlyList<StoryActionHint> Suggestions { get; init; }
	}
}