# Guida Completa: Costruire un LLM in C# da Zero

Questa guida illustra il percorso teorico e pratico per comprendere il funzionamento interno dei Large Language Models (LLM). Attraverso implementazioni progressive in C#, esploreremo la transizione dai modelli statistici tradizionali fino alle reti neurali basate su embeddings semantici e meccanismi di Attention.

---

## Capitolo 1: Il Modello Statistico di Base (N-Grammi)

### La Pipeline Fondamentale
Qualsiasi modello linguistico segue una pipeline sequenziale:
1. **Raccolta Dati (Corpus):** Il testo grezzo da cui il modello apprende.
2. **Tokenizzazione:** Scomposizione del testo in unità minime (token) e assegnazione di un ID numerico.
3. **Addestramento (Training):** Analisi delle sequenze per apprenderne le interdipendenze.
4. **Inferenza:** Calcolo delle probabilità e generazione autoregressiva del token successivo.

Nei modelli statistici primitivi (Bigrammi / Catene di Markov), l'addestramento consiste semplicemente nel contare quante volte la parola B segue la parola A.

### Codice: SimpleLLM

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroLLM
{
    public class SimpleLLM
    {
        // Dizionario: Parola -> (Parola Successiva -> Frequenza)
        private Dictionary<string, Dictionary<string, int>> _brain = new();

        public void Train(string text)
        {
            text = text.Replace(".", " .");
            string[] tokens = text.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tokens.Length - 1; i++)
            {
                string current = tokens[i];
                string next = tokens[i + 1];

                if (!_brain.ContainsKey(current)) _brain[current] = new Dictionary<string, int>();
                if (!_brain[current].ContainsKey(next)) _brain[current][next] = 0;

                _brain[current][next]++; 
            }
        }

        public string Generate(string prompt, int length)
        {
            string[] promptTokens = prompt.ToLower().Split(' ');
            string currentToken = promptTokens.Last(); 
            List<string> generated = new(promptTokens);

            for (int i = 0; i < length; i++)
            {
                if (!_brain.ContainsKey(currentToken)) break;

                var nextToken = _brain[currentToken].OrderByDescending(x => x.Value).First().Key;
                generated.Add(nextToken);
                currentToken = nextToken;
            }
            return string.Join(" ", generated).Replace(" .", ".");
        }
    }
}
```

**Limiti:** Soffre di "rigidità lessicale" (se non conosce la parola, si blocca) e "amnesia del contesto" (guardando solo l'ultima parola, perde il senso globale della frase).

---

## Capitolo 2: Embeddings Semantici e Spazio Vettoriale

Per superare la rigidità esatta, introduciamo gli **Embeddings**. Un embedding mappa un token in un vettore di numeri a virgola mobile (`float[]`). Nello spazio vettoriale, la vicinanza geometrica corrisponde alla somiglianza concettuale.

### Cosine Similarity
Misura l'angolo tra due vettori. Più l'angolo è stretto, più i concetti sono simili (1.0 = identici, 0.0 = scorrelati). In .NET 8+, la `CosineSimilarity` viene accelerata via hardware (SIMD) tramite `System.Numerics.Tensors`.

### Codice: LocalEmbeddingService

Richiede i pacchetti NuGet: `SmartComponents.LocalEmbeddings` (Prerelease) e `System.Numerics.Tensors`.

```csharp
using System;
using System.Numerics.Tensors;
using SmartComponents.LocalEmbeddings;

namespace VectorEngine
{
    public class LocalEmbeddingService : IDisposable
    {
        private readonly LocalEmbedder _embedder;

        public LocalEmbeddingService()
        {
            _embedder = new LocalEmbedder(); // Carica il modello ONNX in memoria
        }

        public ReadOnlyMemory<float> GetEmbedding(string text)
        {
            return _embedder.Embed(text).Values;
        }

        public float CalculateSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
        {
            // Utilizzo di istruzioni CPU vettoriali (SIMD) ad alte prestazioni
            return TensorPrimitives.CosineSimilarity(vectorA, vectorB);
        }

