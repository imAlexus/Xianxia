# Complete Terraria Mod Summary: Xianxia

**Mod:** Xianxia
**Author:** imAlexus
**Version:** 0.1.6
**Source path:** `C:/Users/Utente/Documents/My Games/Terraria/tModLoader/ModSources/Xianxia`
**Workshop:** https://steamcommunity.com/sharedfiles/filedetails/?id=3768052356

Xianxia is a tModLoader mod for Terraria that adds a spiritual cultivation system inspired by the xianxia genre: Qi, meditation, Realms, Stages, techniques, heavenly tribulations, spiritual mines, spirit beasts, alchemy, a sect, permanent formations, equipment, and an in-game manual.

---

## Project overview

The mod contains:

- **100 C# source files**
- **98 PNG textures**
- **3 localizations:** English, Italian, Simplified Chinese
- **8 Markdown/documentation files**
- Main systems under `Common/`
- Playable content under `Content/`
- Documentation, changelog, Workshop description, and release notes

---

## Cultivation progression

Progression is divided into **5 Realms**, each with **9 Stages**, for a total of **45 cultivation levels**.

### Available Realms

1. **Mortal**
2. **Qi Gathering**
3. **Foundation Establishment**
4. **Core Formation**
5. **Nascent Soul**

Every advancement increases stats such as:

- maximum life
- defense
- damage
- speed
- critical chance
- damage reduction
- life regeneration

Entering Stage 1 of a new Realm grants a larger increase than regular stages.

---

## Qi and QiEXP

The mod separates two resources:

- **Qi:** a spendable reserve for techniques, abilities, defenses, and flight.
- **QiEXP:** permanent progress toward the next Stage.

Spending Qi **never removes QiEXP, Stages, or Realms**.

The Qi bar displays the current state and can provide information about:

- the next advancement
- future stat bonuses
- unlockable abilities
- Heavenly Tribulation preparation
- local Qi Concentration

---

## Meditation

The player can meditate to gain QiEXP and recover Qi.

Features:

- configurable keybind
- toggle mode or hold-to-meditate mode
- requires remaining still
- can accelerate world time when enabled
- passive Qi recovery scales by Realm
- if Qi is missing, meditation restores it much faster
- near Spirit Stone veins, gains are multiplied

---

## Heavenly Tribulations

Starting at **Core Formation**, Realm breakthroughs require a **Heavenly Tribulation**.

The system includes:

- a confirmation before it begins
- 9 lightning strikes in the first tribulation
- harsher lightning at higher Realms
- damage that partially penetrates defenses
- percentage-based maximum Qi consumption
- Qi Protection is useful but not sufficient alone
- failing prevents the Realm advancement

---

## Abilities and techniques

The mod includes an Ability Wheel and an ability menu/tree with level-based progression.

Each technique can gain EXP through use and improve up to its maximum level.

### Main passives and abilities

- **Meditation**
- **Spiritual Breathing**
- **Qi Sense**
- **Qi Resistance**
- **Qi Protection**
- **Golden Core Circulation**
- **Nascent Soul Regeneration**
- **Night Vision**
- **Sword Intent**

### Active techniques

- **Fireball**
- **Qi Palm**
- **Flame Step**
- **Qi Flight**
- **Void Step**
- **Spiritual Pressure**
- **Spirit Sword Rain**
- **Sect Protection Formation**

### Technique items

There are also items that let the player use some techniques directly from the inventory:

- Fireball Technique
- Qi Palm Technique

---

## Combat and projectiles

The mod adds custom projectiles:

- Qi Fireball
- Qi Palm
- Flame Step
- Flying Sword
- Qi Protection Shield
- Spiritual Pressure Aura
- Spirit Vein Locator
- Sect Protection Formation
- Permanent Formation Dome
- Permanent Formation Barrier Impact
- Spirit Flame
- Spirit Lightning

Some techniques can destroy terrain:

- Fireball
- Qi Palm
- Flame Step

