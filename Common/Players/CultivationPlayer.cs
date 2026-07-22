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
using Xianxia.Content.Items.Guides;
using Xianxia.Content.Projectiles;
using Xianxia.Content.Tiles;
using Xianxia.Common.Config;
using Xianxia.Common.Abilities;

namespace Xianxia.Common.Players;

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

	private static readonly int[] RealmThresholds =
	[
		GetGlobalStageThreshold(0),
		GetGlobalStageThreshold(9),
		GetGlobalStageThreshold(18),
		GetGlobalStageThreshold(27),
		GetGlobalStageThreshold(36)
	];
	private static readonly int[] RealmEndThresholds =
	[
		GetGlobalStageThreshold(9),
		GetGlobalStageThreshold(18),
		GetGlobalStageThreshold(27),
		GetGlobalStageThreshold(36),
		GetGlobalStageThreshold(MaxGlobalStageIndex)
	];
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
	private int nascentSoulRegenerationTrainingTimer;
	private float qiProtectionDotQiAccumulator;
	private int qiProtectionDotVisualCooldown;
	private int drowningProtectionTimer;
	private int stillnessWarningCooldown;
	private int breakthroughEffectTimer;
	private int breakthroughEffectRealm;
	private bool realmBreakthroughEffect;
	private int tribulationRealm = -1;
	private int tribulationTimer;
	private int tribulationStrikesRemaining;
	private int pendingTribulationRealm = -1;
	private bool awaitingTribulationConfirmation;
	private bool resolvingTribulationLightning;
	private bool flightMaintainedDuringChat;
	private bool meditationToggleRequested;
	private readonly int[] abilityExperience = new int[(int)CultivationAbility.Count];
	private readonly int[] abilityLevels = new int[(int)CultivationAbility.Count];

	public int Qi { get; private set; }
	public int QiExp { get; private set; }
	public int RealmIndex { get; private set; }
	public int Stage { get; private set; }
	public int GlobalStageIndex => RealmIndex * StagesPerRealm + Stage - 1;
	public bool IsMeditating { get; private set; }
	public bool IsFlyingWithQi { get; private set; }
	public bool QiFlightEnabled { get; private set; }
	public bool QiProtectionEnabled { get; private set; }
	public bool QiSenseEnabled { get; private set; }
	public bool SpiritualPressureEnabled { get; private set; }
	public bool NightVisionEnabled { get; private set; }
	public bool IsAbilityWheelOpen { get; private set; }
	public bool IsAbilityTreeOpen { get; private set; }
	public bool HasReceivedCultivatorManual { get; private set; }
	public int EquipmentPassiveQiBonus { get; set; }
	public int EquipmentMeditationQiBonus { get; set; }
	public int NearbySpiritCrystalCount { get; private set; }
	public int SpiritualQiZoneTier => Math.Clamp(
		(NearbySpiritCrystalCount + 49) / 50,
		0,
		10);
	public int SpiritualQiZoneBonusPercent => SpiritualQiZoneTier * 100;
	public bool IsInSpiritualQiZone => SpiritualQiZoneTier > 0;
	public float SpiritualQiZoneMultiplier => 1f + SpiritualQiZoneBonusPercent / 100f;
	public float MeditationQiPerSecond =>
		(MeditationQiGainByRealm[RealmIndex] + EquipmentMeditationQiBonus)
		* SpiritualQiZoneMultiplier * GetAbilityPowerMultiplier(CultivationAbility.Meditation, 0.05f);
	public float PassiveQiRecoveryPerSecond =>
		(PassiveQiRecoveryByRealm[RealmIndex] + EquipmentPassiveQiBonus)
		* SpiritualQiZoneMultiplier
		* (1.10f + (GetAbilityLevel(CultivationAbility.SpiritBreathing) - 1) * 0.03f);
	public int CurrentRealmThreshold => RealmThresholds[RealmIndex];
	public bool IsAtMaxRealm => RealmIndex >= RealmThresholds.Length - 1;
	public bool IsCultivationMaxed => GlobalStageIndex >= MaxGlobalStageIndex;
	public int NextRealmThreshold => RealmEndThresholds[RealmIndex];
	public int CurrentStageThreshold => GetGlobalStageThreshold(GlobalStageIndex);
	public int NextStageThreshold => GetGlobalStageThreshold(Math.Min(GlobalStageIndex + 1, MaxGlobalStageIndex));
	public int QiRequiredForNextStage => NextStageThreshold - CurrentStageThreshold;
	public int MaxQi => QiExp;
	public bool CanUseQiProtection => QiProtectionEnabled && RealmIndex >= 2;
	public bool HasUnlockedQiProtection => RealmIndex >= 2;
	public bool HasUnlockedQiSense => RealmIndex >= 1;
	public bool CanUseQiSense => QiSenseEnabled && HasUnlockedQiSense && Qi > 0;
	public bool IsAwaitingTribulationConfirmation =>
		awaitingTribulationConfirmation && pendingTribulationRealm >= TribulationStartingRealm;
	public string PendingTribulationRealmName => pendingTribulationRealm >= 0
		? GetRealmName(pendingTribulationRealm)
		: string.Empty;
	public int PendingTribulationStrikeCount => pendingTribulationRealm >= TribulationStartingRealm
		? 9 + (pendingTribulationRealm - TribulationStartingRealm) * 2
		: 0;
	public bool NextBreakthroughRequiresTribulation =>
		!IsCultivationMaxed && Stage == StagesPerRealm && RealmIndex + 1 >= TribulationStartingRealm;
	public int NextBreakthroughTargetRealm => Math.Min(RealmIndex + (Stage == StagesPerRealm ? 1 : 0),
		TotalRealms - 1);
	public int NextBreakthroughTargetStage => Stage == StagesPerRealm ? 1 : Stage + 1;
	public int NextBreakthroughTribulationStrikes => NextBreakthroughRequiresTribulation
		? 9 + (NextBreakthroughTargetRealm - TribulationStartingRealm) * 2
		: 0;
	public bool IsAbilityUnlocked(CultivationAbility ability) =>
		RealmIndex >= CultivationAbilityInfo.RequiredRealm(ability);
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
		nascentSoulRegenerationTrainingTimer = 0;
		qiProtectionDotQiAccumulator = 0f;
		qiProtectionDotVisualCooldown = 0;
		drowningProtectionTimer = 0;
		stillnessWarningCooldown = 0;
		breakthroughEffectTimer = 0;
		breakthroughEffectRealm = 0;
		realmBreakthroughEffect = false;
		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;
		pendingTribulationRealm = -1;
		awaitingTribulationConfirmation = false;
		IsMeditating = false;
		meditationToggleRequested = false;
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
	}

	public override void SaveData(TagCompound tag)
	{
		tag["qi"] = Qi;
		tag["qiExp"] = QiExp;
		tag["progressionVersion"] = ProgressionVersion;
		tag["qiProtectionEnabled"] = QiProtectionEnabled;
		tag["qiSenseEnabled"] = QiSenseEnabled;
		tag["hasReceivedCultivatorManual"] = HasReceivedCultivatorManual;
		tag["abilityExperience"] = new System.Collections.Generic.List<int>(abilityExperience);
		tag["abilityLevels"] = new System.Collections.Generic.List<int>(abilityLevels);
		if (pendingTribulationRealm >= TribulationStartingRealm)
		{
			tag["pendingTribulationRealm"] = pendingTribulationRealm;
			tag["awaitingTribulationConfirmation"] = awaitingTribulationConfirmation;
		}
	}

	public override void LoadData(TagCompound tag)
	{
		int savedQi = tag.GetInt("qi");
		int savedQiExp = tag.ContainsKey("qiExp") ? tag.GetInt("qiExp") : savedQi;
		int savedProgressionVersion = tag.ContainsKey("progressionVersion")
			? tag.GetInt("progressionVersion")
			: 1;
		if (savedProgressionVersion < ProgressionVersion)
		{
			MigrateLegacyProgression(savedQiExp, savedQi, out savedQiExp, out savedQi);
		}

		QiExp = savedQiExp;
		QiExp = Math.Clamp(QiExp, 0, GetGlobalStageThreshold(MaxGlobalStageIndex));
		Qi = Math.Clamp(savedQi, 0, QiExp);
		QiProtectionEnabled = tag.GetBool("qiProtectionEnabled");
		QiSenseEnabled = tag.GetBool("qiSenseEnabled");
		HasReceivedCultivatorManual = tag.GetBool("hasReceivedCultivatorManual");
		System.Collections.Generic.IList<int> savedAbilityExperience = tag.GetList<int>("abilityExperience");
		System.Collections.Generic.IList<int> savedAbilityLevels = tag.GetList<int>("abilityLevels");
		for (int i = 0; i < abilityLevels.Length; i++)
		{
			abilityExperience[i] = i < savedAbilityExperience.Count ? Math.Max(0, savedAbilityExperience[i]) : 0;
			abilityLevels[i] = i < savedAbilityLevels.Count
				? Math.Clamp(savedAbilityLevels[i], 1, CultivationAbilityInfo.MaxLevel)
				: 1;
		}
		pendingTribulationRealm = tag.ContainsKey("pendingTribulationRealm")
			? Math.Clamp(tag.GetInt("pendingTribulationRealm"), TribulationStartingRealm, TotalRealms - 1)
			: -1;
		awaitingTribulationConfirmation = pendingTribulationRealm >= TribulationStartingRealm
			&& tag.GetBool("awaitingTribulationConfirmation");

		if (pendingTribulationRealm >= TribulationStartingRealm)
		{
			RealmIndex = pendingTribulationRealm - 1;
			Stage = StagesPerRealm;
			QiExp = Math.Min(QiExp, GetGlobalStageThreshold(pendingTribulationRealm * StagesPerRealm));
			Qi = Math.Min(Qi, QiExp);
		}
		else
		{
			UpdateRealm(showMessage: false);
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

	private int GetQiProtectionCostPerDamage() => Math.Max(2,
		QiProtectionCostPerDamage - (GetAbilityLevel(CultivationAbility.QiProtection) - 1) / 5);

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
		if (IsAbilityWheelOpen || IsAbilityTreeOpen || IsAwaitingTribulationConfirmation)
		{
			Player.controlUseItem = false;
			Player.controlUseTile = false;
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
		float cultivationPerSecond = MeditationQiPerSecond;
		int missingQi = Math.Max(0, QiExp - Qi);
		int totalGained = 0;

		if (missingQi <= 0)
		{
			int cultivationGain = TakeWholeQiGain(
				cultivationPerSecond,
				ref meditationQiGainRemainder);
			AddQi(cultivationGain);
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
				AddQi(cultivationGain);
				totalGained += cultivationGain;
			}
		}

		if (totalGained > 0)
		{
			AddAbilityExperience(CultivationAbility.Meditation, 5);
		}
	}

	public void CloseAbilityTree() => IsAbilityTreeOpen = false;

	internal void DebugSetProgression(int realmIndex, int stage)
	{
		RealmIndex = Math.Clamp(realmIndex, 0, TotalRealms - 1);
		Stage = Math.Clamp(stage, 1, StagesPerRealm);
		int globalStageIndex = RealmIndex * StagesPerRealm + Stage - 1;
		QiExp = GetGlobalStageThreshold(globalStageIndex);
		Qi = QiExp;
		ClearDebugTribulationState();
		QiFlightEnabled = false;
		QiProtectionEnabled = false;
		QiSenseEnabled = false;
		SpiritualPressureEnabled = false;
		NightVisionEnabled = false;
	}

	internal void DebugSetQi(int amount)
	{
		Qi = Math.Clamp(amount, 0, QiExp);
	}

	internal bool DebugAdvanceStage()
	{
		if (IsCultivationMaxed || pendingTribulationRealm >= TribulationStartingRealm)
		{
			return false;
		}

		Qi = QiExp;
		int required = NextStageThreshold - QiExp;
		if (required <= 0)
		{
			return false;
		}

		AddQi(required);
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
			FailTribulation();
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
		awaitingTribulationConfirmation = false;
		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;
	}

	public void AddAbilityExperience(CultivationAbility ability, int amount)
	{
		if (amount <= 0 || !IsAbilityUnlocked(ability))
			return;

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

		if (Xianxia.QiResistanceKeybind.JustPressed)
		{
			TryUseQiResistance();
		}

		if (Xianxia.FireballKeybind.JustPressed)
		{
			TryCastFireball(
				Main.MouseWorld - Player.Center,
				Player.GetSource_Misc("XianxiaFireball")
			);
		}

		if (Xianxia.QiPalmKeybind.JustPressed)
		{
			TryUseQiPalm();
		}

		if (Xianxia.FlameStepKeybind.JustPressed)
		{
			TryUseFlameStep();
		}

		if (Xianxia.NascentTeleportKeybind.JustPressed)
		{
			TryUseNascentTeleport();
		}

		if (Xianxia.SpiritualPressureKeybind.JustPressed)
		{
			ToggleSpiritualPressure();
		}

		if (Xianxia.NightVisionKeybind.JustPressed)
		{
			ToggleNightVision();
		}

		if (Xianxia.QiFlightKeybind.JustPressed)
		{
			if (QiFlightEnabled)
			{
				QiFlightEnabled = false;
				Main.NewText(Mod.GetLocalization("Abilities.QiFlightDisabled").Value, Color.LightGray);
			}
			else if (RealmIndex < 3)
			{
				Main.NewText(Mod.GetLocalization("Abilities.RequiresRealm").Format(
					Mod.GetLocalization("Cultivation.Realms.CoreFormation").Value), Color.OrangeRed);
			}
			else if (Qi <= 0)
			{
				Main.NewText(Mod.GetLocalization("Abilities.NotEnoughQi").Format(1), Color.OrangeRed);
			}
			else
			{
				QiFlightEnabled = true;
				Main.NewText(Mod.GetLocalization("Abilities.QiFlightEnabled").Value, Color.Cyan);
			}
		}

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
		if (flightQiTimer < QiFlightCostInterval)
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
		int qiCost = Math.Max(MinimumFireballQiCost, (int)Math.Ceiling(damage / FireballDamagePerQi));
		float projectileScale = MathHelper.Clamp(0.8f + damage / 100f, 1.15f, 2.5f);

		if (!SpendQi(qiCost))
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
		int qiCost = Math.Max(18, (int)Math.Ceiling(damage / 5f));
		if (!SpendQi(qiCost))
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
		int qiCost = NascentTeleportBaseQiCost
			+ (int)MathF.Ceiling(distanceBlocks / NascentTeleportBlocksPerQi)
				* NascentTeleportQiCostPerDistanceStep;
		qiCost = Math.Max(NascentTeleportBaseQiCost,
			(int)MathF.Ceiling(qiCost / GetAbilityPowerMultiplier(CultivationAbility.NascentTeleport, 0.025f)));
		if (!SpendQi(qiCost))
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
		}
	}

	public override void PostUpdate()
	{
		if (Player.whoAmI == Main.myPlayer && !Player.dead)
		{
			UpdateSpiritualQiZone();
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

		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;
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

	private void StartTribulation(int targetRealm)
	{
		if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer)
		{
			return;
		}

		pendingTribulationRealm = targetRealm;
		awaitingTribulationConfirmation = false;
		tribulationRealm = targetRealm;
		tribulationStrikesRemaining = 9 + (targetRealm - TribulationStartingRealm) * 2;
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
		awaitingTribulationConfirmation = true;
		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;
		Main.NewText(Mod.GetLocalization("Cultivation.TribulationReady").Format(
			GetRealmName(targetRealm)), Color.Gold);
		SoundEngine.PlaySound(SoundID.MenuOpen);
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
		int previousGlobalStage = cancelledRealm * StagesPerRealm - 1;
		QiExp = Math.Min(QiExp, GetGlobalStageThreshold(previousGlobalStage));
		Qi = Math.Min(Qi, QiExp);
		RealmIndex = cancelledRealm - 1;
		Stage = StagesPerRealm;
		pendingTribulationRealm = -1;
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

		tribulationTimer = TribulationStrikeInterval;
	}

	private void SpawnTribulationWarningDust()
	{
		float intensity = 1f + tribulationRealm * 0.15f;
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
		damage = ApplyTribulationQiShield(damage, realmOffset);
		damage = Math.Max(1, (int)MathF.Ceiling(damage
			* Player.GetModPlayer<AlchemyPillEffectPlayer>().TribulationDamageMultiplier));
		float armorPenetration = 45f + realmOffset * 55f;
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
		awaitingTribulationConfirmation = false;
		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;
		RealmIndex = reachedRealm;
		Stage = 1;

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

	private void FailTribulation()
	{
		int failedRealm = pendingTribulationRealm;
		int previousGlobalStage = failedRealm * StagesPerRealm - 1;
		QiExp = Math.Min(QiExp, GetGlobalStageThreshold(previousGlobalStage));
		Qi = Math.Min(Qi, QiExp);
		RealmIndex = failedRealm - 1;
		Stage = StagesPerRealm;
		pendingTribulationRealm = -1;
		awaitingTribulationConfirmation = false;
		tribulationRealm = -1;
		tribulationTimer = 0;
		tribulationStrikesRemaining = 0;

		if (Main.netMode != NetmodeID.Server)
		{
			Main.NewText(Mod.GetLocalization("Cultivation.TribulationFailed").Value, Color.OrangeRed);
		}
	}

	private void UpdatePassiveQiRecovery()
	{
		if (Qi >= QiExp)
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
		Point playerTile = Player.Center.ToTileCoordinates();
		int radius = config.SpiritualQiZoneRadiusBlocks;
		int radiusSquared = radius * radius;
		int minX = Math.Max(1, playerTile.X - radius);
		int maxX = Math.Min(Main.maxTilesX - 2, playerTile.X + radius);
		int minY = Math.Max(1, playerTile.Y - radius);
		int maxY = Math.Min(Main.maxTilesY - 2, playerTile.Y + radius);
		int spiritCrystalType = ModContent.TileType<SpiritCrystalOreTile>();
		int crystalCount = 0;

		for (int x = minX; x <= maxX; x++)
		{
			int offsetX = x - playerTile.X;
			for (int y = minY; y <= maxY; y++)
			{
				int offsetY = y - playerTile.Y;
				if (offsetX * offsetX + offsetY * offsetY > radiusSquared)
				{
					continue;
				}

				Tile tile = Main.tile[x, y];
				if (tile.HasTile && tile.TileType == spiritCrystalType)
				{
					crystalCount++;
				}
			}
		}

		NearbySpiritCrystalCount = crystalCount;
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
		int qiSenseInterval = QiSenseCostInterval
			+ (GetAbilityLevel(CultivationAbility.QiSense) - 1) * 3;
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
		if (amount <= 0 || Qi >= QiExp)
		{
			return;
		}

		int previousQi = Qi;
		Qi = Math.Min(Qi + amount, QiExp);
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
		Qi = Math.Min(Qi + amount, QiExp);

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

		int actualAmount = Math.Max(1, (int)MathF.Ceiling(amount
			* Player.GetModPlayer<AlchemyPillEffectPlayer>().QiCostMultiplier));
		if (IsAbilityUnlocked(CultivationAbility.GoldenCoreCirculation))
		{
			float reduction = 0.05f
				+ (GetAbilityLevel(CultivationAbility.GoldenCoreCirculation) - 1) * 0.01f;
			actualAmount = Math.Max(1, (int)MathF.Ceiling(amount * (1f - reduction)));
		}

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

	public bool SetQiProtectionEnabled(bool enabled)
	{
		if (enabled && RealmIndex < 2)
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
		if (enabled && !HasUnlockedQiSense)
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

	private void UpdateRealm(bool showMessage)
	{
		if (pendingTribulationRealm >= TribulationStartingRealm)
		{
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

		if (showMessage && targetRealm > previousRealm && targetRealm >= TribulationStartingRealm)
		{
			int realmToChallenge = previousRealm + 1;
			int realmThreshold = GetGlobalStageThreshold(realmToChallenge * StagesPerRealm);
			QiExp = Math.Min(QiExp, realmThreshold);
			Qi = Math.Min(Qi, QiExp);
			RealmIndex = previousRealm;
			Stage = StagesPerRealm;
			RequestTribulationConfirmation(realmToChallenge);
			return;
		}

		RealmIndex = targetRealm;
		Stage = targetStage;

		if (showMessage && RealmIndex > previousRealm)
		{
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

		return totalQi;
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
