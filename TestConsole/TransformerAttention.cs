using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsole
{
	/// <summary>
	/// Demo didattica di self-attention semplificata.
	/// Calcola punteggi Q*K^T (qui con vettori input diretti), applica softmax e
	/// visualizza la matrice dei pesi di attenzione.
	/// </summary>
	internal class TransformerAttention
	{
		/// <summary>
		/// Step eseguiti:
		/// 1) definisce embeddings e frase,
		/// 2) costruisce la matrice score,
		/// 3) normalizza con softmax,
		/// 4) stampa la matrice attention interpretabile.
		/// </summary>
		public static void Execute()
		{

			// 1. Il nostro mini-vocabolario di Embeddings 3D (X=Forza, Y=Magia, Z=Genere/Articolo)
			var vocab = new Dictionary<string, double[]>
			{
				{ "il",         new double[] { 0.1, 0.1, 0.8 } },
				{ "guerriero",  new double[] { 0.9, 0.1, 0.1 } },
				{ "saluta",     new double[] { 0.2, 0.2, 0.2 } },
				{ "mago",       new double[] { 0.1, 0.9, 0.1 } }
			};

			// La nostra frase di input
			string frase = "il guerriero saluta il mago";
			string[] parole = frase.Split(' ');

			// Convertiamo la frase in una lista di vettori (La nostra matrice di input X)
			List<double[]> inputSequence = parole.Select(p => vocab[p]).ToList();

			Console.WriteLine($"Analisi della Self-Attention per la frase: '{frase}'\n");

			// 2. Calcoliamo i pesi di attenzione (La matrice Q*K^T)
			// In un vero Transformer, gli input verrebbero moltiplicati per matrici di pesi Wq, Wk, Wv apprese.
			// Qui, per semplicità didattica, usiamo i vettori stessi (Basic Self-Attention)
			double[,] attentionScores = CalculateAttentionScores(inputSequence);

			// Applichiamo la funzione Softmax per trasformare i punteggi in probabilità (0.0 -> 1.0)
			double[,] attentionWeights = ApplySoftmax(attentionScores);

			// 3. Mostriamo la Matrice di Attenzione a schermo
			PrintAttentionMatrix(parole, attentionWeights);

			Console.WriteLine("\nOsservazione: Guarda i valori alti! Il sistema matematicamente ha capito");
			Console.WriteLine("che 'guerriero' e 'mago' sono entità rilevanti, mentre gli articoli 'il'");
			Console.WriteLine("vengono messi in secondo piano.");
		}

		// Calcola il Prodotto Scalare tra tutti i vettori della sequenza (Simulazione Q * K^T)
		/// <summary>
		/// Simula Q*K^T tramite prodotto scalare tra tutte le coppie di token.
		/// </summary>
		static double[,] CalculateAttentionScores(List<double[]> sequence)
		{
			int seqLen = sequence.Count;
			double[,] scores = new double[seqLen, seqLen];

			for (int i = 0; i < seqLen; i++) // Query word
			{
				for (int j = 0; j < seqLen; j++) // Key word
				{
					scores[i, j] = DotProduct(sequence[i], sequence[j]);
				}
			}
			return scores;
		}

		// La funzione Softmax evidenzia i valori grandi e schiaccia quelli piccoli
		/// <summary>
		/// Applica softmax riga per riga per ottenere pesi probabilistici per query.
		/// </summary>
		static double[,] ApplySoftmax(double[,] scores)
		{
			int size = scores.GetLength(0);
			double[,] weights = new double[size, size];

			for (int i = 0; i < size; i++) // Per ogni riga (per ogni Query)
			{
				double sumExp = 0;
				for (int j = 0; j < size; j++) sumExp += Math.Exp(scores[i, j]);

				for (int j = 0; j < size; j++)
				{
					weights[i, j] = Math.Exp(scores[i, j]) / sumExp;
				}
			}
			return weights;
		}

		/// <summary>
		/// Prodotto scalare standard fra due vettori.
		/// </summary>
		static double DotProduct(double[] a, double[] b)
		{
			double sum = 0;
			for (int i = 0; i < a.Length; i++) sum += a[i] * b[i];
			return sum;
		}

		/// <summary>
		/// Stampa a console una heatmap tabellare dei pesi di attenzione in percentuale.
		/// </summary>
		static void PrintAttentionMatrix(string[] parole, double[,] matrix)
		{
			int len = parole.Length;
			Console.Write("          ");
			foreach (var p in parole) Console.Write($"{p,-10}");
			Console.WriteLine("\n" + new string('-', 60));

			for (int i = 0; i < len; i++)
			{
				Console.Write($"{parole[i],-10}|");
				for (int j = 0; j < len; j++)
				{
					// Formattiamo il peso in percentuale per facile lettura
					Console.Write($"{(matrix[i, j] * 100):F1}%      ");
				}
				Console.WriteLine();
			}
		}
	}
}
