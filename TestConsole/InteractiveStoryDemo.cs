using SimpleLLM.Library.TextAdventure;
using SimpleLLM.Library.VectorEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace TestConsole
{
	/// <summary>
	/// Demo completa del motore StoryGameEngine con una mini avventura fantasy.
	/// </summary>
	internal static class InteractiveStoryDemo
	{
		private const string DefaultSaveFile = "savegame.json";
		private const string StoryDefinitionFile = @"D:\Alberto\SVILUPPO\SimpleLLM\neo-venice.json"; //"story-definition.json";

		public static void Execute()
		{
			var story = BuildStory();
			using var embeddingService = new LocalEmbeddingService();
			var engine = new StoryGameEngine(story, embeddingService);

			Console.WriteLine("Motore semantico attivo: embeddings + cosine similarity.");
			Console.WriteLine($"Soglia semantica corrente: {story.SemanticThreshold:F2}");
			Console.WriteLine(engine.Begin());
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
				Console.ForegroundColor = result.IsBlocked ? ConsoleColor.Yellow : ConsoleColor.Green;
				Console.WriteLine(result.Response);
				Console.ResetColor();

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

		private static StoryDefinition BuildStory()
		{
			if (File.Exists(StoryDefinitionFile))
			{
				Console.WriteLine($"Caricamento storia da file: {Path.GetFullPath(StoryDefinitionFile)}");
				return StoryDefinition.FromFile(StoryDefinitionFile);
			}

			Console.WriteLine("Nessun file story-definition.json trovato: uso fallback JSON embedded.");
			return StoryDefinition.FromJson(EmbeddedStoryJson);
		}

		private const string EmbeddedStoryJson = """
{
  "Titolo": "Le Cronache del Faro Spezzato",
  "Protagonista": "Ardan, sentinella del villaggio",
  "Introduzione": "Una nebbia innaturale avvolge la costa. Il faro, spento da settimane, ha risvegliato antiche creature marine. Tu sei l'unico a conoscere il sentiero verso la torre.",
  "InitialSceneId": "piazza",
  "SemanticThreshold": 0.58,
  "EnableSemanticScoreLogging": false,
  "Scenes": [
    {
      "Id": "piazza",
      "Titolo": "Piazza del Porto",
      "Descrizione": "I pescatori barricano le botteghe. Vedi la vecchia lanterna del guardiano, un vicolo verso la scogliera e la cappella chiusa.",
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
