using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLLM.Library.NeuralNetworkFromScratch
{
	/// <summary>
	/// Rete neurale feed-forward minimale con un hidden layer e training via backpropagation.
	/// Pipeline per ogni campione:
	/// 1) Forward pass (input -> hidden -> output),
	/// 2) Calcolo errore (MSE),
	/// 3) Backpropagation e aggiornamento pesi/bias.
	/// </summary>
	public class SimpleNeuralNetwork
	{
		private int _inputNodes, _hiddenNodes, _outputNodes;
		private double[,] _weightsIH; // Pesi tra Input e Hidden layer
		private double[,] _weightsHO; // Pesi tra Hidden e Output layer
		private double[] _biasH;      // Bias per i neuroni Hidden
		private double[] _biasO;      // Bias per i neuroni Output

		private Random _rnd = new Random();

		/// <summary>
		/// Inizializza topologia, pesi e bias con valori casuali in [-1, 1]
		/// per rompere la simmetria iniziale dei neuroni.
		/// </summary>
		public SimpleNeuralNetwork(int inputNodes, int hiddenNodes, int outputNodes)
		{
			_inputNodes = inputNodes;
			_hiddenNodes = hiddenNodes;
			_outputNodes = outputNodes;

			_weightsIH = new double[_hiddenNodes, _inputNodes];
			_weightsHO = new double[_outputNodes, _hiddenNodes];
			_biasH = new double[_hiddenNodes];
			_biasO = new double[_outputNodes];

			// Inizializzazione casuale dei pesi (fondamentale per rompere la simmetria)
			InitializeRandom(_weightsIH);
			InitializeRandom(_weightsHO);
			InitializeRandom(_biasH);
			InitializeRandom(_biasO);
		}

		/// <summary>
		/// Esegue solo inferenza: propaga l'input nei layer e restituisce l'output predetto.
		/// </summary>
		public double[] Predict(double[] inputs)
		{
			double[] hiddenOutputs = CalculateLayer(inputs, _weightsIH, _biasH, _hiddenNodes, _inputNodes);
			double[] finalOutputs = CalculateLayer(hiddenOutputs, _weightsHO, _biasO, _outputNodes, _hiddenNodes);
			return finalOutputs;
		}

		/// <summary>
		/// Esegue un ciclo completo di training su un singolo esempio.
		/// Restituisce la loss quadratica per monitorare la convergenza.
		/// </summary>
		public double Train(double[] inputs, double[] targets, double learningRate)
		{
			// --- FORWARD PASS ---
			double[] hiddenOutputs = CalculateLayer(inputs, _weightsIH, _biasH, _hiddenNodes, _inputNodes);
			double[] finalOutputs = CalculateLayer(hiddenOutputs, _weightsHO, _biasO, _outputNodes, _hiddenNodes);

			// Calcolo Errore (Mean Squared Error semplice)
			double loss = 0;
			double[] outputErrors = new double[_outputNodes];
			for (int i = 0; i < _outputNodes; i++)
			{
				double error = targets[i] - finalOutputs[i];
				outputErrors[i] = error;
				loss += Math.Pow(error, 2); // Somma dei quadrati degli errori
			}

			// --- BACKPROPAGATION ---

			// 1. Calcolo gradienti per lo strato di Output
			double[] outputGradients = new double[_outputNodes];
			for (int i = 0; i < _outputNodes; i++)
			{
				// La derivata della Sigmoide è: output * (1 - output)
				double derivative = finalOutputs[i] * (1 - finalOutputs[i]);
				outputGradients[i] = outputErrors[i] * derivative * learningRate;
			}

			// Aggiornamento Pesi e Bias (Hidden -> Output)
			for (int i = 0; i < _outputNodes; i++)
			{
				for (int j = 0; j < _hiddenNodes; j++)
				{
					_weightsHO[i, j] += outputGradients[i] * hiddenOutputs[j];
				}
				_biasO[i] += outputGradients[i];
			}

			// 2. Calcolo errori per lo strato Nascosto (quanto errore ha passato l'hidden all'output?)
			double[] hiddenErrors = new double[_hiddenNodes];
			for (int i = 0; i < _hiddenNodes; i++)
			{
				double error = 0;
				for (int j = 0; j < _outputNodes; j++)
				{
					error += outputErrors[j] * _weightsHO[j, i];
				}
				hiddenErrors[i] = error;
			}

			// 3. Calcolo gradienti per lo strato Nascosto
			double[] hiddenGradients = new double[_hiddenNodes];
			for (int i = 0; i < _hiddenNodes; i++)
			{
				double derivative = hiddenOutputs[i] * (1 - hiddenOutputs[i]);
				hiddenGradients[i] = hiddenErrors[i] * derivative * learningRate;
			}

			// Aggiornamento Pesi e Bias (Input -> Hidden)
			for (int i = 0; i < _hiddenNodes; i++)
			{
				for (int j = 0; j < _inputNodes; j++)
				{
					_weightsIH[i, j] += hiddenGradients[i] * inputs[j];
				}
				_biasH[i] += hiddenGradients[i];
			}

			return loss;
		}

		/// <summary>
		/// Calcola l'output di un layer denso: somma pesata + bias + attivazione sigmoide.
		/// </summary>
		private double[] CalculateLayer(double[] inputs, double[,] weights, double[] biases, int currentNodes, int previousNodes)
		{
			double[] outputs = new double[currentNodes];
			for (int i = 0; i < currentNodes; i++)
			{
				double sum = biases[i];
				for (int j = 0; j < previousNodes; j++)
				{
					sum += inputs[j] * weights[i, j];
				}
				outputs[i] = Sigmoid(sum); // Funzione di Attivazione
			}
			return outputs;
		}

		/// <summary>
		/// Attivazione sigmoide: comprime i valori nell'intervallo (0,1).
		/// </summary>
		private double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));

		/// <summary>
		/// Inizializza una matrice con valori casuali in [-1, 1].
		/// </summary>
		private void InitializeRandom(double[,] matrix)
		{
			for (int i = 0; i < matrix.GetLength(0); i++)
				for (int j = 0; j < matrix.GetLength(1); j++)
					matrix[i, j] = (_rnd.NextDouble() * 2) - 1; // Tra -1 e 1
		}

		/// <summary>
		/// Inizializza un vettore con valori casuali in [-1, 1].
		/// </summary>
		private void InitializeRandom(double[] array)
		{
			for (int i = 0; i < array.Length; i++) array[i] = (_rnd.NextDouble() * 2) - 1;
		}
	}
}