# Guida Dettagliata agli Esempi di SimpleLLM

Questa guida descrive in dettaglio tutti gli esempi presenti nella soluzione e spiega il ruolo di ogni classe coinvolta.

## Panoramica della soluzione

La soluzione contiene tre progetti principali:

- `SimpleLLM.Library`: libreria con i componenti riutilizzabili (modelli, rete neurale, motore embedding).
- `TestConsole`: raccolta di esempi eseguibili per capire i vari approcci passo-passo.
- `InteractiveStories`: progetto dedicato alle demo di avventure testuali interattive.

Per avviare direttamente la demo interattiva dal nuovo progetto:

- `dotnet run --project InteractiveStories/InteractiveStories.csproj`

## Come eseguire un esempio

Il punto di ingresso e `TestConsole/Program.cs`.

Attualmente viene eseguito:

- `DocumentTest.Execute()`

Per provare un altro esempio, sostituisci la chiamata in `Main` con una delle seguenti:

- `MicroLLM.Execute()`
- `LocalEmbedderSemanticLLM.Execute()`
- `NeuralNetwork.Execute()`
- `NeuralSemanticLLM.Execute()`
- `TransformerAttention.Execute()`
- `DocumentTest.Execute()`
- `InteractiveStoryDemo.Execute()`

## Esempio 1: MicroLLM (modello statistico base)

File coinvolti:

- `TestConsole/MicroLLM.cs`
- `SimpleLLM.Library/SimpleLLM.cs`

Obiettivo:

- Costruire un modello testuale minimale basato su frequenze di coppie di parole (bigrammi).

Step interni:

1. Definisce un corpus di addestramento.
2. Addestra `SimpleLLM` con `Train(corpus)`.
3. La classe `SimpleLLM` tokenizza il testo e salva le transizioni `parola_corrente -> parola_successiva` con frequenza.
4. In inferenza (`Generate`) parte dall ultimo token del prompt e sceglie ogni volta il token successivo piu frequente.

Cosa mostra:

- Come funziona una catena di Markov di ordine 1.
- Limite principale: dipende da parole viste e da contesto molto corto.

## Esempio 2: LocalEmbedderSemanticLLM (semantica locale ONNX)

File coinvolti:

- `TestConsole/LocalEmbedderSemanticLLM.cs`
- `SimpleLLM.Library/SemanticLLM.cs`

Obiettivo:

- Passare da matching lessicale a matching semantico tramite embeddings.

Step interni:

1. Inizializza `LocalEmbedder` (motore ONNX locale).
2. Addestra `SemanticLLM` su un corpus fantasy.
3. Durante il training, ogni token viene convertito in embedding e memorizzato in un `SemanticNode` insieme al token successivo.
4. In generazione, il token corrente viene embedded e confrontato con tutti i nodi tramite cosine similarity.
5. Il nodo piu vicino determina il prossimo token.

Cosa mostra:

- Generalizzazione su parole non viste esplicitamente (es. "paladino", "stregone").
- Recupero semantico con costo lineare rispetto ai nodi in memoria.

## Esempio 3: NeuralNetwork (XOR da zero)

File coinvolti:

- `TestConsole/SimpleNeuralNetwork.cs`
- `SimpleLLM.Library/SimpleNeuralNetwork.cs`

Obiettivo:

- Dimostrare il training di una rete neurale feed-forward tramite backpropagation.

Step interni:

1. Definisce dataset XOR.
2. Istanzia rete con topologia `2 -> 3 -> 1`.
3. Esegue epoche di training:
   - Forward pass
   - Calcolo loss (MSE)
   - Backpropagation (output e hidden)
   - Aggiornamento pesi e bias
4. Valida le predizioni finali con `Predict`.

Cosa mostra:

- Meccanica base dell apprendimento supervisionato.
- Riduzione progressiva dell errore con iterazioni.

## Esempio 4: NeuralSemanticLLM (predizione vettoriale + nearest token)

File coinvolti:

- `TestConsole/NeuralSemanticLLM.cs`
- `SimpleLLM.Library/SimpleNeuralNetwork.cs`

Obiettivo:

- Unire embeddings e rete neurale per predire la parola successiva in spazio vettoriale.

Step interni:

1. Definisce un mini vocabolario con embedding mock 3D.
2. Crea training pairs `embedding_parola_corrente -> embedding_parola_successiva`.
3. Addestra una rete `3 -> 8 -> 3`.
4. In generazione:
   - predice il prossimo vettore con `Predict`
   - converte il vettore in token reale con ricerca del massimo coseno nel vocabolario.

Cosa mostra:

- Pipeline neurale-semantica end-to-end in forma didattica.
- Separazione tra spazio continuo (vettori) e output discreto (token).

## Esempio 5: TransformerAttention (self-attention semplificata)

File coinvolti:

- `TestConsole/TransformerAttention.cs`

Obiettivo:

- Visualizzare come nasce una matrice di attenzione.

Step interni:

1. Definisce embeddings per una frase.
2. Costruisce matrice score con prodotto scalare su ogni coppia token-token.
3. Applica softmax riga per riga per ottenere pesi normalizzati.
4. Stampa la matrice finale in percentuale.

