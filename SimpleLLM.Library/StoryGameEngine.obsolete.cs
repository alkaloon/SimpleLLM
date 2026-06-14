using SimpleLLM.Library.VectorEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLLM.Library.TextAdventure
{
	public partial class StoryGameEngine
	{
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
	}
}
