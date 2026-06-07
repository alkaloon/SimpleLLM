using SimpleLLM.Library.TextAdventure;
using SimpleLLM.Library.VectorEngine;
using System;
using System.IO;

namespace InteractiveStories
{
	/// <summary>
	/// Demo completa del motore StoryGameEngine con una mini avventura fantasy.
	/// </summary>
	public static class InteractiveStoryDemo
	{
		private const string DefaultSaveFile = "savegame.json";

		public static void Execute(string[]? args = null)
		{
			var story = BuildStory(args);
			StoryDifficulty selectedDifficulty = PromptDifficulty(story.Difficulty);
			story = WithDifficulty(story, selectedDifficulty);
			using var embeddingService = new LocalEmbeddingService();
			var engine = new StoryGameEngine(story, embeddingService);
			bool showHintsOnStart = story.ShowHintsOnStart;

			RenderBanner(story, engine, showHintsOnStart);
			Console.WriteLine("Motore semantico attivo: embeddings + cosine similarity.");
			Console.WriteLine($"Soglia semantica corrente: {story.SemanticThreshold:F2}");
			Console.WriteLine($"Difficolta: {story.Difficulty}");
			if (story.EnableSemanticScoreLogging)
			{
				Console.WriteLine("Log semantico attivo: utile per il tuning, ma più verboso.");
			}
			Console.WriteLine(engine.Begin(showHintsOnStart));
			if (showHintsOnStart)
			{
				RenderActionHints(engine);
			}
			Console.WriteLine();

			while (true)
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write("Azione> ");
				Console.ResetColor();

				string? input = Console.ReadLine();
				if (string.IsNullOrWhiteSpace(input))
				{
					continue;
				}

				if (input.Trim().Equals("esci", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Sessione terminata.");
					break;
				}

				try
				{
					if (TryHandlePersistenceCommand(engine, input, out string persistenceOutput))
					{
						Console.ForegroundColor = ConsoleColor.Blue;
						Console.WriteLine(persistenceOutput);
						Console.ResetColor();
						Console.WriteLine();
						continue;
					}
				}
				catch (Exception ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"Errore persistenza: {ex.Message}");
					Console.ResetColor();
					Console.WriteLine();
					continue;
				}

				StoryTurnResult result = engine.HandleInput(input);
				RenderTurn(engine, result);
				Console.WriteLine();

				if (showHintsOnStart)
				{
					RenderActionHints(engine);
				}

				if (story.EnableSemanticScoreLogging && !string.IsNullOrWhiteSpace(engine.LastSemanticLog))
				{
					Console.ForegroundColor = ConsoleColor.DarkGray;
					Console.WriteLine(engine.LastSemanticLog);
					Console.ResetColor();
				}

				Console.WriteLine();

				if (engine.State.Flags.Contains("quest-completata"))
				{
					Console.ForegroundColor = ConsoleColor.Magenta;
					Console.WriteLine("Hai completato la storia. Puoi continuare a esplorare o digitare 'esci'.");
					Console.ResetColor();
					Console.WriteLine();
				}
			}
		}

		private static void RenderBanner(StoryDefinition story, StoryGameEngine engine, bool showHintsOnStart)
		{
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine(new string('=', 56));
			Console.WriteLine($"{story.Titolo}");
			Console.WriteLine($"Protagonista: {story.Protagonista}");
			Console.WriteLine(new string('=', 56));
			Console.ResetColor();
			var status = engine.GetSceneStatus();
			Console.WriteLine($"Scena iniziale: {status.SceneTitle}");
			Console.WriteLine($"Obiettivo: {status.Objective}");
			Console.WriteLine($"Inventario: {status.InventorySummary}");
			Console.WriteLine($"Stato trama: {status.FlagSummary}");
			Console.WriteLine("Comandi utili: guarda, inventario, stato, aiuto, salva [file], carica [file], esci");
			Console.WriteLine(showHintsOnStart
				? "Suggerimento: segui l'obiettivo della scena, poi consulta le azioni suggerite."
				: "Suggerimento: questa difficolta nasconde i suggerimenti iniziali. Usa 'aiuto' se vuoi una guida.");
			Console.WriteLine();
		}

		private static StoryDifficulty PromptDifficulty(StoryDifficulty defaultDifficulty)
		{
			Console.WriteLine("Modalita di gioco:");
			Console.WriteLine("1) Easy - suggerimenti visibili");
			Console.WriteLine("2) Normal - suggerimenti visibili");
			Console.WriteLine("3) Hard - suggerimenti nascosti all'avvio");
			Console.WriteLine("4) Expert - suggerimenti nascosti all'avvio");
			Console.Write($"Seleziona una modalita [invio per {defaultDifficulty}]: ");

			string? input = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(input))
			{
				return defaultDifficulty;
			}