Cosa mostra:

- Come una parola distribuisce la propria attenzione sulle altre parole della frase.
- Intuizione su Q, K e softmax prima di un Transformer completo.

## Esempio 6: DocumentTest (semantic retrieval su FAQ)

File coinvolti:

- `TestConsole/DocumentTest.cs`

Obiettivo:

- Realizzare una mini assistenza basata su similarita semantica documento-query.

Step interni:

1. Carica `LocalEmbedder`.
2. Definisce knowledge base (`Document`) con domanda e risposta.
3. Indicizza le domande convertendole in embedding.
4. In loop:
   - legge input utente
   - calcola embedding query
   - confronta con tutti i documenti via cosine similarity
   - seleziona il best match
5. Applica soglia di confidenza (0.55):
   - sopra soglia: risponde con la FAQ migliore
   - sotto soglia: chiede una riformulazione.

Cosa mostra:

- Architettura retrieval-first molto utile per help desk e knowledge base locali.
- Uso pratico di embedding + ranking senza generazione libera.

## Esempio 7: InteractiveStoryDemo (gioco testuale a trama guidata)

File coinvolti:

- `InteractiveStories/InteractiveStoryDemo.cs`
- `SimpleLLM.Library/StoryGameEngine.cs`

Obiettivo:

- Creare una mini avventura testuale in cui l utente impersona il protagonista e compie azioni libere in linguaggio naturale.

Step interni:

1. Definisce una `StoryDefinition` con titolo, protagonista, introduzione e scena iniziale.
2. Costruisce le scene (`StoryScene`) con descrizioni e regole locali.
3. Definisce regole azione (`StoryActionRule`) con:
   - trigger testuali (verbi/frasi riconosciute),
   - prerequisiti (flag o oggetti necessari),
   - effetti narrativi (spostamento scena, oggetti, avanzamento trama).
4. Avvia `StoryGameEngine` che mantiene lo stato runtime (`StoryGameState`) e usa un parser ibrido:
   - match lessicale sui trigger,
   - fallback semantico tramite embeddings + cosine similarity.
5. In loop legge le azioni utente e restituisce risposte coerenti con il contesto narrativo.
6. Supporta persistenza partita con i comandi:
   - `salva [percorso]` (default `savegame.json`)
   - `carica [percorso]` (default `savegame.json`)
7. Supporta configurazione da file JSON della storia (`story-definition.json`), inclusi:
   - `SemanticThreshold` per tarare la sensibilita del match semantico,
   - `EnableSemanticScoreLogging` per stampare score e regola selezionata.

Nota implementativa demo:

- Il metodo `BuildStory()` non costruisce piu la storia tramite oggetti hardcoded.
- `BuildStory()` deserializza l'intera storia da JSON (`story-definition.json`).
- Se il file non e' presente, usa un fallback JSON embedded e lo deserializza con la stessa pipeline.

Formato salvataggio:

- JSON con scena corrente, inventario e flag trama.
- Validazione automatica: se il save appartiene a una storia diversa o contiene una scena non valida, il caricamento viene bloccato con errore.

Cosa mostra:

- Architettura event/rule-driven per giochi testuali deterministici.
- Gestione stato di trama, inventario e transizioni scena in modo riusabile.
- Possibilita di estendere facilmente la storia aggiungendo nuove regole e scene.
- Comprensione robusta dell input: frasi parafrasate possono attivare la stessa azione grazie al matching semantico.
- Tuning operativo: puoi regolare la soglia semantica dal JSON senza ricompilare il progetto.

## Componenti chiave della libreria

### `SimpleLLM.Library/SimpleLLM.cs`

- Modello bigram con dizionario annidato per frequenze.
- API principali:
  - `Train(string text)`
  - `Generate(string prompt, int length)`

### `SimpleLLM.Library/SemanticLLM.cs`

- Modello semantico con `LocalEmbedder`.
- Memoria interna basata su `List<SemanticNode>`.
- API principali:
  - `Train(string text)`
  - `Generate(string prompt, int length)`

### `SimpleLLM.Library/LocalVectorEngine.cs`

- `IEmbeddingService`: contratto per embedding + similarita.
- `LocalEmbeddingService`: implementazione locale ONNX con cosine similarity SIMD.

### `SimpleLLM.Library/SimpleNeuralNetwork.cs`

- Implementazione MLP da zero:
  - inizializzazione casuale
  - forward pass
  - backpropagation
  - aggiornamento pesi/bias

### `SimpleLLM.Library/StoryGameEngine.cs`

- Motore narrativo riusabile per interactive fiction.
- Modelli principali:
   - `StoryDefinition`: metadati storia + scene + regole globali.
   - `StoryScene`: nodo narrativo con descrizione e azioni disponibili.
   - `StoryActionRule`: trigger, prerequisiti ed effetti.
   - `StoryGameState`: scena corrente, inventario, flag trama.
   - `StoryGameEngine`: parser input e applicazione transizioni.

API principali:

