using SimpleLLM.Library.MicroLLM;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsole
{
	/// <summary>
	/// Esempio base di utilizzo del modello statistico SimpleLLM.
	/// Mostra l'intera pipeline: corpus -> train -> inferenza autoregressiva.
	/// </summary>
	public class MicroLLM
	{
		/// <summary>
		/// Step eseguiti:
		/// 1) Definizione corpus,
		/// 2) addestramento del modello bigram,
		/// 3) generazione testo da prompt.
		/// </summary>
		public static void Execute()
		{
			// 1. Il nostro "Corpus" di addestramento
			string corpus = "il gatto mangia il topo. il cane mangia la carne. il gatto dorme. il cane abbaia.";

			// Inizializziamo il nostro modello
			SimpleLLM.Library.MicroLLM.SimpleLLM model = new SimpleLLM.Library.MicroLLM.SimpleLLM();

			// 2 & 3. Tokenizzazione e Addestramento
			Console.WriteLine("Addestramento del modello in corso...");
			model.Train(corpus);
			Console.WriteLine("Addestramento completato!\n");

			// 4. Inferenza (Generazione del testo)
			string prompt = "il gatto";
			int paroleDaGenerare = 3;

			Console.WriteLine($"Prompt: '{prompt}'");
			string output = model.Generate(prompt, paroleDaGenerare);
			Console.WriteLine($"Testo Generato: {output}");
		}
	}
}