This terrain destruction is configurable by the server.

---

## Spiritual mines and world resources

World generation creates **Spirit Stone mines** in the Cavern layer.

Features:

- small, medium, and large mines
- every world contains at least one mine of each size
- Spirit Stones are finite and cannot be crafted
- mines create high Qi Concentration zones
- Qi Concentration ranges from level 0 to 10
- the multiplier affects both meditation and passive recovery

### Mining resources

- Spirit Stone
- Spirit Crystal
- Spirit Crystal Cluster
- Spirit Jade Ore
- Spirit Jade Bar
- Profound Iron Ore
- Profound Iron Bar

---

## Spirit Vein Compasses

There are three compass versions for finding spiritual veins:

1. **Spirit Vein Compass**
2. **Resonant Spirit Vein Compass**
3. **Heavenly Spirit Vein Compass**

They show the direction and distance of the nearest vein within a configurable range.

---

## Equipment

### Novice Disciple armor

An early-game set with dye support:

- Novice Disciple Headband
- Novice Disciple Robe
- Novice Disciple Trousers

Bonuses include:

- critical chance
- damage
- movement speed
- a meditation set bonus

### Spirit Jade armor

A magic/spiritual set:

- Spirit Jade Headpiece
- Spirit Jade Robe
- Spirit Jade Leggings

Bonuses include:

- magic critical chance
- magic damage
- movement speed
- passive Qi regeneration
- meditation bonus

### Weapons

- Spirit Jade Sword
- Profound Iron Spear
- homing Flying Sword

### Accessories

- Spirit Jade Pendant
- Spirit Gathering Talisman
- Profound Iron Ring

---

## Alchemy

The mod includes a complete **Alchemy Path**.

### Alchemy Tiers

Five tiers, matching the cultivation Realms:

1. Mortal
2. Qi Gathering
3. Foundation Establishment
4. Core Formation
5. Nascent Soul

Each Tier has three stages:

- Low
- Middle
- High

Creating pills gives Path EXP. Advancing improves:

- bonus yield
- impurity reduction
- access to stronger pills

### Cauldrons

- Alchemy Cauldron
- Spirit Jade Cauldron
- Profound Alchemy Cauldron

Higher-grade cauldrons improve yield and reduce impurities.

### Spiritual herbs

- Spirit Grass
- Fire Lotus
- Moon Dew Flower
- Ironroot

Each herb also has plantable seeds and a three-stage growth cycle.

### Pill Saturation

Consuming pills increases saturation. High saturation reduces effectiveness down to 50% and prevents unlimited buff stacking.

### Available pills

- Qi Recovery Pill
- Greater Qi Recovery Pill
- Spirit Gathering Pill
- Body Tempering Pill
- Meridian Cleansing Pill
- Beast Blood Tempering Pill
- Flame Meridian Pill
- Thunder Resistance Pill
- Core Refinement Pill
- Foundation Stabilization Pill
- Golden Core Tempering Pill
- Nascent Soul Awakening Pill
- Soul Nourishing Pill
- Void Insight Pill
- Heavenly Rebirth Pill
- Tribulation Ward Pill
- Spirit Beast Lure Pill
- Concealment Pill

---

## Spirit Beasts

The mod adds spiritual NPCs with their own Realm, Stage, scaling, and drops.

### Available beasts

1. **Spirit Hare**
   - Realm: Mortal
   - spawns on the surface during the day
   - a timid creature

2. **Jade Horn Deer**
   - Realm: Qi Gathering
   - spawns on the surface during the day
   - charges when threatened

3. **Flame-Tailed Fox**
   - Realm: Foundation Establishment
   - spawns on the surface at night
   - uses dashes and spiritual flames

4. **Thunderclaw Tiger**
   - Realm: Core Formation
   - spawns in the Jungle at night
   - elite beast with leaps and lightning

### Scaling

Stronger beasts appear farther from world spawn:

