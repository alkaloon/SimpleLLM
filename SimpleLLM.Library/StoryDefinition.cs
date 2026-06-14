using System.Text;
using System.Text.Json;

namespace SimpleLLM.Library.TextAdventure
{
	/// <summary>
	/// Definizione ad alto livello del gioco narrativo.
	/// </summary>
	public sealed class StoryDefinition
	{
		public required string Titolo { get; init; }
		public required string Protagonista { get; init; }
		public required string Introduzione { get; init; }
		public required string InitialSceneId { get; init; }
		public required IReadOnlyList<StoryScene> Scenes { get; init; }
		public IReadOnlyList<StoryActionRule> GlobalRules { get; init; } = [];
		public float SemanticThreshold { get; init; } = 0.62f;
		public bool EnableSemanticScoreLogging { get; init; } = false;
		public StoryDifficulty Difficulty { get; init; } = StoryDifficulty.Normal;
		public bool ShowHintsOnStart => Difficulty is StoryDifficulty.Easy or StoryDifficulty.Normal;
		public bool ShowHintsDuringGame => ShowHintsOnStart;

		public static StoryDefinition FromJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				throw new ArgumentException("JSON storia vuoto.", nameof(json));
			}

			StoryDefinition? definition = JsonSerializer.Deserialize<StoryDefinition>(json);
			if (definition is null)
			{
				throw new InvalidOperationException("Impossibile deserializzare la definizione storia.");
			}

			Validate(definition);
			return definition;
		}

		public static StoryDefinition FromFile(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				throw new ArgumentException("Percorso file storia non valido.", nameof(filePath));
			}

			string fullPath = Path.GetFullPath(filePath);
			if (!File.Exists(fullPath))
			{
				throw new FileNotFoundException("File definizione storia non trovato.", fullPath);
			}

			string json = File.ReadAllText(fullPath, Encoding.UTF8);
			return FromJson(json);
		}

		private static void Validate(StoryDefinition definition)
		{
			if (definition.Scenes is null || definition.Scenes.Count == 0)
			{
				throw new InvalidOperationException("La storia deve contenere almeno una scena.");
			}

			if (definition.SemanticThreshold <= 0f || definition.SemanticThreshold > 1f)
			{
				throw new InvalidOperationException("SemanticThreshold deve essere compreso nell'intervallo (0, 1].");
			}
		}
	}
}