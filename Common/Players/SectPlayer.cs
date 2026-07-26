using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Xianxia.Common.Abilities;
using Xianxia.Content.Buffs;
using Xianxia.Content.Items;
using Xianxia.Content.Items.Alchemy;
using Xianxia.Content.Items.Sect;
using Xianxia.Content.Items.Weapons;
using Xianxia.Content.Projectiles;

namespace Xianxia.Common.Players;

public enum SectMissionType : byte
{
	None,
	SpiritBeastHunt,
	SpiritStoneDelivery,
	PillRefinement,
	SpiritVeinSurvey,
	HeavenlyTribulation
}

public class SectPlayer : ModPlayer
{
	private const int MissionCooldownTicks = 60 * 60;
	private int swordRainCooldown;
	private int formationCooldown;
	private int missionCooldown;
	private int formationBarrier;
	private int formationBarrierMax;
	private int formationBaseBarrierMax;
	private int formationMaintenanceTimer;
	private int formationContributionTimer;
	private int formationQiReserve;
	private readonly int[] formationSupportTimers = new int[Main.maxPlayers];

	public bool JoinedSect { get; private set; }
	public int LifetimeContribution { get; private set; }
	public SectMissionType MissionType { get; private set; }
	public int MissionTarget { get; private set; }
	public int MissionProgress { get; private set; }
	public bool SwordIntentUnlocked { get; private set; }
	public bool SpiritSwordRainUnlocked { get; private set; }
	public bool SectProtectionFormationUnlocked { get; private set; }

	public int Rank => LifetimeContribution >= 750 ? 3
		: LifetimeContribution >= 300 ? 2
		: LifetimeContribution >= 100 ? 1
		: 0;
	public int NextRankRequirement => Rank switch { 0 => 100, 1 => 300, 2 => 750, _ => 750 };
	public bool HasActiveMission => MissionType != SectMissionType.None;
	public int MissionCooldownSeconds => (missionCooldown + 59) / 60;
	public int CurrentContribution => Player.CountItem(ModContent.ItemType<SectContributionToken>());
	public int FormationBarrier => formationBarrier;
	public int FormationBarrierMax => formationBarrierMax;
	public int FormationQiReserve => formationQiReserve;
	public int ActiveFormationSupporters
	{
		get
		{
			int count = 0;
			for (int i = 0; i < formationSupportTimers.Length; i++)
			{
				if (formationSupportTimers[i] > 0)
					count++;
			}
			return count;
		}
	}
	public float FormationVisualScale => 1f + Math.Min(4, ActiveFormationSupporters) * 0.08f;

	public override void Initialize()
	{
		JoinedSect = false;
		LifetimeContribution = 0;
		MissionType = SectMissionType.None;
		MissionTarget = 0;
		MissionProgress = 0;
		SwordIntentUnlocked = false;
		SpiritSwordRainUnlocked = false;
		SectProtectionFormationUnlocked = false;
		swordRainCooldown = 0;
		formationCooldown = 0;
		missionCooldown = 0;
		formationBarrier = 0;
		formationBarrierMax = 0;
		formationBaseBarrierMax = 0;
		formationMaintenanceTimer = 0;
		formationContributionTimer = 0;
		formationQiReserve = 0;
		Array.Clear(formationSupportTimers, 0, formationSupportTimers.Length);
	}

	public override void SaveData(TagCompound tag)
	{
		tag["joinedSect"] = JoinedSect;
		tag["sectLifetimeContribution"] = LifetimeContribution;
		tag["sectMissionType"] = (byte)MissionType;
		tag["sectMissionTarget"] = MissionTarget;
		tag["sectMissionProgress"] = MissionProgress;
		tag["sectMissionCooldown"] = missionCooldown;
		tag["swordIntentUnlocked"] = SwordIntentUnlocked;
		tag["spiritSwordRainUnlocked"] = SpiritSwordRainUnlocked;
		tag["sectProtectionFormationUnlocked"] = SectProtectionFormationUnlocked;
	}

