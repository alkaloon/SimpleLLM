namespace SimpleLLM.Library.TextAdventure
{
	internal sealed record RuleEmbeddingEntry(
			StoryActionRule Rule,
			string SceneId,
			bool IsGlobal,
			string TriggerText,
			ReadOnlyMemory<float> Embedding);
}