using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Xianxia.Content.Buffs;
using Xianxia.Content.Items;
using Xianxia.Content.Items.Alchemy;
using Xianxia.Content.Items.Guides;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.NPCs;
using Xianxia.Content.Projectiles;
using Xianxia.Content.TileEntities;
using Xianxia.Content.Tiles;
using Xianxia.Common.Utilities;
using Xianxia.Common.Config;
using Xianxia.Common.Abilities;
using Xianxia.Common.Elements;
using Xianxia.Common.Items;

namespace Xianxia.Common.Players;

public enum FoundationQuality : byte
{
	Inferior,
	Stable,
	Perfect,
	Heavenly
}

public class CultivationPlayer : ModPlayer
{
	private const int StagesPerRealm = 9;
	private const int TotalRealms = 5;
	private const int ProgressionVersion = 2;
	private const int LegacyBaseStageQiCost = 25;
	private const int LegacyStageQiCostIncrease = 10;
	private const int MaxGlobalStageIndex = TotalRealms * StagesPerRealm - 1;
	private const int MeditationQiRecoveryMultiplier = 10;
	private const int QiResistanceCost = 60;
	private const int QiProtectionCostPerDamage = 5;
	private const int QiResistanceDuration = 60 * 60;
	private const int QiFlightCostInterval = 60;
	private const int MinimumFireballQiCost = 12;
	private const float FireballDamagePerQi = 6f;
	private const int FireballCooldownTicks = 30;
	private const int QiSenseCostInterval = 60;
	private const int QiPalmCooldownTicks = 36;
	private const int FlameStepCooldownTicks = 90;
	private const int NascentTeleportCooldownTicks = 60;
	private const int NascentTeleportBaseQiCost = 20;
	private const int NascentTeleportQiCostPerDistanceStep = 2;
	private const float NascentTeleportBlocksPerQi = 2f;
	private const int NascentTeleportSafeSearchRadius = 12;
	private const int SpiritualPressureQiCostPerSecond = 25;
	private const int NightVisionBaseQiCostPerSecond = 3;
	private const int StageBreakthroughEffectDuration = 75;
	private const int RealmBreakthroughEffectDuration = 150;
	private const int TribulationStartingRealm = 3;
	private const int TribulationInitialDelay = 120;
	private const int TribulationStrikeInterval = 90;
	private const int TribulationWarningTime = 45;
	private const int QiBurnPulseInterval = 3 * 60;
	private const int QiBurnPerPulseBps = 200;
	private const int DefaultMaximumBurnedQiBps = 5000;
	private const int BurnedQiMeditationRepairInterval = 30 * 60;
	private const int BurnedQiMeditationRepairBps = 25;
	private const int QiBurnCombatWindow = 5 * 60;
	private const int QiBurnExperiencePerMinute = 100;
	private const int MaximumHeartDemonPoints = 9;
	private const int BreakthroughFailuresPerHeartDemonPoint = 2;
	private const int DeathsPerHeartDemonPoint = 5;
	private const int HeartDemonTrialRetryCooldown = 30 * 60;
	public const int HeavenlyTreasureRequired = 1;
	public const int TechniqueLoadoutPresetCount = 3;
	public const int MaximumTechniqueLoadoutSlots = 6;
	private const int TechniqueLoadoutSaveVersion = 2;

	private static readonly CultivationAbility[] DefaultLoadoutOrder =
	[
		CultivationAbility.QiResistance,
		CultivationAbility.Fireball,
		CultivationAbility.QiPalm,
		CultivationAbility.SpiritualRain,
		CultivationAbility.FlameStep,
		CultivationAbility.SpiritSwordRain,
		CultivationAbility.NascentTeleport
	];

	private readonly record struct CultivationBonus(
		float MaxLife,
		float Defense,
		float DamagePercent,
		float MoveSpeedPercent,
		float CritChance,
		float EndurancePercent,
		float LifeRegen
	);

	private static readonly int[] StageQiBaseCostByRealm = [10, 300, 1500, 7500, 37500];
	private static readonly int[] StageQiCostIncreaseByRealm = [15, 100, 500, 2500, 12500];
	private static readonly int[] MeditationQiGainByRealm = [1, 4, 16, 64, 256];
	private static readonly int[] PassiveQiRecoveryByRealm = [1, 2, 4, 8, 16];

	private static readonly string[] RealmKeys =
	[
		"Mortal",
		"QiCondensation",
		"FoundationEstablishment",
		"CoreFormation",
		"NascentSoul"
	];
	private static readonly CultivationBonus[] StageGrowthByRealm =
	[
		new(3f, 0.5f, 0.5f, 0.15f, 0.2f, 0.1f, 0.1f),
		new(8f, 1f, 1.5f, 0.3f, 0.35f, 0.2f, 0.2f),
		new(20f, 2f, 4f, 0.6f, 0.6f, 0.45f, 0.45f),
		new(50f, 5f, 9f, 1f, 1f, 0.85f, 0.85f),
		new(120f, 12f, 20f, 1.6f, 1.5f, 1.5f, 1.5f)
	];

	private int meditationTimer;
	private int passiveQiRecoveryTimer;
	private int spiritualQiScanTimer;
	private float meditationQiGainRemainder;
	private float meditationQiRecoveryRemainder;
	private float passiveQiGainRemainder;
	private int flightQiTimer;
	private int fireballCooldown;
	private int qiSenseCostTimer;
	private int qiPalmCooldown;
	private int flameStepCooldown;
	private int nascentTeleportCooldown;
	private int spiritualPressureQiTimer;
	private int nightVisionQiTimer;
	private bool qiBurningEnabled;
	private int qiBurningPulseTimer;
	private int burnedQiCapacityBps;
	private int qiDeviationTimer;
	private int qiBurningCombatTimer;
	private int qiBurningExperienceWindowTimer;
	private int qiBurningExperienceThisWindow;
	private int burnedQiMeditationRepairTimer;
	private int heartDemonPoints;
	private int breakthroughFailuresTowardHeartDemon;
	private int deathsTowardHeartDemon;
	private bool heartDemonTrialActive;
	private int heartDemonTrialNpcIndex = -1;
	private int heartDemonTrialCooldown;
	private bool awaitingHeartDemonTrialConfirmation;
	private int heartDemonVisualTimer;
	private bool suppressRiskTracking;
	private int spiritualRainCooldown;
	private int nascentSoulRegenerationTrainingTimer;
	private float qiProtectionDotQiAccumulator;
	private int qiProtectionDotVisualCooldown;
	private int drowningProtectionTimer;
	private int stillnessWarningCooldown;
	private int breakthroughWarningCooldown;
	private int breakthroughEffectTimer;
	private int breakthroughEffectRealm;
	private bool realmBreakthroughEffect;
	private int tribulationRealm = -1;
	private int tribulationTimer;
	private int tribulationStrikesRemaining;
	private int pendingTribulationRealm = -1;
	private int deferredTribulationRealm = -1;
	private int pendingRealmBreakthroughConfirmation = -1;
	private int confirmedRealmBreakthrough = -1;
	private bool awaitingTribulationConfirmation;
	private bool resolvingTribulationLightning;
	private bool flightMaintainedDuringChat;
	private bool meditationToggleRequested;
	private bool forceNextRealmBreakthrough;
	private int appliedCultivationRequirementMultiplier;
	private int heavenlyEyeImprints;
	private int heavenlyRoyalNectarImprints;
	private int heavenlyBoneMarrowImprints;
	private int realmBreakthroughAttempts;
	private int realmBreakthroughSuccesses;
	private int realmBreakthroughFailures;
	private int breakthroughPillsConsumed;
	private int successfulBreakthroughPillMask;
	private int successfulBreakthroughRecordedMask;
	private int pendingBreakthroughTreasure;
	private bool pendingBreakthroughUsedPill;
	private FoundationQuality foundationQuality;
	private int goldenCoreTier = 9;
	private FoundationQuality pendingFoundationQuality;
	private int pendingGoldenCoreTier = 9;
	private int pendingBreakthroughGoldenCoreTier = 9;
	private int selectedBreakthroughTreasureType;
	private int selectedBreakthroughPillType;
	private int activeTechniqueLoadoutPreset;
	private int selectedTechniqueLoadoutSlot;
	private readonly CultivationAbility[,] techniqueLoadouts =
		new CultivationAbility[
			TechniqueLoadoutPresetCount, MaximumTechniqueLoadoutSlots];
	private readonly int[] successfulBreakthroughTreasures =
		new int[TotalRealms];
	private readonly int[] abilityExperience = new int[(int)CultivationAbility.Count];
	private readonly int[] abilityLevels = new int[(int)CultivationAbility.Count];

	public int Qi { get; private set; }
	public int QiExp { get; private set; }
	public int RealmIndex { get; private set; }
	public int Stage { get; private set; }
	public FoundationQuality FoundationQuality => foundationQuality;
	public int GoldenCoreTier => goldenCoreTier;
	public FoundationQuality PendingFoundationQuality =>
		pendingFoundationQuality;
	public int PendingGoldenCoreTier => pendingGoldenCoreTier;
	public int SelectedBreakthroughTreasureType =>
		selectedBreakthroughTreasureType;
	public int SelectedBreakthroughPillType =>
		GetEffectiveSelectedBreakthroughPillType();
	public float FoundationStatMultiplier => foundationQuality switch
	{
		FoundationQuality.Stable => 1.15f,
		FoundationQuality.Perfect => 1.35f,
		FoundationQuality.Heavenly => 1.60f,
		_ => 1f
	};
	public float FoundationQiGatheringMultiplier => foundationQuality switch
	{
		FoundationQuality.Stable => 1.07f,
		FoundationQuality.Perfect => 1.15f,
		FoundationQuality.Heavenly => 1.25f,
		_ => 1f
	};
	public float GoldenCoreStatMultiplier => goldenCoreTier switch
	{
		8 => 1.05f, 7 => 1.10f, 6 => 1.16f, 5 => 1.23f,
		4 => 1.31f, 3 => 1.40f, 2 => 1.50f, 1 => 1.65f,
		_ => 1f
	};
	public float GoldenCoreQiGatheringMultiplier => goldenCoreTier switch
	{
		8 => 1.03f, 7 => 1.06f, 6 => 1.09f, 5 => 1.12f,
		4 => 1.15f, 3 => 1.18f, 2 => 1.22f, 1 => 1.27f,
		_ => 1f
	};
	public float BreakthroughGradeQiGatheringMultiplier =>
		(RealmIndex >= 2 ? FoundationQiGatheringMultiplier : 1f)
		* (RealmIndex >= 3 ? GoldenCoreQiGatheringMultiplier : 1f);
	public int GlobalStageIndex => RealmIndex * StagesPerRealm + Stage - 1;
	public bool IsMeditating { get; private set; }
	public bool IsFlyingWithQi { get; private set; }
	public bool QiFlightEnabled { get; private set; }
	public bool QiProtectionEnabled { get; private set; }
	public bool QiSenseEnabled { get; private set; }
	public bool SpiritualPressureEnabled { get; private set; }
	public bool NightVisionEnabled { get; private set; }
	public int BaseMaxQi => QiExp;
	public int BurnedQiCapacityBps => burnedQiCapacityBps;
	public float BurnedQiCapacityPercent => burnedQiCapacityBps / 100f;
	public int BurnedMaxQi =>
		(int)MathF.Ceiling(BaseMaxQi * burnedQiCapacityBps / 10000f);
	public bool HasBurnedQi => burnedQiCapacityBps > 0;
	public bool QiBurningEnabled => qiBurningEnabled;
	public bool HasQiDeviation => qiDeviationTimer > 0;
	public int QiDeviationTicksRemaining => qiDeviationTimer;
	public int QiDeviationSecondsRemaining =>
		(int)MathF.Ceiling(qiDeviationTimer / 60f);
	public float QiBurningDamageBonusPercent =>
		MathHelper.Lerp(30f, 45f,
			(GetAbilityLevel(CultivationAbility.QiBurning) - 1f)
				/ (CultivationAbilityInfo.MaxLevel - 1f));
	public float QiBurningAttackSpeedBonusPercent =>
		MathHelper.Lerp(10f, 20f,
			(GetAbilityLevel(CultivationAbility.QiBurning) - 1f)
				/ (CultivationAbilityInfo.MaxLevel - 1f));
	public int MaximumBurnedQiBps =>
		Math.Clamp(CultivationServerConfig.Instance?.MaximumBurnedQiPercent
			?? DefaultMaximumBurnedQiBps / 100, 20, 80) * 100;
	public int HeartDemonPoints => heartDemonPoints;
	public int BreakthroughFailuresTowardHeartDemon =>
		breakthroughFailuresTowardHeartDemon;
	public int DeathsTowardHeartDemon => deathsTowardHeartDemon;
	public bool HeartDemonTrialActive => heartDemonTrialActive;
	public int HeartDemonTrialNpcIndex => heartDemonTrialNpcIndex;
	public int HeartDemonTrialCooldown => heartDemonTrialCooldown;
	public bool IsAwaitingHeartDemonTrialConfirmation =>
		awaitingHeartDemonTrialConfirmation;
	public float HeartDemonBreakthroughPenalty =>
		heartDemonPoints * 2f * HeartDemonPenaltyStrength;
	public float HeartDemonCultivationGainMultiplier =>
		Math.Max(0.1f, 1f - heartDemonPoints * 0.02f
			* HeartDemonPenaltyStrength);
	private float HeartDemonPenaltyStrength =>
		Math.Clamp(CultivationServerConfig.Instance
			?.HeartDemonPenaltyStrengthPercent ?? 100, 0, 200) / 100f;
	public int SpiritualRainCooldown => spiritualRainCooldown;
	public bool IsAbilityWheelOpen { get; private set; }
	public bool IsAbilityTreeOpen { get; private set; }
	public bool HasReceivedCultivatorManual { get; private set; }
	public int EquipmentPassiveQiBonus { get; set; }
	public int EquipmentMeditationQiBonus { get; set; }
	public int NearbySpiritCrystalCount { get; private set; }
	public int SpiritualQiZoneTier => Math.Clamp(
		SpiritualQiConcentration.GetLevel(NearbySpiritCrystalCount),
		0, SpiritualQiConcentration.MaximumLevel);
	public int SpiritualQiZoneBonusPercent => SpiritualQiZoneTier * 100;
	public bool IsInSpiritualQiZone => SpiritualQiZoneTier > 0;
	public float SpiritualQiZoneMultiplier => 1f + SpiritualQiZoneBonusPercent / 100f;
	public float PermanentFormationQiMultiplier =>
		Player.HasBuff<PermanentFormationRelayGatheringBuff>() ? 2.25f
		: Player.HasBuff<PermanentFormationGatheringBuff>() ? 1.5f : 1f;
	public float MeditationQiPerSecond =>
		(MeditationQiGainByRealm[RealmIndex] + EquipmentMeditationQiBonus)
		* SpiritualQiZoneMultiplier * PermanentFormationQiMultiplier
		* GetAbilityPowerMultiplier(CultivationAbility.Meditation, 0.05f)
		* Player.GetModPlayer<SpiritualRootPlayer>().CultivationGainMultiplier
		* Player.GetModPlayer<SpiritualRootPlayer>().BiomeMeditationMultiplier
		* BreakthroughGradeQiGatheringMultiplier
		* (HasQiDeviation ? 0.5f : 1f);
	public float PassiveQiRecoveryPerSecond =>
		(PassiveQiRecoveryByRealm[RealmIndex] + EquipmentPassiveQiBonus)
		* SpiritualQiZoneMultiplier * PermanentFormationQiMultiplier
		* (1.10f + (GetAbilityLevel(CultivationAbility.SpiritBreathing) - 1) * 0.03f)
		* BreakthroughGradeQiGatheringMultiplier
		* (RealmIndex >= 2 && foundationQuality == FoundationQuality.Heavenly
			? 1.10f : 1f)
		* (HasQiDeviation ? 0.5f : 1f);
	public int CurrentRealmThreshold =>
		GetGlobalStageThreshold(RealmIndex * StagesPerRealm);
	public bool IsAtMaxRealm => RealmIndex >= TotalRealms - 1;
	public bool IsCultivationMaxed => GlobalStageIndex >= MaxGlobalStageIndex;
	public int NextRealmThreshold => GetGlobalStageThreshold(
		Math.Min((RealmIndex + 1) * StagesPerRealm, MaxGlobalStageIndex));
	public int CurrentStageThreshold => GetGlobalStageThreshold(GlobalStageIndex);
	public int NextStageThreshold => GetGlobalStageThreshold(Math.Min(GlobalStageIndex + 1, MaxGlobalStageIndex));
	public int QiRequiredForNextStage => NextStageThreshold - CurrentStageThreshold;
	public int MaxQi => Math.Max(0, BaseMaxQi - BurnedMaxQi);
	public bool CanUseQiProtection => QiProtectionEnabled && RealmIndex >= 2;
	public bool HasUnlockedQiProtection => RealmIndex >= 2;
	public bool HasUnlockedQiSense => RealmIndex >= 1;
	public bool CanUseQiSense => QiSenseEnabled && HasUnlockedQiSense && Qi > 0;
	public bool IsAwaitingTribulationConfirmation =>
		awaitingTribulationConfirmation && pendingTribulationRealm >= TribulationStartingRealm;
	public bool IsAwaitingRealmBreakthroughConfirmation =>
		pendingRealmBreakthroughConfirmation is >= 1 and < TotalRealms;
	public int PendingRealmBreakthroughTargetRealm =>
		IsAwaitingRealmBreakthroughConfirmation
			? pendingRealmBreakthroughConfirmation
			: NextBreakthroughTargetRealm;
	public string PendingRealmBreakthroughTargetName =>
		GetRealmName(PendingRealmBreakthroughTargetRealm);
	public float PendingRealmBreakthroughBaseChance =>
		GetBaseRealmBreakthroughChance(PendingRealmBreakthroughTargetRealm);
	public float PendingRealmBreakthroughRootModifier =>
		Player.GetModPlayer<SpiritualRootPlayer>().BreakthroughChanceModifier;
	public float PendingRealmBreakthroughPillModifier =>
		GetSelectedBreakthroughPillChanceBonus();
	public float PendingBreakthroughGradeChanceModifier =>
		GetBreakthroughGradeChanceModifier(
			PendingRealmBreakthroughTargetRealm);
	public float PendingBreakthroughStatMultiplier =>
		PendingRealmBreakthroughTargetRealm switch
		{
			2 => pendingFoundationQuality switch
			{
				FoundationQuality.Stable => 1.15f,
				FoundationQuality.Perfect => 1.35f,
				FoundationQuality.Heavenly => 1.60f,
				_ => 1f
			},
			3 => pendingGoldenCoreTier switch
			{
				8 => 1.05f, 7 => 1.10f, 6 => 1.16f,
				5 => 1.23f, 4 => 1.31f, 3 => 1.40f,
				2 => 1.50f, 1 => 1.65f, _ => 1f
			},
			_ => 1f
		};
	public float PendingBreakthroughQiGatheringBonusPercent =>
		PendingRealmBreakthroughTargetRealm switch
		{
			2 => pendingFoundationQuality switch
			{
				FoundationQuality.Stable => 7f,
				FoundationQuality.Perfect => 15f,
				FoundationQuality.Heavenly => 25f,
				_ => 0f
			},
			3 => pendingGoldenCoreTier switch
			{
				8 => 3f, 7 => 6f, 6 => 9f, 5 => 12f,
				4 => 15f, 3 => 18f, 2 => 22f, 1 => 27f,
				_ => 0f
			},
			_ => 0f
		};
	public float PendingRealmBreakthroughChance => Math.Clamp(
		PendingRealmBreakthroughBaseChance
			+ PendingRealmBreakthroughRootModifier
			+ PendingRealmBreakthroughPillModifier
			+ PendingBreakthroughGradeChanceModifier
			- HeartDemonBreakthroughPenalty,
		10f, 95f);
	public bool CanConfirmRealmBreakthrough =>
		!HasBurnedQi && PendingRealmBreakthroughTargetRealm switch
		{
			2 => pendingFoundationQuality switch
			{
				FoundationQuality.Inferior or FoundationQuality.Stable =>
					HasSelectedBreakthroughTreasure
						|| HasSelectedBreakthroughPill,
				FoundationQuality.Perfect =>
					HasSelectedBreakthroughTreasure,
				FoundationQuality.Heavenly =>
					HasSelectedBreakthroughTreasure
						&& HasSelectedBreakthroughPill,
				_ => false
			},
			3 => HasSelectedBreakthroughTreasure
				&& (pendingGoldenCoreTier != 1
					|| HasSelectedBreakthroughPill),
			_ => true
		};
	public bool HasSelectedBreakthroughTreasure =>
		IsHeavenlyTreasureType(selectedBreakthroughTreasureType)
		&& Player.CountItem(selectedBreakthroughTreasureType) > 0;
	public bool HasSelectedBreakthroughPill =>
		CanConsumeSelectedBreakthroughPill();
	public string PendingTribulationRealmName => pendingTribulationRealm >= 0
		? GetRealmName(pendingTribulationRealm)
		: string.Empty;
	public int PendingTribulationStrikeCount => pendingTribulationRealm >= TribulationStartingRealm
		? GetTribulationStrikeCount(pendingTribulationRealm)
		: 0;
	public float PendingTribulationPowerBonusPercent =>
		pendingTribulationRealm >= TribulationStartingRealm
			? (GetTribulationPowerMultiplier(pendingTribulationRealm) - 1f)
				* 100f
			: 0f;
	public int PendingTribulationGoldenCoreTier =>
		pendingTribulationRealm >= TribulationStartingRealm
			? GetTribulationGoldenCoreTier(pendingTribulationRealm)
			: 9;
	public bool NextBreakthroughRequiresTribulation =>
		!IsCultivationMaxed && Stage == StagesPerRealm && RealmIndex + 1 >= TribulationStartingRealm;
	public int NextBreakthroughTargetRealm => Math.Min(RealmIndex + (Stage == StagesPerRealm ? 1 : 0),
		TotalRealms - 1);
	public int NextBreakthroughTargetStage => Stage == StagesPerRealm ? 1 : Stage + 1;
	public int NextBreakthroughTribulationStrikes => NextBreakthroughRequiresTribulation
		? GetTribulationStrikeCount(NextBreakthroughTargetRealm)
		: 0;
	public bool NextAdvancementIsRealmBreakthrough =>
		!IsCultivationMaxed && Stage == StagesPerRealm;
	public bool NextBreakthroughRequiresHeavenlyTreasures =>
		NextAdvancementIsRealmBreakthrough
		&& NextBreakthroughTargetRealm is 2 or 3;
	public int HeavenlyEyeEssenceCount =>
		Player.CountItem(ModContent.ItemType<HeavenlyEyeEssence>());
	public int HeavenlyRoyalNectarCount =>
		Player.CountItem(ModContent.ItemType<HeavenlyRoyalNectar>());
	public int HeavenlyBoneMarrowCount =>
		Player.CountItem(ModContent.ItemType<HeavenlyBoneMarrow>());
	public bool HasAnyHeavenlyTreasure =>
		HeavenlyEyeEssenceCount + HeavenlyRoyalNectarCount
			+ HeavenlyBoneMarrowCount >= HeavenlyTreasureRequired;
	public bool HasFoundationBreakthroughCatalyst =>
		Player.GetModPlayer<AlchemyPillEffectPlayer>().FoundationAscension
		|| HasAnyHeavenlyTreasure;
	public bool HasGoldenCoreHeavenlyTreasures => HasAnyHeavenlyTreasure;
	public int HeavenlyEyeImprints => heavenlyEyeImprints;
	public int HeavenlyRoyalNectarImprints => heavenlyRoyalNectarImprints;
	public int HeavenlyBoneMarrowImprints => heavenlyBoneMarrowImprints;
	public float HeavenlyEyeQiSenseRangeMultiplier =>
		1f + heavenlyEyeImprints * 0.10f;
	public int RealmBreakthroughAttempts => realmBreakthroughAttempts;
	public int RealmBreakthroughSuccesses => realmBreakthroughSuccesses;
	public int RealmBreakthroughFailures => realmBreakthroughFailures;
	public int BreakthroughPillsConsumed => breakthroughPillsConsumed;
	public float NextRealmBreakthroughBaseChance =>
		GetBaseRealmBreakthroughChance(NextBreakthroughTargetRealm);
	public float NextRealmBreakthroughRootModifier =>
		Player.GetModPlayer<SpiritualRootPlayer>().BreakthroughChanceModifier;
	public float NextRealmBreakthroughPillModifier =>
		Player.GetModPlayer<AlchemyPillEffectPlayer>()
			.GetBreakthroughChanceBonus(NextBreakthroughTargetRealm);
	public float NextRealmBreakthroughChance => Math.Clamp(
		NextRealmBreakthroughBaseChance
			+ NextRealmBreakthroughRootModifier
			+ NextRealmBreakthroughPillModifier
			+ GetDefaultBreakthroughGradeChanceModifier(
				NextBreakthroughTargetRealm)
			- HeartDemonBreakthroughPenalty,
		10f, 95f);
	public int ActiveTechniqueLoadoutPreset =>
		activeTechniqueLoadoutPreset;
	public int SelectedTechniqueLoadoutSlot =>
		selectedTechniqueLoadoutSlot;
	public CultivationAbility SelectedTechnique =>
		GetActiveTechniqueLoadoutAbility(selectedTechniqueLoadoutSlot);
	public int AvailableTechniqueLoadoutPresets =>
		RealmIndex >= 2 ? TechniqueLoadoutPresetCount : 1;
	public int TechniqueLoadoutSlotCount => RealmIndex switch
	{
		<= 0 => 2,
		1 => 3,
		2 => 4,
		3 => 5,
		_ => MaximumTechniqueLoadoutSlots
	};
	public bool IsAbilityUnlocked(CultivationAbility ability) =>
		RealmIndex >= CultivationAbilityInfo.RequiredRealm(ability)
		&& Player.GetModPlayer<SectPlayer>().HasUnlockedTechnique(ability);
	public CultivationAbility GetTechniqueLoadoutAbility(
		int preset, int slot)
	{
		if (preset < 0 || preset >= TechniqueLoadoutPresetCount
			|| slot < 0 || slot >= MaximumTechniqueLoadoutSlots)
		{
			return CultivationAbility.Count;
		}
		return techniqueLoadouts[preset, slot];
	}
	public CultivationAbility GetActiveTechniqueLoadoutAbility(int slot) =>
		GetTechniqueLoadoutAbility(activeTechniqueLoadoutPreset, slot);
	public bool IsTechniqueEquipped(CultivationAbility ability)
	{
		if (!CultivationAbilityInfo.IsTechniqueLoadoutAbility(ability))
			return true;
		for (int slot = 0; slot < TechniqueLoadoutSlotCount; slot++)
		{
			if (techniqueLoadouts[activeTechniqueLoadoutPreset, slot]
				== ability)
			{
				return true;
			}
		}
		return false;
	}
	public int GetAbilityLevel(CultivationAbility ability) => abilityLevels[(int)ability];
	public int GetAbilityExperience(CultivationAbility ability) => abilityExperience[(int)ability];
	public int GetAbilityExperienceRequired(CultivationAbility ability) =>
		GetAbilityLevel(ability) >= CultivationAbilityInfo.MaxLevel
			? 0
			: CultivationAbilityInfo.ExperienceForNextLevel(GetAbilityLevel(ability));
	public float GetAbilityPowerMultiplier(CultivationAbility ability, float bonusPerLevel)
	{
		float multiplier = 1f + (GetAbilityLevel(ability) - 1) * bonusPerLevel;
		AlchemyPillEffectPlayer pillEffects = Player.GetModPlayer<AlchemyPillEffectPlayer>();
		if (pillEffects.NascentSoulAwakening
			&& ability is CultivationAbility.NascentTeleport or CultivationAbility.SpiritualPressure)
			multiplier *= 1.25f;
		SpiritualElement elements = CultivationAbilityInfo.GetSpiritualElements(ability);
		if (elements != SpiritualElement.None)
		{
			ElementalCultivationPlayer elemental =
				Player.GetModPlayer<ElementalCultivationPlayer>();
			multiplier *= elemental.GetPowerMultiplier(elements)
				* (1f + elemental.GetAffinity(elements) * 0.0015f);
		}
		return multiplier;
	}

