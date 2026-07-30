# Qi Burning e Heart Demons — Specifica di implementazione

Stato: implementato nella versione 0.1.8.

Questa specifica descrive due sistemi collegati alla coltivazione:

1. **Qi Burning**, una tecnica d'emergenza che concede grande potenza consumando in modo persistente una parte del Qi massimo.
2. **Heart Demons**, una conseguenza progressiva di breakthrough falliti e morti ripetute, superabile tramite una prova personale.

L'obiettivo non è aggiungere due normali buff, ma introdurre decisioni di preparazione, rischio e recupero. Entrambi i sistemi devono integrarsi con salvataggi, multiplayer, interfaccia, tecniche, alchimia e breakthrough già presenti.

---

## 1. Principi di design

### Qi Burning

- Deve essere usato quando un combattimento importante continua a fallire.
- Deve fornire un vantaggio abbastanza forte da poter cambiare l'esito dello scontro.
- Il costo deve continuare a esistere dopo la fine del combattimento.
- Non deve ridurre QiEXP, Realm o Stage.
- Il danno alla capacità spirituale deve poter essere riparato con preparazione, tempo e alchimia.
- Il costo stabilito è **2% del Qi massimo base ogni 3 secondi**.

### Heart Demons

- Devono rappresentare instabilità mentale e spirituale, non una perdita permanente del personaggio.
- Devono accumularsi a causa di ripetuti fallimenti reali.
- Devono rendere la coltivazione più difficile senza creare un blocco inevitabile.
- Non devono essere eliminabili semplicemente pagando un NPC.
- La purificazione deve essere una prova proporzionata al Realm del giocatore.

---

# Parte I — Qi Burning

## 2. Identità della tecnica

Nome inglese:

> Qi Burning

Possibili nomi localizzati:

- Italiano: **Combustione del Qi**
- Cinese semplificato: **燃烧真气**

Tipo:

- Tecnica attiva toggle.
- Sblocco consigliato: **Foundation Establishment** (`RealmIndex >= 2`).
- La tecnica deve essere aggiunta in fondo a `CultivationAbility`, prima di `Count`, per non modificare gli indici delle abilità già salvate.
- Tasto predefinito suggerito: `H`.
- Deve essere disponibile anche nella Ability Wheel.

## 3. Stato persistente

Il sistema non deve sottrarre direttamente QiEXP. Attualmente `MaxQi` corrisponde a `QiExp`; questa relazione deve essere separata in:

```csharp
public int BaseMaxQi => QiExp;
public int BurnedQiCapacityBps { get; private set; }
public int BurnedMaxQi =>
	(int)MathF.Ceiling(BaseMaxQi * BurnedQiCapacityBps / 10_000f);
public int MaxQi => Math.Max(0, BaseMaxQi - BurnedMaxQi);
```

`Bps` significa basis points:

- `100 Bps` = 1%.
- Ogni impulso di Qi Burning aggiunge `200 Bps`.
- Il limite predefinito di `5000 Bps` equivale al 50% del Qi massimo base.

Salvare una percentuale anziché un valore assoluto mantiene coerente la ferita quando:

- il personaggio aumenta QiEXP;
- cambia il moltiplicatore dei requisiti di coltivazione;
- avviene una migrazione della progressione;
- viene caricato un personaggio creato con una configurazione diversa.

Campi richiesti in `CultivationPlayer`:

```csharp
private const int QiBurnPulseInterval = 3 * 60;
private const int QiBurnPerPulseBps = 200;
private const int MaximumBurnedQiBps = 5000;

private bool qiBurningEnabled;
private int qiBurningPulseTimer;
private int burnedQiCapacityBps;
private int qiDeviationTimer;
private int qiBurningCombatExperienceTimer;

public bool QiBurningEnabled => qiBurningEnabled;
public int BurnedQiCapacityBps => burnedQiCapacityBps;
public float BurnedQiCapacityPercent => burnedQiCapacityBps / 100f;
public bool HasBurnedQi => burnedQiCapacityBps > 0;
public bool HasQiDeviation => qiDeviationTimer > 0;
```

