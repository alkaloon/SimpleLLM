using SmartComponents.LocalEmbeddings;
using System;
using System.Collections.Generic;
using System.Numerics.Tensors;
using System.Text;

namespace TestConsole
{
	/// <summary>
	/// Esempio di semantic search su documenti (mini RAG senza generazione).
	/// Indicizza una knowledge base in embedding-space e recupera la risposta piu' affine.
	/// </summary>
	internal class DocumentTest
	{
		/// <summary>
		/// Step eseguiti:
		/// 1) carica embedder locale,
		/// 2) indicizza le FAQ in vettori,
		/// 3) legge input utente e calcola embedding query,
		/// 4) seleziona documento piu' simile,
		/// 5) applica soglia di confidenza e restituisce risposta.
		/// </summary>
		public static void Execute()
		{

			Console.WriteLine("Avvio del Motore Neurale (Caricamento modello ONNX in memoria)...");

			// Inizializziamo la Rete Neurale Locale
			using var embedder = new LocalEmbedder();

			// 1. Prepariamo la nostra Base di Conoscenza (es. FAQ di Supporto)
			var knowledgeBase = new List<Document>
			{
				new Document(
					"Password dimenticata o smarrita",
					"Per ripristinare le credenziali, vai nella pagina di login e clicca su 'Recupera Password'. Riceverai un link via email."
				),
				new Document(
					"Come scaricare le fatture aziendali",
					"Tutte le tue fatture sono archiviate nella sezione 'Amministrazione' del portale, alla voce 'Storico Pagamenti'."
				),
				new Document(
					"Il sistema restituisce errore di connessione",
					"Verifica che i parametri proxy siano configurati correttamente e che la porta 443 non sia bloccata dal firewall aziendale."
				)
			};

			Console.WriteLine("Indicizzazione vettoriale dei documenti in corso...");

			// Facciamo leggere i documenti alla rete neurale per calcolarne il posizionamento spaziale
			foreach (var doc in knowledgeBase)
			{
				doc.Embedding = embedder.Embed(doc.Domanda).Values;
			}

			Console.WriteLine("\nSistema pronto! Inserisci il tuo problema in linguaggio naturale.");
			Console.WriteLine("(Scrivi 'esci' per chiudere l'applicazione)\n");

			// 2. Loop Interattivo: Elaborazione Input Utente
			while (true)
			{
				Console.Write("Utente> ");
				string? inputStr = Console.ReadLine();

				if (string.IsNullOrWhiteSpace(inputStr)) continue;
				if (inputStr.ToLower() == "esci") break;

				// 3. Interroghiamo la Rete Neurale con il testo in input
				// La rete "legge" il prompt e genera il vettore semantico a 384 dimensioni
				var inputVector = embedder.Embed(inputStr).Values;

				// 4. Calcoliamo le distanze matematiche per trovare la risposta
				var bestMatch = knowledgeBase
					.Select(doc => new
					{
						Doc = doc,
						// Sfruttiamo l'accelerazione hardware della CPU
						Score = TensorPrimitives.CosineSimilarity(inputVector.Span, doc.Embedding.Span)
					})
					.OrderByDescending(x => x.Score)
					.First();

				// 5. Output con soglia di confidenza
				// Se lo score è troppo basso, la rete sa di non avere la risposta
				if (bestMatch.Score > 0.55f)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"[Confidenza: {bestMatch.Score:P1}] Rilevata affinità con: '{bestMatch.Doc.Domanda}'");
					Console.ResetColor();
					Console.WriteLine($"Assistente> {bestMatch.Doc.Risposta}\n");
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine($"[Confidenza: {bestMatch.Score:P1}] Valore troppo basso.");
					Console.ResetColor();
					Console.WriteLine("Assistente> Non sono sicuro di aver compreso. Puoi descrivere il problema con altre parole?\n");
				}
			}
		}
	}

	/// <summary>
	/// Entita' documentale della base di conoscenza:
	/// domanda, risposta e embedding della domanda per la ricerca semantica.
	/// </summary>
	class Document
	{
		public string Domanda { get; }
		public string Risposta { get; }
		public ReadOnlyMemory<float> Embedding { get; set; }

		public Document(string domanda, string risposta)
		{
			Domanda = domanda;
			Risposta = risposta;
		}
	}
}