	public float GetAbilityCooldownMultiplier(CultivationAbility ability)
	{
		float multiplier = Math.Max(0.55f, 1f - (GetAbilityLevel(ability) - 1) * 0.025f);
		AlchemyPillEffectPlayer pillEffects = Player.GetModPlayer<AlchemyPillEffectPlayer>();
		if (pillEffects.VoidInsight
			&& ability is CultivationAbility.FlameStep or CultivationAbility.NascentTeleport)
			multiplier *= 0.75f;
		return multiplier;
	}

	public bool TrySetTechniqueLoadoutSlot(
		int preset, int slot, CultivationAbility ability)
	{
		if (preset < 0 || preset >= AvailableTechniqueLoadoutPresets
			|| slot < 0 || slot >= TechniqueLoadoutSlotCount)
		{
			return false;
		}
		if (ability != CultivationAbility.Count
			&& (!CultivationAbilityInfo.IsTechniqueLoadoutAbility(ability)
				|| !IsAbilityUnlocked(ability)))
		{
			return false;
		}

		if (ability != CultivationAbility.Count)
		{
			for (int otherSlot = 0;
				otherSlot < TechniqueLoadoutSlotCount; otherSlot++)
			{
				if (otherSlot == slot
					|| techniqueLoadouts[preset, otherSlot] != ability)
				continue;
				techniqueLoadouts[preset, otherSlot] =
					techniqueLoadouts[preset, slot];
				break;
			}
		}
		techniqueLoadouts[preset, slot] = ability;
		NormalizeTechniqueLoadout();
		SyncTechniqueLoadout();
		return true;
	}

	public bool TrySelectTechniqueLoadoutPreset(int preset)
	{
		if (preset < 0 || preset >= AvailableTechniqueLoadoutPresets)
			return false;
		activeTechniqueLoadoutPreset = preset;
		selectedTechniqueLoadoutSlot = Math.Clamp(
			selectedTechniqueLoadoutSlot, 0,
			TechniqueLoadoutSlotCount - 1);
		NormalizeTechniqueLoadout();
		SyncTechniqueLoadout();
		return true;
	}

	public bool TrySelectActiveTechniqueSlot(int slot)
	{
		if (slot < 0 || slot >= TechniqueLoadoutSlotCount
			|| GetActiveTechniqueLoadoutAbility(slot)
				== CultivationAbility.Count)
		{
			return false;
		}
		selectedTechniqueLoadoutSlot = slot;
		return true;
	}

	internal byte[] GetTechniqueLoadoutSnapshot()
	{
		byte[] snapshot = new byte[
			TechniqueLoadoutPresetCount * MaximumTechniqueLoadoutSlots];
		for (int preset = 0; preset < TechniqueLoadoutPresetCount; preset++)
		{
			for (int slot = 0; slot < MaximumTechniqueLoadoutSlots; slot++)
			{
				snapshot[preset * MaximumTechniqueLoadoutSlots + slot] =
					(byte)techniqueLoadouts[preset, slot];
			}
		}
		return snapshot;
	}

	internal void ApplyTechniqueLoadoutState(
		int preset, byte[] snapshot, bool validateUnlocks)
	{
		activeTechniqueLoadoutPreset = Math.Clamp(
			preset, 0, AvailableTechniqueLoadoutPresets - 1);
		for (int loadout = 0;
			loadout < TechniqueLoadoutPresetCount; loadout++)
		{
			for (int slot = 0; slot < MaximumTechniqueLoadoutSlots; slot++)
			{
				int index = loadout * MaximumTechniqueLoadoutSlots + slot;
				CultivationAbility ability = index < snapshot.Length
					? (CultivationAbility)snapshot[index]
					: CultivationAbility.Count;
				bool valid = slot < TechniqueLoadoutSlotCount
					&& CultivationAbilityInfo
						.IsTechniqueLoadoutAbility(ability)
					&& (!validateUnlocks || IsAbilityUnlocked(ability));
				if (valid)
				techniqueLoadouts[loadout, slot] = ability;
				else
					techniqueLoadouts[loadout, slot] =
						CultivationAbility.Count;
			}
			RemoveDuplicateLoadoutAbilities(loadout);
		}
		NormalizeTechniqueLoadout();
	}

	private void RemoveDuplicateLoadoutAbilities(int preset)
	{
		for (int slot = 0; slot < MaximumTechniqueLoadoutSlots; slot++)
		{
			CultivationAbility ability = techniqueLoadouts[preset, slot];
			if (ability == CultivationAbility.Count)
				continue;
			for (int previous = 0; previous < slot; previous++)
			{
				if (techniqueLoadouts[preset, previous] != ability)
					continue;
				techniqueLoadouts[preset, slot] =
					CultivationAbility.Count;
				break;
			}
		}
	}

	private void ResetTechniqueLoadouts()
	{
		activeTechniqueLoadoutPreset = 0;
		selectedTechniqueLoadoutSlot = 0;
		for (int preset = 0; preset < TechniqueLoadoutPresetCount; preset++)
		{
			for (int slot = 0; slot < MaximumTechniqueLoadoutSlots; slot++)
				techniqueLoadouts[preset, slot] = CultivationAbility.Count;
		}
	}

	private void FillEmptyTechniqueLoadoutSlots()
	{
		for (int preset = 0; preset < TechniqueLoadoutPresetCount; preset++)
		{
			foreach (CultivationAbility ability in DefaultLoadoutOrder)
			{
				if (!IsAbilityUnlocked(ability))
					continue;
				bool alreadyEquipped = false;
				int emptySlot = -1;
				for (int slot = 0; slot < TechniqueLoadoutSlotCount; slot++)
				{
					if (techniqueLoadouts[preset, slot] == ability)
						alreadyEquipped = true;
					if (emptySlot < 0
						&& techniqueLoadouts[preset, slot]
							== CultivationAbility.Count)
					{
						emptySlot = slot;
					}
				}
				if (!alreadyEquipped && emptySlot >= 0)
					techniqueLoadouts[preset, emptySlot] = ability;
			}
		}
	}