## 4. Attivazione e disattivazione

### Condizioni di attivazione

Qi Burning può essere attivato soltanto se:

- il giocatore è vivo;
- la tecnica è sbloccata;
- il giocatore non sta meditando;
- `BaseMaxQi > 0`;
- non è presente Qi Deviation;
- la capacità bruciata è inferiore al limite del 50%;
- non è aperta una schermata di conferma del breakthrough;
- non è già attivo un Heart Demon Trial appartenente al giocatore.

L'attivazione:

1. interrompe la meditazione;
2. imposta `qiBurningEnabled = true`;
3. azzera `qiBurningPulseTimer`;
4. riproduce suono, aura e messaggio;
5. sincronizza lo stato in multiplayer.

### Disattivazione

Qi Burning termina quando:

- il giocatore preme nuovamente il tasto;
- il giocatore muore;
- viene raggiunto il limite di capacità bruciata;
- il Realm effettivo non soddisfa più il requisito;
- il server invalida lo stato;
- il personaggio abbandona il mondo mentre la tecnica è attiva.

La disattivazione volontaria o automatica applica Qi Deviation. La capacità già bruciata rimane.

## 5. Consumo ogni tre secondi

Mentre la tecnica è attiva:

```text
ogni 180 tick:
    capacità bruciata += 200 Bps
    Qi corrente = min(Qi corrente, nuovo MaxQi effettivo)
```

Esempi:

| Tempo attivo | Capacità bruciata |
|---:|---:|
| 3 secondi | 2% |
| 15 secondi | 10% |
| 30 secondi | 20% |
| 60 secondi | 40% |
| 75 secondi | 50%, limite massimo |

Quando il prossimo impulso raggiunge il limite:

1. applicare l'ultimo 2%;
2. disattivare immediatamente Qi Burning;
3. applicare Qi Deviation;
4. mostrare che la capacità spirituale ha raggiunto il limite di sicurezza.

Il costo del 2% ogni 3 secondi è fisso e non viene ridotto aumentando il livello della tecnica.

## 6. Bonus durante Qi Burning

Valori iniziali consigliati:

- `+30%` danni generici;
- `+10%` velocità d'attacco;
- `+8` probabilità critica;
- `+15%` velocità di movimento;
- `+8%` riduzione del danno;
- immunità al knockback leggero;
- effetti visivi più intensi sulle tecniche di coltivazione.

Progressione dal livello 1 al livello 20:

| Statistica | Livello 1 | Livello 20 |
|---|---:|---:|
| Danni generici | +30% | +45% |
| Velocità d'attacco | +10% | +20% |
| Critico | +8 | +12 |
| Movimento | +15% | +22% |
| Riduzione danno | +8% | +12% |

Il livello non modifica il costo di 2% ogni 3 secondi. Migliora soltanto il rendimento della capacità sacrificata.

Applicare i bonus in `ResetEffects` o in un metodo dedicato richiamato da `ResetEffects`, assicurandosi che:

- i bonus siano applicati una sola volta per tick;
- il danno generico influenzi anche le tecniche che usano `GetTotalDamage`;
- `Player.endurance` non superi i limiti sicuri;
- la tecnica non duplichi accidentalmente moltiplicatori elementali.

## 7. Esperienza della tecnica

Per evitare che il giocatore livelli Qi Burning bruciando Qi al sicuro:

- assegnare EXP a ogni impulso soltanto se è presente un boss attivo oppure se il giocatore ha inflitto o ricevuto danno negli ultimi 5 secondi;
- valore iniziale suggerito: `5 EXP` per impulso valido;
- applicare un limite massimo di EXP ottenibile per minuto;
- non concedere EXP quando il comando debug forza lo stato.

## 8. Qi Deviation

Nome inglese:

> Qi Deviation

Possibili localizzazioni:

- Italiano: **Deviazione del Qi**
- Cinese semplificato: **走火入魔**

