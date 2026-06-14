using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using SimpleLLM.Library.VectorEngine;

namespace SimpleLLM.Library.TextAdventure
{
	/// <summary>
	/// Livello di difficolta della storia e dei suggerimenti iniziali.
	/// </summary>
	public enum StoryDifficulty
	{
		Easy,
		Normal,
		Hard,
		Expert
	}

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
		public IReadOnlyList<string> RequiredFlags { get; init; } = [];
		public IReadOnlyList<string> RequiredItems { get; init; } = [];
		public IReadOnlyList<string> GrantedFlags { get; init; } = [];
		public IReadOnlyList<string> GrantedItems { get; init; } = [];
		public IReadOnlyList<string> ConsumedItems { get; init; } = [];
	}

	/// <summary>
	/// Stato runtime della sessione di gioco.
	/// </summary>
	public sealed class StoryGameState
	{
		public required string CurrentSceneId { get; set; }
		public HashSet<string> Flags { get; } = new(StringComparer.OrdinalIgnoreCase);
		public HashSet<string> Inventory { get; } = new(StringComparer.OrdinalIgnoreCase);
	}

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

	/// <summary>
	/// Stato sintetico della scena corrente per la UI testuale.
	/// </summary>
	public sealed record StorySceneStatus
	{
		public required string SceneTitle { get; init; }
		public required string SceneDescription { get; init; }
		public required string Objective { get; init; }
		public required string InventorySummary { get; init; }
		public required string FlagSummary { get; init; }
		public required IReadOnlyList<StoryActionHint> Suggestions { get; init; }
	}

	/// <summary>
	/// Motore testuale: interpreta input utente e applica regole di trama.
	/// </summary>
	public sealed class StoryGameEngine
	{
		private static readonly JsonSerializerOptions SaveJsonOptions = new()
		{
			WriteIndented = true
		};

		private readonly StoryDefinition _definition;
		private readonly Dictionary<string, StoryScene> _scenesById;
		private readonly StoryGameState _state;
		private readonly IEmbeddingService? _embeddingService;
		private readonly float _semanticThreshold;
		private readonly bool _enableSemanticScoreLogging;
		private readonly List<RuleEmbeddingEntry> _semanticIndex;
		private bool ShowHintsDuringGame => _definition.ShowHintsOnStart;

		public StoryGameEngine(StoryDefinition definition, IEmbeddingService? embeddingService = null, float? semanticThreshold = null)
		{
			_definition = definition;
			_embeddingService = embeddingService;
			_semanticThreshold = semanticThreshold ?? definition.SemanticThreshold;
			_enableSemanticScoreLogging = definition.EnableSemanticScoreLogging;
			_scenesById = definition.Scenes.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);

			if (!_scenesById.ContainsKey(definition.InitialSceneId))
			{
				throw new ArgumentException($"Scena iniziale '{definition.InitialSceneId}' non trovata.");
			}

			_state = new StoryGameState
			{
				CurrentSceneId = definition.InitialSceneId
			};

			_semanticIndex = BuildSemanticIndex();
		}

		public StoryGameState State => _state;

		public StoryScene CurrentScene => _scenesById[_state.CurrentSceneId];

		public string CurrentObjective => BuildCurrentObjective();

		public string? LastSemanticLog { get; private set; }

		public string Begin(bool includeHints = true)
		{
			return BuildStartMessage(includeHints);
		}

		/// <summary>
		/// Salva lo stato della sessione in formato JSON su file.
		/// </summary>
		public void SaveToFile(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				throw new ArgumentException("Percorso di salvataggio non valido.", nameof(filePath));
			}

			string fullPath = Path.GetFullPath(filePath);
			string? folder = Path.GetDirectoryName(fullPath);
			if (!string.IsNullOrWhiteSpace(folder))
			{
				Directory.CreateDirectory(folder);
			}

			File.WriteAllText(fullPath, SaveToJson(), Encoding.UTF8);
		}

		/// <summary>
		/// Carica lo stato della sessione da file JSON.
		/// </summary>
		public void LoadFromFile(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				throw new ArgumentException("Percorso di caricamento non valido.", nameof(filePath));
			}

			string fullPath = Path.GetFullPath(filePath);
			if (!File.Exists(fullPath))
			{
				throw new FileNotFoundException("File di salvataggio non trovato.", fullPath);
			}

			string json = File.ReadAllText(fullPath, Encoding.UTF8);
			LoadFromJson(json);
		}

		/// <summary>
		/// Serializza lo stato corrente in JSON.
		/// </summary>
		public string SaveToJson()
		{
			var save = new StoryGameSaveData
			{
				StoryTitle = _definition.Titolo,
				CurrentSceneId = _state.CurrentSceneId,
				Flags = _state.Flags.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray(),
				Inventory = _state.Inventory.OrderBy(i => i, StringComparer.OrdinalIgnoreCase).ToArray()
			};

			return JsonSerializer.Serialize(save, SaveJsonOptions);
		}

		/// <summary>
		/// Carica lo stato corrente da un payload JSON.
		/// </summary>
		public void LoadFromJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				throw new ArgumentException("Payload JSON vuoto.", nameof(json));
			}

			StoryGameSaveData? save = JsonSerializer.Deserialize<StoryGameSaveData>(json);
			if (save is null)
			{
				throw new InvalidOperationException("Impossibile deserializzare il salvataggio.");
			}

			if (string.IsNullOrWhiteSpace(save.CurrentSceneId) || !_scenesById.ContainsKey(save.CurrentSceneId))
			{
				throw new InvalidOperationException("Il salvataggio contiene una scena non valida.");
			}

			if (!string.IsNullOrWhiteSpace(save.StoryTitle)
					&& !string.Equals(save.StoryTitle, _definition.Titolo, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Il salvataggio appartiene a una storia diversa.");
			}

			_state.CurrentSceneId = save.CurrentSceneId;

			_state.Flags.Clear();
			foreach (string flag in save.Flags ?? [])
			{
				_state.Flags.Add(flag);
			}

			_state.Inventory.Clear();
			foreach (string item in save.Inventory ?? [])
			{
				_state.Inventory.Add(item);
			}
		}

		public StoryTurnResult HandleInput(string playerInput)
		{
			LastSemanticLog = null;

			string normalized = Normalize(playerInput);
			if (string.IsNullOrWhiteSpace(normalized))
			{
				return BuildResult("Non sento alcuna azione. Descrivi cosa vuoi fare.", isBlocked: true, ruleId: null);
			}

			if (normalized is "aiuto" or "help" or "comandi")
			{
				return BuildResult(BuildHelpMessage(), isBlocked: false, ruleId: null);
			}

			if (normalized is "guarda" or "osserva" or "look")
			{
				return BuildResult(FormatScene(CurrentScene), isBlocked: false, ruleId: null);
			}

			if (normalized is "inventario" or "zaino" or "inventory")
			{
				return BuildResult(BuildInventoryMessage(), isBlocked: false, ruleId: null);
			}

			if (normalized is "stato")
			{
				return BuildResult(BuildFlagsMessage(), isBlocked: false, ruleId: null);
			}
			
			var candidates = CurrentScene.Rules
					.Concat(_definition.GlobalRules)
					.Select(r => new
					{
						Rule = r,
						Score = GetMatchScore(normalized, r.TriggerVerbs)
					})
					.Where(r => r.Score > 0f)
					.OrderByDescending(r => r.Score)
					.ToList();

			if (candidates.Count == 0)
			{
				SemanticMatchResult? semanticMatch = FindSemanticRule(normalized);
				if (_enableSemanticScoreLogging && semanticMatch is not null)
				{
					LastSemanticLog = semanticMatch.Accepted
							? $"[Semantica] Match '{semanticMatch.TriggerText}' (regola: {semanticMatch.Rule.Id}) con score {semanticMatch.Score:F3} >= soglia {_semanticThreshold:F3}."
							: $"[Semantica] Candidato '{semanticMatch.TriggerText}' (regola: {semanticMatch.Rule.Id}) con score {semanticMatch.Score:F3} < soglia {_semanticThreshold:F3}.";
				}

				if (semanticMatch is not null && semanticMatch.Accepted)
				{
					candidates.Add(new { semanticMatch.Rule, semanticMatch.Score });
				}
			}

			if (candidates.Count == 0)
			{
				IReadOnlyList<StoryActionHint> hints = GetActionHints(3);
				string suggestion = ShowHintsDuringGame && hints.Count > 0
						? $" Prova: {string.Join(", ", hints.Select(h => h.Command))}."
						: string.Empty;

				return BuildResult($"Non riesco ad associare questa azione alla scena corrente.{suggestion}", isBlocked: true, ruleId: null);
			}

			foreach (var candidate in candidates)
			{
				if (!HasRequirements(candidate.Rule))
				{
					string blockedText = string.IsNullOrWhiteSpace(candidate.Rule.BlockedResponse)
							? "Ci provi, ma ti manca qualcosa per riuscirci."
							: candidate.Rule.BlockedResponse;

					string? missingRequirements = BuildMissingRequirementsNote(candidate.Rule);
					if (!string.IsNullOrWhiteSpace(missingRequirements))
					{
						blockedText = $"{blockedText} {missingRequirements}.";
					}

					blockedText = AppendHelpfulNudge(blockedText);

					return BuildResult(blockedText, isBlocked: true, ruleId: candidate.Rule.Id);
				}

				ApplyRule(candidate.Rule);
				string response = BuildNarrativeResponse(candidate.Rule.SuccessResponse, sceneChanged: candidate.Rule.NextSceneId is not null);
				return BuildResult(response, isBlocked: false, ruleId: candidate.Rule.Id);
			}

			return BuildResult("L'azione non produce effetti narrativi rilevanti.", isBlocked: true, ruleId: null);
		}
		private float GetMatchScore(string normalizedInput, IReadOnlyList<string> verbs)
		{
			if (string.IsNullOrEmpty(normalizedInput) || verbs == null || verbs.Count == 0)
				return 0f;

			// Calcola lo score per ogni verbo nel set della regola
			var scores = verbs.Select(v => {
				// Se il testo è identico, il match è perfetto (1.0)
				if (v == normalizedInput) return 1.0f;

				// Utilizza il servizio di embedding per calcolare la similarità tra l'input e il singolo verbo
				// Nota: Assumiamo che _embeddingService sia disponibile nel contesto della classe
				return _embeddingService?.GetSimilarity(normalizedInput, v) ?? 0f;
			});

			// Il punteggio finale della regola è il massimo dei match trovati tra i suoi verbi
			// Questo assicura che se l'utente "parla" in modo simile a uno qualsiasi dei verbi validi, 
			// la regola venga selezionata correttamente.
			return scores?.Max() ?? 0f;
		}
		[Obsolete("Usa GetMatchScore per ottenere un punteggio di similarità più accurato. IsMatch ora restituisce una tupla con match booleano e punteggio float.")]
		private (bool, float) IsMatch(string normalizedInput, IReadOnlyList<string> verbs)
		{
			// 1. Controllo Lessico (Veloce): se l'utente scrive la parola esatta, accettiamo subito.
			foreach (string verb in verbs)
			{
				string normalizedVerb = Normalize(verb);
				if (normalizedInput == normalizedVerb || normalizedInput.StartsWith(normalizedVerb + " ", StringComparison.Ordinal))
				{
					return (true, 1.0f);
				}

				//if (normalizedInput.Contains(" " + normalizedVerb + " ", StringComparison.Ordinal))
				//{
				//        return true;
				//}

				//if (normalizedInput.EndsWith(" " + normalizedVerb, StringComparison.Ordinal))
				//{
				//        return true;
				//}
			}
			// 2. Controllo Semantico: se non c'è match esatto e abbiamo un servizio di embedding attivo,
			// cerchiamo una corrispondenza nel "Semantic Index".
			if (_embeddingService != null && _semanticIndex.Count > 0)
			{
				ReadOnlyMemory<float> inputVector = _embeddingService.GetEmbedding(normalizedInput);

				foreach (var entry in _semanticIndex)
				{
					// Calcoliamo la similarità del coseno tra l'input dell'utente e il trigger della regola.
					// Nota: Se vogliamo che questa logica sia gestita da IsMatch, dobbiamo assicurarci 
					// che "verbs" o le regole correlate siano accessibili tramite l'indice semantico.

					// Tuttavia, la struttura attuale di IsMatch riceve una lista di stringhe (verbs).
					// Se vogliamo mantenere la firma del metodo ma renderlo intelligente:
					float similarity = _embeddingService.CalculateSimilarity(inputVector.Span, entry.Embedding.Span);

					if (similarity >= _semanticThreshold)
					{
						return (true, similarity);
					}
				}
			}
			return (false, 0f);
		}

		private bool HasRequirements(StoryActionRule rule)
		{
			bool hasFlags = rule.RequiredFlags.All(flag => _state.Flags.Contains(flag));
			bool hasItems = rule.RequiredItems.All(item => _state.Inventory.Contains(item));
			return hasFlags && hasItems;
		}

		private void ApplyRule(StoryActionRule rule)
		{
			foreach (string item in rule.ConsumedItems)
			{
				_state.Inventory.Remove(item);
			}

			foreach (string item in rule.GrantedItems)
			{
				_state.Inventory.Add(item);
			}

			foreach (string flag in rule.GrantedFlags)
			{
				_state.Flags.Add(flag);
			}

			if (!string.IsNullOrWhiteSpace(rule.NextSceneId))
			{
				if (!_scenesById.ContainsKey(rule.NextSceneId))
				{
					throw new InvalidOperationException($"La scena '{rule.NextSceneId}' non esiste nella definizione.");
				}

				_state.CurrentSceneId = rule.NextSceneId;
			}
		}

		private StoryTurnResult BuildResult(string text, bool isBlocked, string? ruleId)
		{
			return new StoryTurnResult
			{
				Response = text,
				CurrentScene = CurrentScene,
				IsBlocked = isBlocked,
				MatchedRuleId = ruleId
			};
		}

		private string BuildNarrativeResponse(string actionOutcome, bool sceneChanged)
		{
			if (!sceneChanged)
			{
				return actionOutcome;
			}

			var sb = new StringBuilder();
			sb.AppendLine(actionOutcome);
			sb.AppendLine();
			sb.Append(FormatScene(CurrentScene));
			return sb.ToString();
		}

		private string BuildStartMessage(bool includeHints)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"Titolo: {_definition.Titolo}");
			sb.AppendLine($"Protagonista: {_definition.Protagonista}");
			sb.AppendLine();
			sb.AppendLine(_definition.Introduzione);
			sb.AppendLine();
			sb.AppendLine(FormatScene(CurrentScene));
			sb.AppendLine();
			sb.AppendLine($"Obiettivo attuale: {CurrentObjective}");
			if (includeHints)
			{
				sb.AppendLine();
				sb.AppendLine("Azioni suggerite ora:");

				foreach (StoryActionHint hint in GetActionHints(4))
				{
					string status = hint.IsAvailable ? hint.Note : $"bloccata, {hint.Note}";
					sb.AppendLine($"- {hint.Command}: {status}");
				}
			}

			sb.AppendLine();
			sb.Append(includeHints
					? "Digita 'aiuto' per i comandi di supporto e i suggerimenti contestuali."
					: "Difficolta alta: i suggerimenti iniziali sono nascosti. Digita 'aiuto' quando vuoi una guida.");
			return sb.ToString();
		}

		private string BuildHelpMessage()
		{
			string suggestedActions = ShowHintsDuringGame
					? string.Join(Environment.NewLine, GetActionHints(4).Select(h => h.IsAvailable
							? $"- {h.Command}: {h.Note}"
							: $"- {h.Command}: bloccata, {h.Note}"))
					: "- Suggerimenti nascosti in questa modalita. Usa le azioni di base e osserva la scena.";

			return string.Join(Environment.NewLine,
			[
					"Comandi utili:",
								"- guarda: ristampa la scena corrente",
								"- inventario: mostra oggetti raccolti",
								"- stato: mostra flag narrativi attivi",
								"- salva [file]: salva la partita in JSON",
								"- carica [file]: carica la partita da JSON",
								"- aiuto: mostra questo messaggio",
								"- esci: termina la sessione",
								"",
								$"Obiettivo attuale: {CurrentObjective}",
								"",
								"Azioni suggerite ora:",
								suggestedActions
			]);
		}

		private string BuildInventoryMessage()
		{
			if (_state.Inventory.Count == 0)
			{
				return "Inventario vuoto. Se trovi un oggetto utile, provalo subito sulla scena corrente.";
			}

			return "Inventario: " + string.Join(", ", _state.Inventory.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
		}

		private string BuildFlagsMessage()
		{
			if (_state.Flags.Count == 0)
			{
				return "Nessun avanzamento narrativo registrato. Esplora la scena o usa 'guarda' per orientarti.";
			}

			return "Stato trama: " + string.Join(", ", _state.Flags.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
		}

		public StorySceneStatus GetSceneStatus(int maxSuggestions = 4)
		{
			return new StorySceneStatus
			{
				SceneTitle = CurrentScene.Titolo,
				SceneDescription = CurrentScene.Descrizione,
				Objective = CurrentObjective,
				InventorySummary = BuildInventoryMessage(),
				FlagSummary = BuildFlagsMessage(),
				Suggestions = GetActionHints(maxSuggestions)
			};
		}

		private string BuildCurrentObjective()
		{
			if (!string.IsNullOrWhiteSpace(CurrentScene.Objective))
			{
				return CurrentScene.Objective!;
			}

			IReadOnlyList<StoryActionHint> hints = GetActionHints(3);
			if (hints.Count > 0)
			{
				StoryActionHint available = hints.FirstOrDefault(h => h.IsAvailable) ?? hints[0];
				return available.IsAvailable
						? $"Prova: {available.Command}."
						: $"Recupera ciò che manca per provare: {available.Command}.";
			}

			return "Esplora la scena e prova azioni semplici come 'guarda' o 'aiuto'.";
		}

		private static string AppendHelpfulNudge(string text)
		{
			return string.Join(" ",
					text.TrimEnd('.', ' '),
					"Puoi usare 'inventario' per controllare gli oggetti o 'aiuto' per vedere i comandi utili.");
		}

		private static string Normalize(string value)
		{
			var sb = new StringBuilder(value.Length);
			foreach (char c in value.ToLowerInvariant().Trim())
			{
				if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
				{
					sb.Append(c);
				}
			}

			string normalized = sb.ToString();
			while (normalized.Contains("  ", StringComparison.Ordinal))
			{
				normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
			}

			return normalized;
		}

		private static string FormatScene(StoryScene scene)
		{
			return $"[{scene.Titolo}]\n{scene.Descrizione}";
		}

		private List<RuleEmbeddingEntry> BuildSemanticIndex()
		{
			if (_embeddingService is null)
			{
				return [];
			}

			var entries = new List<RuleEmbeddingEntry>();

			foreach (StoryScene scene in _definition.Scenes)
			{
				foreach (StoryActionRule rule in scene.Rules)
				{
					foreach (string trigger in rule.TriggerVerbs)
					{
						string normalizedTrigger = Normalize(trigger);
						if (string.IsNullOrWhiteSpace(normalizedTrigger))
						{
							continue;
						}

						entries.Add(new RuleEmbeddingEntry(
								rule,
								scene.Id,
								IsGlobal: false,
								normalizedTrigger,
								_embeddingService.GetEmbedding(normalizedTrigger)));
					}
				}
			}

			foreach (StoryActionRule rule in _definition.GlobalRules)
			{
				foreach (string trigger in rule.TriggerVerbs)
				{
					string normalizedTrigger = Normalize(trigger);
					if (string.IsNullOrWhiteSpace(normalizedTrigger))
					{
						continue;
					}

					entries.Add(new RuleEmbeddingEntry(
							rule,
							SceneId: string.Empty,
							IsGlobal: true,
							normalizedTrigger,
							_embeddingService.GetEmbedding(normalizedTrigger)));
				}
			}

			return entries;
		}

		public IReadOnlyList<StoryActionHint> GetActionHints(int maxCount = 5)
		{
			if (!ShowHintsDuringGame)
			{
				return Array.Empty<StoryActionHint>();
			}

			if (maxCount <= 0)
			{
				return Array.Empty<StoryActionHint>();
			}

			return CurrentScene.Rules
					.Concat(_definition.GlobalRules)
					.Select(rule => new
					{
						Rule = rule,
						Command = rule.TriggerVerbs.FirstOrDefault() ?? rule.Id,
						IsAvailable = HasRequirements(rule),
						Note = HasRequirements(rule)
									? BuildAvailableActionNote(rule)
									: BuildMissingRequirementsNote(rule) ?? "azione non disponibile"
					})
					.OrderByDescending(item => item.IsAvailable)
					.ThenByDescending(item => item.Rule.TriggerVerbs.Max(v => v.Length))
					.Take(maxCount)
					.Select(item => new StoryActionHint
					{
						Command = item.Command,
						RuleId = item.Rule.Id,
						IsAvailable = item.IsAvailable,
						Note = item.Note,
						NextSceneId = item.Rule.NextSceneId
					})
					.ToList();
		}

		private string BuildAvailableActionNote(StoryActionRule rule)
		{
			if (!string.IsNullOrWhiteSpace(rule.NextSceneId))
			{
				return $"porta a {rule.NextSceneId}";
			}

			var effects = new List<string>();
			if (rule.GrantedItems.Count > 0)
			{
				effects.Add($"oggetti: {string.Join(", ", rule.GrantedItems)}");
			}

			if (rule.GrantedFlags.Count > 0)
			{
				effects.Add($"stato: {string.Join(", ", rule.GrantedFlags)}");
			}

			if (rule.ConsumedItems.Count > 0)
			{
				effects.Add($"consuma: {string.Join(", ", rule.ConsumedItems)}");
			}

			return effects.Count == 0 ? "azione disponibile" : string.Join(" | ", effects);
		}

		private string? BuildMissingRequirementsNote(StoryActionRule rule)
		{
			var missingItems = rule.RequiredItems.Where(item => !_state.Inventory.Contains(item)).ToArray();
			var missingFlags = rule.RequiredFlags.Where(flag => !_state.Flags.Contains(flag)).ToArray();
			var parts = new List<string>();

			if (missingItems.Length > 0)
			{
				parts.Add($"mancano oggetti: {string.Join(", ", missingItems)}");
			}

			if (missingFlags.Length > 0)
			{
				parts.Add($"mancano flag: {string.Join(", ", missingFlags)}");
			}

			return parts.Count == 0 ? null : string.Join("; ", parts);
		}

		private SemanticMatchResult? FindSemanticRule(string normalizedInput)
		{
			if (_embeddingService is null || _semanticIndex.Count == 0)
			{
				return null;
			}

			ReadOnlyMemory<float> inputVector = _embeddingService.GetEmbedding(normalizedInput);

			var best = _semanticIndex
					.Where(e => e.IsGlobal || string.Equals(e.SceneId, _state.CurrentSceneId, StringComparison.OrdinalIgnoreCase))
					.Select(e => new
					{
						Entry = e,
						Score = _embeddingService.CalculateSimilarity(inputVector.Span, e.Embedding.Span)
					})
					.OrderByDescending(x => x.Score)
					.FirstOrDefault();

			if (best is null || best.Score < _semanticThreshold)
			{
				if (best is null)
				{
					return null;
				}

				return new SemanticMatchResult(
						best.Entry.Rule,
						best.Entry.TriggerText,
						best.Score,
						Accepted: false);
			}

			return new SemanticMatchResult(
					best.Entry.Rule,
					best.Entry.TriggerText,
					best.Score,
					Accepted: true);
		}
	}

	internal sealed record RuleEmbeddingEntry(
			StoryActionRule Rule,
			string SceneId,
			bool IsGlobal,
			string TriggerText,
			ReadOnlyMemory<float> Embedding);

	internal sealed record SemanticMatchResult(
			StoryActionRule Rule,
			string TriggerText,
			float Score,
			bool Accepted);

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