        public void Dispose() => _embedder.Dispose();
    }
}
```

---

## Capitolo 3: Il Modello Semantico

Unendo N-Grammi ed Embeddings, il modello non cerca più la parola esatta, ma calcola l'embedding del prompt e cerca nel suo dizionario il concetto vettoriale più vicino imparato in addestramento.

### Codice: SemanticLLM

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using SmartComponents.LocalEmbeddings;

namespace EmbeddedMicroLLM
{
    public class SemanticLLM
    {
        private readonly LocalEmbedder _embedder;
        private readonly List<SemanticNode> _brain = new();

        public SemanticLLM(LocalEmbedder embedder) => _embedder = embedder;

        public void Train(string text)
        {
            text = text.Replace(".", " .");
            string[] tokens = text.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tokens.Length - 1; i++)
            {
                var embedding = _embedder.Embed(tokens[i]).Values;
                _brain.Add(new SemanticNode(tokens[i], embedding, tokens[i + 1]));
            }
        }

        public string Generate(string prompt, int length)
        {
            string currentToken = prompt.ToLower().Split(' ').Last(); 
            List<string> output = new(prompt.Split(' '));

            for (int i = 0; i < length; i++)
            {
                var currentEmbedding = _embedder.Embed(currentToken).Values;

                // Trova il nodo semantico matematicamente più vicino
                var bestMatch = _brain
                    .Select(node => new { 
                        Node = node, 
                        Similarity = TensorPrimitives.CosineSimilarity(currentEmbedding.Span, node.Embedding.Span) 
                    })
                    .OrderByDescending(x => x.Similarity)
                    .First();

                string nextToken = bestMatch.Node.NextToken;
                output.Add(nextToken);
                currentToken = nextToken;

                if (nextToken == ".") break;
            }
            return string.Join(" ", output).Replace(" .", ".");
        }
    }
    public record SemanticNode(string TokenOriginale, ReadOnlyMemory<float> Embedding, string NextToken);
}
```

---

## Capitolo 4: Reti Neurali e Backpropagation

Per imparare astrazioni matematiche complesse (non solo contare frequenze) servono le Reti Neurali. Il ciclo vitale è:
1. **Forward Pass:** Calcolo della previsione.
2. **Loss:** Calcolo dell'errore.
3. **Backpropagation:** Calcolo delle derivate parziali per aggiornare i pesi e ridurre l'errore per le previsioni future.

---

## Capitolo 5: Motore per Gioco Testuale Interattivo

Per gestire una storia in cui l utente impersona il protagonista, la libreria include un motore narrativo basato su scene e regole azione.

### Componenti

- `StoryDefinition`: titolo, protagonista, introduzione, scena iniziale.
- `StoryScene`: nodo della storia con descrizione e regole locali.
- `StoryActionRule`: associa trigger testuali a effetti narrativi.
- `StoryGameState`: mantiene scena corrente, inventario e flag di progressione.
- `StoryGameEngine`: interpreta input utente e produce risposte coerenti.

### Comprensione semantica dell input

Il motore supporta un parser ibrido:

1. Matching lessicale sui trigger (`TriggerVerbs`).
2. Se non trova match testuale, prova un fallback semantico confrontando embedding dell input utente e embedding dei trigger tramite cosine similarity.

Per attivarlo passa un `IEmbeddingService` al costruttore:

```csharp
using var embeddingService = new LocalEmbeddingService();
var engine = new StoryGameEngine(story, embeddingService, semanticThreshold: 0.58f);
```

`semanticThreshold` controlla la sensibilita del fallback semantico.

Puoi anche configurare la soglia direttamente nella definizione storia:

```json
{
    "Titolo": "La Torre nella Nebbia",
    "Protagonista": "Elia",
    "Introduzione": "...",
    "InitialSceneId": "porto",
    "SemanticThreshold": 0.58,
    "EnableSemanticScoreLogging": true,
    "Scenes": [ ... ]
}
```