Qi Deviation viene applicato quando Qi Burning termina.

Durata:

- livello 1: 180 secondi;
- livello 20: 90 secondi;
- interpolazione lineare fra i due valori.

Effetti consigliati:

- `-25%` danni generici;
- `-25%` difesa finale;
- `-25%` velocità di movimento;
- `-50%` recupero passivo del Qi;
- `-50%` efficienza della meditazione;
- impossibilità di riattivare Qi Burning.

Non ridurre la vita massima: una riduzione improvvisa potrebbe uccidere il giocatore dopo lo scontro.

Il timer deve essere persistente. Uscire e rientrare nel mondo non deve cancellare la penalità.

Se il giocatore abbandona il mondo mentre Qi Burning è attivo:

- disattivare la tecnica durante il salvataggio o al caricamento;
- applicare la durata completa di Qi Deviation;
- conservare tutta la capacità già bruciata.

## 9. Recupero della capacità bruciata

La capacità bruciata non viene recuperata da:

- normale rigenerazione del Qi;
- Qi Recovery Pill;
- Greater Qi Recovery Pill;
- cambio di Realm;
- morte;
- uscita dal mondo.

### Recupero tramite meditazione

Il recupero naturale deve essere lento:

- richiede meditazione in una Spiritual Qi Zone oppure dentro una Gathering Formation;
- ogni 30 secondi ininterrotti ripara `25 Bps`, cioè `0,25%`;
- una Gathering Formation può raddoppiare il recupero;
- muoversi o interrompere la meditazione azzera soltanto il timer parziale, non i progressi già riparati.

Con il recupero base, una combustione del 20% richiede circa 40 minuti. Questo rende utile preparare pillole prima di un boss.

### Nuova pillola: Meridian Mending Pill

Effetto:

- ripara `1000 Bps`, cioè il 10% del Qi massimo base;
- non ripristina direttamente il Qi corrente;
- non può essere consumata se non esiste capacità bruciata;
- l'efficacia alchemica può modificare la quantità riparata, senza superare il danno presente.

Requisito suggerito:

- Alchemy Tier Foundation, Middle Stage.

Ingredienti suggeriti:

- Bottled Water;
- 3 Ironroot;
- 2 Spirit Jade Bars;
- 3 Spirit Beast Blood;
- 1 Foundation Beast Core;
- 5 Spirit Stones;
- Alchemy Cauldron.

### Pillola avanzata opzionale

`Soul Meridian Restoration Pill`:

- ripara il 25%;
- richiede Core Formation Alchemy;
- usa materiali più rari;
- destinata a recuperare dopo un uso prolungato contro boss avanzati.

## 10. Correzioni necessarie alla gestione di MaxQi

Con una capacità effettiva inferiore a `QiExp`, ogni punto che usa `QiExp` come limite del Qi corrente deve essere controllato.

Aggiornamenti obbligatori:

- `RestoreQi`: limite `MaxQi`, non `QiExp`;
- `ProcessMeditationQiGain`: `missingQi = MaxQi - Qi`;
- `AddQi`: aumenta QiEXP normalmente, ma limita il Qi corrente al nuovo `MaxQi`;
- `LoadData`: limita il Qi caricato a `MaxQi` dopo aver caricato la capacità bruciata;
- `DebugSetQi`: usa `MaxQi`;
- tutti i fallimenti e rollback dei breakthrough devono limitare `Qi` a `MaxQi`;
- il recupero delle pillole deve confrontare `Qi < MaxQi`;
- la UI deve mostrare `Qi / MaxQi effettivo` e anche `BaseMaxQi`;
- le condizioni che aspettano una riserva piena devono usare `Qi >= MaxQi`.

Per evitare un breakthrough con meridiani danneggiati:

- un nuovo breakthrough di Realm non può iniziare finché `BurnedQiCapacityBps > 0`;
- mostrare un messaggio che richiede di riparare completamente la capacità;
- una Tribulation già iniziata può continuare anche se Qi Burning viene usato durante la prova.

