# Xianxia 0.1.7 - Riepilogo aggiornamento

Confronto di riferimento: `v0.1.6` → `0.1.7`.

## Tema dell'aggiornamento

La versione 0.1.7 introduce il **Path della Forgiatura degli Artefatti**:
una professione indipendente dalla coltivazione del personaggio, con progressione,
qualità degli oggetti, forge dedicate e artefatti costruiti usando materiali degli
Spirit Beast.

## Forgiatura degli Artefatti

- Aggiunto un Path con 5 Tier, da Mortal a Nascent Soul.
- Ogni Tier contiene gli stadi Low, Middle e High.
- La progressione avviene fabbricando artefatti, non salendo di Realm.
- Ogni artefatto assegna EXP alla Forgiatura.
- Tier, Stage ed EXP vengono salvati sul personaggio.
- Lo stato del Path viene sincronizzato in multiplayer.
- Aggiunta una pagina dedicata nel menu `Cultivation > Paths`.
- La pagina mostra rango, EXP, progresso e artefatti sbloccati per ogni Tier.
- Aggiunto il comando debug:
  `/xiadebug forging <tier 0-4> <low|middle|high>`.

## Qualità degli artefatti

Ogni artefatto creato riceve una qualità individuale:

| Qualità | Potenza | Costo Qi |
|---|---:|---:|
| Crude | 82% | 120% |
| Common | 100% | 100% |
| Refined | 112% | 92% |
| Earth Grade | 127% | 82% |
| Heaven Grade | 148% | 70% |

- Una forge migliore aumenta la probabilità di ottenere qualità elevate.
- Superare il requisito di maestria della ricetta migliora ulteriormente il tiro.
- La qualità modifica danno, bonus e costo Qi dell'artefatto.
- La qualità è salvata sul singolo oggetto e sincronizzata in multiplayer.
- I tooltip mostrano qualità, potenza, costo Qi, requisito e EXP ricevuta.

## Nuove forge

### Artifact Forge

Forge Mortal di base per i primi artefatti ottenuti dai materiali degli Spirit Beast.

### Spirit Jade Artifact Forge

Forge intermedia che migliora la qualità e permette la raffinazione degli artefatti
Qi Gathering e Foundation Establishment.

### Profound Artifact Forge

Forge avanzata necessaria per gli artefatti Core Formation e Nascent Soul.

Tutte e tre le forge:

- sono piazzabili;
- emettono luce e particelle spirituali;
- hanno una ricetta;
- sono presenti nella sezione delle ricette del manuale.

## Nuovi artefatti

### Verdant Antler Staff

- Usa Jade Antler, Spirit Jade e un Mortal Beast Core.
- Lancia un proiettile verde a ricerca.
- Avvelena i bersagli.
- Corretto il comportamento che in precedenza faceva apparire spade.

### Jade Antler Talisman

- Accessorio difensivo.
- Aumenta difesa, rigenerazione vitale e danno magico.
- I bonus scalano con la qualità dell'artefatto.

### Flame Spirit Fan

- Lancia tre palle di fuoco Qi con traiettorie divergenti.
- Consuma Qi.
- La distruzione del terreno è disabilitata per impostazione predefinita.

### Thunderclap Seal

- Consuma Qi e lancia un attacco elettrico a lunga distanza.
- Il corpo del proiettile è invisibile.
- L'effetto visivo finale utilizza esclusivamente una scia di particelle elettriche.
- Infligge `Electrified` ai nemici colpiti.
- Rimossi i fasci e le linee anomale prodotti dalla precedente texture vanilla.

### Beast Soul Banner

- Accessorio Nascent Soul.
- Evoca un Beast Soul Guardian visibile che orbita intorno al giocatore.
- Il Guardian cerca i nemici e lancia nuclei spirituali a ricerca.
- Aumenta danno summon, danno magico, difesa e capacità dei minion.
- Corretto il problema per cui lo stendardo non produceva alcun effetto.

## Manuale e localizzazione

- Il manuale passa da 18 a 19 pagine.
- Aggiunta una voce all'indice per la Forgiatura degli Artefatti.
- Aggiunta una pagina che spiega Path, qualità, forge e artefatti.
- Aggiunte le ricette illustrate di tutte e tre le forge e dei cinque artefatti.
- Aggiornati i testi inglesi e italiani.
- Aggiornate anche le chiavi generate della localizzazione cinese semplificata.

## Workshop

- `build.txt` aggiornato alla versione `0.1.7`.
- La descrizione Workshop include il collegamento al server Discord ufficiale:
  <https://discord.gg/qgNTXt8cs8>

## File modificati

1. `Common/Commands/XianxiaDebugCommand.cs`
2. `Common/Systems/CultivationManualSystem.cs`
3. `Common/Systems/CultivationUISystem.cs`
4. `Localization/en-US_Mods.Xianxia.hjson`
5. `Localization/it-IT_Mods.Xianxia.hjson`
6. `Localization/zh-Hans_Mods.Xianxia.hjson`
7. `Xianxia.cs`
8. `build.txt`
9. `description_workshop.txt`

## File aggiunti

### Sistema di Forgiatura

1. `Common/Items/ArtifactGlobalItem.cs`
2. `Common/Players/ArtifactForgingPlayer.cs`

### Oggetti e texture degli artefatti

3. `Content/Items/Artifacts/ArtifactForges.cs`
4. `Content/Items/Artifacts/SpiritualArtifacts.cs`
5. `Content/Items/Artifacts/ArtifactForge.png`
6. `Content/Items/Artifacts/SpiritJadeArtifactForge.png`
7. `Content/Items/Artifacts/ProfoundArtifactForge.png`
8. `Content/Items/Artifacts/VerdantAntlerStaff.png`
9. `Content/Items/Artifacts/JadeAntlerTalisman.png`
10. `Content/Items/Artifacts/FlameSpiritFan.png`
11. `Content/Items/Artifacts/ThunderclapSeal.png`
12. `Content/Items/Artifacts/BeastSoulBanner.png`

### Proiettili

13. `Content/Projectiles/ArtifactProjectiles.cs`

### Tile e texture delle forge

14. `Content/Tiles/ArtifactForgeTiles.cs`
15. `Content/Tiles/ArtifactForgeTile.png`
16. `Content/Tiles/SpiritJadeArtifactForgeTile.png`
17. `Content/Tiles/ProfoundArtifactForgeTile.png`

## Totale

- 9 file esistenti modificati.
- 17 file funzionali o grafici aggiunti.
- 1 file di riepilogo aggiunto: `UPDATE_0.1.7_SUMMARY.md`.
- Totale dell'aggiornamento documentato: **27 file**.

`ROADMAP_0.2.0.md` non è incluso nel conteggio perché non fa parte
dell'implementazione della versione 0.1.7.

## Verifica

Ultima compilazione eseguita:

```text
Compilazione completata.
Avvisi: 0
Errori: 0
```
