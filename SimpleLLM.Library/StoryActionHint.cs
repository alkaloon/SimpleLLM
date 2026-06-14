namespace SimpleLLM.Library.TextAdventure
{
	/// <summary>
	/// Suggerimento sintetico per guidare il giocatore nella scena corrente.
	/// </summary>
	public sealed record StoryActionHint
	{
		public required string Command { get; init; }
		public required string RuleId { get; init; }
		public required bool IsAvailable { get; init; }
		public required string Note { get; init; }
		public string? NextSceneId { get; init; }
	}
}