Stage normali possono continuare a progredire: il danno riguarda la riserva utilizzabile, non QiEXP.

## 11. UI e feedback visivo

### Barra del Qi

La barra mantiene la larghezza corrispondente al Qi massimo base:

- parte ciano: Qi corrente;
- parte vuota scura: Qi disponibile ma non riempito;
- parte rosso scuro/grigia: capacità bruciata.

Tooltip:

```text
Qi: {Current}/{EffectiveMax}
Base capacity: {BaseMax}
Burned capacity: {BurnedPercent}%
```

### Ability Tree

Aggiungere Qi Burning nella riga Foundation Establishment:

- icona dedicata;
- livello ed EXP;
- stato `Active`, `Inactive` oppure `Qi Deviation`;
- bonus correnti;
- costo fisso `2% base max Qi every 3 seconds`;
- durata attuale di Qi Deviation.

### Ability Wheel

Aggiungere una scheda toggle con:

- tasto;
- stato;
- percentuale già bruciata;
- limite di sicurezza;
- avviso rosso oltre il 40%.

### Effetti

Durante Qi Burning:

- aura rossa, arancione e viola;
- particelle che scorrono verso l'esterno, opposte alla meditazione;
- impulso forte ogni 3 secondi;
- suono crescente avvicinandosi al limite;
- colore più instabile oltre il 40%.

Gli effetti devono rispettare `CultivationClientConfig.VisualEffectIntensity`.

---

# Parte II — Heart Demons

## 12. Stato persistente

Campi richiesti:

```csharp
private const int MaximumHeartDemonPoints = 9;
private const int BreakthroughFailuresPerHeartDemonPoint = 2;
private const int DeathsPerHeartDemonPoint = 5;

private int heartDemonPoints;
private int breakthroughFailuresTowardHeartDemon;
private int deathsTowardHeartDemon;
private bool heartDemonTrialActive;
private int heartDemonTrialNpcIndex = -1;

public int HeartDemonPoints => heartDemonPoints;
public int BreakthroughFailuresTowardHeartDemon =>
	breakthroughFailuresTowardHeartDemon;
public int DeathsTowardHeartDemon => deathsTowardHeartDemon;
```

Valori:

- minimo: 0;
- massimo: 9;
- i contatori parziali vengono conservati nel salvataggio;
- raggiunto il massimo, ulteriori eventi non aumentano i punti ma non devono produrre overflow.

## 13. Accumulo

### Breakthrough falliti

Ogni fallimento reale incrementa:

```text
breakthroughFailuresTowardHeartDemon += 1
```

Quando raggiunge 2:

```text
breakthroughFailuresTowardHeartDemon -= 2
heartDemonPoints += 1
```

Devono contare:

- fallimento del tiro percentuale in `TryRealmBreakthrough`;
- fallimento di una Heavenly Tribulation in `FailTribulation`.

Non devono contare:

- mancanza di una pillola o Heavenly Treasure;
- annullamento della finestra di conferma;
- Tribulation rimandata;
- comandi debug;
- errori di rete o caricamento.

### Morti

Ogni 5 morti PvE reali:

```text
deathsTowardHeartDemon -= 5
heartDemonPoints += 1
```

Usare un hook di morte eseguito una sola volta, non `UpdateDead`, che viene eseguito ogni tick.

Non devono contare:

- morte PvP;
- morte durante un Heart Demon Trial;
- morte annullata dalla Heavenly Rebirth Pill;
- comandi debug;
- morte causata da una Tribulation già conteggiata come breakthrough fallito.

Quest'ultima regola impedisce che una singola morte da Tribulation avanzi contemporaneamente entrambi i contatori.

## 14. Penalità

Ogni Heart Demon Point applica:

- `-2` punti percentuali alla probabilità di breakthrough del Realm;
- `-2%` guadagno di QiEXP dalla coltivazione.

Esempi:

| Punti | Breakthrough | Guadagno QiEXP |
|---:|---:|---:|
| 0 | nessuna penalità | 100% |
| 1 | −2 punti | 98% |
| 5 | −10 punti | 90% |
| 9 | −18 punti | 82% |

La penalità di QiEXP:

- influenza la crescita permanente;
- non rallenta la normale ricarica del Qi corrente;
- non modifica ricompense oggetto, EXP Terraria o professioni;
- si applica dopo Root, zona spirituale, Formation e livello di Meditation.

La probabilità finale continua a rispettare il limite esistente di 10–95%.

Aggiornare tutti i calcoli, non soltanto il tiro reale:

- `PendingRealmBreakthroughChance`;
- `NextRealmBreakthroughChance`;
- tooltip del breakthrough;
- finestra di conferma;
- `TryRealmBreakthrough`.

Il valore mostrato al giocatore deve coincidere con quello realmente utilizzato.

## 15. Ottenimento di un punto

Quando nasce un nuovo Heart Demon Point:

- mostrare un messaggio localizzato;
- riprodurre un suono basso e distorto;
- applicare brevemente un effetto visivo oscuro;
- mostrare il nuovo totale, ad esempio `Heart Demons: 3/9`;
- se si raggiungono 9 punti, mostrare un avviso speciale.

Non applicare un debuff Terraria permanente: la sorgente autorevole deve essere il dato salvato nel `ModPlayer`.

## 16. Heart Demon Trial

La purificazione avviene tramite un combattimento personale contro un nuovo NPC:

> Heart Demon

Possibili localizzazioni:

- Italiano: **Demone del Cuore**
- Cinese semplificato: **心魔**

### Avvio

Nel Cultivation Menu aggiungere un pannello Heart Demons con:

- punti attuali;
- penalità totali;
- avanzamento verso il prossimo punto;
- pulsante `Confront Heart Demon`.

Il pulsante è disponibile se:

- `heartDemonPoints > 0`;
- il giocatore è vivo;
- non esistono boss o invasioni attive;
- non è già in corso una Tribulation;
- non è attivo Qi Burning;
- non è già attiva una prova;
- esiste spazio valido per lo spawn.

Mostrare una conferma prima di iniziare.

### Proprietà della prova

- Il boss scala sul Realm e Stage reali del proprietario.
- I punti Heart Demon aumentano ulteriormente vita, danno e aggressività.
- Soltanto il proprietario può infliggere danno.
- Il boss attacca soltanto il proprietario.
- Gli altri giocatori possono osservare ma non aiutare.
- Non rilascia bottino normale.
- Non può essere catturato, trasformato o usato per farming.
- Allontanarsi troppo o uscire dal mondo fallisce la prova senza rimuovere punti.
- Morire nella prova non incrementa il contatore delle morti.

### Tecniche del boss

Il set di attacchi cresce con il Realm:

| Realm | Tecniche del Heart Demon |
|---|---|
| Mortal | dash, colpi ravvicinati, onde corte |
| Qi Gathering | Qi Palm, Fireball, scatti spirituali |
| Foundation Establishment | Qi Protection, Flame Step, proiettili combinati |
| Core Formation | volo, pioggia di spade, attacchi ad area |
| Nascent Soul | teletrasporto, Spiritual Pressure, immagini residue |

Il boss deve sembrare una versione instabile del cultivator:

- silhouette umanoide scura;
- aura con il colore della Spiritual Root del proprietario;
- nome con Realm e Stage;
- effetti più intensi per ogni Heart Demon Point.

### Scaling iniziale suggerito

I valori finali devono essere provati in gioco. Base di partenza:

```text
lifeMultiplier = 1 + RealmIndex * 1.8 + (Stage - 1) * 0.10
heartMultiplier = 1 + HeartDemonPoints * 0.12

damageMultiplier = 1 + RealmIndex * 0.55 + (Stage - 1) * 0.05
heartDamageMultiplier = 1 + HeartDemonPoints * 0.08
```

