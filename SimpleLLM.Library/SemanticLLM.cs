using SmartComponents.LocalEmbeddings;
using System;
using System.Collections.Generic;
using System.Numerics.Tensors;
using System.Text;

namespace SimpleLLM.Library.EmbeddedMicroLLM
{
	/// <summary>
	/// Variante semantica del micro LLM: invece di confrontare solo stringhe,
	/// converte i token in embeddings e sceglie il prossimo token tramite similarita' del coseno.
	/// Step principali:
	/// 1) Train: tokenizza e salva (token corrente, embedding, token successivo).
	/// 2) Generate: embed del token corrente, ricerca del nodo piu' simile,
	///    emissione del NextToken associato.
	/// </summary>
	public class SemanticLLM
	{
		private readonly LocalEmbedder _embedder;

		// Il "cervello" ora non mappa stringa->stringa, ma Vettore->Parola Successiva.
		private readonly List<SemanticNode> _brain = new();

		public SemanticLLM(LocalEmbedder embedder)
		{
			_embedder = embedder;
		}

		/// <summary>
		/// Costruisce la memoria semantica del modello a partire da una sequenza testuale.
		/// Ogni coppia adiacente di token genera un nodo nella knowledge list.
		/// </summary>
		public void Train(string text)
		{
			text = text.Replace(".", " .");
			string[] tokens = text.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 0; i < tokens.Length - 1; i++)
			{
				string currentToken = tokens[i];
				string nextToken = tokens[i + 1];

				// Trasformiamo la parola in un vettore a ~384 dimensioni e lo salviamo
				var embedding = _embedder.Embed(currentToken).Values;

				_brain.Add(new SemanticNode(currentToken, embedding, nextToken));
			}
		}

		/// <summary>
		/// Genera testo in modo autoregressivo usando ricerca del vicino semantico piu' vicino.
		/// Il ciclo termina quando raggiunge la lunghezza richiesta o incontra il token '.'.
		/// </summary>
		public string Generate(string prompt, int length)
		{
			string[] promptTokens = prompt.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string currentToken = promptTokens.Last();

			List<string> output = new List<string>(promptTokens);

			for (int i = 0; i < length; i++)
			{
				// 1. Convertiamo la parola corrente nel suo significato matematico (vettore)
				var currentEmbedding = _embedder.Embed(currentToken).Values;

				// 2. Cerchiamo nel nostro "cervello" il nodo col significato più simile 
				// utilizzando la Cosine Similarity accelerata via SIMD
				var bestMatch = _brain
					.Select(node => new
					{
						Node = node,
						// Calcoliamo la distanza tra il vettore del prompt e ogni vettore in memoria
						Similarity = TensorPrimitives.CosineSimilarity(currentEmbedding.Span, node.Embedding.Span)
					})
					.OrderByDescending(x => x.Similarity)
					.First();

				// 3. Prendiamo la parola successiva del nodo che ha vinto la gara di similarità
				string nextToken = bestMatch.Node.NextToken;
				output.Add(nextToken);

				// La parola generata diventa il nuovo contesto per il ciclo successivo
				currentToken = nextToken;

				if (nextToken == ".") break;
			}

			return string.Join(" ", output).Replace(" .", ".");
		}
	}

	/// <summary>
	/// Nodo atomico della memoria semantica:
	/// - TokenOriginale: token visto in training,
	/// - Embedding: rappresentazione vettoriale del token,
	/// - NextToken: token osservato subito dopo nel corpus.
	/// </summary>
	public record SemanticNode(string TokenOriginale, ReadOnlyMemory<float> Embedding, string NextToken);
}