- `Begin()` per inizializzare la sessione e mostrare contesto iniziale.
- `HandleInput(string playerInput)` per elaborare l azione utente.
- `SaveToFile(string filePath)` e `LoadFromFile(string filePath)` per persistenza su disco.
- `SaveToJson()` e `LoadFromJson(string json)` per integrazione con API/web/storage custom.

## Lettura consigliata degli esempi (ordine didattico)

1. `MicroLLM`
2. `LocalEmbedderSemanticLLM`
3. `NeuralNetwork`
4. `NeuralSemanticLLM`
5. `TransformerAttention`
6. `DocumentTest`
7. `InteractiveStoryDemo`

Con questo ordine vedi l evoluzione: statistica -> semantica -> neurale -> attention -> retrieval applicativo.

## Output attesi (esempio per esempio)

Nota generale:

- Alcuni output possono variare leggermente in base al corpus, all inizializzazione casuale della rete neurale e alla versione del runtime.
- Gli esempi qui sotto rappresentano il comportamento atteso e una forma tipica dell output a console.

### 1) MicroLLM

Output tipico:

```text
Addestramento del modello in corso...
Addestramento completato!

Prompt: 'il gatto'
Testo Generato: il gatto mangia il topo.
```

Perche questo output:

- Il modello sceglie il token successivo piu frequente nel corpus.
- Con il prompt "il gatto" tende a seguire la catena vista in training (es. "mangia il topo.").

### 2) LocalEmbedderSemanticLLM

Output tipico:

```text
Addestramento semantico in corso (calcolo dei vettori)...
Addestramento completato!

Prompt: 'il paladino'
Generato: il paladino impugna la spada.

Prompt: 'lo stregone'
Generato: lo stregone lancia la palla di fuoco.
```

Perche questo output:

- "paladino" tende a essere vicino semanticamente a "guerriero".
- "stregone" tende a essere vicino semanticamente a "mago".
- Il modello usa similarita del coseno per agganciare il ramo semantico piu affine.

### 3) NeuralNetwork (XOR)

Output tipico:

```text
Inizio addestramento (Backpropagation)...

Epoca 0 | Errore Totale (Loss): 1.1xxx
Epoca 2000 | Errore Totale (Loss): 0.2xxx
Epoca 4000 | Errore Totale (Loss): 0.0xxx
Epoca 6000 | Errore Totale (Loss): 0.0xxx
Epoca 8000 | Errore Totale (Loss): 0.0xxx

Addestramento completato! Testiamo la rete:
Input: 0,0 -> Previsione: 0.0xxx (Atteso: 0)
Input: 0,1 -> Previsione: 0.9xxx (Atteso: 1)
Input: 1,0 -> Previsione: 0.9xxx (Atteso: 1)
Input: 1,1 -> Previsione: 0.0xxx (Atteso: 0)
```

Perche questo output:

- La loss scende progressivamente durante il training.
- Le previsioni convergono verso i target XOR (vicino a 0 o vicino a 1).

### 4) NeuralSemanticLLM

Output tipico:

```text
mago incantesimo .
```

Perche questo output:

- Dal prompt "mago", la rete predice un vettore vicino a "incantesimo".
- Al passo successivo predice un vettore vicino a "." e la generazione si ferma.

### 5) TransformerAttention

Output tipico (forma):

```text
Analisi della Self-Attention per la frase: 'il guerriero saluta il mago'

          il        guerriero saluta    il        mago
------------------------------------------------------------
il        | ...%      ...%      ...%      ...%      ...%
guerriero | ...%      ...%      ...%      ...%      ...%
saluta    | ...%      ...%      ...%      ...%      ...%
il        | ...%      ...%      ...%      ...%      ...%
mago      | ...%      ...%      ...%      ...%      ...%

Osservazione: Guarda i valori alti! ...
```

Perche questo output:

- Viene stampata una matrice NxN (N = numero token nella frase).
- Ogni riga contiene pesi normalizzati (softmax) che sommano circa a 100%.

### 6) DocumentTest

Output tipico all avvio:

```text
Avvio del Motore Neurale (Caricamento modello ONNX in memoria)...
Indicizzazione vettoriale dei documenti in corso...

Sistema pronto! Inserisci il tuo problema in linguaggio naturale.
(Scrivi 'esci' per chiudere l'applicazione)
```

Caso A (confidenza alta), esempio input utente:

```text
Utente> non ricordo la password
[Confidenza: 78.3 %] Rilevata affinità con: 'Password dimenticata o smarrita'
Assistente> Per ripristinare le credenziali, vai nella pagina di login e clicca su 'Recupera Password'. Riceverai un link via email.
```

Caso B (confidenza bassa), esempio input utente:

```text
Utente> voglio cambiare colore al tema del portale
[Confidenza: 41.2 %] Valore troppo basso.
Assistente> Non sono sicuro di aver compreso. Puoi descrivere il problema con altre parole?
```

Perche questo output:

- La risposta dipende dal best match semantico tra query e FAQ indicizzate.
- Se il punteggio non supera la soglia (0.55), il sistema evita risposte potenzialmente errate.