Applicare inoltre i moltiplicatori Expert/Master attraverso le API normali di Terraria.

Evitare di copiare direttamente tutte le statistiche dell'equipaggiamento del giocatore: combinazioni con altre mod potrebbero rendere il boss impossibile o facilmente sfruttabile.

### Vittoria

Alla morte del Heart Demon:

- verificare sul server che il proprietario e l'istanza siano corretti;
- impostare `heartDemonPoints = 0`;
- azzerare entrambi i contatori parziali;
- terminare lo stato della prova;
- mostrare un messaggio di purificazione;
- riprodurre un effetto di breakthrough ridotto;
- non concedere denaro o drop ripetibili.

Una singola vittoria elimina tutti i punti perché la difficoltà del boss è già proporzionale al totale accumulato.

### Fallimento

La prova fallisce se:

- il proprietario muore;
- il boss perde il proprietario;
- il giocatore esce dal mondo;
- la distanza supera il limite per troppo tempo.

Il fallimento:

- non rimuove punti;
- non aggiunge automaticamente nuovi punti;
- applica un breve cooldown prima di poter riprovare;
- non consuma oggetti, salvo eventuali costi aggiunti in futuro.

## 17. Interazioni fra i due sistemi

- Qi Burning non può essere attivato durante un Heart Demon Trial.
- Un Heart Demon Trial non può iniziare con Qi Burning attivo.
- Qi Deviation non impedisce la prova, ma le sue penalità rimangono: il giocatore può scegliere di attendere.
- La capacità bruciata rimane durante la prova.
- Heart Demon Points non aumentano il costo di Qi Burning.
- Qi Burning usato durante una Heavenly Tribulation è permesso.
- Morire nella Tribulation genera soltanto l'evento di breakthrough fallito, non anche una morte verso Heart Demons.
- Un nuovo breakthrough di Realm richiede capacità bruciata completamente riparata, ma i normali Stage possono avanzare.

---

# Parte III — Integrazione tecnica

## 18. File da modificare

### Core

- `Common/Abilities/CultivationAbility.cs`
  - aggiungere `QiBurning` alla fine dell'enum;
  - impostare Realm richiesto;
  - definire eventuali elementi spirituali.

- `Common/Players/CultivationPlayer.cs`
  - campi, proprietà e timer;
  - calcolo del Qi massimo effettivo;
  - toggle e impulsi di Qi Burning;
  - bonus e Qi Deviation;
  - salvataggio/caricamento;
  - accumulo Heart Demons;
  - penalità breakthrough e QiEXP;
  - avvio e risoluzione della prova;
  - sincronizzazione.

- `Xianxia.cs`
  - keybind Qi Burning;
  - nuovi tipi di pacchetto;
  - validazione server;
  - broadcast dello stato visivo.

### UI

- `Common/Systems/CultivationUISystem.cs`
  - Qi Burning nell'Ability Tree;
  - Qi Burning nell'Ability Wheel;
  - segmento bruciato nella barra Qi;
  - tooltip della capacità;
  - pannello Heart Demons;
  - pulsante e conferma della prova;
  - penalità nel tooltip del breakthrough.

### Contenuti

- `Content/Buffs/QiDeviationDebuff.cs`
- `Content/Items/Alchemy/MeridianMendingPill.cs`
- `Content/NPCs/HeartDemon.cs`
- eventuali proiettili dedicati in `Content/Projectiles/HeartDemonProjectiles.cs`
- texture e icone corrispondenti.

### Manuale e ricette

- `Common/Systems/CultivationManualSystem.cs`
  - spiegazione di Qi Burning;
  - riparazione della capacità;
  - Heart Demon Points;
  - Heart Demon Trial;
  - ricette delle pillole.

- registrare la nuova pillola nel percorso Alchemy e nelle pagine dinamiche delle ricette.

### Localizzazione

Aggiornare almeno:

- `Localization/en-US_Mods.Xianxia.hjson`
- `Localization/it-IT_Mods.Xianxia.hjson`
- `Localization/zh-Hans_Mods.Xianxia.hjson`

