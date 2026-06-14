namespace SimpleLLM.Library.TextAdventure
{
	/// <summary>
	/// Regola che mappa un intento testuale a una transizione narrativa.
	/// </summary>
	public sealed class StoryActionRule
	{
		public required string Id { get; init; }
		public required IReadOnlyList<string> TriggerVerbs { get; init; }
		public required string SuccessResponse { get; init; }
		public string? BlockedResponse { get; init; }
		public string? NextSceneId { get; init; }
		public string? NextSceneObjective { get; init; }
		public string? IntentDescription { get; init; } = string.Empty;
		public IReadOnlyList<string> RequiredFlags { get; init; } = [];
		public IReadOnlyList<string> RequiredItems { get; init; } = [];
		public IReadOnlyList<string> GrantedFlags { get; init; } = [];
		public IReadOnlyList<string> GrantedItems { get; init; } = [];
		public IReadOnlyList<string> ConsumedItems { get; init; } = [];
	}
}