- 0+ blocks: Spirit Hare
- 200+ blocks: Jade Horn Deer
- 450+ blocks: Flame-Tailed Fox
- 700+ blocks: Thunderclaw Tiger

Life, damage, defense, and rewards scale with Realm and Stage.

### Beast drops

- Spirit Fur
- Spirit Beast Blood
- Jade Antler
- Flame Essence
- Thunder Essence
- Mortal Beast Core
- Qi Gathering Beast Core
- Foundation Beast Core
- Core Formation Beast Core

---

## Green Cloud Sect

The mod has a sect system with an NPC, missions, currency, ranks, and techniques.

### NPC

- **Sect Elder / Elder Jian**

The player can join the sect after reaching Qi Gathering.

### Sect ranks

1. Outer Disciple
2. Inner Disciple
3. Core Disciple
4. Sect Elder

### Currency

- Sect Contribution Tokens

### Missions

Possible missions include:

- hunting Spirit Beasts
- delivering Spirit Stones
- refining pills
- exploring spiritual veins
- surviving Heavenly Tribulations

### Sect techniques

- Sword Intent Manual
- Spirit Sword Rain Manual
- Sect Protection Formation Manual

---

## Permanent formations

The mod includes an advanced **Permanent Array Formation** system.

### Array Formation Path

It has its own Tiers and Stages. It is trained through:

- maintaining arrays during combat
- absorbing damage
- connecting to spiritual veins
- intercepting Tribulation lightning

### Permanent Formation Core

A placeable item that creates protected territory.

Functions:

- stores Qi by using Spirit Stones
- has its own Integrity stat
- can connect to spiritual veins
- opens a dedicated UI panel
- enables multiple array types
- saves its state in the world

### Formation types

1. **Protection**
   - defense
   - damage reduction
   - projectile blocking
   - enemy knockback
   - intercepts Tribulation lightning
   - prevents hostile natural spawning inside its territory

2. **Spirit Gathering**
   - increases meditation and passive Qi recovery

3. **Suppression**
   - slows enemies
   - weakens enemy armor

4. **Restoration**
   - regenerates the life of cultivators inside the territory

### Formation Relay Flags

**Formation Relay Flags** extend a Core's territory.

Features:

- their own 40-block radius
- must be placed within 80 blocks of a Core
- share Qi and Integrity with the Core
- increase maintenance consumption
- enable territorial expansion

---

## Interface and quality of life

The mod includes several custom UIs:

- configurable Qi bar
- advanced Qi bar tooltip
- Ability Wheel
- Cultivation Menu
- Abilities tab
- Paths tab
- Sect tab
- Cultivator's Manual
- Permanent Formation Core UI

### Cultivator's Manual

An in-game guide item with:

- interactive index
- scrollable pages
- recipes organized by category
- explanations of Realms, Qi, abilities, beasts, alchemy, sects, and formations
- alchemy requirements on recipes

---

## Configuration

### Server configuration

Includes options for:

- time acceleration during meditation
- time multiplier
- high Qi Concentration zones
- Qi zone radius
- Spirit Vein Compass range
- enabling Spirit Beasts
- Spirit Beast distance scaling
- Spirit Beast spawn rate
- debug commands in multiplayer
- terrain destruction by abilities

### Client configuration

Includes options for:

- toggle meditation
- Qi bar position
- Qi bar scale
- showing/hiding Qi Concentration
- showing/hiding Spirit Beast nameplates
- visual-effect intensity

---

## Commands

### Normal commands

- `/cultivation` shows cultivation status
- `/qisense on|off` controls Qi Sense
- `/qiprotection on|off` controls Qi Protection

### Debug commands

Main command:

- `/xiadebug help`

Available debug actions include:

- current status
- set Realm/Stage
- modify Qi
- advance breakthrough
- test Tribulations
- set ability levels
- play breakthrough effects
- reset progression
- spawn Spirit Beasts
- check Spirit Beast spawning
- set alchemy progression
- set pill saturation
- maximize sect rank

In multiplayer, debug commands are disabled by default unless enabled in the server config.