API utili per caricare la storia da JSON:

```csharp
StoryDefinition def1 = StoryDefinition.FromFile("story-definition.json");
StoryDefinition def2 = StoryDefinition.FromJson(jsonText);
```

Se `EnableSemanticScoreLogging` e' `true`, il motore espone `LastSemanticLog` con informazioni di tuning (score, soglia, regola candidata).

### Esempio minimo di utilizzo

```csharp
using SimpleLLM.Library.TextAdventure;

var story = new StoryDefinition
{
    Titolo = "La Torre nella Nebbia",
    Protagonista = "Elia, guardiano del porto",
    Introduzione = "La nebbia cresce e il faro e' spento.",
    InitialSceneId = "porto",
    Scenes =
    [
        new StoryScene
        {
            Id = "porto",
            Titolo = "Porto",
            Descrizione = "Un molo vuoto, una lanterna a terra, un sentiero verso la torre.",
            Rules =
            [
                new StoryActionRule
                {
                    Id = "prendi-lanterna",
                    TriggerVerbs = ["prendi lanterna", "lanterna"],
                    SuccessResponse = "Raccogli la lanterna e senti il calore della fiamma.",
                    GrantedItems = ["lanterna"]
                }
            ]
        }
    ]
};

var engine = new StoryGameEngine(story);
Console.WriteLine(engine.Begin());

StoryTurnResult turn = engine.HandleInput("prendi lanterna");
Console.WriteLine(turn.Response);
```

### Comandi di supporto integrati

- `aiuto`
- `guarda`
- `inventario`
- `stato`

### Persistenza sessione

Il motore espone API native per salvataggio/caricamento:

```csharp
// Salvataggio su file JSON
engine.SaveToFile("savegame.json");

// Caricamento da file JSON
engine.LoadFromFile("savegame.json");

// Variante in memoria (string JSON)
string payload = engine.SaveToJson();
engine.LoadFromJson(payload);
```

Nel progetto console demo puoi usare direttamente:

- `salva` oppure `salva percorso-file.json`
- `carica` oppure `carica percorso-file.json`

### Codice: Multi-Layer Perceptron (Problema XOR)

