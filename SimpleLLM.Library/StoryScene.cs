namespace SimpleLLM.Library.TextAdventure
{
	/// <summary>
	/// Un nodo narrativo con descrizione e regole locali.
	/// </summary>
	public sealed class StoryScene
	{
		public required string Id { get; init; }
		public required string Titolo { get; init; }
		public required string Descrizione { get; init; }
		public string? Objective { get; init; }
		public required IReadOnlyList<StoryActionRule> Rules { get; init; }
	}
}