	public override void LoadData(TagCompound tag)
	{
		JoinedSect = tag.GetBool("joinedSect");
		LifetimeContribution = Math.Max(0, tag.GetInt("sectLifetimeContribution"));
		MissionType = (SectMissionType)Math.Clamp(tag.GetByte("sectMissionType"),
			(byte)SectMissionType.None, (byte)SectMissionType.HeavenlyTribulation);
		MissionTarget = Math.Max(0, tag.GetInt("sectMissionTarget"));
		MissionProgress = Math.Max(0, tag.GetInt("sectMissionProgress"));
		missionCooldown = Math.Max(0, tag.GetInt("sectMissionCooldown"));
		SwordIntentUnlocked = tag.GetBool("swordIntentUnlocked");
		SpiritSwordRainUnlocked = tag.GetBool("spiritSwordRainUnlocked");
		SectProtectionFormationUnlocked = tag.GetBool("sectProtectionFormationUnlocked");
		if (!JoinedSect)
			ClearMission();
	}

	public override void OnEnterWorld()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
			SendState();
	}

	public override void CopyClientState(ModPlayer targetCopy)
	{
		SectPlayer clone = (SectPlayer)targetCopy;
		clone.JoinedSect = JoinedSect;
		clone.LifetimeContribution = LifetimeContribution;
		clone.MissionType = MissionType;
		clone.MissionTarget = MissionTarget;
		clone.MissionProgress = MissionProgress;
		clone.SwordIntentUnlocked = SwordIntentUnlocked;
		clone.SpiritSwordRainUnlocked = SpiritSwordRainUnlocked;
		clone.SectProtectionFormationUnlocked = SectProtectionFormationUnlocked;
	}

	public override void SendClientChanges(ModPlayer clientPlayer)
	{
		SectPlayer old = (SectPlayer)clientPlayer;
		if (old.JoinedSect != JoinedSect
			|| old.LifetimeContribution != LifetimeContribution
			|| old.MissionType != MissionType
			|| old.MissionTarget != MissionTarget
			|| old.MissionProgress != MissionProgress
			|| old.SwordIntentUnlocked != SwordIntentUnlocked
			|| old.SpiritSwordRainUnlocked != SpiritSwordRainUnlocked
			|| old.SectProtectionFormationUnlocked != SectProtectionFormationUnlocked)
			SendState();
	}

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
	{
		Xianxia.SendSectState(Player.whoAmI, JoinedSect, LifetimeContribution,
			MissionType, MissionTarget, MissionProgress, SwordIntentUnlocked,
			SpiritSwordRainUnlocked, SectProtectionFormationUnlocked, toWho, fromWho);
	}

	internal void SetSectStateFromNetwork(bool joined, int lifetimeContribution,
		SectMissionType missionType, int missionTarget, int missionProgress,
		bool swordIntent, bool swordRain, bool formation)
	{
		JoinedSect = joined;
		LifetimeContribution = Math.Max(0, lifetimeContribution);
		MissionType = missionType;
		MissionTarget = Math.Max(0, missionTarget);
		MissionProgress = Math.Max(0, missionProgress);
		SwordIntentUnlocked = swordIntent;
		SpiritSwordRainUnlocked = swordRain;
		SectProtectionFormationUnlocked = formation;
		if (!JoinedSect)
			ClearMission();
	}

	public override void PostUpdate()
	{
		if (swordRainCooldown > 0)
			swordRainCooldown--;
		if (formationCooldown > 0)
			formationCooldown--;
		if (missionCooldown > 0)
			missionCooldown--;
		for (int i = 0; i < formationSupportTimers.Length; i++)
		{
			if (formationSupportTimers[i] > 0)
				formationSupportTimers[i]--;
		}

		if (MissionType == SectMissionType.SpiritVeinSurvey
			&& Player.GetModPlayer<CultivationPlayer>().NearbySpiritCrystalCount > 0)
			MissionProgress = MissionTarget;

		if (Player.HasBuff<SectProtectionFormationBuff>()
			&& Main.netMode != NetmodeID.Server
			&& Player.ownedProjectileCounts[
				ModContent.ProjectileType<SectProtectionFormationProjectile>()] <= 0)
		{
			Projectile.NewProjectile(Player.GetSource_Misc("SectProtectionFormation"),
				Player.Center, Vector2.Zero,
				ModContent.ProjectileType<SectProtectionFormationProjectile>(),
				0, 0f, Player.whoAmI);
		}

		if (Player.HasBuff<SectProtectionFormationBuff>()
			&& Main.netMode != NetmodeID.MultiplayerClient)
			RepelEnemiesFromFormation();
		else if (!Player.HasBuff<SectProtectionFormationBuff>())
		{
			formationBarrier = 0;
			formationBarrierMax = 0;
			formationBaseBarrierMax = 0;
			formationQiReserve = 0;
		}

		if (Player.whoAmI == Main.myPlayer && !Player.dead)
		{
			UpdateOwnedFormation();
			UpdateFormationContribution();
		}
	}

	public override void PostUpdateEquips()
	{
		if (Player.HasBuff<SectProtectionFormationBuff>())
			return;
		if (!TryFindNearbyFormationOwner(out Player formationOwner))
			return;

		int level = formationOwner.GetModPlayer<CultivationPlayer>()
			.GetAbilityLevel(CultivationAbility.SectProtectionFormation);
		Player.statDefense += 9 + level;
		Player.endurance += 0.04f + level * 0.002f;
	}

	private void UpdateOwnedFormation()
	{
		if (!Player.HasBuff<SectProtectionFormationBuff>() || formationBarrier <= 0)
			return;

		formationMaintenanceTimer++;
		if (formationMaintenanceTimer < 60)
			return;
		formationMaintenanceTimer = 0;

		CultivationPlayer cultivation = Player.GetModPlayer<CultivationPlayer>();
		int upkeep = Math.Max(1, 3 - cultivation.RealmIndex / 2);
		bool poweredBySpiritVein = cultivation.IsInSpiritualQiZone;
		bool maintained = poweredBySpiritVein;
		if (!maintained && formationQiReserve >= upkeep)
		{
			formationQiReserve -= upkeep;
			maintained = true;
		}
		if (!maintained)
			maintained = cultivation.SpendQi(upkeep);
		if (!maintained
			&& Player.ConsumeItem(ModContent.ItemType<SpiritStone>()))
		{
			const int spiritStoneQi = 250;
			formationQiReserve = spiritStoneQi - upkeep;
			maintained = true;
			Main.NewText(Mod.GetLocalization("Sect.FormationConsumedSpiritStone")
				.Format(spiritStoneQi), new Color(100, 235, 255));
		}

		if (!maintained)
		{
			Player.ClearBuff(ModContent.BuffType<SectProtectionFormationBuff>());
			Main.NewText(Mod.GetLocalization("Sect.FormationCollapsedNoQi").Value,
				new Color(255, 170, 90));
			return;
		}

		RefreshFormationDuration();
		int passiveRepair = 8 + cultivation.GetAbilityLevel(
			CultivationAbility.SectProtectionFormation) * 2;
		if (poweredBySpiritVein)
			passiveRepair += cultivation.SpiritualQiZoneTier * 15;
		formationBarrier = Math.Min(formationBarrierMax,
			formationBarrier + passiveRepair);
		UpdateFormationMaximum();
	}

	private void UpdateFormationContribution()
	{
		formationContributionTimer++;
		if (formationContributionTimer < 60)
			return;
		formationContributionTimer = 0;

		CultivationPlayer cultivation = Player.GetModPlayer<CultivationPlayer>();
		if (!cultivation.IsMeditating
			|| !TryFindNearbyFormationOwner(out Player owner)
			|| owner.whoAmI == Player.whoAmI)
			return;

		int contribution = Math.Clamp(1 + cultivation.RealmIndex / 2, 1, 4);
		if (!cultivation.SpendQi(contribution))
			return;

		if (Main.netMode == NetmodeID.SinglePlayer)
			owner.GetModPlayer<SectPlayer>()
				.ReceiveFormationContribution(Player.whoAmI, contribution);
		else
			Xianxia.SendFormationContribution(Player.whoAmI, owner.whoAmI, contribution);
	}

	private bool TryFindNearbyFormationOwner(out Player owner)
	{
		owner = null;
		float closestDistanceSquared = float.MaxValue;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player candidate = Main.player[i];
			if (!candidate.active || candidate.dead
				|| !candidate.HasBuff<SectProtectionFormationBuff>())
				continue;
			SectPlayer candidateSect = candidate.GetModPlayer<SectPlayer>();
			float radius = candidateSect.GetFormationRadius();
			float distanceSquared = Vector2.DistanceSquared(Player.Center, candidate.Center);
			if (distanceSquared > radius * radius
				|| distanceSquared >= closestDistanceSquared)
				continue;
			owner = candidate;
			closestDistanceSquared = distanceSquared;
		}
		return owner is not null;
	}

	public void ReceiveFormationContribution(int contributorIndex, int qiAmount)
	{
		if (!Player.HasBuff<SectProtectionFormationBuff>()
			|| contributorIndex < 0 || contributorIndex >= Main.maxPlayers
			|| qiAmount <= 0)
			return;

		formationSupportTimers[contributorIndex] = 180;
		formationQiReserve = Math.Min(9999, formationQiReserve + qiAmount);
		UpdateFormationMaximum();
		int repair = qiAmount * 30;
		formationBarrier = Math.Min(formationBarrierMax, formationBarrier + repair);
		RefreshFormationDuration();

		if (Main.netMode != NetmodeID.Server)
			CombatText.NewText(Player.Hitbox, new Color(105, 255, 205),
				Mod.GetLocalization("Sect.FormationContribution")
					.Format(Main.player[contributorIndex].name, qiAmount, repair));
	}

	private void UpdateFormationMaximum()
	{
		int supporters = Math.Min(4, ActiveFormationSupporters);
		formationBarrierMax = formationBaseBarrierMax
			+ formationBaseBarrierMax * supporters / 4;
		formationBarrier = Math.Min(formationBarrier, formationBarrierMax);
	}

	private void RefreshFormationDuration()
	{
		int buffType = ModContent.BuffType<SectProtectionFormationBuff>();
		int index = Player.FindBuffIndex(buffType);
		if (index >= 0)
			Player.buffTime[index] = Math.Max(Player.buffTime[index], 600);
	}

	public float GetFormationRadius()
	{
		int level = Player.GetModPlayer<CultivationPlayer>()
			.GetAbilityLevel(CultivationAbility.SectProtectionFormation);
		return MathHelper.Clamp(195f + level * 4.5f, 195f, 285f)
			* FormationVisualScale;
	}

	public bool CanFormationAbsorb(int damage) =>
		damage > 0
		&& formationBarrier >= damage
		&& Player.HasBuff<SectProtectionFormationBuff>();

	public int AbsorbAndBreakFormation(int incomingDamage)
	{
		if (incomingDamage <= 0 || formationBarrier <= 0
			|| !Player.HasBuff<SectProtectionFormationBuff>()
			|| formationBarrier >= incomingDamage)
			return 0;

		int absorbedDamage = formationBarrier;
		formationBarrier = 0;
		Player.ClearBuff(ModContent.BuffType<SectProtectionFormationBuff>());
		CombatText.NewText(Player.Hitbox, new Color(255, 185, 80),
			Mod.GetLocalization("Sect.FormationShattered")
				.Format(absorbedDamage));
		SoundEngine.PlaySound(SoundID.Shatter with { Pitch = -0.25f }, Player.Center);
		return absorbedDamage;
	}

	public override bool ConsumableDodge(Player.HurtInfo info)
	{
		if (!CanFormationAbsorb(info.Damage))
			return false;

		formationBarrier -= info.Damage;
		Player.immune = true;
		Player.immuneTime = Math.Max(Player.immuneTime, 20);
		CombatText.NewText(Player.Hitbox, new Color(95, 255, 215),
			Mod.GetLocalization("Sect.FormationBlocked")
				.Format(info.Damage, formationBarrier, formationBarrierMax));
		SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.25f, Volume = 0.65f },
			Player.Center);
		return true;
	}

	private void RepelEnemiesFromFormation()
	{
		int level = Player.GetModPlayer<CultivationPlayer>()
			.GetAbilityLevel(CultivationAbility.SectProtectionFormation);
		float radius = GetFormationRadius();

		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (!npc.CanBeChasedBy(Player) || npc.friendly || npc.townNPC)
				continue;

			Vector2 away = npc.Center - Player.Center;
			float distance = away.Length();
			if (distance <= 0.01f || distance >= radius)
				continue;

			float proximity = 1f - distance / radius;
			float force = (0.45f + proximity * 1.15f) * (npc.boss ? 0.2f : 1f);
			npc.velocity += away / distance * force;
			if (!npc.boss)
				npc.velocity.Y -= 0.08f * proximity;
			if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 12 == 0)
				npc.netUpdate = true;
		}
	}

	public override void ProcessTriggers(TriggersSet triggersSet)
	{
		if (Player.dead || Main.drawingPlayerChat
			|| Player.GetModPlayer<CultivationPlayer>().IsAbilityTreeOpen)
			return;
		if (Xianxia.SpiritSwordRainKeybind.JustPressed)
			TryUseSpiritSwordRain();
		if (Xianxia.SectFormationKeybind.JustPressed)
			TryUseSectProtectionFormation();
	}

	public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
	{
		if (!SwordIntentUnlocked || item.ModItem is not FlyingSword)
			return;
		int level = Player.GetModPlayer<CultivationPlayer>()
			.GetAbilityLevel(CultivationAbility.SwordIntent);
		damage += 0.10f + (level - 1) * 0.015f;
	}

	public bool HasUnlockedTechnique(CultivationAbility ability) => ability switch
	{
		CultivationAbility.SwordIntent => SwordIntentUnlocked,
		CultivationAbility.SpiritSwordRain => SpiritSwordRainUnlocked,
		CultivationAbility.SectProtectionFormation => SectProtectionFormationUnlocked,
		_ => true
	};

	public bool JoinSect()
	{
		if (JoinedSect || Player.GetModPlayer<CultivationPlayer>().RealmIndex < 1)
			return false;
		JoinedSect = true;
		Player.QuickSpawnItem(Player.GetSource_Misc("VerdantCloudSectJoining"),
			ModContent.ItemType<SectContributionToken>(), 10);
		return true;
	}

	public bool AssignMission()
	{
		if (!JoinedSect || HasActiveMission || missionCooldown > 0)
			return false;

		int realm = Player.GetModPlayer<CultivationPlayer>().RealmIndex;
		List<SectMissionType> pool =
		[
			SectMissionType.SpiritBeastHunt,
			SectMissionType.SpiritStoneDelivery,
			SectMissionType.PillRefinement,
			SectMissionType.SpiritVeinSurvey
		];
		if (realm == 3)
			pool.Add(SectMissionType.HeavenlyTribulation);
		MissionType = pool[Main.rand.Next(pool.Count)];
		MissionTarget = MissionType switch
		{
			SectMissionType.SpiritBeastHunt => 3 + realm * 2,
			SectMissionType.SpiritStoneDelivery => 5 + realm * 3,
			SectMissionType.PillRefinement => 2 + realm,
			_ => 1
		};
		MissionProgress = 0;
		return true;
	}

	public bool IsMissionComplete()
	{
		if (!HasActiveMission)
			return false;
		if (MissionType == SectMissionType.SpiritStoneDelivery)
			return Player.CountItem(ModContent.ItemType<SpiritStone>()) >= MissionTarget;
		return MissionProgress >= MissionTarget;
	}

	public int ClaimMission()
	{
		if (!IsMissionComplete())
			return 0;
		int previousRank = Rank;
		if (MissionType == SectMissionType.SpiritStoneDelivery)
		{
			for (int i = 0; i < MissionTarget; i++)
				Player.ConsumeItem(ModContent.ItemType<SpiritStone>());
		}

		int realm = Player.GetModPlayer<CultivationPlayer>().RealmIndex;
		int reward = 18 + realm * 12 + (MissionType == SectMissionType.HeavenlyTribulation ? 25 : 0);
		LifetimeContribution += reward;
		Player.QuickSpawnItem(Player.GetSource_Misc("VerdantCloudSectMission"),
			ModContent.ItemType<SectContributionToken>(), reward);
		ClearMission();
		missionCooldown = MissionCooldownTicks;
		if (Rank > previousRank && Player.whoAmI == Main.myPlayer)
		{
			Main.NewText(Mod.GetLocalization("Sect.RankUp").Format(GetRankName()),
				new Color(245, 210, 95));
			SoundEngine.PlaySound(SoundID.Item29, Player.Center);
		}
		return reward;
	}

	public void RecordSpiritBeastKill()
	{
		if (MissionType == SectMissionType.SpiritBeastHunt)
			MissionProgress = Math.Min(MissionTarget, MissionProgress + 1);
	}

	public void RecordPillCrafted()
	{
		if (MissionType == SectMissionType.PillRefinement)
			MissionProgress = Math.Min(MissionTarget, MissionProgress + 1);
	}

	public void RecordTribulationSurvived()
	{
		if (MissionType == SectMissionType.HeavenlyTribulation)
			MissionProgress = MissionTarget;
	}

	public void UnlockSwordIntent() => SwordIntentUnlocked = true;
	public void UnlockSpiritSwordRain() => SpiritSwordRainUnlocked = true;
	public void UnlockSectProtectionFormation() => SectProtectionFormationUnlocked = true;

	public void DebugMaxRank()
	{
		JoinedSect = true;
		LifetimeContribution = Math.Max(LifetimeContribution, 750);
		if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
			SendState();
	}

	public string GetRankName() =>
		Mod.GetLocalization($"Sect.Ranks.Rank{Rank}").Value;

	public string GetMissionDescription()
	{
		if (!HasActiveMission)
			return Mod.GetLocalization("Sect.Missions.None").Value;
		int progress = MissionType == SectMissionType.SpiritStoneDelivery
			? Math.Min(MissionTarget, Player.CountItem(ModContent.ItemType<SpiritStone>()))
			: Math.Min(MissionTarget, MissionProgress);
		return Mod.GetLocalization($"Sect.Missions.{MissionType}").Format(progress, MissionTarget);
	}

	private void TryUseSpiritSwordRain()
	{
		CultivationPlayer cultivation = Player.GetModPlayer<CultivationPlayer>();
		if (!SpiritSwordRainUnlocked || cultivation.RealmIndex < 2 || swordRainCooldown > 0)
			return;
		int level = cultivation.GetAbilityLevel(CultivationAbility.SpiritSwordRain);
		int cost = Math.Max(35, 80 - (level - 1) * 2);
		if (!cultivation.SpendQi(cost))
			return;

		const int count = 5;
		int damage = (int)Player.GetTotalDamage(DamageClass.Magic).ApplyTo(52 + level * 4);
		Vector2 aimDirection = Player.DirectionTo(Main.MouseWorld);
		if (aimDirection.LengthSquared() < 0.001f)
			aimDirection = new Vector2(Player.direction, 0f);
		// Sword Rain never has less reach than the ordinary flying sword, but
		// aiming farther away extends its maximum travel distance to the cursor.
		float maximumRange = Math.Max(960f, Vector2.Distance(Player.Center, Main.MouseWorld));
		Vector2 formationAxis = aimDirection.RotatedBy(MathHelper.PiOver2);
		if (Math.Abs(aimDirection.X) > 0.01f)
			Player.direction = Math.Sign(aimDirection.X);

		for (int i = 0; i < count; i++)
		{
			float centeredIndex = i - (count - 1) * 0.5f;
			Vector2 spawn = Player.Center
				+ aimDirection * 12f
				+ formationAxis * centeredIndex * 3f;
			Vector2 velocity = aimDirection
				.RotatedBy(centeredIndex * 0.045f)
				* (14.5f + Math.Abs(centeredIndex) * 0.25f);
			Projectile.NewProjectile(Player.GetSource_Misc("SpiritSwordRain"), spawn, velocity,
				ModContent.ProjectileType<FlyingSwordProjectile>(), damage, 5f, Player.whoAmI,
				ai2: maximumRange);

			for (int dustIndex = 0; dustIndex < 3; dustIndex++)
			{
				Dust dust = Dust.NewDustPerfect(
					spawn,
					DustID.MagicMirror,
					-velocity * Main.rand.NextFloat(0.04f, 0.1f)
						+ Main.rand.NextVector2Circular(0.8f, 0.8f),
					newColor: Color.Cyan,
					Scale: Main.rand.NextFloat(0.75f, 1.05f));
				dust.noGravity = true;
			}
		}
		swordRainCooldown = (int)(180 * cultivation.GetAbilityCooldownMultiplier(
			CultivationAbility.SpiritSwordRain));
		cultivation.AddAbilityExperience(CultivationAbility.SpiritSwordRain, 14);
		SoundEngine.PlaySound(SoundID.Item84, Player.Center);
	}

	private void TryUseSectProtectionFormation()
	{
		CultivationPlayer cultivation = Player.GetModPlayer<CultivationPlayer>();
		if (Player.HasBuff<SectProtectionFormationBuff>())
		{
			Player.ClearBuff(ModContent.BuffType<SectProtectionFormationBuff>());
			Main.NewText(Mod.GetLocalization("Sect.FormationDismissed").Value,
				new Color(105, 235, 210));
			return;
		}
		if (!SectProtectionFormationUnlocked || cultivation.RealmIndex < 3 || formationCooldown > 0)
			return;
		int level = cultivation.GetAbilityLevel(CultivationAbility.SectProtectionFormation);
		int cost = Math.Max(60, 120 - (level - 1) * 3);
		if (!cultivation.SpendQi(cost))
			return;
		formationBaseBarrierMax = 800 + level * 160 + cultivation.RealmIndex * 250;
		formationBarrierMax = formationBaseBarrierMax;
		formationBarrier = formationBarrierMax;
		formationQiReserve = 0;
		formationMaintenanceTimer = 0;
		Array.Clear(formationSupportTimers, 0, formationSupportTimers.Length);
		Player.AddBuff(ModContent.BuffType<SectProtectionFormationBuff>(), 600);
		formationCooldown = (int)(10800 * cultivation.GetAbilityCooldownMultiplier(
			CultivationAbility.SectProtectionFormation));
		cultivation.AddAbilityExperience(CultivationAbility.SectProtectionFormation, 18);
		SoundEngine.PlaySound(SoundID.Item29, Player.Center);
	}

	private void ClearMission()
	{
		MissionType = SectMissionType.None;
		MissionTarget = 0;
		MissionProgress = 0;
	}

	private void SendState()
	{
		Xianxia.SendSectState(Player.whoAmI, JoinedSect, LifetimeContribution,
			MissionType, MissionTarget, MissionProgress, SwordIntentUnlocked,
			SpiritSwordRainUnlocked, SectProtectionFormationUnlocked);
	}
}
