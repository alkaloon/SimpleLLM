# Roadmap delle Migliorie: Sistema di Narrazione Local-LLM

## 1. Obiettivo Principale
Trasformare l'interazione dell'utente da un sistema rigido di "Match String" (keyword matching) a un sistema dinamico di **"Interpretazione Semantica"**, permettendo al giocatore di interagire in modo naturale con l'ambiente senza dover indovinare le parole esatte richieste dal motore.

## 2. Analisi del Problema Attuale
Attualmente, il sistema si affida alla lista `TriggerVerbs` nel file `story-definition.json`.
- **Limite:** Se un utente scrive "prendi la lanterna" e il trigger è "prendi_lanterna", il sistema potrebbe fallire se non trova una corrispondenza esatta o molto vicina.
- **Conseguenza:** L'immersione viene rotta perché il gioco non "capisce" l'intenzione dell'utente dietro un linguaggio naturale fluido.

## 3. Soluzioni Proposte e Migliorie Tecniche

### A. Integrazione del `LocalVectorEngine` (Priorità Alta)
Sostituire la logica di confronto delle stringhe in `StoryGameEngine.cs` con una ricerca vettoriale.
- **Azione:** Utilizzare i modelli ONNX già presenti per generare embedding dell'input dell'utente.
- **Logica:** Invece di `if (userInput == trigger)`, utilizzare `if (cosineSimilarity(userEmbedding, actionEmbedding) > threshold)`.
- **Vantaggio:** Permette al sistema di riconoscere che "prendi", "raccogli" e "afferra" hanno la stessa intenzione semantica.

### B. Pipeline di Elaborazione a Due Stadi (Priorità Alta)
Implementare una separazione tra "Intento del Giocatore" e "Generazione Narrativa".
1. **Fase 1 (Interpretazione):** Un modello leggero identifica l'azione (es: `ACTION_PICKUP`, `ITEM_LANTERN`).
2. **Fase 2 (Narrativa):** Il motore genera la risposta basata sul successo dell'azione interpretata, utilizzando il contesto del gioco per descrivere l'evento in modo fluido.

### C. Ottimizzazione della "Contextual Prompting" (Priorità Media)
Migliorare il modo in cui il sistema costruisce il prompt per il modello locale prima di generare la risposta finale.
- **Azione:** Utilizzare la `success_response` definita nel JSON non solo come testo da mostrare, ma come guida contestuale per il LLM.
- **Risultato:** La narrazione sarà meno ripetitiva e più coerente con gli eventi precedenti (es. se una luce si spegne, il modello "capirà" che l'atmosfera deve diventare cupa).

## 4. Roadmap di Implementazione

| Fase | Azione Tecnica | File Coinvolti | Obiettivo |
| :--- | :--- | :--- | :--- |
| **1** | Refactoring `StoryGameEngine.cs` | `StoryGameEngine.cs`, `LocalVectorEngine.cs` | Sostituire il match testuale con la distanza vettoriale per i Trigger. |
| **2** | Mapping degli Intent | `story-definition.json` | Definire cluster di sinonimi per ogni azione chiave. |
| **3** | Contextual Layer | `SimpleLLM.cs` | Migliorare la costruzione del prompt finale integrando lo stato attuale (inventario, flag). |

## 5. Vincoli Tecnici e Requisiti
- **Offline Total:** Tutte le operazioni devono restare locali (no API esterne).
- **Performance:** La ricerca vettoriale deve essere ottimizzata per girare su hardware consumer senza causare lag significativi tra un input e la risposta del gioco.