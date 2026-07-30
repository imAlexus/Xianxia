# Xianxia Mod — Roadmap 0.1.8.2 → 0.2.0

**Versione corrente:** 0.1.8.2 "Spiritual Roots and Breakthrough Expansion"
**Target:** 0.2.0 "Spirit Beasts, Soul & Tribulation Expansion"
**Obiettivo:** Aggiungere i 2 Realms mancanti (Soul Formation, Void Refinement), espandere Spirit Beasts a tutti i Realms, aggiungere Soul Formation Tribulations, Soul system, Artifact Crafting e Artifact Spirit system.

---

## 🎯 Obiettivo 0.2.0: "Soul & Void"

Aggiungere i **2 Realms finali** (Soul Formation + Void Refinement = 18 Stages), **Spirit Beasts per tutti i Realms** (6 nuovi beast), **Soul Formation Heavenly Tribulations**, **Soul & Artifact Spirit system**, **Artifact Crafting & Refining**, **Dual Cultivation / Dao Companion system**, **Sect Wars / Territory Wars**, **World Boss Spirit Beasts**, **Multiplayer Sect Alliance system**.

---

## 📦 Milestone 0.1.7 — "Soul Formation Foundation" (Core Systems)

**Obiettivo:** Aggiungere il 6° Realm (Soul Formation, 9 Stages) con progressione completa.

### ✅ Task Core
- [ ] **Realm 6: Soul Formation** — 9 Stages (Soul Formation Early/Mid/Late/Perfection × 3 sub-stages o Low/Mid/High × 3)
  - Nuove stat bonuses per Stage (max life, damage, defense, speed, crit, DR, regen, Qi capacity)
  - Nuovi breakthrough requirement: Soul Formation Tribulation (diverso da Core Formation/Nascent Soul)