			return input.Trim().ToLowerInvariant() switch
			{
				"1" or "easy" => StoryDifficulty.Easy,
				"2" or "normal" => StoryDifficulty.Normal,
				"3" or "hard" => StoryDifficulty.Hard,
				"4" or "expert" => StoryDifficulty.Expert,
				_ => defaultDifficulty
			};
		}

		private static StoryDefinition WithDifficulty(StoryDefinition story, StoryDifficulty difficulty)
		{
			return new StoryDefinition
			{
				Titolo = story.Titolo,
				Protagonista = story.Protagonista,
				Introduzione = story.Introduzione,
				InitialSceneId = story.InitialSceneId,
				Scenes = story.Scenes,
				GlobalRules = story.GlobalRules,
				SemanticThreshold = story.SemanticThreshold,
				EnableSemanticScoreLogging = story.EnableSemanticScoreLogging,
				Difficulty = difficulty
			};
		}

		private static void RenderActionHints(StoryGameEngine engine)
		{
			var hints = engine.GetActionHints(4);
			if (hints.Count == 0)
			{
				return;
			}

			Console.ForegroundColor = ConsoleColor.DarkCyan;
			Console.WriteLine($"Azioni suggerite ora - obiettivo: {engine.CurrentObjective}");
			foreach (StoryActionHint hint in hints)
			{
				string status = hint.IsAvailable ? hint.Note : $"bloccata: {hint.Note}";
				string sceneTarget = string.IsNullOrWhiteSpace(hint.NextSceneId) ? string.Empty : $" -> {hint.NextSceneId}";
				Console.WriteLine($"- {hint.Command}{sceneTarget} ({status})");
			}
			Console.ResetColor();
			Console.WriteLine();
		}

		private static void RenderTurn(StoryGameEngine engine, StoryTurnResult result)
		{
			Console.ForegroundColor = result.IsBlocked ? ConsoleColor.Yellow : ConsoleColor.Green;
			Console.WriteLine(result.Response);
			Console.ResetColor();
			Console.WriteLine();

			var status = engine.GetSceneStatus();
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.WriteLine($"[{status.SceneTitle}] Obiettivo: {status.Objective}");
			Console.WriteLine($"Inventario: {status.InventorySummary}");
			Console.WriteLine($"Stato trama: {status.FlagSummary}");
			Console.ResetColor();
		}

		private static bool TryHandlePersistenceCommand(StoryGameEngine engine, string rawInput, out string output)
		{
			string trimmed = rawInput.Trim();

			if (trimmed.Equals("salva", StringComparison.OrdinalIgnoreCase)
				|| trimmed.StartsWith("salva ", StringComparison.OrdinalIgnoreCase))
			{
				string target = ExtractPathArgument(trimmed, "salva");
				string fullPath = Path.GetFullPath(target);

				engine.SaveToFile(fullPath);
				output = $"Partita salvata in: {fullPath}";
				return true;
			}

			if (trimmed.Equals("carica", StringComparison.OrdinalIgnoreCase)
				|| trimmed.StartsWith("carica ", StringComparison.OrdinalIgnoreCase))
			{
				string target = ExtractPathArgument(trimmed, "carica");
				string fullPath = Path.GetFullPath(target);

				engine.LoadFromFile(fullPath);
				StoryTurnResult look = engine.HandleInput("guarda");
				output = $"Partita caricata da: {fullPath}{Environment.NewLine}{Environment.NewLine}{look.Response}";
				return true;
			}

			output = string.Empty;
			return false;
		}

		private static string ExtractPathArgument(string command, string keyword)
		{
			if (command.Length <= keyword.Length)
			{
				return DefaultSaveFile;
			}

			string suffix = command.Substring(keyword.Length).Trim();
			return string.IsNullOrWhiteSpace(suffix) ? DefaultSaveFile : suffix;
		}

		private static StoryDefinition BuildStory(string[]? args)
		{
			string? storyPath = ResolveStoryDefinitionPath(args);
			if (!string.IsNullOrWhiteSpace(storyPath))
			{
				Console.WriteLine($"Caricamento storia da file: {Path.GetFullPath(storyPath)}");
				return StoryDefinition.FromFile(storyPath);
			}

			Console.WriteLine("Nessun file story-definition.json trovato: uso fallback JSON embedded.");
			return StoryDefinition.FromJson(EmbeddedStoryJson);
		}

		private static string? ResolveStoryDefinitionPath(string[]? args)
		{
			if (args is { Length: > 0 })
			{
				string candidate = args[0].Trim('"');
				if (File.Exists(candidate))
				{
					return candidate;
				}
			}

			string[] candidates =
			[
				"story-definition.json",
				"neo-venice.json",
				Path.Combine(AppContext.BaseDirectory, "story-definition.json"),
				Path.Combine(AppContext.BaseDirectory, "neo-venice.json"),
				Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "story-definition.json")),
				Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "neo-venice.json"))
			];

			foreach (string candidate in candidates)
			{
				if (File.Exists(candidate))
				{
					return candidate;
				}
			}

			return null;
		}

		private const string EmbeddedStoryJson = """
{
  "Titolo": "Le Cronache del Faro Spezzato",
  "Protagonista": "Ardan, sentinella del villaggio",
  "Introduzione": "Una nebbia innaturale avvolge la costa. Il faro, spento da settimane, ha risvegliato antiche creature marine. Tu sei l'unico a conoscere il sentiero verso la torre.",
  "InitialSceneId": "piazza",
  "SemanticThreshold": 0.58,
  "EnableSemanticScoreLogging": false,
	"Difficulty": "Normal",
  "Scenes": [
    {
      "Id": "piazza",
      "Titolo": "Piazza del Porto",
      "Descrizione": "I pescatori barricano le botteghe. Vedi la vecchia lanterna del guardiano, un vicolo verso la scogliera e la cappella chiusa.",
			"Objective": "Recupera la lanterna del guardiano e raggiungi la scogliera.",
      "Rules": [
        {
          "Id": "prendi-lanterna",
          "TriggerVerbs": ["prendi lanterna", "raccogli lanterna", "lanterna"],
          "SuccessResponse": "Afferri la lanterna del guardiano. La fiamma azzurra pulsa debolmente.",
          "GrantedItems": ["lanterna-azzurra"],
          "GrantedFlags": ["lanterna-raccolta"]
        },
        {
          "Id": "vai-scogliera",
          "TriggerVerbs": ["vai alla scogliera", "scogliera", "sentiero"],
          "SuccessResponse": "Ti infili nel sentiero bagnato dal salmastro e raggiungi la scogliera.",
          "NextSceneId": "scogliera"
        }
      ]
    },
    {
      "Id": "scogliera",
      "Titolo": "Scogliera del Vento",
      "Descrizione": "Le onde colpiscono le rocce. La porta del faro e' arrugginita e una creatura d'ombra striscia tra i massi.",
			"Objective": "Respingi l'ombra con la lanterna e apri la porta del faro.",
      "Rules": [
        {
          "Id": "respingi-ombra",
          "TriggerVerbs": ["usa lanterna", "illumina creatura", "scaccia ombra"],
          "RequiredItems": ["lanterna-azzurra"],
          "SuccessResponse": "La luce azzurra lacera la nebbia e la creatura fugge urlando nel mare.",
          "GrantedFlags": ["ombra-dissolta"],
          "BlockedResponse": "La creatura e' troppo vicina. Senza una fonte di luce non puoi passare."
        },
        {
          "Id": "entra-faro",
          "TriggerVerbs": ["entra nel faro", "apri porta", "faro"],
          "RequiredFlags": ["ombra-dissolta"],
          "SuccessResponse": "Spingi la porta del faro. Gli ingranaggi interni gemono, ma il passaggio si apre.",
          "NextSceneId": "faro-interno",
          "BlockedResponse": "Appena ti avvicini, l'ombra ti sbarra la strada. Devi prima respingerla."
        },
        {
          "Id": "torna-piazza",
          "TriggerVerbs": ["torna in piazza", "torna indietro", "piazza"],
          "SuccessResponse": "Ripercorri il sentiero e torni al porto.",
          "NextSceneId": "piazza"
        }
      ]
    },
    {
      "Id": "faro-interno",
      "Titolo": "Sala del Faro",
      "Descrizione": "Una scala a chiocciola sale fino alla lente principale. Il meccanismo e' fermo; manca il cristallo di accensione custodito in un altare di metallo.",
			"Objective": "Attiva il faro e salva il villaggio.",
      "Rules": [
        {
          "Id": "innesta-lanterna",
          "TriggerVerbs": ["inserisci lanterna", "usa lanterna nell altare", "attiva faro"],
          "RequiredItems": ["lanterna-azzurra"],
          "SuccessResponse": "Inserisci la lanterna nell'altare. La lente si accende e un raggio luminoso squarcia la tempesta. Le creature si disperdono e il villaggio e' salvo.",
          "GrantedFlags": ["quest-completata"]
        },
        {
          "Id": "scendi-scogliera",
          "TriggerVerbs": ["scendi", "torna alla scogliera", "esci dal faro"],
          "SuccessResponse": "Scendi la scala e torni davanti all'ingresso del faro.",
          "NextSceneId": "scogliera"
        }
      ]
    }
  ],
  "GlobalRules": [
    {
      "Id": "parla-villaggio",
      "TriggerVerbs": ["parla con i pescatori", "chiedi aiuto", "parla"],
      "SuccessResponse": "I pescatori tremano: 'Riaccendi il faro e la nebbia perdera' forza. Cerca la luce del vecchio guardiano.'"
    }
  ]
}
""";
	}
}
