namespace SimpleLLM.Library.TextAdventure
{
	internal sealed record SemanticMatchResult(
			StoryActionRule Rule,
			string TriggerText,
			float Score,
			bool Accepted);
}