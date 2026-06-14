using SimpleLLM.Library.VectorEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SimpleLLM.Library.TextAdventure
{
	public partial class StoryGameEngine
	{
		private readonly StoryGameState _state;
		private static readonly JsonSerializerOptions SaveJsonOptions = new()
		{
			WriteIndented = true
		};
		public StoryGameState State => _state;

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
	}
}