## 19. Chiavi di salvataggio

Chiavi suggerite:

```text
burnedQiCapacityBps
qiDeviationTimer
heartDemonPoints
heartDemonBreakthroughProgress
heartDemonDeathProgress
```

Non è necessario salvare `qiBurningEnabled` come stato attivo. In caso di uscita:

- salvare la capacità già bruciata;
- convertire lo stato attivo nella durata completa di Qi Deviation;
- caricare sempre Qi Burning come disattivato.

Valori caricati:

- clamp `burnedQiCapacityBps` tra 0 e 5000;
- clamp `heartDemonPoints` tra 0 e 9;
- clamp fallimenti parziali tra 0 e 1;
- clamp morti parziali tra 0 e 4;
- clamp del timer Qi Deviation alla durata massima valida;
- `Qi = min(Qi, MaxQi)` dopo il caricamento.

I personaggi precedenti senza queste chiavi ricevono tutti valori zero.

## 20. Multiplayer

Il server deve essere autorevole per:

- attivazione e disattivazione di Qi Burning;
- impulsi del 2%;
- capacità bruciata;
- timer Qi Deviation;
- Heart Demon Points e contatori;
- creazione, scaling e risoluzione del Heart Demon;
- uso delle pillole riparatrici.

Pacchetti suggeriti:

```text
QiBurningToggleRequest
QiBurningState
CultivationRiskState
HeartDemonTrialRequest
HeartDemonTrialState
```

Regole:

- il client invia soltanto richieste, mai percentuali o punti scelti;
- il server usa `whoAmI` invece dell'indice dichiarato dal client;
- validare range, stato del giocatore e cooldown;
- inviare ai client vicini il flag Qi Burning per gli effetti visivi;
- inviare capacità bruciata, Qi Deviation e punti al proprietario;
- sincronizzare il proprietario del Heart Demon tramite `NPC.ai` o `SendExtraAI`.

## 21. Configurazione server

Opzioni consigliate:

```csharp
[Header("CultivationRisks")]
[DefaultValue(true)]
public bool EnableQiBurning;

[DefaultValue(50)]
[Range(20, 80)]
public int MaximumBurnedQiPercent;

[DefaultValue(true)]
public bool EnableHeartDemons;

[DefaultValue(100)]
[Range(0, 200)]
public int HeartDemonPenaltyStrengthPercent;
```

I valori fondamentali approvati restano:

- 2% bruciato;
- ogni 3 secondi;
- 2 breakthrough falliti per punto;
- 5 morti per punto;
- massimo 9 punti.

Non esporre questi quattro valori inizialmente, salvo necessità di test.

## 22. Localizzazione minima

Categorie suggerite:

```text
Abilities.QiBurning.*
Buffs.QiDeviation.*
Cultivation.BurnedQi.*
Cultivation.HeartDemons.*
Cultivation.HeartDemonTrial.*
Items.MeridianMendingPill.*
NPCs.HeartDemon.*
Configs.CultivationRisks.*
```

Messaggi necessari:

- tecnica attivata/disattivata;
- impulso e percentuale bruciata;
- limite raggiunto;
- blocco durante Qi Deviation;
- capacità riparata;
- breakthrough bloccato dalla capacità danneggiata;
- nuovo Heart Demon Point;
- massimo di 9 raggiunto;
- prova avviata, vinta o fallita;
- ragione per cui la prova non può iniziare.

---

# Parte IV — Test

## 23. Test Qi Burning

### Funzionali

- Attivazione al Realm corretto.
- Rifiuto nei Realm inferiori.
- Esattamente 2% dopo 180 tick.
- 10% dopo 15 secondi.
- Disattivazione manuale.
- Disattivazione automatica al limite.
- Qi corrente limitato al nuovo MaxQi.
- QiEXP, Realm e Stage invariati.
- Qi Deviation applicata e persistente.
- Livello della tecnica modifica bonus e durata, non il costo.
- Pillola ripara la percentuale corretta.
- Meditazione ripara soltanto nelle zone previste.