```csharp
using System;

namespace NeuralNetworkFromScratch
{
    public class SimpleNeuralNetwork
    {
        private int _inputNodes, _hiddenNodes, _outputNodes;
        private double[,] _weightsIH, _weightsHO;
        private double[] _biasH, _biasO;
        private Random _rnd = new Random();

        public SimpleNeuralNetwork(int inputNodes, int hiddenNodes, int outputNodes)
        {
            _inputNodes = inputNodes; _hiddenNodes = hiddenNodes; _outputNodes = outputNodes;
            _weightsIH = new double[hiddenNodes, inputNodes];
            _weightsHO = new double[outputNodes, hiddenNodes];
            _biasH = new double[hiddenNodes];
            _biasO = new double[outputNodes];

            InitializeRandom(_weightsIH); InitializeRandom(_weightsHO);
            InitializeRandom(_biasH); InitializeRandom(_biasO);
        }

        public double[] Predict(double[] inputs)
        {
            double[] hiddenOutputs = CalculateLayer(inputs, _weightsIH, _biasH, _hiddenNodes, _inputNodes);
            return CalculateLayer(hiddenOutputs, _weightsHO, _biasO, _outputNodes, _hiddenNodes);
        }

        public double Train(double[] inputs, double[] targets, double learningRate)
        {
            // 1. Forward Pass
            double[] hiddenOutputs = CalculateLayer(inputs, _weightsIH, _biasH, _hiddenNodes, _inputNodes);
            double[] finalOutputs = CalculateLayer(hiddenOutputs, _weightsHO, _biasO, _outputNodes, _hiddenNodes);

            // 2. Calcolo Loss (MSE)
            double loss = 0;
            double[] outputErrors = new double[_outputNodes];
            for (int i = 0; i < _outputNodes; i++)
            {
                outputErrors[i] = targets[i] - finalOutputs[i];
                loss += Math.Pow(outputErrors[i], 2);
            }

            // 3. Backpropagation Output -> Hidden
            double[] outputGradients = new double[_outputNodes];
            for (int i = 0; i < _outputNodes; i++)
            {
                outputGradients[i] = outputErrors[i] * (finalOutputs[i] * (1 - finalOutputs[i])) * learningRate;
                for (int j = 0; j < _hiddenNodes; j++) _weightsHO[i, j] += outputGradients[i] * hiddenOutputs[j];
                _biasO[i] += outputGradients[i];
            }

            // Backpropagation Hidden -> Input
            double[] hiddenErrors = new double[_hiddenNodes];
            for (int i = 0; i < _hiddenNodes; i++)
                for (int j = 0; j < _outputNodes; j++) hiddenErrors[i] += outputErrors[j] * _weightsHO[j, i];

            double[] hiddenGradients = new double[_hiddenNodes];
            for (int i = 0; i < _hiddenNodes; i++)
            {
                hiddenGradients[i] = hiddenErrors[i] * (hiddenOutputs[i] * (1 - hiddenOutputs[i])) * learningRate;
                for (int j = 0; j < _inputNodes; j++) _weightsIH[i, j] += hiddenGradients[i] * inputs[j];
                _biasH[i] += hiddenGradients[i];
            }

            return loss;
        }

        private double[] CalculateLayer(double[] inputs, double[,] weights, double[] biases, int current, int previous)
        {
            double[] outputs = new double[current];
            for (int i = 0; i < current; i++)
            {
                double sum = biases[i];
                for (int j = 0; j < previous; j++) sum += inputs[j] * weights[i, j];
                outputs[i] = 1.0 / (1.0 + Math.Exp(-sum)); // Attivazione Sigmoide
            }
            return outputs;
        }

        private void InitializeRandom(double[,] m) { for (int i=0; i<m.GetLength(0); i++) for (int j=0; j<m.GetLength(1); j++) m[i,j] = _rnd.NextDouble()*2-1; }
        private void InitializeRandom(double[] a) { for (int i=0; i<a.Length; i++) a[i] = _rnd.NextDouble()*2-1; }
    }
}
```

---

## Capitolo 5: Il Modello Linguistico Neurale (Neural Semantic LLM)

Sostituiamo la ricerca lineare con la nostra Rete Neurale.
1. La parola diventa un Vettore Embedding.
2. La Rete Neurale calcola e restituisce un *nuovo vettore astratto* (la sua previsione spaziale del futuro).
3. Usiamo la Similarità del Coseno tra il vettore astratto generato e il Vocabolario per trovare la parola reale da stampare.

### Codice: NeuralSemanticLLM

```csharp
using System;
using System.Collections.Generic;

namespace NeuralSemanticLLM
{
    class Program
    {
        static void Main()
        {
            // Vocabolario mock a 3 dimensioni (X=Forza, Y=Magia, Z=Punteggiatura)
            var vocab = new Dictionary<string, double[]>
            {
                { "il",         new double[] { 0.1, 0.1, 0.1 } }, 
                { "cavaliere",  new double[] { 0.9, 0.1, 0.1 } }, 
                { "spada",      new double[] { 0.8, 0.2, 0.1 } }, 
                { "mago",       new double[] { 0.1, 0.9, 0.1 } }, 
                { "incantesimo",new double[] { 0.2, 0.8, 0.1 } },
                { ".",          new double[] { 0.0, 0.0, 0.9 } }  
            };

            var trainingInputs = new List<double[]> { vocab["il"], vocab["cavaliere"], vocab["spada"], vocab["il"], vocab["mago"], vocab["incantesimo"] };
            var trainingTargets = new List<double[]> { vocab["cavaliere"], vocab["spada"], vocab["."], vocab["mago"], vocab["incantesimo"], vocab["."] };

            var nn = new SimpleNeuralNetwork(3, 8, 3);
            double learningRate = 0.5;
            
            for (int epoch = 0; epoch < 5000; epoch++)
            {
                for (int i = 0; i < trainingInputs.Count; i++)
                    nn.Train(trainingInputs[i], trainingTargets[i], learningRate);
            }

            GenerateText(nn, vocab, "mago", 2);
        }

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

        static string GetClosestWord(double[] targetVector, Dictionary<string, double[]> vocab)
        {
            string bestWord = "";
            double bestScore = -2.0;
            foreach (var kvp in vocab)
            {
                double score = CosineSimilarity(targetVector, kvp.Value);
                if (score > bestScore) { bestScore = score; bestWord = kvp.Key; }
            }
            return bestWord;
        }

        static double CosineSimilarity(double[] a, double[] b)
        {
            double dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i]; magA += a[i] * a[i]; magB += b[i] * b[i];
            }
            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }
    }
}
```

