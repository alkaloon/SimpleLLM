using SimpleLLM.Library.EmbeddedMicroLLM;
using SmartComponents.LocalEmbeddings;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsole
{
	/// <summary>
	/// Esempio semantico con embeddings locali ONNX.
	/// Dimostra come il modello possa generalizzare anche su parole non viste in training.
	/// </summary>
	internal class LocalEmbedderSemanticLLM
	{
		/// <summary>
		/// Step eseguiti:
		/// 1) inizializza LocalEmbedder,
		/// 2) addestra SemanticLLM su corpus dominio fantasy,
		/// 3) testa prompt con termini nuovi (paladino/stregone).
		/// </summary>
		public static void Execute()
		{
			// Inizializziamo il motore ONNX locale
			using var embedder = new LocalEmbedder();

			// 1. Il nostro Corpus di addestramento
			string corpus = "il guerriero impugna la spada. il mago lancia la palla di fuoco. il ranger scocca la freccia. il chierico cura le ferite.";

			var model = new SemanticLLM(embedder);

			Console.WriteLine("Addestramento semantico in corso (calcolo dei vettori)...");
			model.Train(corpus);
			Console.WriteLine("Addestramento completato!\n");

			// 2. Inferenza con parole MAI VISTE nel corpus
			// "Paladino" e "Stregone" non esistono nel testo originale.
			string prompt1 = "il paladino";
			string prompt2 = "lo stregone";

			Console.WriteLine($"Prompt: '{prompt1}'");
			Console.WriteLine($"Generato: {model.Generate(prompt1, 4)}\n");

			Console.WriteLine($"Prompt: '{prompt2}'");
			Console.WriteLine($"Generato: {model.Generate(prompt2, 5)}");
		}


	}
}
