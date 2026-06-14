namespace SimpleLLM.Library.TextAdventure
{
	/// <summary>
	/// DTO serializzabile per persistenza della sessione.
	/// </summary>
	public sealed class StoryGameSaveData
	{
		public string StoryTitle { get; set; } = string.Empty;
		public string CurrentSceneId { get; set; } = string.Empty;
		public string[] Flags { get; set; } = [];
		public string[] Inventory { get; set; } = [];
	}
}