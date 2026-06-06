namespace SimpleLLM.Library.MicroLLM
{
	/// <summary>
	/// Modello linguistico minimale basato su bigrammi (catena di Markov di ordine 1).
	/// Step principali:
	/// 1) Analizza il corpus token per token.
	/// 2) Conta quante volte una parola segue la precedente.
	/// 3) In generazione, seleziona la parola successiva piu' frequente.
	/// </summary>
	public class SimpleLLM
	{
		// Questo dizionario è il "cervello" (i pesi) del nostro modello.
		// Mappa una parola (es. "il") a una lista di parole che la seguono e alla loro frequenza.
		private Dictionary<string, Dictionary<string, int>> _brain;
		private Random _random;

		public SimpleLLM()
		{
			_brain = new Dictionary<string, Dictionary<string, int>>();
			_random = new Random();
		}

		/// <summary>
		/// Fase di addestramento:
		/// - normalizza il testo,
		/// - costruisce la tabella di transizione parola -> prossime parole,
		/// - aggiorna le frequenze osservate nel corpus.
		/// </summary>
		public void Train(string text)
		{
			// Tokenizzazione base: convertiamo tutto in minuscolo e dividiamo per spazi
			text = text.Replace(".", " ."); // Trattiamo il punto come un token separato
			string[] tokens = text.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			// Costruiamo le probabilità analizzando i token adiacenti
			for (int i = 0; i < tokens.Length - 1; i++)
			{
				string currentToken = tokens[i];
				string nextToken = tokens[i + 1];

				if (!_brain.TryGetValue(currentToken, out Dictionary<string, int>? value))
				{
					value = [];
					_brain[currentToken] = value;
				}

				if (!value.TryGetValue(nextToken, out int value1))
				{
					value1 = 0;
					value[nextToken] = value1;
				}

				value[nextToken] = ++value1;
			}
		}

		/// <summary>
		/// Fase di inferenza autoregressiva:
		/// - parte dall'ultimo token del prompt,
		/// - cerca i candidati successivi nel dizionario,
		/// - aggiunge il token piu' frequente e ripete il ciclo.
		/// </summary>
		public string Generate(string prompt, int length)
		{
			string[] promptTokens = prompt.ToLower().Split(' ');
			string currentToken = promptTokens.Last(); // Prende l'ultima parola del prompt per iniziare

			List<string> generatedTokens = new List<string>(promptTokens);

			for (int i = 0; i < length; i++)
			{
				// Se la parola non è nel nostro vocabolario, ci fermiamo
				if (!_brain.ContainsKey(currentToken))
					break;

				// Otteniamo tutte le possibili parole successive e le loro frequenze
				var possibleNextTokens = _brain[currentToken];

				// Scegliamo il token successivo (in questo esempio prendiamo semplicemente il più probabile/frequente)
				// In un vero LLM, qui si introduce la "Temperature" per dare casualità
				var nextToken = possibleNextTokens.OrderByDescending(x => x.Value).First().Key;

				generatedTokens.Add(nextToken);
				currentToken = nextToken; // La nuova parola diventa il contesto per la successiva
			}

			return string.Join(" ", generatedTokens).Replace(" .", ".");
		}
	}
}