	private void NormalizeTechniqueLoadout()
	{
		activeTechniqueLoadoutPreset = Math.Clamp(
			activeTechniqueLoadoutPreset,
			0, AvailableTechniqueLoadoutPresets - 1);
		selectedTechniqueLoadoutSlot = Math.Clamp(
			selectedTechniqueLoadoutSlot,
			0, TechniqueLoadoutSlotCount - 1);
		if (SelectedTechnique == CultivationAbility.Count)
		{
			for (int slot = 0; slot < TechniqueLoadoutSlotCount; slot++)
			{
				if (GetActiveTechniqueLoadoutAbility(slot)
					== CultivationAbility.Count)
					continue;
				selectedTechniqueLoadoutSlot = slot;
				break;
			}
		}
		if (!IsTechniqueEquipped(CultivationAbility.QiFlight))
		{
			QiFlightEnabled = false;
			IsFlyingWithQi = false;
			flightQiTimer = 0;
		}
		if (!IsTechniqueEquipped(CultivationAbility.QiSense))
			SetQiSenseEnabled(false);
		if (!IsTechniqueEquipped(CultivationAbility.QiProtection))
			SetQiProtectionEnabled(false);
		if (!IsTechniqueEquipped(CultivationAbility.SpiritualPressure))
			DisableSpiritualPressure(showMessage: false);
		if (!IsTechniqueEquipped(CultivationAbility.NightVision))
			DisableNightVision(showMessage: false);
		if (qiBurningEnabled
			&& !IsTechniqueEquipped(CultivationAbility.QiBurning))
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				qiBurningEnabled = false;
			else
				DisableQiBurning(applyDeviation: true, showMessage: true);
		}
	}

	private void SyncTechniqueLoadout()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient
			&& Player.whoAmI == Main.myPlayer)
		{
			Xianxia.SendTechniqueLoadoutRequest(
				activeTechniqueLoadoutPreset,
				GetTechniqueLoadoutSnapshot());
		}
		else if (Main.netMode == NetmodeID.Server)
		{
			Xianxia.SendTechniqueLoadoutState(
				Player.whoAmI, this);
		}
	}

	public override void Initialize()
	{
		Qi = 0;
		QiExp = 0;
		RealmIndex = 0;
		Stage = 1;
		meditationTimer = 0;
		passiveQiRecoveryTimer = 0;
		spiritualQiScanTimer = 60;
		meditationQiGainRemainder = 0f;
		meditationQiRecoveryRemainder = 0f;
		passiveQiGainRemainder = 0f;
		flightQiTimer = 0;
		fireballCooldown = 0;
		qiSenseCostTimer = 0;
		qiPalmCooldown = 0;
		flameStepCooldown = 0;
		nascentTeleportCooldown = 0;
		spiritualPressureQiTimer = 0;
		nightVisionQiTimer = 0;
		qiBurningEnabled = false;
		qiBurningPulseTimer = 0;
		burnedQiCapacityBps = 0;
		qiDeviationTimer = 0;
		qiBurningCombatTimer = 0;
		qiBurningExperienceWindowTimer = 0;
		qiBurningExperienceThisWindow = 0;
		burnedQiMeditationRepairTimer = 0;
		heartDemonPoints = 0;
		breakthroughFailuresTowardHeartDemon = 0;
		deathsTowardHeartDemon = 0;
		heartDemonTrialActive = false;
		heartDemonTrialNpcIndex = -1;
		heartDemonTrialCooldown = 0;
		awaitingHeartDemonTrialConfirmation = false;
		heartDemonVisualTimer = 0;
		suppressRiskTracking = false;
		spiritualRainCooldown = 0;
		nascentSoulRegenerationTrainingTimer = 0;
		qiProtectionDotQiAccumulator = 0f;
		qiProtectionDotVisualCooldown = 0;
		drowningProtectionTimer = 0;
		stillnessWarningCooldown = 0;
		breakthroughWarningCooldown = 0;
		breakthroughEffectTimer = 0;
		breakthroughEffectRealm = 0;
		realmBreakthroughEffect = false;
		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;
		pendingTribulationRealm = -1;
		deferredTribulationRealm = -1;
		pendingRealmBreakthroughConfirmation = -1;
		confirmedRealmBreakthrough = -1;
		awaitingTribulationConfirmation = false;
		IsMeditating = false;
		meditationToggleRequested = false;
		appliedCultivationRequirementMultiplier =
			GetConfiguredCultivationRequirementMultiplier();
		IsFlyingWithQi = false;
		QiFlightEnabled = false;
		QiProtectionEnabled = false;
		QiSenseEnabled = false;
		SpiritualPressureEnabled = false;
		NightVisionEnabled = false;
		IsAbilityWheelOpen = false;
		IsAbilityTreeOpen = false;
		for (int i = 0; i < abilityLevels.Length; i++)
		{
			abilityLevels[i] = 1;
			abilityExperience[i] = 0;
		}
		NearbySpiritCrystalCount = 0;
		HasReceivedCultivatorManual = false;
		heavenlyEyeImprints = 0;
		heavenlyRoyalNectarImprints = 0;
		heavenlyBoneMarrowImprints = 0;
		realmBreakthroughAttempts = 0;
		realmBreakthroughSuccesses = 0;
		realmBreakthroughFailures = 0;
		breakthroughPillsConsumed = 0;
		successfulBreakthroughPillMask = 0;
		successfulBreakthroughRecordedMask = 0;
		pendingBreakthroughTreasure = 0;
		pendingBreakthroughUsedPill = false;
		pendingBreakthroughGoldenCoreTier = 9;
		foundationQuality = FoundationQuality.Inferior;
		goldenCoreTier = 9;
		pendingFoundationQuality = FoundationQuality.Inferior;
		pendingGoldenCoreTier = 9;
		selectedBreakthroughTreasureType = 0;
		selectedBreakthroughPillType = 0;
		Array.Clear(successfulBreakthroughTreasures);
		ResetTechniqueLoadouts();
	}

	public override void SaveData(TagCompound tag)
	{
		tag["qi"] = Qi;
		tag["qiExp"] = QiExp;
		tag["burnedQiCapacityBps"] = burnedQiCapacityBps;
		tag["qiDeviationTimer"] = qiBurningEnabled
			? Math.Max(qiDeviationTimer, GetQiDeviationDuration())
			: qiDeviationTimer;
		tag["heartDemonPoints"] = heartDemonPoints;
		tag["heartDemonBreakthroughProgress"] =
			breakthroughFailuresTowardHeartDemon;
		tag["heartDemonDeathProgress"] = deathsTowardHeartDemon;
		tag["heartDemonTrialCooldown"] = heartDemonTrialCooldown;
		tag["foundationQuality"] = (byte)foundationQuality;
		tag["goldenCoreTier"] = goldenCoreTier;
		if (pendingTribulationRealm == 3 || deferredTribulationRealm == 3)
			tag["pendingBreakthroughGoldenCoreTier"] =
				pendingBreakthroughGoldenCoreTier;
		tag["progressionVersion"] = ProgressionVersion;
		tag["cultivationRequirementMultiplier"] =
			appliedCultivationRequirementMultiplier;
		tag["qiProtectionEnabled"] = QiProtectionEnabled;
		tag["qiSenseEnabled"] = QiSenseEnabled;
		tag["hasReceivedCultivatorManual"] = HasReceivedCultivatorManual;
		tag["heavenlyEyeImprints"] = heavenlyEyeImprints;
		tag["heavenlyRoyalNectarImprints"] = heavenlyRoyalNectarImprints;
		tag["heavenlyBoneMarrowImprints"] = heavenlyBoneMarrowImprints;
		tag["realmBreakthroughAttempts"] = realmBreakthroughAttempts;
		tag["realmBreakthroughSuccesses"] = realmBreakthroughSuccesses;
		tag["realmBreakthroughFailures"] = realmBreakthroughFailures;
		tag["breakthroughPillsConsumed"] = breakthroughPillsConsumed;
		tag["successfulBreakthroughPillMask"] = successfulBreakthroughPillMask;
		tag["successfulBreakthroughRecordedMask"] =
			successfulBreakthroughRecordedMask;
		tag["successfulBreakthroughTreasures"] =
			new System.Collections.Generic.List<int>(
				successfulBreakthroughTreasures);
		if (pendingBreakthroughTreasure > 0)
			tag["pendingBreakthroughTreasure"] = pendingBreakthroughTreasure;
		if (pendingBreakthroughUsedPill)
			tag["pendingBreakthroughUsedPill"] = true;
		tag["abilityExperience"] = new System.Collections.Generic.List<int>(abilityExperience);
		tag["abilityLevels"] = new System.Collections.Generic.List<int>(abilityLevels);
		tag["activeTechniqueLoadoutPreset"] =
			activeTechniqueLoadoutPreset;
		tag["selectedTechniqueLoadoutSlot"] =
			selectedTechniqueLoadoutSlot;
		tag["techniqueLoadoutVersion"] = TechniqueLoadoutSaveVersion;
		byte[] loadoutSnapshot = GetTechniqueLoadoutSnapshot();
		tag["techniqueLoadouts"] =
			new System.Collections.Generic.List<int>(
				Array.ConvertAll(loadoutSnapshot, value => (int)value));
		if (pendingTribulationRealm >= TribulationStartingRealm)
		{
			tag["pendingTribulationRealm"] = pendingTribulationRealm;
			tag["awaitingTribulationConfirmation"] = awaitingTribulationConfirmation;
		}
		if (deferredTribulationRealm >= TribulationStartingRealm)
			tag["deferredTribulationRealm"] = deferredTribulationRealm;
		if (IsAwaitingRealmBreakthroughConfirmation)
		{
			tag["pendingRealmBreakthroughConfirmation"] =
				pendingRealmBreakthroughConfirmation;
		}
	}

	public override void LoadData(TagCompound tag)
	{
		int savedQi = tag.GetInt("qi");
		int savedQiExp = tag.ContainsKey("qiExp") ? tag.GetInt("qiExp") : savedQi;
		int savedProgressionVersion = tag.ContainsKey("progressionVersion")
			? tag.GetInt("progressionVersion")
			: 1;
		int configuredRequirementMultiplier =
			GetConfiguredCultivationRequirementMultiplier();
		int savedRequirementMultiplier =
			tag.ContainsKey("cultivationRequirementMultiplier")
				? Math.Clamp(tag.GetInt("cultivationRequirementMultiplier"), 1, 10)
				: 1;
		if (savedProgressionVersion < ProgressionVersion)
		{
			MigrateLegacyProgression(savedQiExp, savedQi, out savedQiExp, out savedQi);
			savedRequirementMultiplier = configuredRequirementMultiplier;
		}
		else if (savedRequirementMultiplier != configuredRequirementMultiplier)
		{
			RebaseProgressionForRequirementMultiplier(ref savedQiExp, ref savedQi,
				savedRequirementMultiplier, configuredRequirementMultiplier);
		}
		appliedCultivationRequirementMultiplier = configuredRequirementMultiplier;

		burnedQiCapacityBps = Math.Clamp(
			tag.GetInt("burnedQiCapacityBps"), 0, MaximumBurnedQiBps);
		qiDeviationTimer = Math.Clamp(tag.GetInt("qiDeviationTimer"),
			0, 3 * 60 * 60);
		qiBurningEnabled = false;
		qiBurningPulseTimer = 0;
		heartDemonPoints = Math.Clamp(
			tag.GetInt("heartDemonPoints"), 0, MaximumHeartDemonPoints);
		breakthroughFailuresTowardHeartDemon = Math.Clamp(
			tag.GetInt("heartDemonBreakthroughProgress"),
			0, BreakthroughFailuresPerHeartDemonPoint - 1);
		deathsTowardHeartDemon = Math.Clamp(
			tag.GetInt("heartDemonDeathProgress"),
			0, DeathsPerHeartDemonPoint - 1);
		heartDemonTrialCooldown = Math.Max(
			0, tag.GetInt("heartDemonTrialCooldown"));
		foundationQuality = (FoundationQuality)Math.Clamp(
			tag.GetByte("foundationQuality"),
			(byte)FoundationQuality.Inferior,
			(byte)FoundationQuality.Heavenly);
		goldenCoreTier = Math.Clamp(
			tag.ContainsKey("goldenCoreTier")
				? tag.GetInt("goldenCoreTier") : 9, 1, 9);
		pendingBreakthroughGoldenCoreTier = Math.Clamp(
			tag.ContainsKey("pendingBreakthroughGoldenCoreTier")
				? tag.GetInt("pendingBreakthroughGoldenCoreTier") : 9,
			1, 9);
		pendingFoundationQuality = FoundationQuality.Inferior;
		pendingGoldenCoreTier = 9;
		selectedBreakthroughTreasureType = 0;
		selectedBreakthroughPillType = 0;
		heartDemonTrialActive = false;
		heartDemonTrialNpcIndex = -1;
		awaitingHeartDemonTrialConfirmation = false;

		QiExp = savedQiExp;
		QiExp = Math.Clamp(QiExp, 0, GetGlobalStageThreshold(MaxGlobalStageIndex));
		Qi = Math.Clamp(savedQi, 0, MaxQi);
		QiProtectionEnabled = tag.GetBool("qiProtectionEnabled");
		QiSenseEnabled = tag.GetBool("qiSenseEnabled");
		HasReceivedCultivatorManual = tag.GetBool("hasReceivedCultivatorManual");
		heavenlyEyeImprints = Math.Clamp(tag.GetInt("heavenlyEyeImprints"), 0, 10);
		heavenlyRoyalNectarImprints =
			Math.Clamp(tag.GetInt("heavenlyRoyalNectarImprints"), 0, 10);
		heavenlyBoneMarrowImprints =
			Math.Clamp(tag.GetInt("heavenlyBoneMarrowImprints"), 0, 10);
		realmBreakthroughAttempts =
			Math.Max(0, tag.GetInt("realmBreakthroughAttempts"));
		realmBreakthroughSuccesses =
			Math.Max(0, tag.GetInt("realmBreakthroughSuccesses"));
		realmBreakthroughFailures =
			Math.Max(0, tag.GetInt("realmBreakthroughFailures"));
		breakthroughPillsConsumed =
			Math.Max(0, tag.GetInt("breakthroughPillsConsumed"));
		successfulBreakthroughPillMask =
			Math.Clamp(tag.GetInt("successfulBreakthroughPillMask"), 0, 31);
		successfulBreakthroughRecordedMask =
			Math.Clamp(tag.GetInt("successfulBreakthroughRecordedMask"), 0, 31);
		System.Collections.Generic.IList<int> savedBreakthroughTreasures =
			tag.GetList<int>("successfulBreakthroughTreasures");
		for (int i = 0; i < successfulBreakthroughTreasures.Length; i++)
		{
			successfulBreakthroughTreasures[i] =
				i < savedBreakthroughTreasures.Count
					? Math.Clamp(savedBreakthroughTreasures[i], 0, 3)
					: 0;
		}
		pendingBreakthroughTreasure =
			Math.Clamp(tag.GetInt("pendingBreakthroughTreasure"), 0, 3);
		pendingBreakthroughUsedPill =
			tag.GetBool("pendingBreakthroughUsedPill");
		System.Collections.Generic.IList<int> savedAbilityExperience = tag.GetList<int>("abilityExperience");
		System.Collections.Generic.IList<int> savedAbilityLevels = tag.GetList<int>("abilityLevels");
		for (int i = 0; i < abilityLevels.Length; i++)
		{
			abilityExperience[i] = i < savedAbilityExperience.Count ? Math.Max(0, savedAbilityExperience[i]) : 0;
			abilityLevels[i] = i < savedAbilityLevels.Count
				? Math.Clamp(savedAbilityLevels[i], 1, CultivationAbilityInfo.MaxLevel)
				: 1;
		}
		System.Collections.Generic.IList<int> savedTechniqueLoadouts =
			tag.GetList<int>("techniqueLoadouts");
		int savedTechniqueLoadoutVersion =
			tag.GetInt("techniqueLoadoutVersion");
		byte[] techniqueLoadoutSnapshot = new byte[
			TechniqueLoadoutPresetCount * MaximumTechniqueLoadoutSlots];
		for (int i = 0; i < techniqueLoadoutSnapshot.Length; i++)
		{
			techniqueLoadoutSnapshot[i] = i < savedTechniqueLoadouts.Count
				? (byte)Math.Clamp(savedTechniqueLoadouts[i], 0,
					(int)CultivationAbility.Count)
				: (byte)CultivationAbility.Count;
		}
		pendingTribulationRealm = tag.ContainsKey("pendingTribulationRealm")
			? Math.Clamp(tag.GetInt("pendingTribulationRealm"), TribulationStartingRealm, TotalRealms - 1)
			: -1;
		awaitingTribulationConfirmation = pendingTribulationRealm >= TribulationStartingRealm
			&& tag.GetBool("awaitingTribulationConfirmation");
		deferredTribulationRealm = tag.ContainsKey("deferredTribulationRealm")
			? Math.Clamp(tag.GetInt("deferredTribulationRealm"),
				TribulationStartingRealm, TotalRealms - 1)
			: -1;
		pendingRealmBreakthroughConfirmation =
			tag.ContainsKey("pendingRealmBreakthroughConfirmation")
				? Math.Clamp(tag.GetInt("pendingRealmBreakthroughConfirmation"),
					1, TotalRealms - 1)
				: -1;
		confirmedRealmBreakthrough = -1;

		if (pendingTribulationRealm >= TribulationStartingRealm)
		{
			RealmIndex = pendingTribulationRealm - 1;
			Stage = StagesPerRealm;
			QiExp = Math.Min(QiExp, GetGlobalStageThreshold(pendingTribulationRealm * StagesPerRealm));
			Qi = Math.Min(Qi, MaxQi);
		}
		else if (deferredTribulationRealm >= TribulationStartingRealm)
		{
			RealmIndex = deferredTribulationRealm - 1;
			Stage = StagesPerRealm;
			int threshold = GetGlobalStageThreshold(
				deferredTribulationRealm * StagesPerRealm);
			QiExp = Math.Min(QiExp, threshold);
			Qi = Math.Min(Qi, MaxQi);
		}
		else if (IsAwaitingRealmBreakthroughConfirmation)
		{
			RealmIndex = pendingRealmBreakthroughConfirmation - 1;
			Stage = StagesPerRealm;
			int threshold = GetGlobalStageThreshold(
				pendingRealmBreakthroughConfirmation * StagesPerRealm);
			QiExp = Math.Min(QiExp, threshold);
			Qi = Math.Min(Qi, MaxQi);
		}
		else
		{
			UpdateRealm(showMessage: false);
		}
		if (savedTechniqueLoadouts.Count > 0)
		{
			ApplyTechniqueLoadoutState(
				tag.GetInt("activeTechniqueLoadoutPreset"),
				techniqueLoadoutSnapshot, validateUnlocks: true);
			if (savedTechniqueLoadoutVersion < TechniqueLoadoutSaveVersion)
				FillEmptyTechniqueLoadoutSlots();
			selectedTechniqueLoadoutSlot = Math.Clamp(
				tag.GetInt("selectedTechniqueLoadoutSlot"),
				0, TechniqueLoadoutSlotCount - 1);
		}
		else
		{
			ResetTechniqueLoadouts();
			FillEmptyTechniqueLoadoutSlots();
		}
	}

	public override void OnEnterWorld()
	{
		if (!HasReceivedCultivatorManual && Player.whoAmI == Main.myPlayer)
		{
			Player.QuickSpawnItem(
				Player.GetSource_Misc("XianxiaStarterManual"),
				ModContent.ItemType<CultivatorManual>()
			);
			HasReceivedCultivatorManual = true;
			Main.NewText(Mod.GetLocalization("Manual.Received").Value, new Color(95, 235, 205));
		}

		if (pendingTribulationRealm >= TribulationStartingRealm
			&& !awaitingTribulationConfirmation
			&& Player.whoAmI == Main.myPlayer)
		{
			StartTribulation(pendingTribulationRealm);
		}
		if (Player.whoAmI == Main.myPlayer)
			SyncTechniqueLoadout();
	}

	public override void ResetEffects()
	{
		flightMaintainedDuringChat = false;
		EquipmentPassiveQiBonus = 0;
		EquipmentMeditationQiBonus = 0;

		CultivationBonus bonus = CalculateCultivationBonus();
		Player.statLifeMax2 += (int)MathF.Round(bonus.MaxLife);
		Player.statDefense += (int)MathF.Round(bonus.Defense);
		Player.GetDamage(DamageClass.Generic) += bonus.DamagePercent / 100f;
		Player.moveSpeed += bonus.MoveSpeedPercent / 100f;
		Player.GetCritChance(DamageClass.Generic) += bonus.CritChance;
		Player.endurance += bonus.EndurancePercent / 100f;
		Player.GetCritChance(DamageClass.Generic) += heavenlyEyeImprints * 5f;
		Player.statLifeMax2 += heavenlyRoyalNectarImprints * 20;
		Player.lifeRegen += heavenlyRoyalNectarImprints;
		Player.statDefense += heavenlyBoneMarrowImprints * 4;
		Player.endurance += heavenlyBoneMarrowImprints * 0.02f;
		if (qiBurningEnabled)
			ApplyQiBurningBonuses();
		if (HasQiDeviation)
		{
			Player.GetDamage(DamageClass.Generic) -= 0.25f;
			Player.statDefense *= 0.75f;
			Player.moveSpeed *= 0.75f;
		}
		if (CanUseQiSense)
		{
			Player.findTreasure = true;
			Player.detectCreature = true;
			Player.dangerSense = true;
		}
		if (NightVisionEnabled && RealmIndex >= 2 && Qi > 0)
		{
			Player.nightVision = true;
		}
	}

	public override void PreUpdate()
	{
		MaintainQiFlightDuringChat();

		bool aboutToDrown = Player.wet
			&& !Player.lavaWet
			&& !Player.gills
			&& !Player.merman
			&& Player.breath <= 3;
		if (!aboutToDrown || !CanUseQiProtection || Qi <= 0 || Player.dead)
		{
			drowningProtectionTimer = 0;
			return;
		}

		if (drowningProtectionTimer <= 0)
		{
			// Vanilla drowning deals roughly two damage per second. Prepay that
			// second using the same protection ratio as an ordinary hit.
			int protectedDamage = 2;
			int qiCost = protectedDamage * GetQiProtectionCostPerDamage();
			if (!SpendQi(qiCost))
			{
				return;
			}

			drowningProtectionTimer = 60;
			ShowQiProtectionEffect(qiCost, fullyBlocked: false);
			AddAbilityExperience(CultivationAbility.QiProtection, protectedDamage);
		}

		// Keep the breath meter at its last sliver. The shield does not refill air;
		// it prevents the transition from zero breath to health damage.
		Player.breath = Math.Max(Player.breath, 3);
		drowningProtectionTimer--;
	}

	public override void UpdateLifeRegen()
	{
		// Apply cultivation regeneration after vanilla debuffs. Effects such as On
		// Fire normally erase all positive regeneration before adding their damage.
		CultivationBonus bonus = CalculateCultivationBonus();
		int cultivationLifeRegen = (int)MathF.Round(bonus.LifeRegen);
		if (IsAbilityUnlocked(CultivationAbility.NascentSoulRegeneration))
		{
			cultivationLifeRegen += 4
				+ (GetAbilityLevel(CultivationAbility.NascentSoulRegeneration) - 1) * 2;
		}
		Player.lifeRegen += cultivationLifeRegen;

		if (qiProtectionDotVisualCooldown > 0)
		{
			qiProtectionDotVisualCooldown--;
		}

		if (!CanUseQiProtection || Qi <= 0 || Player.lifeRegen >= 0)
		{
			qiProtectionDotQiAccumulator = 0f;
			return;
		}

		// Terraria lifeRegen uses two points as one HP per second. Convert the
		// remaining negative regeneration into the same Qi-per-damage ratio used by
		// regular hits, retaining fractions between ticks.
		int qiPerDamage = GetQiProtectionCostPerDamage();
		float damagePerTick = -Player.lifeRegen / 120f;
		qiProtectionDotQiAccumulator += damagePerTick * qiPerDamage;
		int qiToSpend = (int)MathF.Floor(qiProtectionDotQiAccumulator);
		if (qiToSpend > 0)
		{
			if (!SpendQi(qiToSpend))
			{
				qiProtectionDotQiAccumulator = 0f;
				return;
			}

			qiProtectionDotQiAccumulator -= qiToSpend;
			AddAbilityExperience(CultivationAbility.QiProtection,
				Math.Max(1, (int)MathF.Ceiling(qiToSpend / (float)qiPerDamage)));
			if (qiProtectionDotVisualCooldown <= 0)
			{
				ShowQiProtectionEffect(qiToSpend, fullyBlocked: false);
				qiProtectionDotVisualCooldown = 30;
			}
		}

		Player.lifeRegen = 0;
		Player.lifeRegenTime = 0;
		Player.lifeRegenCount = 0;
	}

	public override void ModifyHurt(ref Player.HurtModifiers modifiers)
	{
		if (resolvingTribulationLightning || !CanUseQiProtection || Qi <= 0)
		{
			return;
		}

		modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
		{
			SectPlayer sect = Player.GetModPlayer<SectPlayer>();
			if (sect.CanFormationAbsorb(info.Damage))
			{
				return;
			}

			info.Damage -= sect.AbsorbAndBreakFormation(info.Damage);
			if (info.Damage <= 0)
			{
				return;
			}

			// A fully covered hit is handled by ConsumableDodge so it can deal zero
			// damage. This callback only handles hits larger than the remaining Qi.
			int qiPerDamage = GetQiProtectionCostPerDamage();
			int fullProtectionCost = info.Damage * qiPerDamage;
			if (!CanUseQiProtection || Qi <= 0 || Qi >= fullProtectionCost)
			{
				return;
			}

			int absorbedDamage = Math.Min(
				Qi / qiPerDamage,
				Math.Max(0, info.Damage - 1)
			);
			if (absorbedDamage <= 0)
			{
				return;
			}

			int consumedQi = absorbedDamage * qiPerDamage;
			SpendQi(consumedQi);
			info.Damage -= absorbedDamage;
			ShowQiProtectionEffect(consumedQi, fullyBlocked: false);
			AddAbilityExperience(CultivationAbility.QiProtection, Math.Max(2, absorbedDamage));
		};
	}

	public override bool ConsumableDodge(Player.HurtInfo info)
	{
		if (resolvingTribulationLightning)
		{
			return false;
		}

		if (Player.GetModPlayer<SectPlayer>().CanFormationAbsorb(info.Damage))
		{
			return false;
		}

		int qiCost = info.Damage * GetQiProtectionCostPerDamage();
		if (!CanUseQiProtection || info.Damage <= 0 || Qi < qiCost)
		{
			return false;
		}

		SpendQi(qiCost);
		ShowQiProtectionEffect(qiCost, fullyBlocked: true);
		AddAbilityExperience(CultivationAbility.QiProtection, Math.Max(2, info.Damage));
		return true;
	}

	private int GetQiProtectionCostPerDamage()
	{
		int baseCost = Math.Max(2, QiProtectionCostPerDamage
			- (GetAbilityLevel(CultivationAbility.QiProtection) - 1) / 5);
		return Math.Max(1, (int)MathF.Ceiling(baseCost
			/ GetAbilityPowerMultiplier(CultivationAbility.QiProtection, 0f)));
	}

	public override void ProcessTriggers(TriggersSet triggersSet)
	{
		if (Player.dead)
		{
			IsAbilityTreeOpen = false;
			meditationToggleRequested = false;
		}
		if (Main.drawingPlayerChat)
		{
			meditationToggleRequested = false;
			StopMeditating(syncMultiplayer: true);
			MaintainQiFlightDuringChat();
			return;
		}
		if (Xianxia.AbilityTreeKeybind.JustPressed && !Player.dead)
		{
			IsAbilityTreeOpen = !IsAbilityTreeOpen;
			SoundEngine.PlaySound(IsAbilityTreeOpen ? SoundID.MenuOpen : SoundID.MenuClose);
		}

		IsAbilityWheelOpen = Xianxia.AbilityWheelKeybind.Current && !Player.dead;
		if (IsAbilityWheelOpen || IsAbilityTreeOpen
			|| IsAwaitingTribulationConfirmation
			|| IsAwaitingRealmBreakthroughConfirmation
			|| IsAwaitingHeartDemonTrialConfirmation)
		{
			Player.controlUseItem = false;
			Player.controlUseTile = false;
		}
		if (IsAwaitingTribulationConfirmation
			|| IsAwaitingRealmBreakthroughConfirmation
			|| IsAwaitingHeartDemonTrialConfirmation)
		{
			meditationToggleRequested = false;
			StopMeditating(syncMultiplayer: true);
			return;
		}
		if (IsAbilityTreeOpen)
		{
			meditationToggleRequested = false;
			StopMeditating(syncMultiplayer: true);
			MaintainQiFlightDuringAbilityMenu();
			return;
		}

		ProcessAbilityTriggers();

		bool toggleMeditation = CultivationClientConfig.Instance?.ToggleMeditation ?? true;
		if (toggleMeditation && Xianxia.MeditateKeybind.JustPressed && !Player.dead)
		{
			meditationToggleRequested = !meditationToggleRequested;
			Main.NewText(Mod.GetLocalization(meditationToggleRequested
				? "Cultivation.MeditationEnabled"
				: "Cultivation.MeditationDisabled").Value,
				meditationToggleRequested ? new Color(90, 235, 225) : Color.LightGray);
		}
		else if (!toggleMeditation)
		{
			meditationToggleRequested = false;
		}

		bool wantsToMeditate = toggleMeditation
			? meditationToggleRequested
			: Xianxia.MeditateKeybind.Current;
		if (!wantsToMeditate || Player.dead)
		{
			StopMeditating(syncMultiplayer: true);
			return;
		}

		if (Player.velocity.LengthSquared() > 0.01f || Player.itemAnimation > 0 || Player.mount.Active)
		{
			meditationToggleRequested = false;
			StopMeditating(syncMultiplayer: true);
			if (stillnessWarningCooldown <= 0)
			{
				Main.NewText(Mod.GetLocalization("Cultivation.NeedStillness").Value, Color.LightGray);
				stillnessWarningCooldown = 120;
			}

			return;
		}

		if (!IsMeditating)
		{
			IsMeditating = true;
			SyncMeditationState();
		}

		meditationTimer++;
		if (meditationTimer >= 60)
		{
			meditationTimer = 0;
			ProcessMeditationQiGain();
		}
	}

	private void ProcessMeditationQiGain()
	{
		if (deferredTribulationRealm >= TribulationStartingRealm && Qi >= MaxQi)
		{
			int targetRealm = deferredTribulationRealm;
			deferredTribulationRealm = -1;
			RequestTribulationConfirmation(targetRealm);
			return;
		}

		float cultivationPerSecond = MeditationQiPerSecond;

		// Meditation used to repair Qi Burning wounds must restore usable Qi
		// without also advancing cultivation. QiEXP resumes after full repair.
		if (HasBurnedQi)
		{
			int previousQi = Qi;
			int recoveryGain = TakeWholeQiGain(
				cultivationPerSecond * MeditationQiRecoveryMultiplier,
				ref meditationQiRecoveryRemainder);
			RestoreQi(recoveryGain);
			if (Qi > previousQi)
				AddAbilityExperience(CultivationAbility.Meditation, 5);
			return;
		}

		int missingQi = Math.Max(0, MaxQi - Qi);
		int totalGained = 0;

		if (missingQi <= 0)
		{
			int cultivationGain = TakeWholeQiGain(
				cultivationPerSecond,
				ref meditationQiGainRemainder);
			AddQi(ApplyHeartDemonCultivationPenalty(cultivationGain));
			totalGained = cultivationGain;
		}
		else
		{
			float maximumRecoveryThisSecond = cultivationPerSecond
				* MeditationQiRecoveryMultiplier;
			if (missingQi >= maximumRecoveryThisSecond)
			{
				int previousQi = Qi;
				int recoveryGain = TakeWholeQiGain(
					maximumRecoveryThisSecond,
					ref meditationQiRecoveryRemainder);
				RestoreQi(recoveryGain);
				totalGained = Qi - previousQi;
			}
			else
			{
				// Only the fraction of the second required to refill missing Qi uses
				// the 10x recovery rate. The remaining time cultivates new QiEXP at
				// the normal rate, so toggle upkeep cannot stall progression.
				float recoveryTimeFraction = missingQi / maximumRecoveryThisSecond;
				RestoreQi(missingQi);
				totalGained += missingQi;
				meditationQiRecoveryRemainder = 0f;

				float remainingCultivation = cultivationPerSecond
					* (1f - recoveryTimeFraction);
				int cultivationGain = TakeWholeQiGain(
					remainingCultivation,
					ref meditationQiGainRemainder);
				int adjustedGain =
					ApplyHeartDemonCultivationPenalty(cultivationGain);
				AddQi(adjustedGain);
				totalGained += adjustedGain;
			}
		}

		if (totalGained > 0)
		{
			AddAbilityExperience(CultivationAbility.Meditation, 5);
		}
	}

	private int ApplyHeartDemonCultivationPenalty(int amount)
	{
		if (amount <= 0 || heartDemonPoints <= 0)
			return amount;
		return Math.Max(1,
			(int)MathF.Floor(amount * HeartDemonCultivationGainMultiplier));
	}

	public void CloseAbilityTree() => IsAbilityTreeOpen = false;

	internal void DebugSetProgression(int realmIndex, int stage)
	{
		RealmIndex = Math.Clamp(realmIndex, 0, TotalRealms - 1);
		Stage = Math.Clamp(stage, 1, StagesPerRealm);
		int globalStageIndex = RealmIndex * StagesPerRealm + Stage - 1;
		QiExp = GetGlobalStageThreshold(globalStageIndex);
		Qi = MaxQi;
		ClearDebugTribulationState();
		QiFlightEnabled = false;
		QiProtectionEnabled = false;
		QiSenseEnabled = false;
		SpiritualPressureEnabled = false;
		NightVisionEnabled = false;
	}

	internal void DebugSetQi(int amount)
	{
		Qi = Math.Clamp(amount, 0, MaxQi);
	}

	internal bool DebugAdvanceStage()
	{
		if (IsCultivationMaxed || pendingTribulationRealm >= TribulationStartingRealm)
		{
			return false;
		}

		Qi = MaxQi;
		int required = NextStageThreshold - QiExp;
		if (required <= 0)
		{
			return false;
		}

		forceNextRealmBreakthrough = Stage == StagesPerRealm;
		AddQi(required);
		forceNextRealmBreakthrough = false;
		return true;
	}

	internal bool DebugPrepareTribulation(int targetRealm)
	{
		if (targetRealm < TribulationStartingRealm || targetRealm >= TotalRealms)
		{
			return false;
		}

		DebugSetProgression(targetRealm - 1, StagesPerRealm);
		return DebugAdvanceStage();
	}

	internal bool DebugResolveTribulation(bool success)
	{
		if (pendingTribulationRealm < TribulationStartingRealm)
		{
			return false;
		}

		if (success)
		{
			CompleteTribulation();
		}
		else
		{
			FailTribulation(recordHeartDemon: false);
		}
		return true;
	}

	internal void DebugSetAbilityLevel(CultivationAbility ability, int level)
	{
		int index = (int)ability;
		if (index < 0 || index >= (int)CultivationAbility.Count)
		{
			return;
		}

		abilityLevels[index] = Math.Clamp(level, 1, CultivationAbilityInfo.MaxLevel);
		abilityExperience[index] = 0;
	}

	internal void DebugSetAllAbilityLevels(int level)
	{
		for (int i = 0; i < (int)CultivationAbility.Count; i++)
		{
			DebugSetAbilityLevel((CultivationAbility)i, level);
		}
	}

	internal void DebugPlayBreakthroughEffect(bool realmEffect)
	{
		StartBreakthroughEffect(realmEffect);
	}

	internal void DebugResetProgression()
	{
		DebugSetProgression(0, 1);
		DebugSetAllAbilityLevels(1);
	}

	private void ClearDebugTribulationState()
	{
		pendingTribulationRealm = -1;
		deferredTribulationRealm = -1;
		awaitingTribulationConfirmation = false;
		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;
	}

	public void AddAbilityExperience(CultivationAbility ability, int amount)
	{
		if (amount <= 0 || !IsAbilityUnlocked(ability))
			return;

		SpiritualElement elements = CultivationAbilityInfo.GetSpiritualElements(ability);
		if (elements != SpiritualElement.None)
		{
			ElementalCultivationPlayer elemental =
				Player.GetModPlayer<ElementalCultivationPlayer>();
			float multiplier = elemental.GetMasteryGainMultiplier(elements)
				* (1f + elemental.GetAffinity(elements) * 0.001f);
			amount = Math.Max(1, (int)MathF.Round(amount * multiplier));
		}

		int index = (int)ability;
		if (abilityLevels[index] >= CultivationAbilityInfo.MaxLevel)
			return;

		abilityExperience[index] += amount;
		while (abilityLevels[index] < CultivationAbilityInfo.MaxLevel)
		{
			int required = CultivationAbilityInfo.ExperienceForNextLevel(abilityLevels[index]);
			if (abilityExperience[index] < required)
				break;
			abilityExperience[index] -= required;
			abilityLevels[index]++;
			if (Player.whoAmI == Main.myPlayer)
			{
				Main.NewText(Mod.GetLocalization("AbilityTree.LevelUp").Format(
					Mod.GetLocalization($"AbilityTree.Abilities.{ability}.Name").Value,
					abilityLevels[index]), new Color(105, 245, 225));
				SoundEngine.PlaySound(SoundID.Item29, Player.Center);
			}
		}
	}

	private void TryUseSelectedTechnique()
	{
		CultivationAbility ability = SelectedTechnique;
		if (ability == CultivationAbility.Count
			|| !IsTechniqueEquipped(ability)
			|| !IsAbilityUnlocked(ability))
		{
			Main.NewText(Mod.GetLocalization(
				"TechniqueLoadout.NoSelected").Value,
				Color.OrangeRed);
			return;
		}

		switch (ability)
		{
			case CultivationAbility.QiResistance:
				TryUseQiResistance();
				break;
			case CultivationAbility.Fireball:
				TryCastFireball(
					Main.MouseWorld - Player.Center,
					Player.GetSource_Misc("XianxiaFireball"));
				break;
			case CultivationAbility.QiPalm:
				TryUseQiPalm();
				break;
			case CultivationAbility.SpiritualRain:
				SpiritualRainTechnique.TryCast(
					Player,
					Player.GetSource_Misc("XianxiaSpiritualRain"));
				break;
			case CultivationAbility.FlameStep:
				TryUseFlameStep();
				break;
			case CultivationAbility.SpiritSwordRain:
				Player.GetModPlayer<SectPlayer>()
					.TryUseSpiritSwordRain();
				break;
			case CultivationAbility.NascentTeleport:
				TryUseNascentTeleport();
				break;
			case CultivationAbility.SpiritualPressure:
				ToggleSpiritualPressure();
				break;
			case CultivationAbility.SectProtectionFormation:
				Player.GetModPlayer<SectPlayer>()
					.TryUseSectProtectionFormation();
				break;
			case CultivationAbility.NightVision:
				ToggleNightVision();
				break;
			case CultivationAbility.QiFlight:
				ToggleQiFlight();
				break;
			case CultivationAbility.QiBurning:
				RequestToggleQiBurning();
				break;
			case CultivationAbility.QiSense:
				ToggleSelectedQiSense();
				break;
			case CultivationAbility.QiProtection:
				ToggleSelectedQiProtection();
				break;
		}
	}

	private void TryUseDirectTechnique(CultivationAbility ability)
	{
		if (!IsAbilityUnlocked(ability))
		{
			Main.NewText(Mod.GetLocalization(
				"TechniqueLoadout.DirectLocked").Format(
					Mod.GetLocalization(
						$"AbilityTree.Abilities.{ability}.Name").Value),
				Color.OrangeRed);
			return;
		}

		switch (ability)
		{
			case CultivationAbility.QiResistance:
				TryUseQiResistance();
				break;
			case CultivationAbility.Fireball:
				TryCastFireball(
					Main.MouseWorld - Player.Center,
					Player.GetSource_Misc("XianxiaFireball"));
				break;
			case CultivationAbility.QiPalm:
				TryUseQiPalm();
				break;
			case CultivationAbility.FlameStep:
				TryUseFlameStep();
				break;
			case CultivationAbility.NascentTeleport:
				TryUseNascentTeleport();
				break;
			case CultivationAbility.SpiritualPressure:
				ToggleSpiritualPressure();
				break;
			case CultivationAbility.NightVision:
				ToggleNightVision();
				break;
			case CultivationAbility.QiFlight:
				ToggleQiFlight();
				break;
			case CultivationAbility.QiBurning:
				RequestToggleQiBurning();
				break;
		}
	}

	private void ToggleQiFlight()
	{
		if (QiFlightEnabled)
		{
			QiFlightEnabled = false;
			Main.NewText(Mod.GetLocalization(
				"Abilities.QiFlightDisabled").Value,
				Color.LightGray);
		}
		else if (RealmIndex < 3)
		{
			Main.NewText(Mod.GetLocalization(
				"Abilities.RequiresRealm").Format(
					Mod.GetLocalization(
						"Cultivation.Realms.CoreFormation").Value),
				Color.OrangeRed);
		}
		else if (Qi <= 0)
		{
			Main.NewText(Mod.GetLocalization(
				"Abilities.NotEnoughQi").Format(1),
				Color.OrangeRed);
		}
		else
		{
			QiFlightEnabled = true;
			Main.NewText(Mod.GetLocalization(
				"Abilities.QiFlightEnabled").Value, Color.Cyan);
		}
	}

	private void ToggleSelectedQiSense()
	{
		bool enabled = !QiSenseEnabled;
		if (!SetQiSenseEnabled(enabled))
		{
			Main.NewText(Mod.GetLocalization(
				"Abilities.NotEnoughQi").Format(1),
				Color.OrangeRed);
			return;
		}
		Main.NewText(Mod.GetLocalization(enabled
			? "Abilities.QiSenseEnabled"
			: "Abilities.QiSenseDisabled").Value,
			enabled ? Color.Cyan : Color.LightGray);
	}

	private void ToggleSelectedQiProtection()
	{
		bool enabled = !QiProtectionEnabled;
		if (!SetQiProtectionEnabled(enabled))
			return;
		Main.NewText(Mod.GetLocalization(enabled
			? "Abilities.QiProtectionEnabled"
			: "Abilities.QiProtectionDisabled").Value,
			enabled ? Color.Cyan : Color.LightGray);
	}

	public bool TryToggleTechniqueFromWheel(
		CultivationAbility ability)
	{
		if (!CultivationAbilityInfo.IsToggleTechnique(ability)
			|| !IsAbilityUnlocked(ability))
		{
			return false;
		}
		switch (ability)
		{
			case CultivationAbility.QiSense:
				ToggleSelectedQiSense();
				break;
			case CultivationAbility.QiProtection:
				ToggleSelectedQiProtection();
				break;
			case CultivationAbility.QiBurning:
				RequestToggleQiBurning();
				break;
			case CultivationAbility.NightVision:
				ToggleNightVision();
				break;
			case CultivationAbility.QiFlight:
				ToggleQiFlight();
				break;
			case CultivationAbility.SpiritualPressure:
				ToggleSpiritualPressure();
				break;
			case CultivationAbility.SectProtectionFormation:
				Player.GetModPlayer<SectPlayer>()
					.TryUseSectProtectionFormation();
				break;
			default:
				return false;
		}
		return true;
	}

	private void ProcessAbilityTriggers()
	{
		IsFlyingWithQi = false;
		if (Player.dead)
		{
			flightQiTimer = 0;
			QiFlightEnabled = false;
			DisableSpiritualPressure(showMessage: false);
			DisableNightVision(showMessage: false);
			return;
		}

		if (Xianxia.FireballKeybind.JustPressed)
			TryUseSelectedTechnique();

		if (Xianxia.QiResistanceKeybind.JustPressed)
			TryUseDirectTechnique(CultivationAbility.QiResistance);
		if (Xianxia.DirectFireballKeybind.JustPressed)
			TryUseDirectTechnique(CultivationAbility.Fireball);
		if (Xianxia.QiPalmKeybind.JustPressed)
			TryUseDirectTechnique(CultivationAbility.QiPalm);
		if (Xianxia.FlameStepKeybind.JustPressed)
			TryUseDirectTechnique(CultivationAbility.FlameStep);
		if (Xianxia.NascentTeleportKeybind.JustPressed)
			TryUseDirectTechnique(CultivationAbility.NascentTeleport);
		if (Xianxia.SpiritualPressureKeybind.JustPressed)
			TryUseDirectTechnique(CultivationAbility.SpiritualPressure);
		if (Xianxia.NightVisionKeybind.JustPressed)
			TryUseDirectTechnique(CultivationAbility.NightVision);
		if (Xianxia.QiFlightKeybind.JustPressed)
			TryUseDirectTechnique(CultivationAbility.QiFlight);
		if (Xianxia.QiBurningKeybind.JustPressed)
			TryUseDirectTechnique(CultivationAbility.QiBurning);

		if (!QiFlightEnabled)
		{
			return;
		}

		if (RealmIndex < 3 || Qi <= 0)
		{
			QiFlightEnabled = false;
			flightQiTimer = 0;
			Main.NewText(Mod.GetLocalization("Abilities.QiFlightExhausted").Value, Color.OrangeRed);
			return;
		}

		IsFlyingWithQi = true;
		Player.noFallDmg = true;
		Player.gravity = 0f;
		float flightPower = GetAbilityPowerMultiplier(CultivationAbility.QiFlight, 0.03f);
		float horizontalAcceleration = (0.4f + (RealmIndex + 1) * 0.1f) * flightPower;
		float maximumHorizontalSpeed = (5f + (RealmIndex + 1) * 1.5f) * flightPower;
		float maximumVerticalSpeed = (4f + (RealmIndex + 1) * 0.75f) * flightPower;
		Player.maxFallSpeed = maximumVerticalSpeed;
		Player.fallStart = (int)(Player.position.Y / 16f);

		if (Player.controlLeft && !Player.controlRight)
		{
			Player.velocity.X = Math.Max(Player.velocity.X - horizontalAcceleration, -maximumHorizontalSpeed);
		}
		else if (Player.controlRight && !Player.controlLeft)
		{
			Player.velocity.X = Math.Min(Player.velocity.X + horizontalAcceleration, maximumHorizontalSpeed);
		}
		else
		{
			Player.velocity.X *= 0.8f;
			if (Math.Abs(Player.velocity.X) < 0.08f)
			{
				Player.velocity.X = 0f;
			}
		}

		if (Player.controlUp || Player.controlJump)
		{
			Player.velocity.Y = Math.Max(Player.velocity.Y - 0.4f, -maximumVerticalSpeed);
		}
		else if (Player.controlDown)
		{
			Player.velocity.Y = Math.Min(Player.velocity.Y + 0.4f, maximumVerticalSpeed);
		}
		else
		{
			Player.velocity.Y *= 0.8f;
			if (Math.Abs(Player.velocity.Y) < 0.08f)
			{
				Player.velocity.Y = 0f;
			}
		}

		UpdateQiFlightConsumption();
	}

	private void MaintainQiFlightDuringAbilityMenu()
	{
		IsFlyingWithQi = false;
		if (!QiFlightEnabled)
		{
			return;
		}

		if (RealmIndex < 3 || Qi <= 0)
		{
			QiFlightEnabled = false;
			flightQiTimer = 0;
			return;
		}

		IsFlyingWithQi = true;
		Player.noFallDmg = true;
		Player.gravity = 0f;
		Player.maxFallSpeed = 0f;
		Player.fallStart = (int)(Player.position.Y / 16f);
		Player.velocity = Vector2.Zero;
		Player.controlLeft = false;
		Player.controlRight = false;
		Player.controlUp = false;
		Player.controlDown = false;
		Player.controlJump = false;
		UpdateQiFlightConsumption();
	}

	private void MaintainQiFlightDuringChat()
	{
		if (flightMaintainedDuringChat || !Main.drawingPlayerChat
			|| Player.whoAmI != Main.myPlayer)
		{
			return;
		}

		flightMaintainedDuringChat = true;
		MaintainQiFlightDuringAbilityMenu();
	}

	private void UpdateQiFlightConsumption()
	{
		flightQiTimer++;
		int costInterval = (int)MathF.Ceiling(QiFlightCostInterval
			* GetAbilityPowerMultiplier(CultivationAbility.QiFlight, 0f));
		if (flightQiTimer < costInterval)
		{
			return;
		}

		flightQiTimer = 0;
		SpendQi(1);
		AddAbilityExperience(CultivationAbility.QiFlight, 5);
		if (Qi <= 0)
		{
			QiFlightEnabled = false;
			IsFlyingWithQi = false;
			Main.NewText(Mod.GetLocalization("Abilities.QiFlightExhausted").Value, Color.OrangeRed);
		}
	}

	private void TryUseQiResistance()
	{
		if (RealmIndex < 1)
		{
			Main.NewText(Mod.GetLocalization("Abilities.RequiresRealm").Format(
				Mod.GetLocalization("Cultivation.Realms.QiCondensation").Value), Color.OrangeRed);
			return;
		}

		int buffType = ModContent.BuffType<QiResistanceBuff>();
		if (Player.HasBuff(buffType))
		{
			Main.NewText(Mod.GetLocalization("Abilities.AlreadyActive").Value, Color.LightGray);
			return;
		}

		if (!SpendQi(QiResistanceCost))
		{
			Main.NewText(Mod.GetLocalization("Abilities.NotEnoughQi").Format(QiResistanceCost), Color.OrangeRed);
			return;
		}

		int duration = QiResistanceDuration + (GetAbilityLevel(CultivationAbility.QiResistance) - 1) * 5 * 60;
		Player.AddBuff(buffType, duration);
		AddAbilityExperience(CultivationAbility.QiResistance, 12);
		Main.NewText(Mod.GetLocalization("Abilities.QiResistanceActivated").Value, Color.Cyan);
	}

	public bool TryCastFireball(Vector2 aimDirection, IEntitySource source)
	{
		if (fireballCooldown > 0)
		{
			return false;
		}

		if (RealmIndex < 1)
		{
			Main.NewText(Mod.GetLocalization("Abilities.RequiresRealm").Format(
				Mod.GetLocalization("Cultivation.Realms.QiCondensation").Value), Color.OrangeRed);
			return false;
		}

		if (aimDirection.LengthSquared() < 0.001f)
		{
			aimDirection = Vector2.UnitX * Player.direction;
		}
		else
		{
			aimDirection.Normalize();
		}

		int baseDamage = (int)((35 + RealmIndex * 15 + (Stage - 1) * 2)
			* GetAbilityPowerMultiplier(CultivationAbility.Fireball, 0.04f));
		int damage = (int)Player.GetTotalDamage(DamageClass.Magic).ApplyTo(baseDamage);
		int baseQiCost = Math.Max(MinimumFireballQiCost,
			(int)Math.Ceiling(damage / FireballDamagePerQi));
		int qiCost = GetAbilityQiCost(baseQiCost, CultivationAbility.Fireball);
		float projectileScale = MathHelper.Clamp(0.8f + damage / 100f, 1.15f, 2.5f);

		if (!SpendAbilityQi(baseQiCost, CultivationAbility.Fireball))
		{
			Main.NewText(Mod.GetLocalization("Abilities.NotEnoughQi").Format(qiCost), Color.OrangeRed);
			return false;
		}

		Projectile.NewProjectile(
			source,
			Player.Center,
			aimDirection * 10f,
			ModContent.ProjectileType<QiFireballProjectile>(),
			damage,
			4f,
			Player.whoAmI,
			ai0: projectileScale
		);

		SoundEngine.PlaySound(SoundID.Item20, Player.Center);
		fireballCooldown = (int)MathF.Ceiling(FireballCooldownTicks
			* GetAbilityCooldownMultiplier(CultivationAbility.Fireball));
		AddAbilityExperience(CultivationAbility.Fireball, 10);
		return true;
	}

	private void TryUseQiPalm()
	{
		TryCastQiPalm(
			Main.MouseWorld - Player.Center,
			Player.GetSource_Misc("XianxiaQiPalm")
		);
	}

	public bool TryCastQiPalm(Vector2 aimDirection, IEntitySource source)
	{
		if (qiPalmCooldown > 0)
		{
			return false;
		}

		if (RealmIndex < 1)
		{
			Main.NewText(Mod.GetLocalization("Abilities.RequiresRealm").Format(
				Mod.GetLocalization("Cultivation.Realms.QiCondensation").Value), Color.OrangeRed);
			return false;
		}

		Vector2 direction = aimDirection;
		if (direction.LengthSquared() < 0.001f)
		{
			direction = Vector2.UnitX * Player.direction;
		}
		else
		{
			direction.Normalize();
		}

		int baseDamage = (int)((22 + RealmIndex * 10 + (Stage - 1) * 2)
			* GetAbilityPowerMultiplier(CultivationAbility.QiPalm, 0.04f));
		int damage = (int)Player.GetTotalDamage(DamageClass.Magic).ApplyTo(baseDamage);
		int qiCost = Math.Max(10, (int)Math.Ceiling(damage / 7f));
		float scale = MathHelper.Clamp(0.85f + damage / 120f, 1f, 2.2f);
		if (!SpendQi(qiCost))
		{
			Main.NewText(Mod.GetLocalization("Abilities.NotEnoughQi").Format(qiCost), Color.OrangeRed);
			return false;
		}

		Projectile.NewProjectile(
			source,
			Player.Center + direction * 18f,
			direction * 9.5f,
			ModContent.ProjectileType<QiPalmProjectile>(),
			damage,
			9f + RealmIndex * 1.5f,
			Player.whoAmI,
			ai0: scale
		);
		SoundEngine.PlaySound(SoundID.Item8, Player.Center);
		qiPalmCooldown = (int)MathF.Ceiling(QiPalmCooldownTicks
			* GetAbilityCooldownMultiplier(CultivationAbility.QiPalm));
		AddAbilityExperience(CultivationAbility.QiPalm, 10);
		return true;
	}

	private void TryUseFlameStep()
	{
		if (flameStepCooldown > 0 || Player.mount.Active)
		{
			return;
		}

		if (RealmIndex < 2)
		{
			Main.NewText(Mod.GetLocalization("Abilities.RequiresRealm").Format(
				Mod.GetLocalization("Cultivation.Realms.FoundationEstablishment").Value), Color.OrangeRed);
			return;
		}

		Vector2 direction = new(
			(Player.controlRight ? 1f : 0f) - (Player.controlLeft ? 1f : 0f),
			(Player.controlDown ? 1f : 0f) - (Player.controlUp ? 1f : 0f)
		);
		if (direction.LengthSquared() < 0.001f)
		{
			direction = Main.MouseWorld - Player.Center;
		}
		if (direction.LengthSquared() < 0.001f)
		{
			direction = Vector2.UnitX * Player.direction;
		}
		direction.Normalize();

		int baseDamage = (int)((30 + RealmIndex * 12 + (Stage - 1) * 2)
			* GetAbilityPowerMultiplier(CultivationAbility.FlameStep, 0.04f));
		int damage = (int)Player.GetTotalDamage(DamageClass.Magic).ApplyTo(baseDamage);
		int baseQiCost = Math.Max(18, (int)Math.Ceiling(damage / 5f));
		int qiCost = GetAbilityQiCost(baseQiCost, CultivationAbility.FlameStep);
		if (!SpendAbilityQi(baseQiCost, CultivationAbility.FlameStep))
		{
			Main.NewText(Mod.GetLocalization("Abilities.NotEnoughQi").Format(qiCost), Color.OrangeRed);
			return;
		}

		float dashSpeed = (12f + RealmIndex * 1.5f)
			* GetAbilityPowerMultiplier(CultivationAbility.FlameStep, 0.025f);
		Vector2 dashVelocity = direction * dashSpeed;
		Player.velocity = dashVelocity;
		Player.direction = direction.X < 0f ? -1 : 1;
		Player.noFallDmg = true;
		Player.SetImmuneTimeForAllTypes(12);
		Projectile.NewProjectile(
			Player.GetSource_Misc("XianxiaFlameStep"),
			Player.Center,
			dashVelocity,
			ModContent.ProjectileType<FlameStepProjectile>(),
			damage,
			5f,
			Player.whoAmI,
			ai0: MathHelper.Clamp(1f + damage / 160f, 1.15f, 2f)
		);
		SoundEngine.PlaySound(SoundID.Item74, Player.Center);
		flameStepCooldown = (int)MathF.Ceiling(FlameStepCooldownTicks
			* GetAbilityCooldownMultiplier(CultivationAbility.FlameStep));
		AddAbilityExperience(CultivationAbility.FlameStep, 12);
	}

	private void TryUseNascentTeleport()
	{
		if (nascentTeleportCooldown > 0)
		{
			return;
		}

		if (RealmIndex < 4)
		{
			Main.NewText(Mod.GetLocalization("Abilities.RequiresRealm").Format(
				Mod.GetLocalization("Cultivation.Realms.NascentSoul").Value), Color.OrangeRed);
			return;
		}

		Vector2 desiredCenter = GetNascentTeleportTarget();
		if (!TryFindSafeTeleportPosition(desiredCenter, out Vector2 destination))
		{
			Main.NewText(Mod.GetLocalization("Abilities.NascentTeleportBlocked").Value, Color.OrangeRed);
			return;
		}

		float distanceBlocks = Vector2.Distance(Player.Center, destination + Player.Size * 0.5f) / 16f;
		int baseQiCost = NascentTeleportBaseQiCost
			+ (int)MathF.Ceiling(distanceBlocks / NascentTeleportBlocksPerQi)
				* NascentTeleportQiCostPerDistanceStep;
		baseQiCost = Math.Max(NascentTeleportBaseQiCost,
			(int)MathF.Ceiling(baseQiCost
				/ GetAbilityPowerMultiplier(CultivationAbility.NascentTeleport, 0.025f)));
		int qiCost = GetAbilityQiCost(baseQiCost, CultivationAbility.NascentTeleport);
		if (!SpendAbilityQi(baseQiCost, CultivationAbility.NascentTeleport))
		{
			Main.NewText(Mod.GetLocalization("Abilities.NotEnoughQi").Format(qiCost), Color.OrangeRed);
			return;
		}

		Vector2 origin = Player.Center;
		SpawnNascentTeleportEffect(origin);
		Player.velocity = Vector2.Zero;
		Player.mount.Dismount(Player);

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			NetMessage.SendData(
				MessageID.TeleportEntity,
				number: 0,
				number2: Player.whoAmI,
				number3: destination.X,
				number4: destination.Y,
				number5: TeleportationStyleID.RodOfDiscord
			);
		}
		else
		{
			Player.Teleport(destination, TeleportationStyleID.RodOfDiscord);
		}

		SpawnNascentTeleportEffect(destination + Player.Size * 0.5f);
		SoundEngine.PlaySound(SoundID.Item6, destination);
		Main.mapFullscreen = false;
		nascentTeleportCooldown = (int)MathF.Ceiling(NascentTeleportCooldownTicks
			* GetAbilityCooldownMultiplier(CultivationAbility.NascentTeleport));
		AddAbilityExperience(CultivationAbility.NascentTeleport, Math.Max(10, (int)distanceBlocks / 10));
		Main.NewText(Mod.GetLocalization("Abilities.NascentTeleportUsed").Format(
			qiCost, (int)MathF.Round(distanceBlocks)), new Color(170, 120, 255));
	}

	private void ToggleSpiritualPressure()
	{
		if (SpiritualPressureEnabled)
		{
			DisableSpiritualPressure(showMessage: true);
			return;
		}

		if (RealmIndex < 4)
		{
			Main.NewText(Mod.GetLocalization("Abilities.RequiresRealm").Format(
				Mod.GetLocalization("Cultivation.Realms.NascentSoul").Value), Color.OrangeRed);
			return;
		}

		if (Qi < SpiritualPressureQiCostPerSecond)
		{
			Main.NewText(Mod.GetLocalization("Abilities.NotEnoughQi").Format(
				SpiritualPressureQiCostPerSecond), Color.OrangeRed);
			return;
		}

		SpiritualPressureEnabled = true;
		spiritualPressureQiTimer = 0;
		Main.NewText(Mod.GetLocalization("Abilities.SpiritualPressureEnabled").Value,
			new Color(205, 120, 255));
	}

	private void DisableSpiritualPressure(bool showMessage)
	{
		SpiritualPressureEnabled = false;
		spiritualPressureQiTimer = 0;
		int auraType = ModContent.ProjectileType<SpiritualPressureAuraProjectile>();
		for (int i = 0; i < Main.maxProjectiles; i++)
		{
			Projectile projectile = Main.projectile[i];
			if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == auraType)
			{
				projectile.Kill();
			}
		}

		if (showMessage)
		{
			Main.NewText(Mod.GetLocalization("Abilities.SpiritualPressureDisabled").Value, Color.LightGray);
		}
	}

	private int GetNightVisionQiCostPerSecond() => Math.Max(1,
		NightVisionBaseQiCostPerSecond
		- (GetAbilityLevel(CultivationAbility.NightVision) - 1) / 7);

	private void ToggleNightVision()
	{
		if (NightVisionEnabled)
		{
			DisableNightVision(showMessage: true);
			return;
		}

		if (RealmIndex < 2)
		{
			Main.NewText(Mod.GetLocalization("Abilities.RequiresRealm").Format(
				Mod.GetLocalization("Cultivation.Realms.FoundationEstablishment").Value), Color.OrangeRed);
			return;
		}

		int qiCost = GetNightVisionQiCostPerSecond();
		if (Qi < qiCost)
		{
			Main.NewText(Mod.GetLocalization("Abilities.NotEnoughQi").Format(qiCost), Color.OrangeRed);
			return;
		}

		NightVisionEnabled = true;
		nightVisionQiTimer = 0;
		Main.NewText(Mod.GetLocalization("Abilities.NightVisionEnabled").Value,
			new Color(145, 225, 255));
	}

	private void DisableNightVision(bool showMessage)
	{
		NightVisionEnabled = false;
		nightVisionQiTimer = 0;
		if (showMessage)
		{
			Main.NewText(Mod.GetLocalization("Abilities.NightVisionDisabled").Value, Color.LightGray);
		}
	}

	private Vector2 GetNascentTeleportTarget()
	{
		if (!Main.mapFullscreen)
		{
			return Main.MouseWorld;
		}

		Vector2 screenCenter = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
		Vector2 targetTile = Main.mapFullscreenPos
			+ (Main.MouseScreen - screenCenter) / Main.mapFullscreenScale;
		return targetTile * 16f;
	}

	private bool TryFindSafeTeleportPosition(Vector2 desiredCenter, out Vector2 destination)
	{
		float minimumX = 16f;
		float minimumY = 16f;
		float maximumX = Main.maxTilesX * 16f - Player.width - 16f;
		float maximumY = Main.maxTilesY * 16f - Player.height - 16f;
		Vector2 desiredTopLeft = desiredCenter - Player.Size * 0.5f;

		for (int radius = 0; radius <= NascentTeleportSafeSearchRadius; radius++)
		{
			for (int offsetY = -radius; offsetY <= radius; offsetY++)
			{
				for (int offsetX = -radius; offsetX <= radius; offsetX++)
				{
					if (radius > 0 && Math.Max(Math.Abs(offsetX), Math.Abs(offsetY)) != radius)
					{
						continue;
					}

					Vector2 candidate = desiredTopLeft + new Vector2(offsetX * 16f, offsetY * 16f);
					candidate.X = MathHelper.Clamp(candidate.X, minimumX, maximumX);
					candidate.Y = MathHelper.Clamp(candidate.Y, minimumY, maximumY);
					if (!Collision.SolidCollision(candidate, Player.width, Player.height))
					{
						destination = candidate;
						return true;
					}
				}
			}
		}

		destination = Vector2.Zero;
		return false;
	}

	private static void SpawnNascentTeleportEffect(Vector2 center)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		int particleCount = CultivationClientConfig.ScaleParticleCount(36);
		for (int i = 0; i < particleCount; i++)
		{
			float angle = MathHelper.TwoPi * i / Math.Max(1, particleCount);
			Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(2.5f, 6f);
			Dust dust = Dust.NewDustPerfect(center, DustID.Teleporter, velocity, 80,
				new Color(150, 90, 255), Main.rand.NextFloat(1.1f, 1.7f));
			dust.noGravity = true;
		}
	}

	private void StopMeditating(bool syncMultiplayer)
	{
		meditationTimer = 0;
		burnedQiMeditationRepairTimer = 0;
		if (!IsMeditating)
		{
			return;
		}

		IsMeditating = false;
		if (syncMultiplayer)
		{
			SyncMeditationState();
		}
	}

	private void SyncMeditationState()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
		{
			Xianxia.SendMeditationState(Player.whoAmI, IsMeditating);
		}
	}

	internal void SetMeditatingFromNetwork(bool isMeditating)
	{
		IsMeditating = isMeditating;
		if (!isMeditating)
		{
			meditationTimer = 0;
		}
	}

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			Xianxia.SendMeditationState(Player.whoAmI, IsMeditating, toWho, fromWho);
			Xianxia.SendQiBurningState(Player.whoAmI, qiBurningEnabled,
				toWho, fromWho);
			Xianxia.SendCultivationRiskState(Player.whoAmI, this,
				toWho, fromWho);
			Xianxia.SendTechniqueLoadoutState(
				Player.whoAmI, this, toWho, fromWho);
		}
	}

	public override void PostUpdate()
	{
		UpdateCultivationRequirementMultiplier();
		UpdateQiBurning();
		if (!Player.dead
			&& (Main.netMode != NetmodeID.MultiplayerClient
				|| Player.whoAmI == Main.myPlayer))
		{
			UpdateSpiritualQiZone();
		}
		if (Main.netMode != NetmodeID.MultiplayerClient)
			UpdateBurnedQiMeditationRepair();
		if (Main.netMode != NetmodeID.MultiplayerClient
			&& heartDemonTrialActive
			&& (heartDemonTrialNpcIndex < 0
				|| heartDemonTrialNpcIndex >= Main.maxNPCs
				|| !Main.npc[heartDemonTrialNpcIndex].active
				|| Main.npc[heartDemonTrialNpcIndex].type
					!= ModContent.NPCType<HeartDemon>()))
		{
			FailHeartDemonTrial(showMessage: true);
		}
		if (HasQiDeviation)
			Player.AddBuff(ModContent.BuffType<QiDeviationDebuff>(),
				qiDeviationTimer);
		if (HasBurnedQi)
			Player.AddBuff(ModContent.BuffType<DamagedOriginDebuff>(), 2);
		if (spiritualRainCooldown > 0)
			spiritualRainCooldown--;

		if (Player.whoAmI == Main.myPlayer && !Player.dead)
		{
			UpdatePassiveQiRecovery();
			UpdateQiSense();
			UpdateNightVision();
			ApplyNightVisionAura();
			UpdateSpiritualPressure();
			UpdateNascentSoulRegenerationTraining();
		}

		if (fireballCooldown > 0)
		{
			fireballCooldown--;
		}

		if (qiPalmCooldown > 0)
		{
			qiPalmCooldown--;
		}

		if (flameStepCooldown > 0)
		{
			flameStepCooldown--;
		}

		if (nascentTeleportCooldown > 0)
		{
			nascentTeleportCooldown--;
		}

		if (stillnessWarningCooldown > 0)
		{
			stillnessWarningCooldown--;
		}
		if (breakthroughWarningCooldown > 0)
			breakthroughWarningCooldown--;

		if (IsMeditating && Main.netMode != NetmodeID.Server)
		{
			SpawnQiAbsorptionDust();
			float visualIntensity = CultivationClientConfig.VisualEffectIntensity;
			Lighting.AddLight(Player.Center, 0.05f * visualIntensity,
				0.18f * visualIntensity, 0.22f * visualIntensity);
		}

		if (IsFlyingWithQi && Main.netMode != NetmodeID.Server
			&& CultivationClientConfig.ShouldSpawnParticle())
		{
			Dust dust = Dust.NewDustDirect(Player.Bottom - new Vector2(4f, 6f), 8, 8,
				DustID.MagicMirror, 0f, 1.5f, 80, Color.Cyan, 0.7f);
			dust.noGravity = true;
		}

		if (Main.netMode != NetmodeID.Server)
		{
			UpdateBreakthroughEffect();
			UpdateCultivationRiskVisuals();
		}

		if (Player.whoAmI == Main.myPlayer)
		{
			UpdateTribulation();
		}
	}

	public override void UpdateDead()
	{
		passiveQiRecoveryTimer = 0;
		qiProtectionDotQiAccumulator = 0f;
		qiProtectionDotVisualCooldown = 0;
		drowningProtectionTimer = 0;
		if (pendingTribulationRealm >= TribulationStartingRealm)
		{
			FailTribulation();
		}
		if (qiBurningEnabled)
			DisableQiBurning(applyDeviation: true, showMessage: false);
		if (heartDemonTrialActive)
			FailHeartDemonTrial(showMessage: true);

		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;
	}

	public override void OnHurt(Player.HurtInfo info)
	{
		if (info.Damage > 0)
			qiBurningCombatTimer = QiBurnCombatWindow;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (damageDone > 0)
			qiBurningCombatTimer = QiBurnCombatWindow;
	}

	public override void Kill(double damage, int hitDirection, bool pvp,
		PlayerDeathReason damageSource)
	{
		if (pvp || heartDemonTrialActive
			|| tribulationRealm >= TribulationStartingRealm
			|| resolvingTribulationLightning)
			return;
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			if (Player.whoAmI == Main.myPlayer)
				Xianxia.SendHeartDemonDeathRequest();
			return;
		}
		if (Main.netMode == NetmodeID.SinglePlayer)
			RecordHeartDemonDeath();
	}

	public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
	{
		if (IsMeditating)
		{
			// Sitting legs are shorter than standing legs. Move the visual down so the
			// character rests on the ground instead of hovering above it.
			drawInfo.Position.Y += 8f;
			drawInfo.isSitting = true;
			drawInfo.seatYOffset = -4f;
		}
	}

	private void SpawnQiAbsorptionDust()
	{
		if (!Main.rand.NextBool(3) || !CultivationClientConfig.ShouldSpawnParticle())
		{
			return;
		}

		Vector2 outwardDirection = Main.rand.NextVector2CircularEdge(1f, 0.7f);
		float distance = Main.rand.NextFloat(70f, 130f);
		Vector2 spawnPosition = Player.Center + outwardDirection * distance;
		Vector2 inwardDirection = Vector2.Normalize(Player.Center - spawnPosition);
		Vector2 swirl = inwardDirection.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-0.45f, 0.45f);
		Vector2 velocity = inwardDirection * Main.rand.NextFloat(2.2f, 3.2f) + swirl;

		Color qiColor = Color.Lerp(Color.Cyan, Color.MediumPurple, Main.rand.NextFloat(0.15f, 0.55f));
		Dust dust = Dust.NewDustPerfect(
			spawnPosition,
			DustID.MagicMirror,
			velocity,
			Alpha: 70,
			newColor: qiColor,
			Scale: Main.rand.NextFloat(0.65f, 1f)
		);
		dust.noGravity = true;
		dust.fadeIn = 0.7f;
	}

	private void UpdateCultivationRiskVisuals()
	{
		float intensity = CultivationClientConfig.VisualEffectIntensity;
		if (qiBurningEnabled)
		{
			Lighting.AddLight(Player.Center,
				0.7f * intensity, 0.18f * intensity, 0.1f * intensity);
			int frequency = burnedQiCapacityBps >= 4000 ? 1 : 2;
			if (Main.rand.NextBool(frequency)
				&& CultivationClientConfig.ShouldSpawnParticle())
			{
				Vector2 direction =
					Main.rand.NextVector2CircularEdge(1f, 0.8f);
				Color color = Main.rand.NextBool()
					? Color.OrangeRed : Color.MediumPurple;
				Dust dust = Dust.NewDustPerfect(
					Player.Center + direction * Main.rand.NextFloat(12f, 28f),
					DustID.Torch,
					direction * Main.rand.NextFloat(1.8f, 4.2f),
					40, color,
					Main.rand.NextFloat(0.9f, 1.45f) * intensity);
				dust.noGravity = true;
			}
		}
		if (heartDemonVisualTimer > 0)
		{
			heartDemonVisualTimer--;
			if (Main.rand.NextBool(2)
				&& CultivationClientConfig.ShouldSpawnParticle())
			{
				Dust dust = Dust.NewDustPerfect(
					Player.Center + Main.rand.NextVector2Circular(45f, 70f),
					DustID.Shadowflame,
					Main.rand.NextVector2Circular(1.2f, 1.2f),
					50, Color.MediumPurple, 1.1f * intensity);
				dust.noGravity = true;
			}
		}
	}

	private void ShowQiProtectionEffect(int consumedQi, bool fullyBlocked)
	{
		if (fullyBlocked)
		{
			// ConsumableDodge cancels the hit before vanilla grants immunity. Add a
			// short universal cooldown so contact damage cannot trigger every frame.
			Player.SetImmuneTimeForAllTypes(30);
		}

		Color shieldColor = fullyBlocked ? Color.Cyan : Color.MediumPurple;
		CombatText.NewText(
			Player.Hitbox,
			shieldColor,
			Mod.GetLocalization("Abilities.QiProtectionAbsorbed").Format(consumedQi)
		);

		int particleCount = CultivationClientConfig.ScaleParticleCount(fullyBlocked ? 24 : 14);
		for (int i = 0; i < particleCount; i++)
		{
			Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
			Dust shieldDust = Dust.NewDustPerfect(
				Player.Center + direction * Main.rand.NextFloat(16f, 28f),
				DustID.MagicMirror,
				direction * Main.rand.NextFloat(1.5f, 3.5f),
				Alpha: 35,
				newColor: shieldColor,
				Scale: Main.rand.NextFloat(0.8f, 1.25f)
			);
			shieldDust.noGravity = true;
		}

		SoundEngine.PlaySound(SoundID.Item29, Player.Center);

		if (Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer)
		{
			int shieldType = ModContent.ProjectileType<QiProtectionShieldProjectile>();
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile existingShield = Main.projectile[i];
				if (!existingShield.active || existingShield.owner != Player.whoAmI || existingShield.type != shieldType)
				{
					continue;
				}

				existingShield.timeLeft = 24;
				existingShield.ai[0] = fullyBlocked ? 1f : 0f;
				existingShield.netUpdate = true;
				return;
			}

			Projectile.NewProjectile(
				Player.GetSource_Misc("QiProtectionShield"),
				Player.Center,
				Vector2.Zero,
				shieldType,
				0,
				0f,
				Player.whoAmI,
				ai0: fullyBlocked ? 1f : 0f
			);
		}
	}

	private void StartBreakthroughEffect(bool isRealmBreakthrough)
	{
		SetBreakthroughEffectFromNetwork(RealmIndex, isRealmBreakthrough);

		if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
		{
			Xianxia.SendBreakthroughEffect(Player.whoAmI, RealmIndex, isRealmBreakthrough);
		}
	}

	internal void SetBreakthroughEffectFromNetwork(int realmIndex, bool isRealmBreakthrough)
	{
		breakthroughEffectRealm = Math.Clamp(realmIndex, 0, TotalRealms - 1);
		realmBreakthroughEffect = isRealmBreakthrough;
		breakthroughEffectTimer = isRealmBreakthrough
			? RealmBreakthroughEffectDuration
			: StageBreakthroughEffectDuration;

		if (Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(
				isRealmBreakthrough ? SoundID.Item29 : SoundID.Item4,
				Player.Center
			);
		}
	}

	private void UpdateBreakthroughEffect()
	{
		if (breakthroughEffectTimer <= 0 || Player.dead)
		{
			return;
		}

		float strength = 1f + breakthroughEffectRealm * 0.35f;
		Color qiColor = Color.Lerp(Color.Cyan, Color.Gold, breakthroughEffectRealm / (float)(TotalRealms - 1));
		float visualIntensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(Player.Center, qiColor.ToVector3()
			* (realmBreakthroughEffect ? 0.9f : 0.35f) * visualIntensity);

		if (realmBreakthroughEffect)
		{
			int durationElapsed = RealmBreakthroughEffectDuration - breakthroughEffectTimer;
			if (durationElapsed % 12 == 0)
			{
				int particleCount = CultivationClientConfig.ScaleParticleCount(
					16 + breakthroughEffectRealm * 8);
				for (int i = 0; i < particleCount; i++)
				{
					Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
					Dust dust = Dust.NewDustPerfect(
						Player.Center + direction * 12f,
						DustID.MagicMirror,
						direction * Main.rand.NextFloat(2.5f, 5f) * strength,
						Alpha: 25,
						newColor: qiColor,
						Scale: Main.rand.NextFloat(1f, 1.55f) * strength
					);
					dust.noGravity = true;
				}
			}

			if (Main.rand.NextBool(2) && CultivationClientConfig.ShouldSpawnParticle())
			{
				Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 0.65f);
				Dust aura = Dust.NewDustPerfect(
					Player.Center + direction * Main.rand.NextFloat(55f, 95f) * strength,
					DustID.GemDiamond,
					-direction * Main.rand.NextFloat(1.5f, 3f),
					Alpha: 40,
					newColor: qiColor,
					Scale: Main.rand.NextFloat(0.8f, 1.3f) * strength
				);
				aura.noGravity = true;
			}
		}
		else if (Main.rand.NextBool(2) && CultivationClientConfig.ShouldSpawnParticle())
		{
			Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 0.7f);
			Dust dust = Dust.NewDustPerfect(
				Player.Center + direction * Main.rand.NextFloat(35f, 55f) * strength,
				DustID.MagicMirror,
				-direction * Main.rand.NextFloat(1.2f, 2.4f),
				Alpha: 65,
				newColor: qiColor,
				Scale: Main.rand.NextFloat(0.7f, 1.05f) * strength
			);
			dust.noGravity = true;
		}

		breakthroughEffectTimer--;
	}

	private int GetTribulationGoldenCoreTier(int targetRealm)
	{
		if (targetRealm == TribulationStartingRealm)
			return Math.Clamp(pendingBreakthroughGoldenCoreTier, 1, 9);
		return Math.Clamp(goldenCoreTier, 1, 9);
	}

	private int GetTribulationStrikeCount(int targetRealm)
	{
		int baseStrikes =
			9 + (targetRealm - TribulationStartingRealm) * 2;
		int foundationStrikes = foundationQuality switch
		{
			FoundationQuality.Stable => 1,
			FoundationQuality.Perfect => 2,
			FoundationQuality.Heavenly => 3,
			_ => 0
		};
		int goldenCoreStrikes =
			(10 - GetTribulationGoldenCoreTier(targetRealm)) / 2;
		return baseStrikes + foundationStrikes + goldenCoreStrikes;
	}

	private float GetTribulationPowerMultiplier(int targetRealm)
	{
		float foundationMultiplier = foundationQuality switch
		{
			FoundationQuality.Stable => 1.08f,
			FoundationQuality.Perfect => 1.18f,
			FoundationQuality.Heavenly => 1.30f,
			_ => 1f
		};
		int tier = GetTribulationGoldenCoreTier(targetRealm);
		float goldenCoreMultiplier = 1f + (9 - tier) * 0.04f;
		return foundationMultiplier * goldenCoreMultiplier;
	}

	private int GetTribulationStrikeInterval(int targetRealm) =>
		Math.Max(55, (int)MathF.Round(TribulationStrikeInterval
			/ MathF.Sqrt(GetTribulationPowerMultiplier(targetRealm))));

	private void StartTribulation(int targetRealm)
	{
		if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer)
		{
			return;
		}

		pendingTribulationRealm = targetRealm;
		awaitingTribulationConfirmation = false;
		tribulationRealm = targetRealm;
		tribulationStrikesRemaining = GetTribulationStrikeCount(targetRealm);
		tribulationTimer = TribulationInitialDelay;
		Main.NewText(Mod.GetLocalization("Cultivation.TribulationBegins").Format(
			GetRealmName(targetRealm), tribulationStrikesRemaining), Color.OrangeRed);
		SoundEngine.PlaySound(SoundID.Roar, Player.Center);
	}

	private void RequestTribulationConfirmation(int targetRealm)
	{
		if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer)
		{
			return;
		}

		pendingTribulationRealm = targetRealm;
		deferredTribulationRealm = -1;
		awaitingTribulationConfirmation = true;
		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;
		Main.NewText(Mod.GetLocalization("Cultivation.TribulationReady").Format(
			GetRealmName(targetRealm)), Color.Gold);
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	private void RequestRealmBreakthroughConfirmation(int targetRealm)
	{
		if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer)
			return;
		if (pendingRealmBreakthroughConfirmation == targetRealm)
			return;

		int threshold =
			GetGlobalStageThreshold(targetRealm * StagesPerRealm);
		QiExp = Math.Min(QiExp, threshold);
		Qi = Math.Min(Qi, MaxQi);
		RealmIndex = targetRealm - 1;
		Stage = StagesPerRealm;
		pendingRealmBreakthroughConfirmation = targetRealm;
		confirmedRealmBreakthrough = -1;
		pendingFoundationQuality = FoundationQuality.Inferior;
		pendingGoldenCoreTier = 9;
		selectedBreakthroughTreasureType = 0;
		selectedBreakthroughPillType = 0;
		StopMeditating(syncMultiplayer: true);
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public void ConfirmRealmBreakthrough()
	{
		if (!IsAwaitingRealmBreakthroughConfirmation
			|| !CanConfirmRealmBreakthrough)
		{
			return;
		}

		int targetRealm = pendingRealmBreakthroughConfirmation;
		pendingRealmBreakthroughConfirmation = -1;
		confirmedRealmBreakthrough = targetRealm;
		UpdateRealm(showMessage: true);
		confirmedRealmBreakthrough = -1;
	}

	public void CancelRealmBreakthrough()
	{
		if (!IsAwaitingRealmBreakthroughConfirmation)
			return;

		int targetRealm = pendingRealmBreakthroughConfirmation;
		int threshold =
			GetGlobalStageThreshold(targetRealm * StagesPerRealm);
		pendingRealmBreakthroughConfirmation = -1;
		confirmedRealmBreakthrough = -1;
		ClearPendingBreakthroughSelections();
		QiExp = Math.Min(QiExp, Math.Max(0, threshold - 1));
		Qi = Math.Min(Qi, MaxQi);
		RealmIndex = targetRealm - 1;
		Stage = StagesPerRealm;
		SoundEngine.PlaySound(SoundID.MenuClose);
	}

	public void SelectPendingFoundationQuality(FoundationQuality quality)
	{
		if (PendingRealmBreakthroughTargetRealm != 2)
			return;
		pendingFoundationQuality = (FoundationQuality)Math.Clamp(
			(byte)quality, (byte)FoundationQuality.Inferior,
			(byte)FoundationQuality.Heavenly);
	}

	public void SelectPendingGoldenCoreTier(int tier)
	{
		if (PendingRealmBreakthroughTargetRealm != 3)
			return;
		pendingGoldenCoreTier = Math.Clamp(tier, 1, 9);
	}

	public void CycleSelectedBreakthroughTreasure()
	{
		int[] types =
		[
			ModContent.ItemType<HeavenlyEyeEssence>(),
			ModContent.ItemType<HeavenlyRoyalNectar>(),
			ModContent.ItemType<HeavenlyBoneMarrow>()
		];
		int start = Array.IndexOf(types, selectedBreakthroughTreasureType);
		for (int step = 1; step <= types.Length; step++)
		{
			int candidate = types[(start + step + types.Length)
				% types.Length];
			if (Player.CountItem(candidate) <= 0)
				continue;
			selectedBreakthroughTreasureType = candidate;
			return;
		}
		selectedBreakthroughTreasureType = 0;
	}

	public void ClearSelectedBreakthroughTreasure() =>
		selectedBreakthroughTreasureType = 0;

	public void ToggleSelectedBreakthroughPill()
	{
		int pillType = GetBreakthroughPillItemType(
			PendingRealmBreakthroughTargetRealm);
		if (pillType <= 0
			|| Player.GetModPlayer<AlchemyPillEffectPlayer>()
				.GetBreakthroughChanceBonus(
					PendingRealmBreakthroughTargetRealm) > 0f)
			return;
		selectedBreakthroughPillType =
			selectedBreakthroughPillType == pillType
				? 0
				: Player.CountItem(pillType) > 0 ? pillType : 0;
	}

	public void ClearSelectedBreakthroughPill()
	{
		if (Player.GetModPlayer<AlchemyPillEffectPlayer>()
			.GetBreakthroughChanceBonus(
				PendingRealmBreakthroughTargetRealm) <= 0f)
			selectedBreakthroughPillType = 0;
	}

	public void ConfirmTribulation()
	{
		if (!IsAwaitingTribulationConfirmation)
		{
			return;
		}

		int targetRealm = pendingTribulationRealm;
		awaitingTribulationConfirmation = false;
		StartTribulation(targetRealm);
	}

	public void CancelTribulation()
	{
		if (!IsAwaitingTribulationConfirmation)
		{
			return;
		}

		int cancelledRealm = pendingTribulationRealm;
		RealmIndex = cancelledRealm - 1;
		Stage = StagesPerRealm;
		pendingTribulationRealm = -1;
		deferredTribulationRealm = cancelledRealm;
		awaitingTribulationConfirmation = false;
		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;

		Main.NewText(Mod.GetLocalization("Cultivation.TribulationCancelled").Value, Color.LightGray);
		SoundEngine.PlaySound(SoundID.MenuClose);
	}

	private void UpdateTribulation()
	{
		if (tribulationRealm < TribulationStartingRealm || Player.dead)
		{
			return;
		}

		tribulationTimer--;
		if (tribulationTimer <= TribulationWarningTime && tribulationTimer > 0)
		{
			SpawnTribulationWarningDust();
		}

		if (tribulationTimer > 0)
		{
			return;
		}

		StrikeWithTribulationLightning();
		tribulationStrikesRemaining--;
		if (Player.dead)
		{
			tribulationRealm = -1;
			return;
		}

		if (tribulationStrikesRemaining <= 0)
		{
			CompleteTribulation();
			return;
		}

		tribulationTimer = GetTribulationStrikeInterval(tribulationRealm);
	}

	private void SpawnTribulationWarningDust()
	{
		float intensity = (1f + tribulationRealm * 0.15f)
			* MathF.Sqrt(GetTribulationPowerMultiplier(tribulationRealm));
		if (Main.rand.NextBool(2) && CultivationClientConfig.ShouldSpawnParticle())
		{
			Vector2 position = Player.Top + new Vector2(Main.rand.NextFloat(-45f, 45f), Main.rand.NextFloat(-180f, -45f));
			Dust warning = Dust.NewDustPerfect(
				position,
				DustID.Electric,
				Vector2.UnitY * Main.rand.NextFloat(1f, 3f),
				Alpha: 30,
				newColor: Color.Lerp(Color.White, Color.Cyan, 0.45f),
				Scale: Main.rand.NextFloat(0.8f, 1.25f) * intensity
			);
			warning.noGravity = true;
		}

		float visualIntensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(Player.Top, 0.25f * intensity * visualIntensity,
			0.35f * intensity * visualIntensity, 0.55f * intensity * visualIntensity);
	}

	private void StrikeWithTribulationLightning()
	{
		Vector2 strikeEnd = Player.Center;
		Vector2 strikeStart = strikeEnd + new Vector2(Main.rand.NextFloat(-90f, 90f), -720f);
		int segments = CultivationClientConfig.ScaleParticleCount(55 + tribulationRealm * 8, 8);
		for (int i = 0; segments > 0 && i <= segments; i++)
		{
			float progress = i / (float)segments;
			float edgeFade = MathF.Sin(progress * MathHelper.Pi);
			Vector2 position = Vector2.Lerp(strikeStart, strikeEnd, progress);
			position.X += Main.rand.NextFloat(-18f, 18f) * edgeFade;
			Dust lightning = Dust.NewDustPerfect(
				position,
				DustID.Electric,
				Main.rand.NextVector2Circular(0.7f, 0.7f),
				Alpha: 5,
				newColor: Color.White,
				Scale: Main.rand.NextFloat(1.1f, 1.7f) + tribulationRealm * 0.12f
			);
			lightning.noGravity = true;
		}

		int impactParticles = CultivationClientConfig.ScaleParticleCount(25 + tribulationRealm * 5);
		for (int i = 0; i < impactParticles; i++)
		{
			Dust impact = Dust.NewDustPerfect(
				strikeEnd,
				DustID.Electric,
				Main.rand.NextVector2Circular(6f, 4f),
				Alpha: 10,
				newColor: Color.Cyan,
				Scale: Main.rand.NextFloat(1f, 1.8f)
			);
			impact.noGravity = true;
		}

		float lightningIntensity = CultivationClientConfig.VisualEffectIntensity;
		Lighting.AddLight(strikeEnd, 1.6f * lightningIntensity,
			1.8f * lightningIntensity, 2.2f * lightningIntensity);
		SoundEngine.PlaySound(SoundID.Item122, strikeEnd);

		int realmOffset = tribulationRealm - TribulationStartingRealm;
		int damage = 220 + realmOffset * 300;
		damage = Math.Max(damage,
			(int)MathF.Ceiling(Player.statLifeMax2 * (0.45f + realmOffset * 0.1f)));
		float tribulationPower =
			GetTribulationPowerMultiplier(tribulationRealm);
		damage = Math.Max(1,
			(int)MathF.Ceiling(damage * tribulationPower));
		PermanentFormationCoreEntity.TryProtectFromTribulation(
			Player, damage, realmOffset, out damage);
		damage = ApplyTribulationQiShield(damage, realmOffset);
		damage = Math.Max(1, (int)MathF.Ceiling(damage
			* Player.GetModPlayer<AlchemyPillEffectPlayer>().TribulationDamageMultiplier));
		float armorPenetration = (45f + realmOffset * 55f)
			* MathF.Sqrt(tribulationPower);
		PlayerDeathReason deathReason = PlayerDeathReason.ByCustomReason(
			Mod.GetLocalization("Cultivation.TribulationDeath").ToNetworkText(Player.name)
		);
		resolvingTribulationLightning = true;
		try
		{
			Player.Hurt(
				deathReason,
				damage,
				0,
				dodgeable: false,
				armorPenetration: armorPenetration,
				knockback: 8f
			);
		}
		finally
		{
			resolvingTribulationLightning = false;
		}
	}

	private int ApplyTribulationQiShield(int incomingDamage, int realmOffset)
	{
		if (!CanUseQiProtection || Qi <= 0 || MaxQi <= 0)
		{
			return incomingDamage;
		}

		float baseReserveFraction = 0.12f + realmOffset * 0.03f;
		float masteryCostMultiplier = GetQiProtectionCostPerDamage()
			/ (float)QiProtectionCostPerDamage;
		int fullShieldCost = Math.Max(1,
			(int)MathF.Ceiling(MaxQi * baseReserveFraction * masteryCostMultiplier
				* Player.GetModPlayer<AlchemyPillEffectPlayer>().TribulationShieldCostMultiplier));
		int consumedQi = Math.Min(Qi, fullShieldCost);
		Qi -= consumedQi;

		float shieldCoverage = consumedQi / (float)fullShieldCost;
		float damageReduction = 0.7f * shieldCoverage;
		ShowQiProtectionEffect(consumedQi, fullyBlocked: false);
		AddAbilityExperience(CultivationAbility.QiProtection,
			Math.Clamp((int)MathF.Ceiling(shieldCoverage * 20f), 2, 20));

		return Math.Max(1,
			(int)MathF.Ceiling(incomingDamage * (1f - damageReduction)));
	}

	private void CompleteTribulation()
	{
		int reachedRealm = pendingTribulationRealm;
		pendingTribulationRealm = -1;
		deferredTribulationRealm = -1;
		awaitingTribulationConfirmation = false;
		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;
		RealmIndex = reachedRealm;
		Stage = 1;
		RecordSuccessfulRealmBreakthrough(
			reachedRealm, pendingBreakthroughTreasure,
			pendingBreakthroughUsedPill);
		FillEmptyTechniqueLoadoutSlots();
		NormalizeTechniqueLoadout();
		SyncTechniqueLoadout();
		Player.GetModPlayer<SectPlayer>().RecordTribulationSurvived();

		Main.NewText(Mod.GetLocalization("Cultivation.TribulationSurvived").Format(
			GetRealmName()), Color.Gold);
		Main.NewText(Mod.GetLocalization("Cultivation.Breakthrough").Format(GetRealmName()), Color.Gold);
		Main.NewText(GetRealmBonusSummary(), new Color(120, 240, 255));
		if (reachedRealm == 4)
		{
			Main.NewText(Mod.GetLocalization("Abilities.NascentTeleportUnlocked").Value,
				new Color(190, 140, 255));
			Main.NewText(Mod.GetLocalization("Abilities.SpiritualPressureUnlocked").Value,
				new Color(205, 120, 255));
		}
		StartBreakthroughEffect(isRealmBreakthrough: true);
	}

	private void FailTribulation(bool recordHeartDemon = true)
	{
		int failedRealm = pendingTribulationRealm;
		realmBreakthroughFailures++;
		if (recordHeartDemon)
			RecordHeartDemonBreakthroughFailure();
		pendingBreakthroughTreasure = 0;
		pendingBreakthroughUsedPill = false;
		pendingBreakthroughGoldenCoreTier = 9;
		int previousGlobalStage = failedRealm * StagesPerRealm - 1;
		QiExp = Math.Min(QiExp, GetGlobalStageThreshold(previousGlobalStage));
		Qi = Math.Min(Qi, MaxQi);
		RealmIndex = failedRealm - 1;
		Stage = StagesPerRealm;
		pendingTribulationRealm = -1;
		deferredTribulationRealm = -1;
		awaitingTribulationConfirmation = false;
		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;

		if (Main.netMode != NetmodeID.Server)
		{
			Main.NewText(Mod.GetLocalization("Cultivation.TribulationFailed").Value, Color.OrangeRed);
		}
	}

	public string GetSuccessfulBreakthroughCatalystSummary(int targetRealm)
	{
		if (targetRealm <= 0 || targetRealm >= TotalRealms
			|| targetRealm > RealmIndex)
		{
			return Mod.GetLocalization(
				"CharacterStats.NotCompleted").Value;
		}
		if ((successfulBreakthroughRecordedMask & 1 << targetRealm) == 0)
		{
			return Mod.GetLocalization(
				"CharacterStats.LegacyUnknown").Value;
		}

		bool usedPill =
			(successfulBreakthroughPillMask & 1 << targetRealm) != 0;
		int treasure = successfulBreakthroughTreasures[targetRealm];
		string pill = targetRealm switch
		{
			1 => Lang.GetItemNameValue(
				ModContent.ItemType<MeridianOpeningPill>()),
			2 => Lang.GetItemNameValue(
				ModContent.ItemType<FoundationAscensionPill>()),
			3 => Lang.GetItemNameValue(
				ModContent.ItemType<GoldenCoreCondensationPill>()),
			4 => Lang.GetItemNameValue(
				ModContent.ItemType<NascentSoulIntegrationPill>()),
			_ => string.Empty
		};
		string treasureName = treasure switch
		{
			1 => Lang.GetItemNameValue(
				ModContent.ItemType<HeavenlyEyeEssence>()),
			2 => Lang.GetItemNameValue(
				ModContent.ItemType<HeavenlyRoyalNectar>()),
			3 => Lang.GetItemNameValue(
				ModContent.ItemType<HeavenlyBoneMarrow>()),
			_ => string.Empty
		};
		if (usedPill && treasure > 0)
			return Mod.GetLocalization(
				"CharacterStats.PillAndTreasure").Format(
					pill, treasureName);
		if (usedPill)
			return pill;
		if (treasure > 0)
			return treasureName;
		return Mod.GetLocalization("CharacterStats.NoCatalyst").Value;
	}

	public string GetSelectedHeavenlyTreasureName()
	{
		return HasSelectedBreakthroughTreasure
			? Lang.GetItemNameValue(selectedBreakthroughTreasureType)
			: string.Empty;
	}

	public string GetPendingBreakthroughGradeName() =>
		PendingRealmBreakthroughTargetRealm switch
		{
			2 => Mod.GetLocalization(
				$"BreakthroughGrades.Foundation.{pendingFoundationQuality}")
				.Value,
			3 => Mod.GetLocalization("BreakthroughGrades.GoldenCoreTier")
				.Format(pendingGoldenCoreTier),
			_ => string.Empty
		};

	public string GetFoundationQualityName() => Mod.GetLocalization(
		$"BreakthroughGrades.Foundation.{foundationQuality}").Value;

	private float GetBreakthroughGradeChanceModifier(int targetRealm)
	{
		if (targetRealm == 2)
		{
			return pendingFoundationQuality switch
			{
				FoundationQuality.Inferior => 10f,
				FoundationQuality.Stable => 0f,
				FoundationQuality.Perfect => -15f,
				FoundationQuality.Heavenly => -30f,
				_ => 0f
			};
		}
		if (targetRealm == 3)
		{
			float tierModifier = pendingGoldenCoreTier switch
			{
				9 => 15f, 8 => 10f, 7 => 5f, 6 => 0f,
				5 => -5f, 4 => -10f, 3 => -15f,
				2 => -20f, 1 => -25f, _ => 0f
			};
			float foundationModifier = foundationQuality switch
			{
				FoundationQuality.Inferior => -5f,
				FoundationQuality.Perfect => 10f,
				FoundationQuality.Heavenly => 15f,
				_ => 0f
			};
			return tierModifier + foundationModifier;
		}
		return 0f;
	}

	private float GetDefaultBreakthroughGradeChanceModifier(
		int targetRealm) => targetRealm switch
	{
		2 => 10f,
		3 => 15f + (foundationQuality switch
		{
			FoundationQuality.Inferior => -5f,
			FoundationQuality.Perfect => 10f,
			FoundationQuality.Heavenly => 15f,
			_ => 0f
		}),
		_ => 0f
	};

	private float GetSelectedBreakthroughPillChanceBonus()
	{
		float activeBonus = Player.GetModPlayer<AlchemyPillEffectPlayer>()
			.GetBreakthroughChanceBonus(PendingRealmBreakthroughTargetRealm);
		if (activeBonus > 0f)
			return activeBonus;
		return selectedBreakthroughPillType > 0
			&& Player.CountItem(selectedBreakthroughPillType) > 0
			? PendingRealmBreakthroughTargetRealm switch
			{
				2 => 12f,
				3 => 15f,
				_ => 0f
			}
			: 0f;
	}

	private int GetEffectiveSelectedBreakthroughPillType()
	{
		int targetRealm = PendingRealmBreakthroughTargetRealm;
		if (Player.GetModPlayer<AlchemyPillEffectPlayer>()
			.GetBreakthroughChanceBonus(targetRealm) > 0f)
			return GetBreakthroughPillItemType(targetRealm);
		return selectedBreakthroughPillType > 0
			&& Player.CountItem(selectedBreakthroughPillType) > 0
			? selectedBreakthroughPillType : 0;
	}

	private bool CanConsumeSelectedBreakthroughPill()
	{
		int targetRealm = PendingRealmBreakthroughTargetRealm;
		if (Player.GetModPlayer<AlchemyPillEffectPlayer>()
			.GetBreakthroughChanceBonus(targetRealm) > 0f)
			return true;
		int pillType = GetEffectiveSelectedBreakthroughPillType();
		if (pillType <= 0)
			return false;
		foreach (Item item in Player.inventory)
		{
			if (item.type != pillType
				|| item.ModItem is not IAlchemyPill pill)
				continue;
			int saturation = AlchemyGlobalItem.GetAdjustedSaturationCost(
				item, pill);
			return Player.GetModPlayer<AlchemyPlayer>()
				.CanConsumePill(saturation);
		}
		return false;
	}

	private static int GetBreakthroughPillItemType(int targetRealm) =>
		targetRealm switch
		{
			1 => ModContent.ItemType<MeridianOpeningPill>(),
			2 => ModContent.ItemType<FoundationAscensionPill>(),
			3 => ModContent.ItemType<GoldenCoreCondensationPill>(),
			4 => ModContent.ItemType<NascentSoulIntegrationPill>(),
			_ => 0
		};

	private static bool IsHeavenlyTreasureType(int type) =>
		type == ModContent.ItemType<HeavenlyEyeEssence>()
		|| type == ModContent.ItemType<HeavenlyRoyalNectar>()
		|| type == ModContent.ItemType<HeavenlyBoneMarrow>();

	private void ClearPendingBreakthroughSelections()
	{
		pendingFoundationQuality = FoundationQuality.Inferior;
		pendingGoldenCoreTier = 9;
		selectedBreakthroughTreasureType = 0;
		selectedBreakthroughPillType = 0;
	}

	private void UpdatePassiveQiRecovery()
	{
		if (Qi >= MaxQi)
		{
			passiveQiRecoveryTimer = 0;
			passiveQiGainRemainder = 0f;
			return;
		}

		passiveQiRecoveryTimer++;
		if (passiveQiRecoveryTimer >= 60)
		{
			passiveQiRecoveryTimer = 0;
			int restoredQi = TakeWholeQiGain(PassiveQiRecoveryPerSecond, ref passiveQiGainRemainder);
			RestoreQi(restoredQi);
			if (restoredQi > 0)
			{
				AddAbilityExperience(CultivationAbility.SpiritBreathing, 4);
			}
		}
	}

	private void UpdateNascentSoulRegenerationTraining()
	{
		if (!IsAbilityUnlocked(CultivationAbility.NascentSoulRegeneration)
			|| Player.statLife >= Player.statLifeMax2)
		{
			nascentSoulRegenerationTrainingTimer = 0;
			return;
		}

		nascentSoulRegenerationTrainingTimer++;
		if (nascentSoulRegenerationTrainingTimer >= 60)
		{
			nascentSoulRegenerationTrainingTimer = 0;
			AddAbilityExperience(CultivationAbility.NascentSoulRegeneration, 4);
		}
	}

	private void UpdateSpiritualQiZone()
	{
		CultivationServerConfig config = CultivationServerConfig.Instance;
		if (!config.EnableSpiritualQiZones)
		{
			NearbySpiritCrystalCount = 0;
			spiritualQiScanTimer = 0;
			return;
		}

		spiritualQiScanTimer++;
		if (spiritualQiScanTimer < 60)
		{
			return;
		}

		spiritualQiScanTimer = 0;
		int radius = config.SpiritualQiZoneRadiusBlocks;
		NearbySpiritCrystalCount =
			SpiritualQiConcentration.CountCrystals(Player.Center, radius);
	}

	private static int TakeWholeQiGain(float exactGain, ref float remainder)
	{
		remainder += exactGain;
		int wholeGain = (int)MathF.Floor(remainder);
		remainder -= wholeGain;
		return wholeGain;
	}

	private void UpdateQiSense()
	{
		if (!QiSenseEnabled)
		{
			qiSenseCostTimer = 0;
			return;
		}

		if (!HasUnlockedQiSense || Qi <= 0)
		{
			QiSenseEnabled = false;
			qiSenseCostTimer = 0;
			Main.NewText(Mod.GetLocalization("Abilities.QiSenseExhausted").Value, Color.OrangeRed);
			return;
		}

		qiSenseCostTimer++;
		int qiSenseInterval = (int)MathF.Ceiling(
			(QiSenseCostInterval
				+ (GetAbilityLevel(CultivationAbility.QiSense) - 1) * 3)
			* GetAbilityPowerMultiplier(CultivationAbility.QiSense, 0f));
		if (qiSenseCostTimer >= qiSenseInterval)
		{
			qiSenseCostTimer = 0;
			if (!SpendQi(1))
			{
				QiSenseEnabled = false;
				Main.NewText(Mod.GetLocalization("Abilities.QiSenseExhausted").Value, Color.OrangeRed);
			}
			else
			{
				AddAbilityExperience(CultivationAbility.QiSense, 5);
			}
		}
	}

	private void UpdateSpiritualPressure()
	{
		if (!SpiritualPressureEnabled)
		{
			spiritualPressureQiTimer = 0;
			return;
		}

		if (RealmIndex < 4 || Qi <= 0)
		{
			DisableSpiritualPressure(showMessage: false);
			Main.NewText(Mod.GetLocalization("Abilities.SpiritualPressureExhausted").Value,
				Color.OrangeRed);
			return;
		}

		spiritualPressureQiTimer++;
		if (spiritualPressureQiTimer >= 60)
		{
			spiritualPressureQiTimer = 0;
			if (!SpendQi(SpiritualPressureQiCostPerSecond))
			{
				DisableSpiritualPressure(showMessage: false);
				Main.NewText(Mod.GetLocalization("Abilities.SpiritualPressureExhausted").Value,
					Color.OrangeRed);
				return;
			}
			AddAbilityExperience(CultivationAbility.SpiritualPressure, 6);
		}

		int auraType = ModContent.ProjectileType<SpiritualPressureAuraProjectile>();
		if (Player.ownedProjectileCounts[auraType] <= 0)
		{
			int baseDamage = (int)((60 + Stage * 12)
				* GetAbilityPowerMultiplier(CultivationAbility.SpiritualPressure, 0.04f));
			int damage = (int)Player.GetTotalDamage(DamageClass.Magic).ApplyTo(baseDamage);
			float radius = (360f + Stage * 12f)
				* GetAbilityPowerMultiplier(CultivationAbility.SpiritualPressure, 0.02f);
			Projectile.NewProjectile(
				Player.GetSource_Misc("XianxiaSpiritualPressure"),
				Player.Center,
				Vector2.Zero,
				auraType,
				damage,
				12f,
				Player.whoAmI,
				ai0: radius
			);
		}
	}

	private void UpdateNightVision()
	{
		if (!NightVisionEnabled)
		{
			nightVisionQiTimer = 0;
			return;
		}

		if (RealmIndex < 2 || Qi <= 0)
		{
			DisableNightVision(showMessage: false);
			Main.NewText(Mod.GetLocalization("Abilities.NightVisionExhausted").Value,
				Color.OrangeRed);
			return;
		}

		nightVisionQiTimer++;
		if (nightVisionQiTimer < 60)
		{
			return;
		}

		nightVisionQiTimer = 0;
		int qiCost = GetNightVisionQiCostPerSecond();
		if (!SpendQi(qiCost))
		{
			DisableNightVision(showMessage: false);
			Main.NewText(Mod.GetLocalization("Abilities.NightVisionExhausted").Value,
				Color.OrangeRed);
			return;
		}

		AddAbilityExperience(CultivationAbility.NightVision, 5);
	}

	private void ApplyNightVisionAura()
	{
		if (!NightVisionEnabled || RealmIndex < 2 || Qi <= 0
			|| Main.netMode == NetmodeID.Server)
		{
			return;
		}

		int level = GetAbilityLevel(CultivationAbility.NightVision);
		float auraRadius = 72f + (level - 1) * 5f;
		float brightness = 1.05f + (level - 1) * 0.012f;
		Vector3 centerLight = new(brightness * 0.9f, brightness, brightness);
		Vector3 outerLight = centerLight * 0.82f;
		Lighting.AddLight(Player.Center, centerLight);

		const int lightPoints = 8;
		float rotation = (float)Main.GameUpdateCount * 0.006f;
		for (int i = 0; i < lightPoints; i++)
		{
			float angle = rotation + MathHelper.TwoPi * i / lightPoints;
			Lighting.AddLight(Player.Center + angle.ToRotationVector2() * auraRadius, outerLight);
		}
	}

	public void RestoreQi(int amount)
	{
		if (amount <= 0 || Qi >= MaxQi)
		{
			return;
		}

		int previousQi = Qi;
		Qi = Math.Min(Qi + amount, MaxQi);
		ShowQiGain(Qi - previousQi);
	}

	public void AddQi(int amount)
	{
		if (amount <= 0)
		{
			return;
		}

		int previousQi = Qi;
		int previousQiExp = QiExp;
		int maximumExperience = pendingTribulationRealm >= TribulationStartingRealm
			? GetGlobalStageThreshold(pendingTribulationRealm * StagesPerRealm)
			: GetGlobalStageThreshold(MaxGlobalStageIndex);

		QiExp = Math.Min(QiExp + amount, maximumExperience);
		Qi = Math.Min(Qi + amount, MaxQi);

		ShowQiGain(Qi - previousQi);

		if (QiExp != previousQiExp)
		{
			UpdateRealm(showMessage: true);
		}
	}

	private void ShowQiGain(int amount)
	{
		if (amount > 0 && Main.netMode != NetmodeID.Server)
		{
			CombatText.NewText(Player.Hitbox, Color.Cyan,
				Mod.GetLocalization("Cultivation.QiGain").Format(amount));
		}
	}

	public bool SpendQi(int amount)
	{
		if (amount <= 0)
		{
			return true;
		}

		int actualAmount = GetFinalQiCost(amount);
		if (Qi < actualAmount)
		{
			return false;
		}

		Qi -= actualAmount;
		if (IsAbilityUnlocked(CultivationAbility.GoldenCoreCirculation))
		{
			AddAbilityExperience(CultivationAbility.GoldenCoreCirculation,
				Math.Max(2, actualAmount / 5));
		}
		return true;
	}

	public int GetAbilityQiCost(int amount, CultivationAbility ability)
	{
		int cost = GetFinalQiCost(amount);
		SpiritualElement elements = CultivationAbilityInfo.GetSpiritualElements(ability);
		if (elements == SpiritualElement.None)
			return cost;

		ElementalCultivationPlayer elemental =
			Player.GetModPlayer<ElementalCultivationPlayer>();
		float affinityReduction = elemental.GetAffinity(elements) * 0.08f;
		float totalReduction = Math.Clamp(
			elemental.GetQiCostReductionPercent(elements) + affinityReduction,
			0f, ElementalCultivationPlayer.MaximumQiCostReductionPercent);
		return Math.Max(1, (int)MathF.Ceiling(cost * (1f - totalReduction / 100f)));
	}

	public bool SpendAbilityQi(int amount, CultivationAbility ability)
	{
		int actualAmount = GetAbilityQiCost(amount, ability);
		if (Qi < actualAmount)
			return false;
		Qi -= actualAmount;
		if (IsAbilityUnlocked(CultivationAbility.GoldenCoreCirculation))
			AddAbilityExperience(CultivationAbility.GoldenCoreCirculation,
				Math.Max(2, actualAmount / 5));
		return true;
	}

	public int GetFinalQiCost(int amount)
	{
		if (amount <= 0)
			return 0;

		int actualAmount = Math.Max(1, (int)MathF.Ceiling(amount
			* Player.GetModPlayer<AlchemyPillEffectPlayer>().QiCostMultiplier));
		if (IsAbilityUnlocked(CultivationAbility.GoldenCoreCirculation))
		{
			float reduction = 0.05f
				+ (GetAbilityLevel(CultivationAbility.GoldenCoreCirculation) - 1) * 0.01f;
			actualAmount = Math.Max(1, (int)MathF.Ceiling(amount * (1f - reduction)));
		}

		return actualAmount;
	}

	public void StartSpiritualRainCooldown(int ticks) =>
		spiritualRainCooldown = Math.Max(spiritualRainCooldown, ticks);

	public bool SetQiProtectionEnabled(bool enabled)
	{
		if (enabled && (RealmIndex < 2
			|| !IsTechniqueEquipped(
				CultivationAbility.QiProtection)))
		{
			return false;
		}

		QiProtectionEnabled = enabled;
		if (!enabled)
		{
			drowningProtectionTimer = 0;
		}
		return true;
	}

	public bool SetQiSenseEnabled(bool enabled)
	{
		if (enabled && (!HasUnlockedQiSense
			|| !IsTechniqueEquipped(CultivationAbility.QiSense)))
		{
			return false;
		}

		if (enabled && Qi <= 0)
		{
			return false;
		}

		QiSenseEnabled = enabled;
		qiSenseCostTimer = 0;
		return true;
	}

	private void ApplyQiBurningBonuses()
	{
		int level = GetAbilityLevel(CultivationAbility.QiBurning);
		float progress = (level - 1f) / (CultivationAbilityInfo.MaxLevel - 1f);
		Player.GetDamage(DamageClass.Generic) += MathHelper.Lerp(0.30f, 0.45f, progress);
		Player.GetAttackSpeed(DamageClass.Generic) += MathHelper.Lerp(0.10f, 0.20f, progress);
		Player.GetCritChance(DamageClass.Generic) += MathHelper.Lerp(8f, 12f, progress);
		Player.moveSpeed += MathHelper.Lerp(0.15f, 0.22f, progress);
		Player.endurance = Math.Min(0.9f,
			Player.endurance + MathHelper.Lerp(0.08f, 0.12f, progress));
		Player.noKnockback = true;
	}

	private int GetQiDeviationDuration()
	{
		int level = GetAbilityLevel(CultivationAbility.QiBurning);
		float progress = (level - 1f) / (CultivationAbilityInfo.MaxLevel - 1f);
		float foundationMultiplier =
			RealmIndex >= 2
				&& foundationQuality == FoundationQuality.Heavenly
				? 0.85f : 1f;
		return (int)MathF.Round(
			MathHelper.Lerp(180f, 90f, progress)
				* foundationMultiplier * 60f);
	}

	public void RequestToggleQiBurning()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			Xianxia.SendQiBurningToggleRequest();
			return;
		}
		TryToggleQiBurningAuthoritative();
	}

	internal void TryToggleQiBurningAuthoritative()
	{
		if (qiBurningEnabled)
		{
			DisableQiBurning(applyDeviation: true, showMessage: true);
			return;
		}

		string failureKey = GetQiBurningActivationFailureKey();
		if (!string.IsNullOrEmpty(failureKey))
		{
			if (Main.netMode != NetmodeID.Server)
				Main.NewText(Mod.GetLocalization(failureKey).Value, Color.OrangeRed);
			return;
		}

		StopMeditating(syncMultiplayer: true);
		qiBurningEnabled = true;
		qiBurningPulseTimer = 0;
		if (Main.netMode != NetmodeID.Server)
		{
			Main.NewText(Mod.GetLocalization(
				"Cultivation.BurnedQi.Enabled").Value, Color.OrangeRed);
			SoundEngine.PlaySound(SoundID.Item74, Player.Center);
		}
		SyncQiBurningState();
	}

	private string GetQiBurningActivationFailureKey()
	{
		if (!(CultivationServerConfig.Instance?.EnableQiBurning ?? true))
			return "Cultivation.BurnedQi.DisabledByServer";
		if (!IsTechniqueEquipped(CultivationAbility.QiBurning))
			return "Cultivation.BurnedQi.NotEquipped";
		if (Player.dead)
			return "Cultivation.BurnedQi.Dead";
		if (!IsAbilityUnlocked(CultivationAbility.QiBurning))
			return "Cultivation.BurnedQi.RequiresFoundation";
		if (IsMeditating)
			return "Cultivation.BurnedQi.WhileMeditating";
		if (BaseMaxQi <= 0)
			return "Cultivation.BurnedQi.NoCapacity";
		if (HasQiDeviation)
			return "Cultivation.BurnedQi.DeviationBlocked";
		if (burnedQiCapacityBps >= MaximumBurnedQiBps)
			return "Cultivation.BurnedQi.LimitBlocked";
		if (IsAwaitingRealmBreakthroughConfirmation
			|| IsAwaitingTribulationConfirmation)
			return "Cultivation.BurnedQi.ConfirmationBlocked";
		if (heartDemonTrialActive)
			return "Cultivation.BurnedQi.TrialBlocked";
		return string.Empty;
	}

	private void DisableQiBurning(bool applyDeviation, bool showMessage)
	{
		if (!qiBurningEnabled)
			return;
		qiBurningEnabled = false;
		qiBurningPulseTimer = 0;
		if (applyDeviation)
			qiDeviationTimer = Math.Max(qiDeviationTimer,
				GetQiDeviationDuration());
		if (showMessage && Main.netMode != NetmodeID.Server)
		{
			Main.NewText(Mod.GetLocalization(
				"Cultivation.BurnedQi.Disabled").Format(
					MathF.Round(BurnedQiCapacityPercent, 2)),
				Color.MediumPurple);
			SoundEngine.PlaySound(SoundID.Item8, Player.Center);
		}
		SyncQiBurningState();
	}

	private void UpdateQiBurning()
	{
		if (qiDeviationTimer > 0)
			qiDeviationTimer--;
		if (heartDemonTrialCooldown > 0)
			heartDemonTrialCooldown--;
		if (qiBurningCombatTimer > 0)
			qiBurningCombatTimer--;
		if (++qiBurningExperienceWindowTimer >= 60 * 60)
		{
			qiBurningExperienceWindowTimer = 0;
			qiBurningExperienceThisWindow = 0;
		}
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		if (!qiBurningEnabled)
			return;
		if (Player.dead || RealmIndex < 2
			|| !(CultivationServerConfig.Instance?.EnableQiBurning ?? true)
			|| heartDemonTrialActive)
		{
			DisableQiBurning(applyDeviation: true, showMessage: true);
			return;
		}

		qiBurningPulseTimer++;
		if (qiBurningPulseTimer < QiBurnPulseInterval)
			return;
		qiBurningPulseTimer = 0;
		burnedQiCapacityBps = Math.Min(MaximumBurnedQiBps,
			burnedQiCapacityBps + QiBurnPerPulseBps);
		Qi = Math.Min(Qi, MaxQi);

		bool combatQualified = qiBurningCombatTimer > 0;
		for (int i = 0; !combatQualified && i < Main.maxNPCs; i++)
			combatQualified = Main.npc[i].active && Main.npc[i].boss;
		if (combatQualified
			&& qiBurningExperienceThisWindow + 5
				<= QiBurnExperiencePerMinute)
		{
			AddAbilityExperience(CultivationAbility.QiBurning, 5);
			qiBurningExperienceThisWindow += 5;
		}

		if (Main.netMode != NetmodeID.Server)
		{
			Main.NewText(Mod.GetLocalization(
				"Cultivation.BurnedQi.Pulse").Format(
					MathF.Round(BurnedQiCapacityPercent, 2)),
				burnedQiCapacityBps >= 4000 ? Color.Red : Color.OrangeRed);
			SoundEngine.PlaySound(burnedQiCapacityBps >= 4000
				? SoundID.Item122 : SoundID.Item74, Player.Center);
		}
		SyncRiskState();
		if (burnedQiCapacityBps >= MaximumBurnedQiBps)
		{
			if (Main.netMode != NetmodeID.Server)
				Main.NewText(Mod.GetLocalization(
					"Cultivation.BurnedQi.LimitReached").Value, Color.Red);
			DisableQiBurning(applyDeviation: true, showMessage: false);
		}
	}

	private void UpdateBurnedQiMeditationRepair()
	{
		if (!HasBurnedQi || !IsMeditating)
		{
			burnedQiMeditationRepairTimer = 0;
			return;
		}
		if (++burnedQiMeditationRepairTimer < BurnedQiMeditationRepairInterval)
			return;
		burnedQiMeditationRepairTimer = 0;
		int repaired = BurnedQiMeditationRepairBps
			* (SpiritualQiZoneTier + 1)
			* (PermanentFormationQiMultiplier > 1f ? 2 : 1);
		RepairBurnedQiCapacity(repaired, showMessage: true);
	}

	public int RepairBurnedQiCapacity(int basisPoints, bool showMessage)
	{
		if (basisPoints <= 0 || burnedQiCapacityBps <= 0)
			return 0;
		int repaired = Math.Min(basisPoints, burnedQiCapacityBps);
		burnedQiCapacityBps -= repaired;
		Qi = Math.Min(Qi, MaxQi);
		if (showMessage && Main.netMode != NetmodeID.Server)
		{
			Main.NewText(Mod.GetLocalization(
				"Cultivation.BurnedQi.Repaired").Format(repaired / 100f,
					BurnedQiCapacityPercent), Color.LightGreen);
		}
		SyncRiskState();
		return repaired;
	}

	private void SyncQiBurningState()
	{
		Xianxia.SendQiBurningState(Player.whoAmI, qiBurningEnabled);
		SyncRiskState();
	}

	private void SyncRiskState()
	{
		Xianxia.SendCultivationRiskState(Player.whoAmI, this);
	}

	internal void SetQiBurningFromNetwork(bool enabled)
	{
		bool changed = qiBurningEnabled != enabled;
		qiBurningEnabled = enabled;
		if (!enabled)
			qiBurningPulseTimer = 0;
		if (changed && Player.whoAmI == Main.myPlayer)
		{
			Main.NewText(Mod.GetLocalization(enabled
				? "Cultivation.BurnedQi.Enabled"
				: "Cultivation.BurnedQi.DisabledNetwork").Value,
				enabled ? Color.OrangeRed : Color.MediumPurple);
			SoundEngine.PlaySound(enabled ? SoundID.Item74 : SoundID.Item8,
				Player.Center);
		}
	}

	internal void SetRiskStateFromNetwork(int burnedBps, int deviationTicks,
		int demonPoints, int breakthroughProgress, int deathProgress,
		bool trialActive, int trialNpcIndex, int trialCooldown)
	{
		int previousBurnedBps = burnedQiCapacityBps;
		int previousDemonPoints = heartDemonPoints;
		int previousBreakthroughProgress =
			breakthroughFailuresTowardHeartDemon;
		int previousDeathProgress = deathsTowardHeartDemon;
		bool previousTrialActive = heartDemonTrialActive;
		burnedQiCapacityBps = Math.Clamp(burnedBps, 0, MaximumBurnedQiBps);
		qiDeviationTimer = Math.Max(0, deviationTicks);
		heartDemonPoints = Math.Clamp(demonPoints, 0, MaximumHeartDemonPoints);
		breakthroughFailuresTowardHeartDemon = Math.Clamp(
			breakthroughProgress, 0, BreakthroughFailuresPerHeartDemonPoint - 1);
		deathsTowardHeartDemon = Math.Clamp(
			deathProgress, 0, DeathsPerHeartDemonPoint - 1);
		heartDemonTrialActive = trialActive;
		heartDemonTrialNpcIndex = trialActive ? trialNpcIndex : -1;
		heartDemonTrialCooldown = Math.Max(0, trialCooldown);
		Qi = Math.Min(Qi, MaxQi);
		if (Player.whoAmI == Main.myPlayer)
		{
			if (burnedQiCapacityBps > previousBurnedBps)
			{
				Main.NewText(Mod.GetLocalization(
					"Cultivation.BurnedQi.Pulse").Format(
						MathF.Round(BurnedQiCapacityPercent, 2)),
					burnedQiCapacityBps >= 4000 ? Color.Red : Color.OrangeRed);
			}
			else if (burnedQiCapacityBps < previousBurnedBps)
			{
				Main.NewText(Mod.GetLocalization(
					"Cultivation.BurnedQi.Repaired").Format(
						(previousBurnedBps - burnedQiCapacityBps) / 100f,
						BurnedQiCapacityPercent), Color.LightGreen);
			}
			if (heartDemonPoints > previousDemonPoints)
			{
				heartDemonVisualTimer = 180;
				Main.NewText(Mod.GetLocalization(
					"Cultivation.HeartDemons.PointGained").Format(
						heartDemonPoints, MaximumHeartDemonPoints),
					Color.MediumPurple);
			}
			else if (deathsTowardHeartDemon != previousDeathProgress)
			{
				Main.NewText(Mod.GetLocalization(
					"Cultivation.HeartDemons.DeathCounted").Format(
						deathsTowardHeartDemon, DeathsPerHeartDemonPoint),
					Color.MediumPurple);
			}
			else if (breakthroughFailuresTowardHeartDemon
				!= previousBreakthroughProgress)
			{
				Main.NewText(Mod.GetLocalization(
					"Cultivation.HeartDemons.FailureCounted").Format(
						breakthroughFailuresTowardHeartDemon,
						BreakthroughFailuresPerHeartDemonPoint),
					Color.MediumPurple);
			}
			if (!previousTrialActive && heartDemonTrialActive)
			{
				Main.NewText(Mod.GetLocalization(
					"Cultivation.HeartDemonTrial.Started").Value,
					Color.OrangeRed);
			}
			else if (previousTrialActive && !heartDemonTrialActive)
			{
				Main.NewText(Mod.GetLocalization(heartDemonPoints == 0
					? "Cultivation.HeartDemonTrial.Purified"
					: "Cultivation.HeartDemonTrial.Failed").Value,
					heartDemonPoints == 0 ? Color.LightGreen : Color.MediumPurple);
			}
		}
	}

	private void RecordHeartDemonBreakthroughFailure()
	{
		if (suppressRiskTracking
			|| !(CultivationServerConfig.Instance?.EnableHeartDemons ?? true))
			return;
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			if (Player.whoAmI == Main.myPlayer)
				Xianxia.SendHeartDemonBreakthroughFailureRequest();
			return;
		}
		breakthroughFailuresTowardHeartDemon++;
		bool gainedPoint = false;
		if (breakthroughFailuresTowardHeartDemon
			>= BreakthroughFailuresPerHeartDemonPoint)
		{
			breakthroughFailuresTowardHeartDemon -=
				BreakthroughFailuresPerHeartDemonPoint;
			GainHeartDemonPoint();
			gainedPoint = true;
		}
		if (!gainedPoint && Main.netMode == NetmodeID.SinglePlayer)
		{
			Main.NewText(Mod.GetLocalization(
				"Cultivation.HeartDemons.FailureCounted").Format(
					breakthroughFailuresTowardHeartDemon,
					BreakthroughFailuresPerHeartDemonPoint),
				Color.MediumPurple);
		}
		SyncRiskState();
	}

	internal void RecordHeartDemonBreakthroughFailureAuthoritative()
	{
		if (Main.netMode == NetmodeID.Server)
			RecordHeartDemonBreakthroughFailure();
	}

	private void RecordHeartDemonDeath()
	{
		if (suppressRiskTracking
			|| !(CultivationServerConfig.Instance?.EnableHeartDemons ?? true))
			return;
		deathsTowardHeartDemon++;
		bool gainedPoint = false;
		if (deathsTowardHeartDemon >= DeathsPerHeartDemonPoint)
		{
			deathsTowardHeartDemon -= DeathsPerHeartDemonPoint;
			GainHeartDemonPoint();
			gainedPoint = true;
		}
		if (!gainedPoint && Main.netMode == NetmodeID.SinglePlayer)
		{
			Main.NewText(Mod.GetLocalization(
				"Cultivation.HeartDemons.DeathCounted").Format(
					deathsTowardHeartDemon, DeathsPerHeartDemonPoint),
				Color.MediumPurple);
		}
		SyncRiskState();
	}

	internal void RecordHeartDemonDeathAuthoritative()
	{
		if (Main.netMode == NetmodeID.Server)
			RecordHeartDemonDeath();
	}

	private void GainHeartDemonPoint()
	{
		if (heartDemonPoints >= MaximumHeartDemonPoints)
			return;
		heartDemonPoints++;
		heartDemonVisualTimer = 180;
		if (Main.netMode != NetmodeID.Server)
		{
			Main.NewText(Mod.GetLocalization(
				"Cultivation.HeartDemons.PointGained").Format(
					heartDemonPoints, MaximumHeartDemonPoints),
				Color.MediumPurple);
			if (heartDemonPoints >= MaximumHeartDemonPoints)
			{
				Main.NewText(Mod.GetLocalization(
					"Cultivation.HeartDemons.MaximumReached").Value,
					Color.Red);
			}
			SoundEngine.PlaySound(SoundID.Item103, Player.Center);
		}
	}

	public bool CanStartHeartDemonTrial(out string failureKey)
	{
		failureKey = string.Empty;
		if (!(CultivationServerConfig.Instance?.EnableHeartDemons ?? true))
			failureKey = "Cultivation.HeartDemonTrial.DisabledByServer";
		else if (heartDemonPoints <= 0)
			failureKey = "Cultivation.HeartDemonTrial.NoPoints";
		else if (Player.dead)
			failureKey = "Cultivation.HeartDemonTrial.Dead";
		else if (heartDemonTrialCooldown > 0)
			failureKey = "Cultivation.HeartDemonTrial.Cooldown";
		else if (qiBurningEnabled)
			failureKey = "Cultivation.HeartDemonTrial.QiBurningBlocked";
		else if (heartDemonTrialActive)
			failureKey = "Cultivation.HeartDemonTrial.AlreadyActive";
		else if (pendingTribulationRealm >= TribulationStartingRealm
			|| tribulationRealm >= TribulationStartingRealm)
			failureKey = "Cultivation.HeartDemonTrial.TribulationBlocked";
		else if (Main.invasionType > 0 || Main.pumpkinMoon
			|| Main.snowMoon || Main.eclipse)
			failureKey = "Cultivation.HeartDemonTrial.EventBlocked";
		else
		{
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (Main.npc[i].active && Main.npc[i].boss)
				{
					failureKey = "Cultivation.HeartDemonTrial.BossBlocked";
					break;
				}
			}
		}
		return string.IsNullOrEmpty(failureKey);
	}

	public void RequestHeartDemonTrialConfirmation()
	{
		if (!CanStartHeartDemonTrial(out string failureKey))
		{
			if (Main.netMode != NetmodeID.Server)
			{
				Main.NewText(Mod.GetLocalization(failureKey).Format(
					Math.Max(1, heartDemonTrialCooldown / 60)),
					Color.OrangeRed);
			}
			return;
		}
		awaitingHeartDemonTrialConfirmation = true;
		IsAbilityTreeOpen = false;
		StopMeditating(syncMultiplayer: true);
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public void CancelHeartDemonTrialConfirmation()
	{
		awaitingHeartDemonTrialConfirmation = false;
		SoundEngine.PlaySound(SoundID.MenuClose);
	}

	public void ConfirmHeartDemonTrial()
	{
		if (!awaitingHeartDemonTrialConfirmation)
			return;
		awaitingHeartDemonTrialConfirmation = false;
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			Xianxia.SendHeartDemonTrialRequest();
			return;
		}
		StartHeartDemonTrialAuthoritative();
	}

	internal void StartHeartDemonTrialAuthoritative()
	{
		if (!CanStartHeartDemonTrial(out _))
			return;
		Vector2 spawn = Player.Center + new Vector2(
			Player.direction == 0 ? 320f : Player.direction * 320f, -80f);
		int npcIndex = NPC.NewNPC(
			Player.GetSource_Misc("XianxiaHeartDemonTrial"),
			(int)spawn.X, (int)spawn.Y,
			ModContent.NPCType<HeartDemon>(),
			ai0: Player.whoAmI,
			ai1: RealmIndex,
			ai2: Stage,
			ai3: heartDemonPoints);
		if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
			return;
		heartDemonTrialActive = true;
		heartDemonTrialNpcIndex = npcIndex;
		Main.npc[npcIndex].netUpdate = true;
		if (Main.netMode != NetmodeID.Server)
		{
			Main.NewText(Mod.GetLocalization(
				"Cultivation.HeartDemonTrial.Started").Value,
				Color.MediumPurple);
			SoundEngine.PlaySound(SoundID.Roar, Player.Center);
		}
		SyncRiskState();
	}

	internal void CompleteHeartDemonTrial(int npcIndex)
	{
		if (!heartDemonTrialActive || heartDemonTrialNpcIndex != npcIndex)
			return;
		heartDemonPoints = 0;
		breakthroughFailuresTowardHeartDemon = 0;
		deathsTowardHeartDemon = 0;
		heartDemonTrialActive = false;
		heartDemonTrialNpcIndex = -1;
		heartDemonTrialCooldown = 0;
		heartDemonVisualTimer = 240;
		if (Main.netMode != NetmodeID.Server)
		{
			Main.NewText(Mod.GetLocalization(
				"Cultivation.HeartDemonTrial.Purified").Value,
				Color.LightGreen);
			SoundEngine.PlaySound(SoundID.Item29, Player.Center);
			StartBreakthroughEffect(isRealmBreakthrough: false);
		}
		SyncRiskState();
	}

	internal void FailHeartDemonTrial(bool showMessage)
	{
		if (!heartDemonTrialActive)
			return;
		heartDemonTrialActive = false;
		heartDemonTrialNpcIndex = -1;
		heartDemonTrialCooldown = HeartDemonTrialRetryCooldown;
		if (showMessage && Main.netMode != NetmodeID.Server)
		{
			Main.NewText(Mod.GetLocalization(
				"Cultivation.HeartDemonTrial.Failed").Value,
				Color.OrangeRed);
		}
		SyncRiskState();
	}

	public string GetRealmName()
	{
		return GetRealmName(RealmIndex);
	}

	public string GetRealmName(int realmIndex)
	{
		int safeRealmIndex = Math.Clamp(realmIndex, 0, RealmKeys.Length - 1);
		return Mod.GetLocalization($"Cultivation.Realms.{RealmKeys[safeRealmIndex]}").Value;
	}

	public string GetRealmBonusSummary()
	{
		CultivationBonus bonus = CalculateCultivationBonus();
		return Mod.GetLocalization("Cultivation.RealmBonuses").Format(
			(int)MathF.Round(bonus.MaxLife),
			(int)MathF.Round(bonus.Defense),
			MathF.Round(bonus.DamagePercent, 1),
			MathF.Round(bonus.MoveSpeedPercent, 1),
			MathF.Round(bonus.CritChance, 1),
			MathF.Round(bonus.EndurancePercent, 1),
			(int)MathF.Round(bonus.LifeRegen)
		);
	}

	public string GetRealmBonusPrimarySummary()
	{
		CultivationBonus bonus = CalculateCultivationBonus();
		return Mod.GetLocalization("CharacterStats.CultivationBonusPrimary").Format(
			(int)MathF.Round(bonus.MaxLife),
			(int)MathF.Round(bonus.Defense),
			MathF.Round(bonus.DamagePercent, 1),
			MathF.Round(bonus.MoveSpeedPercent, 1));
	}

	public string GetRealmBonusSecondarySummary()
	{
		CultivationBonus bonus = CalculateCultivationBonus();
		return Mod.GetLocalization("CharacterStats.CultivationBonusSecondary").Format(
			MathF.Round(bonus.CritChance, 1),
			MathF.Round(bonus.EndurancePercent, 1),
			MathF.Round(bonus.LifeRegen, 1));
	}

	public string GetNextStageBonusSummary()
	{
		if (IsCultivationMaxed)
		{
			return Mod.GetLocalization("Cultivation.BreakthroughTooltip.MaximumReached").Value;
		}

		int destinationGlobalStage = GlobalStageIndex + 1;
		int destinationRealm = Math.Clamp(destinationGlobalStage / StagesPerRealm, 0, TotalRealms - 1);
		CultivationBonus growth = StageGrowthByRealm[destinationRealm];
		float multiplier = destinationRealm > 0 && destinationGlobalStage % StagesPerRealm == 0
			? 3f
			: 1f;
		return Mod.GetLocalization("Cultivation.BreakthroughTooltip.Bonuses").Format(
			(int)MathF.Round(growth.MaxLife * multiplier),
			(int)MathF.Round(growth.Defense * multiplier),
			MathF.Round(growth.DamagePercent * multiplier, 1),
			MathF.Round(growth.MoveSpeedPercent * multiplier, 1),
			MathF.Round(growth.CritChance * multiplier, 1),
			MathF.Round(growth.EndurancePercent * multiplier, 1),
			MathF.Round(growth.LifeRegen * multiplier, 1));
	}

	public int EstimateNextTribulationShieldCostPerStrike()
	{
		if (!NextBreakthroughRequiresTribulation)
		{
			return 0;
		}

		int realmOffset = NextBreakthroughTargetRealm - TribulationStartingRealm;
		float reserveFraction = 0.12f + realmOffset * 0.03f;
		float masteryCostMultiplier = GetQiProtectionCostPerDamage()
			/ (float)QiProtectionCostPerDamage;
		int prospectiveMaxQi = Math.Max(MaxQi, NextStageThreshold);
		return Math.Max(1,
			(int)MathF.Ceiling(prospectiveMaxQi * reserveFraction * masteryCostMultiplier));
	}

	private CultivationBonus CalculateCultivationBonus()
	{
		float maxLife = 0f;
		float defense = 0f;
		float damagePercent = 0f;
		float moveSpeedPercent = 0f;
		float critChance = 0f;
		float endurancePercent = 0f;
		float lifeRegen = 0f;

		for (int globalStage = 1; globalStage <= GlobalStageIndex; globalStage++)
		{
			int stageRealm = Math.Clamp(globalStage / StagesPerRealm, 0, TotalRealms - 1);
			CultivationBonus growth = StageGrowthByRealm[stageRealm];
			float multiplier = stageRealm > 0 && globalStage % StagesPerRealm == 0
				? 3f
				: 1f;
			if (stageRealm == 2)
				multiplier *= FoundationStatMultiplier;
			else if (stageRealm == 3)
				multiplier *= GoldenCoreStatMultiplier;

			maxLife += growth.MaxLife * multiplier;
			defense += growth.Defense * multiplier;
			damagePercent += growth.DamagePercent * multiplier;
			moveSpeedPercent += growth.MoveSpeedPercent * multiplier;
			critChance += growth.CritChance * multiplier;
			endurancePercent += growth.EndurancePercent * multiplier;
			lifeRegen += growth.LifeRegen * multiplier;
		}

		return new CultivationBonus(
			maxLife,
			defense,
			damagePercent,
			moveSpeedPercent,
			critChance,
			endurancePercent,
			lifeRegen
		);
	}

	private static float GetBaseRealmBreakthroughChance(int targetRealm) =>
		targetRealm switch
		{
			1 => 90f,
			2 => 75f,
			3 => 55f,
			4 => 35f,
			_ => 100f
		};

	private bool TryRealmBreakthrough(int targetRealm)
	{
		if (forceNextRealmBreakthrough)
			return true;

		int realmThreshold =
			GetGlobalStageThreshold(targetRealm * StagesPerRealm);
		if (HasBurnedQi)
		{
			QiExp = Math.Min(QiExp, Math.Max(0, realmThreshold - 1));
			Qi = Math.Min(Qi, MaxQi);
			Main.NewText(Mod.GetLocalization(
				"Cultivation.BurnedQi.BreakthroughBlocked").Value,
				Color.OrangeRed);
			return false;
		}
		AlchemyPillEffectPlayer pillEffects =
			Player.GetModPlayer<AlchemyPillEffectPlayer>();
		bool usesPill = GetSelectedBreakthroughPillChanceBonus() > 0f;
		bool needsTreasure = targetRealm switch
		{
			2 => pendingFoundationQuality
				is FoundationQuality.Perfect
				or FoundationQuality.Heavenly
				|| !usesPill,
			3 => true,
			_ => false
		};
		bool foundationRequirementsMet = targetRealm != 2
			|| pendingFoundationQuality switch
			{
				FoundationQuality.Inferior or FoundationQuality.Stable =>
					HasSelectedBreakthroughTreasure || usesPill,
				FoundationQuality.Perfect =>
					HasSelectedBreakthroughTreasure,
				FoundationQuality.Heavenly =>
					HasSelectedBreakthroughTreasure && usesPill,
				_ => false
			};
		if (!foundationRequirementsMet)
		{
			QiExp = Math.Min(QiExp, Math.Max(0, realmThreshold - 1));
			Qi = Math.Min(Qi, MaxQi);
			if (breakthroughWarningCooldown <= 0)
			{
				breakthroughWarningCooldown = 300;
				Main.NewText(Mod.GetLocalization(
					"Cultivation.BreakthroughChance.MissingFoundationCatalyst").Value,
					Color.OrangeRed);
			}
			return false;
		}
		if (targetRealm == 3
			&& (!HasSelectedBreakthroughTreasure
				|| pendingGoldenCoreTier == 1 && !usesPill))
		{
			QiExp = Math.Min(QiExp, Math.Max(0, realmThreshold - 1));
			Qi = Math.Min(Qi, MaxQi);
			if (breakthroughWarningCooldown <= 0)
			{
				breakthroughWarningCooldown = 300;
				Main.NewText(Mod.GetLocalization(
					"Cultivation.BreakthroughChance.MissingGoldenCoreTreasure").Value,
					Color.OrangeRed);
			}
			return false;
		}

		float chance = Math.Clamp(
			GetBaseRealmBreakthroughChance(targetRealm)
				+ Player.GetModPlayer<SpiritualRootPlayer>().BreakthroughChanceModifier
				+ Player.GetModPlayer<AlchemyPillEffectPlayer>()
					.GetBreakthroughChanceBonus(targetRealm)
				+ (pillEffects.GetBreakthroughChanceBonus(targetRealm) > 0f
					? 0f : GetSelectedBreakthroughPillChanceBonus())
				+ GetBreakthroughGradeChanceModifier(targetRealm)
				- HeartDemonBreakthroughPenalty,
			10f, 95f);
		bool usedPill = usesPill;
		realmBreakthroughAttempts++;
		if (usedPill)
			breakthroughPillsConsumed++;
		ConsumeSelectedBreakthroughPill(targetRealm);
		if (Main.rand.NextFloat(100f) >= chance)
		{
			realmBreakthroughFailures++;
			RecordHeartDemonBreakthroughFailure();
			int previousThreshold =
				GetGlobalStageThreshold(targetRealm * StagesPerRealm - 1);
			int finalStepCost = Math.Max(1, realmThreshold - previousThreshold);
			int lostProgress = Math.Max(1,
				(int)MathF.Ceiling(finalStepCost * 0.25f));
			QiExp = Math.Max(previousThreshold, realmThreshold - lostProgress);
			Qi = Math.Min(Qi, MaxQi);
			Main.NewText(Mod.GetLocalization(
				"Cultivation.BreakthroughChance.Failed").Format(
					MathF.Round(chance, 1), lostProgress), Color.OrangeRed);
			ClearPendingBreakthroughSelections();
			return false;
		}

		int consumedTreasure = 0;
		if (needsTreasure)
			consumedTreasure = ConsumeHeavenlyTreasureAndApplyImprint(
				selectedBreakthroughTreasureType);
		if (targetRealm >= TribulationStartingRealm)
		{
			pendingBreakthroughTreasure = consumedTreasure;
			pendingBreakthroughUsedPill = usedPill;
			if (targetRealm == 3)
				pendingBreakthroughGoldenCoreTier =
					pendingGoldenCoreTier;
			ClearPendingBreakthroughSelections();
		}
		else
		{
			RecordSuccessfulRealmBreakthrough(
				targetRealm, consumedTreasure, usedPill);
		}
		Main.NewText(Mod.GetLocalization(
			"Cultivation.BreakthroughChance.Succeeded").Format(
				MathF.Round(chance, 1)), Color.LightGreen);
		return true;
	}

	private void ConsumeSelectedBreakthroughPill(int targetRealm)
	{
		AlchemyPillEffectPlayer pillEffects =
			Player.GetModPlayer<AlchemyPillEffectPlayer>();
		if (pillEffects.GetBreakthroughChanceBonus(targetRealm) > 0f)
		{
			pillEffects.ConsumeBreakthroughPill(targetRealm);
			return;
		}
		int pillType = GetEffectiveSelectedBreakthroughPillType();
		if (pillType <= 0)
			return;
		foreach (Item item in Player.inventory)
		{
			if (item.type != pillType)
				continue;
			if (item.ModItem is IAlchemyPill pill)
			{
				Player.GetModPlayer<AlchemyPlayer>().AddSaturation(
					AlchemyGlobalItem.GetAdjustedSaturationCost(item, pill));
			}
			item.stack--;
			if (item.stack <= 0)
				item.TurnToAir();
			return;
		}
	}

	private int ConsumeHeavenlyTreasureAndApplyImprint(int selectedType)
	{
		for (int slot = 0; slot < Player.inventory.Length; slot++)
		{
			Item item = Player.inventory[slot];
			if (item.type != selectedType)
				continue;
			string boonKey;
			if (item.type == ModContent.ItemType<HeavenlyEyeEssence>())
			{
				heavenlyEyeImprints++;
				boonKey = "HeavenlyEyeImprint";
				pendingBreakthroughTreasure = 1;
			}
			else if (item.type == ModContent.ItemType<HeavenlyRoyalNectar>())
			{
				heavenlyRoyalNectarImprints++;
				boonKey = "HeavenlyRoyalNectarImprint";
				pendingBreakthroughTreasure = 2;
			}
			else if (item.type == ModContent.ItemType<HeavenlyBoneMarrow>())
			{
				heavenlyBoneMarrowImprints++;
				boonKey = "HeavenlyBoneMarrowImprint";
				pendingBreakthroughTreasure = 3;
			}
			else
			{
				continue;
			}

			item.stack--;
			if (item.stack <= 0)
				item.TurnToAir();
			Main.NewText(Mod.GetLocalization(
				$"Cultivation.BreakthroughChance.{boonKey}").Value,
				Color.LightGoldenrodYellow);
			return pendingBreakthroughTreasure;
		}
		return 0;
	}

	private void RecordSuccessfulRealmBreakthrough(
		int targetRealm, int treasure, bool usedPill)
	{
		realmBreakthroughSuccesses++;
		if (targetRealm == 2)
		{
			foundationQuality = pendingFoundationQuality;
			if (Main.netMode != NetmodeID.Server)
				Main.NewText(Mod.GetLocalization(
					"Cultivation.BreakthroughGradeAchieved").Format(
						GetFoundationQualityName()), Color.Gold);
		}
		else if (targetRealm == 3)
		{
			goldenCoreTier = Math.Clamp(
				pendingBreakthroughGoldenCoreTier, 1, 9);
			if (Main.netMode != NetmodeID.Server)
				Main.NewText(Mod.GetLocalization(
					"Cultivation.BreakthroughGradeAchieved").Format(
						Mod.GetLocalization(
							"BreakthroughGrades.GoldenCoreTier")
							.Format(goldenCoreTier)), Color.Gold);
		}
		if (targetRealm >= 0 && targetRealm < successfulBreakthroughTreasures.Length)
		{
			successfulBreakthroughTreasures[targetRealm] =
				Math.Clamp(treasure, 0, 3);
			if (usedPill)
				successfulBreakthroughPillMask |= 1 << targetRealm;
			successfulBreakthroughRecordedMask |= 1 << targetRealm;
		}
		pendingBreakthroughTreasure = 0;
		pendingBreakthroughUsedPill = false;
		pendingBreakthroughGoldenCoreTier = 9;
		ClearPendingBreakthroughSelections();
	}

	private void UpdateRealm(bool showMessage)
	{
		if (pendingTribulationRealm >= TribulationStartingRealm)
		{
			return;
		}
		if (deferredTribulationRealm >= TribulationStartingRealm)
		{
			RealmIndex = deferredTribulationRealm - 1;
			Stage = StagesPerRealm;
			return;
		}

		int previousRealm = RealmIndex;
		int previousStage = Stage;
		int globalStageIndex = 0;
		for (int i = 1; i <= MaxGlobalStageIndex; i++)
		{
			if (QiExp < GetGlobalStageThreshold(i))
			{
				break;
			}

			globalStageIndex = i;
		}

		int targetRealm = globalStageIndex / StagesPerRealm;
		int targetStage = globalStageIndex % StagesPerRealm + 1;

		if (showMessage && targetRealm > previousRealm)
		{
			// Keep meditation running while burned Qi capacity is being repaired.
			// Opening the confirmation here would stop meditation and reopen again
			// on every cultivation gain while QiEXP remains above the threshold.
			if (HasBurnedQi)
			{
				RealmIndex = previousRealm;
				Stage = StagesPerRealm;
				return;
			}

			int realmToAttempt = previousRealm + 1;
			if (!forceNextRealmBreakthrough
				&& confirmedRealmBreakthrough != realmToAttempt)
			{
				RequestRealmBreakthroughConfirmation(realmToAttempt);
				return;
			}
			confirmedRealmBreakthrough = -1;
			if (!TryRealmBreakthrough(realmToAttempt))
				return;
		}

		if (showMessage && targetRealm > previousRealm && targetRealm >= TribulationStartingRealm)
		{
			int realmToChallenge = previousRealm + 1;
			int realmThreshold = GetGlobalStageThreshold(realmToChallenge * StagesPerRealm);
			QiExp = Math.Min(QiExp, realmThreshold);
			Qi = Math.Min(Qi, MaxQi);
			RealmIndex = previousRealm;
			Stage = StagesPerRealm;
			RequestTribulationConfirmation(realmToChallenge);
			return;
		}

		RealmIndex = targetRealm;
		Stage = targetStage;

		if (showMessage && RealmIndex > previousRealm)
		{
			FillEmptyTechniqueLoadoutSlots();
			NormalizeTechniqueLoadout();
			SyncTechniqueLoadout();
			Main.NewText(Mod.GetLocalization("Cultivation.Breakthrough").Format(GetRealmName()), Color.Gold);
			Main.NewText(GetRealmBonusSummary(), new Color(120, 240, 255));
			StartBreakthroughEffect(isRealmBreakthrough: true);
			if (previousRealm < 1 && RealmIndex >= 1)
			{
				Main.NewText(Mod.GetLocalization("Abilities.QiGatheringAbilitiesUnlocked").Value, Color.Cyan);
			}
			if (previousRealm < 2 && RealmIndex >= 2)
			{
				Main.NewText(Mod.GetLocalization("Abilities.QiProtectionUnlocked").Value, Color.Cyan);
				Main.NewText(Mod.GetLocalization("Abilities.FlameStepUnlocked").Value, new Color(255, 140, 50));
				Main.NewText(Mod.GetLocalization("Abilities.NightVisionUnlocked").Value, new Color(145, 225, 255));
			}
		}
		else if (showMessage && Stage > previousStage)
		{
			Main.NewText(Mod.GetLocalization("Cultivation.StageAdvanced").Format(
				GetRealmName(), Stage), new Color(120, 240, 255));
			Main.NewText(GetRealmBonusSummary(), new Color(120, 240, 255));
			StartBreakthroughEffect(isRealmBreakthrough: false);
		}
	}

	private static int GetGlobalStageThreshold(int globalStageIndex)
	{
		return GetGlobalStageThreshold(globalStageIndex,
			GetConfiguredCultivationRequirementMultiplier());
	}

	private static int GetGlobalStageThreshold(int globalStageIndex,
		int requirementMultiplier)
	{
		int clampedIndex = Math.Clamp(globalStageIndex, 0, MaxGlobalStageIndex);
		int totalQi = 0;
		for (int destinationGlobalStage = 1;
			destinationGlobalStage <= clampedIndex;
			destinationGlobalStage++)
		{
			int destinationRealm = destinationGlobalStage / StagesPerRealm;
			int stageIndexInRealm = destinationGlobalStage % StagesPerRealm;
			totalQi += StageQiBaseCostByRealm[destinationRealm]
				+ StageQiCostIncreaseByRealm[destinationRealm] * stageIndexInRealm;
		}

		long scaledQi = (long)totalQi * Math.Clamp(requirementMultiplier, 1, 10);
		return (int)Math.Min(int.MaxValue, scaledQi);
	}

	private static int GetConfiguredCultivationRequirementMultiplier() =>
		Math.Clamp(
			CultivationServerConfig.Instance?.CultivationRequirementMultiplier ?? 1,
			1, 10);

	private void UpdateCultivationRequirementMultiplier()
	{
		int configuredMultiplier = GetConfiguredCultivationRequirementMultiplier();
		if (configuredMultiplier == appliedCultivationRequirementMultiplier)
			return;

		int rebasedQiExp = QiExp;
		int rebasedQi = Qi;
		RebaseProgressionForRequirementMultiplier(ref rebasedQiExp, ref rebasedQi,
			appliedCultivationRequirementMultiplier, configuredMultiplier);
		QiExp = rebasedQiExp;
		Qi = Math.Min(rebasedQi, MaxQi);
		appliedCultivationRequirementMultiplier = configuredMultiplier;
		UpdateRealm(showMessage: false);
	}

	private static void RebaseProgressionForRequirementMultiplier(
		ref int qiExp, ref int qi, int oldMultiplier, int newMultiplier)
	{
		oldMultiplier = Math.Clamp(oldMultiplier, 1, 10);
		newMultiplier = Math.Clamp(newMultiplier, 1, 10);
		qiExp = Math.Max(0, qiExp);
		qi = Math.Clamp(qi, 0, qiExp);

		int globalStageIndex = 0;
		for (int i = 1; i <= MaxGlobalStageIndex; i++)
		{
			if (qiExp < GetGlobalStageThreshold(i, oldMultiplier))
				break;
			globalStageIndex = i;
		}

		float storedQiRatio = qiExp > 0 ? qi / (float)qiExp : 0f;
		if (globalStageIndex >= MaxGlobalStageIndex)
		{
			qiExp = GetGlobalStageThreshold(MaxGlobalStageIndex, newMultiplier);
		}
		else
		{
			int oldCurrent = GetGlobalStageThreshold(globalStageIndex, oldMultiplier);
			int oldNext = GetGlobalStageThreshold(globalStageIndex + 1, oldMultiplier);
			float stageProgress = oldNext > oldCurrent
				? MathHelper.Clamp((qiExp - oldCurrent) / (float)(oldNext - oldCurrent),
					0f, 1f)
				: 0f;
			int newCurrent = GetGlobalStageThreshold(globalStageIndex, newMultiplier);
			int newNext = GetGlobalStageThreshold(globalStageIndex + 1, newMultiplier);
			qiExp = newCurrent
				+ (int)MathF.Round((newNext - newCurrent) * stageProgress);
		}

		qi = Math.Clamp((int)MathF.Round(qiExp * storedQiRatio), 0, qiExp);
	}

	private static int GetLegacyGlobalStageThreshold(int globalStageIndex)
	{
		int clampedIndex = Math.Clamp(globalStageIndex, 0, MaxGlobalStageIndex);
		return clampedIndex
			* (2 * LegacyBaseStageQiCost + (clampedIndex - 1) * LegacyStageQiCostIncrease)
			/ 2;
	}

	private static void MigrateLegacyProgression(
		int legacyQiExp,
		int legacyQi,
		out int migratedQiExp,
		out int migratedQi)
	{
		legacyQiExp = Math.Clamp(legacyQiExp, 0, GetLegacyGlobalStageThreshold(MaxGlobalStageIndex));
		int legacyGlobalStage = 0;
		for (int i = 1; i <= MaxGlobalStageIndex; i++)
		{
			if (legacyQiExp < GetLegacyGlobalStageThreshold(i))
			{
				break;
			}

			legacyGlobalStage = i;
		}

		int oldCurrentThreshold = GetLegacyGlobalStageThreshold(legacyGlobalStage);
		int oldNextThreshold = GetLegacyGlobalStageThreshold(
			Math.Min(legacyGlobalStage + 1, MaxGlobalStageIndex));
		float stageProgress = oldNextThreshold > oldCurrentThreshold
			? (legacyQiExp - oldCurrentThreshold) / (float)(oldNextThreshold - oldCurrentThreshold)
			: 0f;

		int newCurrentThreshold = GetGlobalStageThreshold(legacyGlobalStage);
		int newNextThreshold = GetGlobalStageThreshold(
			Math.Min(legacyGlobalStage + 1, MaxGlobalStageIndex));
		migratedQiExp = newCurrentThreshold
			+ (int)MathF.Round((newNextThreshold - newCurrentThreshold) * stageProgress);

		float qiFillRatio = legacyQiExp > 0
			? MathHelper.Clamp(legacyQi / (float)legacyQiExp, 0f, 1f)
			: 0f;
		migratedQi = (int)MathF.Round(migratedQiExp * qiFillRatio);
	}
}