### Salvataggio

- Uscita con capacità bruciata.
- Uscita mentre la tecnica è attiva.
- Caricamento di personaggio precedente.
- Cambio del moltiplicatore dei requisiti.
- Breakthrough e rollback con capacità bruciata.

### Multiplayer

- Toggle richiesto dal proprietario.
- Richiesta falsa con indice di un altro giocatore.
- Effetti visibili agli altri client.
- Costo applicato una sola volta dal server.
- Pillola non duplicata.
- Riconnessione durante Qi Deviation.

## 24. Test Heart Demons

- Due fallimenti percentuali generano un punto.
- Una sola failure non genera un punto.
- Una Tribulation fallita conta come un fallimento.
- Mancanza del catalizzatore non conta.
- Cinque morti generano un punto.
- Heavenly Rebirth evita il conteggio.
- Morte da Tribulation non conta due volte.
- Morte durante la prova non aumenta i punti.
- Massimo rispettato a 9.
- Penalità mostrata uguale alla penalità usata.
- Purificazione azzera punti e contatori.
- Nessun altro giocatore può danneggiare il Heart Demon.
- Disconnessione non concede la vittoria.
- Nessun drop farmabile.

## 25. Test combinati

- Qi Burning durante una Tribulation.
- Morte durante Qi Burning.
- Qi Deviation durante Heart Demon Trial.
- Tentativo di avviare la prova con Qi Burning attivo.
- Tentativo di breakthrough con capacità bruciata.
- Incremento di QiEXP mentre il MaxQi effettivo è ridotto.
- Raggiungimento di un nuovo Stage senza riparare la capacità.
- Configurazione che disabilita uno o entrambi i sistemi.

---

# Parte V — Ordine di sviluppo

## 26. Fasi consigliate

### Fase 1 — Fondamenta del Qi danneggiato

1. Separare `BaseMaxQi` e `MaxQi`.
2. Aggiungere percentuale bruciata, save/load e clamp.
3. Correggere tutti gli usi di `QiExp` come limite del Qi corrente.
4. Aggiornare barra e tooltip.
5. Aggiungere test debug.

### Fase 2 — Qi Burning

1. Enum, keybind e toggle.
2. Impulso 2% ogni 3 secondi.
3. Bonus.
4. Qi Deviation.
5. Ability Tree e Wheel.
6. Multiplayer.
7. Effetti audiovisivi.

### Fase 3 — Recupero

1. Meditazione in zona spirituale/Formation.
2. Meridian Mending Pill.
3. Ricetta, alchimia, manuale e localizzazione.
4. Eventuale pillola avanzata.

### Fase 4 — Heart Demon Points

1. Stato persistente.
2. Conteggio dei breakthrough.
3. Conteggio delle morti.
4. Penalità e UI.
5. Multiplayer.

### Fase 5 — Heart Demon Trial

1. NPC e proprietà del proprietario.
2. Scaling per Realm, Stage e punti.
3. Attacchi progressivi.
4. Pannello e conferma.
5. Vittoria/fallimento.
6. Test multiplayer e compatibilità.

---

## 27. Criteri di completamento

Il lavoro è completo quando:

- Qi Burning non può essere usato come buff gratuito;
- ogni 3 secondi costa realmente il 2% della capacità base;
- il danno rimane dopo il combattimento e dopo il logout;
- la capacità può essere riparata senza perdere QiEXP o Realm;
- tutti i valori sono corretti in singleplayer e multiplayer;
- Heart Demons raggiunge massimo 9 punti;
- due fallimenti o cinque morti producono esattamente un punto;
- UI e tiro reale del breakthrough usano la stessa penalità;
- la prova scala sul Realm reale e non può essere risolta con un boss debole;
- vincere la prova purifica il personaggio;
- vecchi personaggi e mondi continuano a caricarsi senza migrazioni distruttive.