---

## Capitolo 6: L'Architettura Transformer e la Self-Attention

Il limite dei modelli puramente sequenziali è gestire le dipendenze a lungo termine nella frase. L'architettura **Transformer** processa l'intera finestra di contesto contemporaneamente tramite la **Self-Attention**.
Calcola la *Cosine Similarity* tra la *Query* (Q) di una parola e le *Key* (K) delle altre. Il punteggio (Attention Score) viene usato per pesare i *Value* (V), creando un nuovo vettore che contiene il significato della parola miscelato e influenzato da tutto il resto della frase.

### Codice: Implementazione della Self-Attention in C#

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace TransformerAttention
{
    class Program
    {
        static void Main()
        {
            var vocab = new Dictionary<string, double[]>
            {
                { "il",         new double[] { 0.1, 0.1, 0.8 } },
                { "guerriero",  new double[] { 0.9, 0.1, 0.1 } },
                { "saluta",     new double[] { 0.2, 0.2, 0.2 } },
                { "mago",       new double[] { 0.1, 0.9, 0.1 } }
            };

            string frase = "il guerriero saluta il mago";
            string[] parole = frase.Split(' ');
            List<double[]> inputSequence = parole.Select(p => vocab[p]).ToList();

            // Calcolo della matrice dei punteggi di attenzione (Q * K^T)
            double[,] attentionScores = CalculateAttentionScores(inputSequence);
            
            // Normalizzazione tramite funzione Softmax
            double[,] attentionWeights = ApplySoftmax(attentionScores);

            PrintAttentionMatrix(parole, attentionWeights);
        }

        static double[,] CalculateAttentionScores(List<double[]> sequence)
        {
            int seqLen = sequence.Count;
            double[,] scores = new double[seqLen, seqLen];

            for (int i = 0; i < seqLen; i++)
                for (int j = 0; j < seqLen; j++)
                    scores[i, j] = DotProduct(sequence[i], sequence[j]);
                    
            return scores;
        }

        static double[,] ApplySoftmax(double[,] scores)
        {
            int size = scores.GetLength(0);
            double[,] weights = new double[size, size];

            for (int i = 0; i < size; i++)
            {
                double sumExp = 0;
                for (int j = 0; j < size; j++) sumExp += Math.Exp(scores[i, j]);
                for (int j = 0; j < size; j++) weights[i, j] = Math.Exp(scores[i, j]) / sumExp;
            }
            return weights;
        }

        static double DotProduct(double[] a, double[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++) sum += a[i] * b[i];
            return sum;
        }

        static void PrintAttentionMatrix(string[] parole, double[,] matrix)
        {
            int len = parole.Length;
            Console.Write("          ");
            foreach (var p in parole) Console.Write($"{p,-10}");
            Console.WriteLine("\n" + new string('-', 60));

            for (int i = 0; i < len; i++)
            {
                Console.Write($"{parole[i],-10}|");
                for (int j = 0; j < len; j++) Console.Write($"{(matrix[i, j] * 100):F1}%      ");
                Console.WriteLine();
            }
        }
    }
}
```