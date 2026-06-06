using SimpleLLM.Library.NeuralNetworkFromScratch;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsole
{
	/// <summary>
	/// Esempio ibrido neurale-semantico.
	/// Addestra una rete a prevedere il vettore della parola successiva e poi
	/// mappa il vettore predetto alla parola reale piu' vicina nel vocabolario.
	/// </summary>
	internal class NeuralSemanticLLM
	{
		/// <summary>
		/// Step eseguiti:
		/// 1) definisce vocabolario vettoriale mock,
		/// 2) prepara coppie input/target,
		/// 3) addestra il MLP,
		/// 4) genera testo con ricerca del nearest token.
		/// </summary>
		public static void Execute()
		{
			// 1. Vocabolario ed Embeddings mock (es: asse X=forza, Y=magia, Z=punteggiatura)
			var vocab = new Dictionary<string, double[]>
			{
				{ "il",         new double[] { 0.1, 0.1, 0.1 } },
				{ "cavaliere",  new double[] { 0.9, 0.1, 0.1 } },
				{ "spada",      new double[] { 0.8, 0.2, 0.1 } },
				{ "mago",       new double[] { 0.1, 0.9, 0.1 } },
				{ "incantesimo",new double[] { 0.2, 0.8, 0.1 } },
				{ ".",          new double[] { 0.0, 0.0, 0.9 } }
			};

			// 2. Training Set (Sequenze: Parola attuale -> Parola successiva)
			var trainingInputs = new List<double[]> { vocab["il"], vocab["cavaliere"], vocab["spada"], vocab["il"], vocab["mago"], vocab["incantesimo"] };
			var trainingTargets = new List<double[]> { vocab["cavaliere"], vocab["spada"], vocab["."], vocab["mago"], vocab["incantesimo"], vocab["."] };

			// 3. Addestramento Rete Neurale (utilizza SimpleNeuralNetwork del Cap. 4)
			var nn = new SimpleNeuralNetwork(3, 8, 3);
			double learningRate = 0.5;

			for (int epoch = 0; epoch < 5000; epoch++)
			{
				for (int i = 0; i < trainingInputs.Count; i++)
				{
					nn.Train(trainingInputs[i], trainingTargets[i], learningRate);
				}
			}

			// 4. Generazione Testo
			GenerateText(nn, vocab, "mago", 2); // Genererà: mago -> incantesimo -> .
		}

		/// <summary>
		/// Generazione autoregressiva: predice il prossimo vettore e lo converte in token.
		/// </summary>
		static void GenerateText(SimpleNeuralNetwork nn, Dictionary<string, double[]> vocab, string prompt, int length)
		{
			string currentWord = prompt;
			Console.Write(currentWord + " ");

			for (int i = 0; i < length; i++)
			{
				double[] predictedVector = nn.Predict(vocab[currentWord]);
				string nextWord = GetClosestWord(predictedVector, vocab);

				Console.Write(nextWord + " ");
				currentWord = nextWord;
				if (currentWord == ".") break;
			}
		}

		/// <summary>
		/// Restituisce la parola il cui embedding ha similarita' del coseno massima
		/// rispetto al vettore predetto dalla rete.
		/// </summary>
		static string GetClosestWord(double[] targetVector, Dictionary<string, double[]> vocab)
		{
			string bestWord = "";
			double bestScore = -2.0;

			foreach (var kvp in vocab)
			{
				double score = CosineSimilarity(targetVector, kvp.Value);
				if (score > bestScore)
				{
					bestScore = score;
					bestWord = kvp.Key;
				}
			}
			return bestWord;
		}

		/// <summary>
		/// Implementazione elementare della cosine similarity su vettori double.
		/// </summary>
		static double CosineSimilarity(double[] a, double[] b)
		{
			double dot = 0, magA = 0, magB = 0;
			for (int i = 0; i < a.Length; i++)
			{
				dot += a[i] * b[i];
				magA += a[i] * a[i];
				magB += b[i] * b[i];
			}
			return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
		}
	}
}	