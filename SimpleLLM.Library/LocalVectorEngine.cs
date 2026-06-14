using SmartComponents.LocalEmbeddings;
using System.Numerics.Tensors;

namespace SimpleLLM.Library.VectorEngine
{
	/// <summary>
	/// Contratto minimo per un motore vettoriale:
	/// 1) trasformazione testo -> embedding,
	/// 2) metrica di similarita' fra due vettori.
	/// </summary>
	public interface IEmbeddingService
	{
		/// <summary>
		/// Restituisce l'embedding del testo in input.
		/// </summary>
		ReadOnlyMemory<float> GetEmbedding(string text);

		/// <summary>
		/// Calcola la similarita' tra due vettori (tipicamente coseno).
		/// </summary>
		float CalculateSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB);
		float GetSimilarity(string input1, string input2);
	}

	/// <summary>
	/// Implementazione locale basata su modello ONNX tramite SmartComponents.LocalEmbeddings.
	/// Flusso operativo:
	/// - inizializzazione del runtime,
	/// - inferenza embedding,
	/// - confronto semantico accelerato via SIMD.
	/// </summary>
	public class LocalEmbeddingService : IEmbeddingService, IDisposable
	{
		private readonly LocalEmbedder _embedder;

		/// <summary>
		/// Crea e carica il modello locale in memoria.
		/// </summary>
		public LocalEmbeddingService()
		{
			// Inizializza il motore ONNX e carica il modello compatto in memoria.
			// Questa operazione è "costosa", per questo il servizio andrebbe usato come Singleton.
			_embedder = new LocalEmbedder();
		}

		/// <summary>
		/// Esegue validazione input e inferenza ONNX per ottenere il vettore semantico.
		/// </summary>
		public ReadOnlyMemory<float> GetEmbedding(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				throw new ArgumentException("Il testo non può essere vuoto", nameof(text));

			// Esegue la tokenizzazione e l'inferenza ONNX per generare i vettori (es. 384 dimensioni)
			EmbeddingF32 embedding = _embedder.Embed(text);
			return embedding.Values;
		}

		/// <summary>
		/// Calcola la cosine similarity usando primitive numeriche ottimizzate.
		/// </summary>
		public float CalculateSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
		{
			// Sfrutta l'accelerazione hardware (SIMD) della CPU per calcolare il coseno
			// su centinaia di dimensioni contemporaneamente in una singola istruzione
			return TensorPrimitives.CosineSimilarity(vectorA, vectorB);
		}

		public float GetSimilarity(string input1, string input2)
		{
			// Se le stringhe sono identiche dopo la normalizzazione, il punteggio è massimo
			if (input1 == input2) return 1.0f;

			// Recupera i vettori di embedding per entrambi gli input
			var vector1 = GetEmbedding(input1);
			var vector2 = GetEmbedding(input2);

			// Calcola la similarità del coseno tra i due vettori
			float dotProduct = 0f;
			float magnitude1 = 0f;
			float magnitude2 = 0f;

			for (int i = 0; i < vector1.Length; i++)
			{
				dotProduct += vector1.Span[i] * vector2.Span[i];
				magnitude1 += vector1.Span[i] * vector1.Span[i];
				magnitude2 += vector2.Span[i] * vector2.Span[i];
			}

			float magnitude = (float)Math.Sqrt(magnitude1) * (float)Math.Sqrt(magnitude2);

			if (magnitude == 0) return 0f;

			return dotProduct / magnitude;
		}


		/// <summary>
		/// Rilascia le risorse native del runtime embedding.
		/// </summary>
		public void Dispose()
		{
			_embedder.Dispose();
		}

		
	}
}