- [ ] **Soul Formation Tribulation System**
  - Nuovo tipo: **Soul Lightning** (colpisce l'anima, bypassa difesa fisica, danneggia QiEXP se fallito)
  - **Inner Demon Tribulation** (ondate di illusioni nemiche che scalano con Realm/Stage)
  - **Heart Demon** mechanic: se fallisci, perdi Stage progress + temp debuff "Heart Devil" (riduce meditation gain, aumenta Qi consumption)
- [ ] **Soul Cultivation Stats**
  - **Soul Force** (nuova risorsa parallela a Qi, usata per Soul techniques)
  - **Soul EXP** (progressione separata per Soul Stage)
  - **Soul Sea** visualization in UI (nuova barra sotto Qi bar)
- [ ] **Soul Formation Techniques** (9 nuove passive/active)
  - *Soul Projection* (proietta anima per scouting/combat)
  - *Soul Absorption* (assorbe Soul Force da bestie/nemici uccisi)
  - *Soul Domain* (area effect: debuff nemici, buff alleati)
  - *Soul Severing* (executes sotto % vita, consuma Soul Force)
  - *Divine Sense Expansion* (aumenta range Qi Sense, rivela stealth/invisibili)
  - *Soul Link* (condividi Soul Force con party/sect members)
  - *Reincarnation Preparation* (passive: riduce penalty morte, preserva % QiEXP)
  - *Void Touch* (precursore Void Refinement: attacchi ignorano % defense)
  - *Sect Soul Beacon* (teleport to sect core, once per day)

### ✅ UI & Config
- [ ] Nuova UI: **Soul Panel** (tab in Cultivation Menu)
- [ ] Config server: Soul Formation Tribulation difficulty, Soul Force regen rates, Heart Devil severity
- [ ] Config client: Soul bar position/scale, Soul technique keybinds
- [ ] Localization: EN/IT/ZH per tutti i nuovi termini

---

## 📦 Milestone 0.1.8 — "Spirit Beast Expansion" (Content)

**Obiettivo:** Completare Spirit Beasts per TUTTI i 6 Realms (6 nuovi beast + variant system).

### ✅ Nuovi Spirit Beasts (6 Realms × 1 beast each = 6 nuovi + 4 esistenti = 10 total)

| Realm | Beast | Biome/Time | Mechanics |
|-------|-------|------------|-----------|
| Mortal | **Spirit Hare** ✅ | Surface Day | Timid, flee |
| Qi Gathering | **Jade Horn Deer** ✅ | Surface Day | Charge attack |
| Foundation | **Flame-Tailed Fox** ✅ | Surface Night | Dash + flame projectiles |
| Core Formation | **Thunderclaw Tiger** ✅ | Jungle Night | Leap + lightning |
| **Soul Formation** | **Void Raven** 🆕 | Space/Corruption Night | Teleport, void projectiles, applies "Void Corruption" |
| **Void Refinement** | **Astral Kirin** 🆕 | Hallow Night (Hardmode) | Flight, holy/void hybrid beams, buffs allies |
| **Nascent Soul** | **Soul Devourer** 🆕 | Underworld | Steals Soul Force on hit, phases through walls |
| **Foundation (Variant)** | **Frostbite Lynx** 🆕 | Snow Night | Ice trails, freeze, slows Qi regen |
| **Core Formation (Variant)** | **Magma Serpent** 🆕 | Underworld Day | Lava swim, magma pools, fire DoT |
| **Soul Formation (Variant)** | **Dreamweaver Moth** 🆕 | Jungle Day | Sleep dust, illusion clones, confusion |

### ✅ Spirit Beast Systems Expansion
- [ ] **Beast Taming / Contract System** — usa Spirit Beast Lure Pill per tame (chance basata su Realm/Stage player vs beast)
- [ ] **Tamed Beast Companion** — follows player, passive buffs, active skill (cooldown), evolve con player Realm
- [ ] **Beast Core Absorption** — consuma Beast Core per permanente stat bonus o technique unlock
- [ ] **Spirit Beast Breeding** (late 0.1.9) — combina 2 tamed beasts per variant/hybrid
- [ ] **World Boss Spirit Beasts** — spawn rari per Realm (es. **Ancient Void Raven** per Soul Formation), richiedono party/sect, drop unique Artifact materials

### ✅ Drops & Crafting
- [ ] Nuovi materiali: **Void Feather**, **Astral Horn**, **Soul Essence**, **Dream Dust**, **Frost Fang**, **Magma Scale**
- [ ] Nuovi pill: **Soul Condensing Pill**, **Void Resistance Pill**, **Astral Blessing Pill**, **Dream Clarity Pill**

---

## 📦 Milestone 0.1.9 — "Artifacts & Artifact Spirits" (Major System)

**Obiettivo:** Sistema completo di Artifact Crafting, Refining e Artifact Spirits (sentient weapons/tools).

### ✅ Artifact System
- [ ] **Artifact Tier System** (5 Tiers: Mortal → Earth → Heaven → Immortal → Divine)
- [ ] **Artifact Types** (ognuno con slot dedicato, max 3 equipaggiati):
  - **Weapon Artifacts** (Sword, Saber, Spear, Bow, Staff) — active attack skill
  - **Defensive Artifacts** (Shield, Mirror, Bell, Pagoda) — passive/active defense
  - **Utility Artifacts** (Compass, Gourd, Lantern, Map, Cauldron) — utility/QoL
  - **Formation Artifacts** (Flag, Disk, Pearl) — boost Permanent Formations
- [ ] **Artifact Crafting** — nuovo **Artifact Forge** (TileEntity), richiede: Artifact Core + Spirit Materials + Beast Core + Pill + QiEXP investment
- [ ] **Artifact Refining / Upgrading** — feed Spirit Stones, Beast Cores, Pills, QiEXP per aumentare Tier/Stage
- [ ] **Artifact Mastery** — usa artifact per guadagnare Artifact EXP → sblocca abilità passive/active

### ✅ Artifact Spirit System (Sentient Artifacts)
- [ ] **Awakening Ritual** — al Tier Heaven+, usa **Soul Essence** + **Heart Blood** (player sacrifice: permanent -max life per artifact) per risvegliare spirito
- [ ] **Artifact Spirit NPC** — appare in UI, ha personalità (Loyal, Prideful, Lazy, Bloodthirsty, Wise), livello fedeltà
- [ ] **Spirit Cultivation** — nutre spirito con Qi, Pills, Beast Essence → cresce, sblocca:
  - **Spirit Ability** (unique per artifact type)
  - **Auto-combat mode** (attacca nemici autonomamente consumando player Qi)
  - **Spirit Domain** (area buff quando equipaggiato)
  - **Telepathic Communication** (chat messages, warnings, lore)
- [ ] **Spirit Rebellion Risk** — se fedeltà bassa o player usa artifact contro personalità spirito → chance ribellione (artifact unequips, debuff "Spirit Backlash")
- [ ] **Dual Cultivation with Artifact Spirit** — double cultivation session, entrambi guadagnano EXP bonus

### ✅ UI & Integration
- [ ] **Artifact Forge UI** (crafting, refining, awakening)
- [ ] **Artifact Inventory** (dedicated slots, filter by type/tier)
- [ ] **Artifact Spirit Panel** (rapporto, feed, dialog, settings)
- [ ] Cultivator's Manual: Artifact & Artifact Spirit sections

---

## 📦 Milestone 0.1.10 — "Void Refinement & Dual Cultivation" (Endgame Systems)

**Obiettivo:** 7° Realm (Void Refinement, 9 Stages) + Dual Cultivation / Dao Companion system.

### ✅ Realm 7: Void Refinement (9 Stages)
- [ ] **Void Refinement Progression** — richiede **Void Tribulation** (spatial tears, reality distortion, void monsters spawn)
- [ ] **Void Qi** — nuova resource sostituisce Qi parzialmente, immune a Qi drain, ignora defense
- [ ] **Void Techniques** (9 nuove)
  - *Void Walker* (teleport through walls, short range)
  - *Spatial Sever* (ranged attack ignora armor/defense)
  - *Void Domain* (zone: enemies take void damage, player gains void regen)
  - *Reality Anchor* (immune to knockback/teleport/confusion)
  - *Void Absorption* (absorbe projectiles → converte in Void Qi)
  - *Spatial Storage* (access void storage da ovunque)
  - *Void Clone* (summon clone con % stats, usa Void techniques)
  - *Dimensional Slash* (projectile che passa through walls/enemies)
  - *World Severing* (ultimate: massive damage, long cooldown, consuma tutto Void Qi)

### ✅ Dual Cultivation / Dao Companion System
- [ ] **Dao Companion NPC** — sposabile (Elder Jian + nuovi NPC per ogni sect), richiede:
  - Realm compatibile (±1 Realm)
  - Affinity level (gift giving, missioni insieme, dual cultivation sessions)
  - Cerimonia al Sect Altar
- [ ] **Dual Cultivation Session** — entrambi meditano insieme → **Dual QiEXP bonus** (scaling con affinity), chance **Dual Breakthrough** (entrambi breakthrough simultaneo se pronti)
- [ ] **Companion Benefits**:
  - Shared Sect Contribution / Formation network access
  - Companion può essere summoned come temporary ally (cooldown)
  - Companion quest line personale → sblocca technique/artifact unici
  - **Dao Heart Link** — se uno muore, l'altro riceve "Grief" debuff ma guadagna "Resolve" buff permanente
- [ ] **Polygamy / Multiple Dao Companions** (config server: enabled/disabled, max companions)

### ✅ Sect Wars / Territory System
- [ ] **Sect Territory Claims** — posiziona **Sect Banner** (nuovo Formation Core variant) per claim chunk
- [ ] **Territory Benefits** — Qi Concentration bonus, Spirit Vein spawn rate ↑, Sect member buffs inside territory
- [ ] **Sect War Declaration** — formal challenge, 24h prep, poi 2h conflict window
- [ ] **War Mechanics** — destroy enemy Sect Banner / Formation Cores, kill enemy sect members per War Points
- [ ] **Victory Conditions** — capture % territory, destroy main Sect Core, or force surrender
- [ ] **Rewards** — captured territory, Sect Contribution Tokens, unique War Trophies, Artifact fragments
- [ ] **Alliance System** — sects form alliances, shared territory defense, joint wars

---

## 📦 Milestone 0.2.0 — "Ascension & Polish" (Release Candidate)

**Obiettivo:** Polish, balance, Ascension content, release 0.2.0.

### ✅ Ascension System (Post-Void Refinement)
- [ ] **Heavenly Ascension Tribulation** — event mondiale, tutti i player online partecipano/difendono
- [ ] **Ascension Rewards** — titolo "Ascended", cosmetic wings/halo, accesso a **Immortal Realm** (future 1.0), unique Ascension Artifact
- [ ] **Reincarnation / New Game+** — reset Realm/Stage, mantieni: Artifacts, Artifact Spirits, Dao Companions, Sect status, Manual knowledge → bonus "Karmic Wisdom" (permanent EXP multiplier)

### ✅ Polish & Balance
- [ ] **Full Balance Pass** — tutti i 63 Stages (7×9), Tribulation difficulty curve, pill/beast/artifact scaling
- [ ] **Multiplayer Sync** — test e fix: Artifact Spirits, Dual Cultivation, Sect Wars, Formation networks in MP
- [ ] **Performance** — ottimizza Formation network updates, Spirit Beast AI, Artifact Spirit AI
- [ ] **Accessibility** — config per disabilitare: Heart Devil, Spirit Rebellion, Sect Wars, Dual Cultivation
- [ ] **Localization Complete** — EN/IT/ZH per tutto il nuovo contenuto (~2000+ nuove stringhe)
- [ ] **Cultivator's Manual Complete** — tutte le nuove sezioni: Soul Formation, Void Refinement, Artifacts, Spirits, Dual Cultivation, Sect Wars, Ascension

### ✅ Content Pack
- [ ] **10+ nuovi Artifact set** (themed: Void, Astral, Soul, Dream, Frost, Magma, Time, Karma, Void-Sword, Heavenly)
- [ ] **5 World Boss Spirit Beasts** (uno per Realm 3-7)
- [ ] **Sect War Maps / Arenas** (pre-generated structures per war events)
- [ ] **Ascension Dungeon** (post-Void Refinement, solo/party, weekly lockout)

---

## 📅 Timeline Stimata

| Milestone | Stimato | Note |
|-----------|---------|------|
| **0.1.7** Soul Foundation | 3-4 settimane | Core system, richiede testing MP |
| **0.1.8** Beast Expansion | 2-3 settimane | Content-heavy, art assets needed |
| **0.1.9** Artifacts & Spirits | 4-5 settimane | Sistema più complesso, UI pesante |
| **0.1.10** Void & Dual Cult | 3-4 settimane | Endgame systems, balance critico |
| **0.2.0** Polish & Release | 2-3 settimane | Bugfix, localization, balance pass |
| **Totale** | **~14-19 settimane** | ~3.5-5 mesi |

---

## 🎯 Priorità per Iniziare Subito (Primo Sprint 0.1.7)

1. **Soul Formation Realm data** — Realm definition, 9 stages, stat scaling formulas
2. **Soul Force resource system** — nuova resource bar, regen, consumption, save/load, MP sync
3. **Soul Formation Tribulation (v1)** — Soul Lightning + Inner Demon base implementation
4. **Soul Panel UI** — tab in Cultivation Menu, Soul bar, technique slots
5. **2-3 Soul Techniques** — Soul Projection, Divine Sense Expansion, Soul Absorption (MVP)
6. **Config + Localization skeleton** — per nuovi sistemi

---

## 📝 Note Tecniche / Architettura

### Nuovi File/Classi Previsti
```
Common/Players/
  SoulPlayer.cs              // Soul Force, Soul EXP, Soul Stage, Heart Devil
  ArtifactPlayer.cs          // Equipped artifacts, mastery, spirit bonds
  CompanionPlayer.cs         // Dao Companion data, dual cult sessions

Common/Systems/
  SoulTribulationSystem.cs   // Soul Formation + Void Tribulations
  ArtifactSystem.cs          // Crafting, refining, awakening, spirits
  ArtifactSpiritAI.cs        // Spirit behavior, loyalty, rebellion
  DualCultivationSystem.cs   // Session management, bonuses
  SectWarSystem.cs           // Territory, declarations, scoring, rewards
  AscensionSystem.cs         // World event, rewards, reincarnation

Content/Items/Artifacts/
  ArtifactCore.cs            // Base artifact item
  WeaponArtifact.cs
  DefensiveArtifact.cs
  UtilityArtifact.cs
  FormationArtifact.cs
  ArtifactSpiritItem.cs      // Spirit essence per awakening

Content/NPCs/
  DaoCompanionNPC.cs         // Spawnable, marriage, quests
  ArtifactSpiritNPC.cs       // UI-only "NPC" per dialog/behavior
  WorldBossSpiritBeast.cs    // Base class per world bosses

Content/TileEntities/
  ArtifactForgeTE.cs         // Crafting/refining station
  SectBannerTE.cs            // Territory claim
```

### Config Nuove (ServerConfig)
```csharp
// Soul Formation
SoulFormationTribulationDifficulty (0.5-2.0)
HeartDevilSeverity (0.5-2.0)
SoulForceRegenRateMultiplier (0.5-2.0)

// Spirit Beasts
WorldBossSpawnChance (0.0-1.0)
WorldBossMinIntervalDays (1-30)
BeastTameBaseChance (0.01-0.5)

// Artifacts
ArtifactRefiningCostMultiplier (0.5-2.0)
ArtifactAwakeningCostMultiplier (0.5-2.0)
SpiritRebellionChanceBase (0.0-0.2)

// Dual Cultivation
DualCultivationBonusMultiplier (1.0-3.0)
MaxDaoCompanions (1-4)
RequireMarriageForDualCult (true/false)

// Sect Wars
SectWarEnabled (true/false)
TerritoryClaimCost (Sect Tokens)
WarDurationHours (1-6)
```

### Localization Keys Nuove (stima ~2000)
- Soul Formation: Realm, 9 Stages, 9 Techniques, Tribulation types, Heart Devil
- Spirit Beasts: 6 nuovi beast + 4 varianti + 5 world boss + taming/contract
- Artifacts: 5 tiers × 4 types × ~5 each = 100 artifact names + descriptions
- Artifact Spirits: 50+ personality lines, rebellion dialogs, cultivation messages
- Dual Cultivation: Ceremony, session, companion quests, dao heart link
- Sect Wars: Declaration, territory, banner, war score, victory/defeat
- Ascension: Tribulation, rewards, reincarnation, karmic wisdom

---

## ✅ Definition of Done per 0.2.0

- [ ] Tutti e 7 i Realms giocabili (63 Stages totali)
- [ ] 10+ Spirit Beasts + 5 World Bosses con AI unica
- [ ] Artifact System completo (craft, refine, awaken, spirit)
- [ ] Dual Cultivation / Dao Companion funzionante in SP e MP
- [ ] Sect Wars / Territory system funzionante in MP
- [ ] Ascension event + Reincarnation/NG+
- [ ] Cultivator's Manual 100% completo
- [ ] Localizzazione EN/IT/ZH completa
- [ ] Config server/client per tutti i nuovi sistemi
- [ ] Nessun blocking bug in MP (testato 4+ player)
- [ ] Performance: <5ms/frame overhead nuovi sistemi in late-game

---

## 🔄 Post-0.2.0 Ideas (1.0 "Ascension")

- **Immortal Realm** (8° Realm, infinite progression via "Dao Comprehension")
- **Sect Management Sim** (disciples, elders, resources, diplomacy, auto-missions)
- **Pocket World / Grotto Heaven** (player-owned dimension, time dilation, resource generation)
- **Karma System** (actions → karma → affects tribulation, reincarnation, NPC reactions)
- **Inter-Server Sect Alliance** (cross-world wars via tModLoader crossplay)
- **Mod Compatibility Layer** (Calamity, Thorium, Calamity Mod integration: cultivation scaling, boss drops)

---

*Roadmap aggiornata il 2026-07-30 per Xianxia v0.1.8.2 → v0.2.0*
*Aggiornare ad ogni milestone completata.*