---

## Localization

The mod has localizations for:

- English
- Italian
- Simplified Chinese

The Italian localization is extensive and covers:

- keybinds
- buffs
- abilities
- item tooltips
- NPCs
- beasts
- sects
- manual
- debug tools
- configuration
- formation UI
- cultivation messages

---

## Changelog summary

### 0.1.0

Initial release:

- 5 Realms
- 45 Stages
- Qi/QiEXP
- meditation
- breakthroughs
- Tribulations
- techniques
- passive abilities
- Spirit Stone mines
- basic alchemy
- Cultivator's Manual

### 0.1.1

Cultivation balance:

- harder Tribulations
- percentage-based Qi damage
- improved Qi Protection
- Qi Concentration 0-10
- Night Vision
- improved Spiritual Pressure

### 0.1.2

Interface and quality of life:

- breakthrough tooltip
- scrollable manual
- Qi bar position and scale
- visual-effect options

### 0.1.3

Spirit Beasts:

- Spirit Hare
- Jade Horn Deer
- Flame-Tailed Fox
- Thunderclaw Tiger
- beast drops
- beast pills
- diagnostic commands

### 0.1.4

Alchemy expansion:

- 5 Alchemy Tiers
- spiritual herbs
- new cauldrons
- alchemy mastery
- pill saturation
- Chinese localization

### 0.1.5

Equipment and manual:

- Novice Disciple armor
- dye support
- redesigned Spirit Jade armor
- recipes in a scrollable catalogue
- alchemy requirements in the manual
- Spirit Beast nameplate option

### 0.1.6

Sects and permanent formations:

- Verdant Cloud Sect, Elder Jian, ranks, missions, Contribution Tokens, and manuals
- Sword Intent, Spirit Sword Rain, and expanded Sect Protection Formation
- Array Formation Path and persistent Formation Cores
- Protection, Spirit Gathering, Suppression, and Restoration arrays
- Manual structural upgrades with material and Beast Core costs
- Relay Flags, Relay UI, Normal Extension, and exclusive specializations
- Spirit Vein power generation for Core and Relay networks
- Persistent pill quality affecting strength and Saturation
- Updated manual, interfaces, visuals, and localization

---

## Main technical structure

### `Common/Players`

Contains the main ModPlayer classes:

- `CultivationPlayer`: progression, Qi, Realms, Stages, abilities, meditation
- `AlchemyPlayer`: Alchemy Path
- `AlchemyPillEffectPlayer`: pill effects
- `FormationPathPlayer`: Array Formation progression
- `SectPlayer`: sect, rank, missions, techniques

### `Common/Systems`

Contains global systems:

- Cultivator's Manual
- cultivation UI
- permanent formation UI
- time acceleration
- sect currency
- Xianxia world generation

### `Content/Items`

Contains:

- materials
- weapons
- armor
- accessories
- pills
- cauldrons
- techniques
- manuals
- sect items
- formation items

### `Content/NPCs`

Contains:

- Sect Elder
- Spirit Beast base class
- four Spirit Beasts

### `Content/Projectiles`

Contains projectiles for abilities, swords, shields, auras, lightning, and formations.

### `Content/Tiles` and `Content/TileEntities`

Contains:

- spiritual ores
- spiritual crystals
- spiritual herbs
- cauldrons
- Formation Core
- Relay Flag
- TileEntities for permanent systems

---

## In short

Xianxia is a broad Terraria mod, not just a collection of items. It adds a full RPG system on top of Terraria:

- complete spiritual progression
- Qi/QiEXP resource system
- levelable techniques
- breakthroughs and Tribulations
- world-generated resources
- alchemy with mastery and saturation
- scalable Spirit Beasts
- a sect with missions and shop
- permanent territorial formations
- multiple interfaces and configuration options
- full Italian localization

Its current focus is turning Terraria into a xianxia cultivator experience with long-term growth, spiritual combat, crafting, exploration, missions, and persistent world systems.
