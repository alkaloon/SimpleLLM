namespace SimpleLLM.Library.TextAdventure
{
	/// <summary>
	/// Risultato elaborazione input utente.
	/// </summary>
	public sealed class StoryTurnResult
	{
		public required string Response { get; init; }
		public required StoryScene CurrentScene { get; init; }
		public bool IsBlocked { get; init; }
		public string? MatchedRuleId { get; init; }
	}
}