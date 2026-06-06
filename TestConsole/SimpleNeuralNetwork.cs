using SimpleLLM.Library.NeuralNetworkFromScratch;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsole
{
	/// <summary>
	/// Esempio didattico di training rete neurale sul problema XOR.
	/// Serve a mostrare in pratica forward pass, loss e backpropagation.
	/// </summary>
	internal class NeuralNetwork
	{
		/// <summary>
		/// Step eseguiti:
		/// 1) definisce dataset XOR,
		/// 2) configura la rete,
		/// 3) esegue training per epoche,
		/// 4) valida le predizioni finali.
		/// </summary>
		public static void Execute()
		{
			// Il dataset per il problema XOR
			double[][] inputs = {
				new double[] { 0, 0 },
				new double[] { 0, 1 },
				new double[] { 1, 0 },
				new double[] { 1, 1 }
			};

			double[][] expectedOutputs = {
				new double[] { 0 },
				new double[] { 1 },
				new double[] { 1 },
				new double[] { 0 }
			};

			// Inizializziamo una rete con 2 input, 3 neuroni nel layer nascosto, e 1 output
			SimpleNeuralNetwork nn = new SimpleNeuralNetwork(2, 3, 1);

			double learningRate = 0.5;
			int epochs = 10000;

			Console.WriteLine("Inizio addestramento (Backpropagation)...\n");

			for (int epoch = 0; epoch < epochs; epoch++)
			{
				double totalLoss = 0;

				for (int i = 0; i < inputs.Length; i++)
				{
					// 1. Forward Pass e 2. Backpropagation
					double loss = nn.Train(inputs[i], expectedOutputs[i], learningRate);
					totalLoss += loss;
				}

				if (epoch % 2000 == 0)
				{
					Console.WriteLine($"Epoca {epoch} | Errore Totale (Loss): {totalLoss:F4}");
				}
			}

			Console.WriteLine("\nAddestramento completato! Testiamo la rete:");

			foreach (var input in inputs)
			{
				double[] prediction = nn.Predict(input);
				Console.WriteLine($"Input: {input[0]},{input[1]} -> Previsione: {prediction[0]:F4} (Atteso: {(int)input[0] ^ (int)input[1]})");
			}
		}
	}
}
