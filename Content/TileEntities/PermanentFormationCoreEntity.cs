using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Xianxia.Common.Players;
using Xianxia.Common.GlobalProjectiles;
using Xianxia.Common.Utilities;
using Xianxia.Content.Buffs;
using Xianxia.Content.Items;
using Xianxia.Content.Items.Materials;
using Xianxia.Content.Items.Materials.SpiritBeasts;
using Xianxia.Content.Projectiles;
using Xianxia.Content.Tiles;

namespace Xianxia.Content.TileEntities;

public enum PermanentFormationKind : byte
{
	Protection,
	SpiritGathering,
	Suppression,
	Restoration
}

public enum FormationAccessMode : byte
{
	OwnerOnly,
	Team,
	Everyone
}

public readonly record struct FormationCoreUpgradeCost(
	int SpiritStones,
	int ProfoundIronBars,
	int SpiritJadeBars,
	int SpecialItemType,
	int SpecialItemCount);

public class PermanentFormationCoreEntity : ModTileEntity
{
	public const int QiPerSpiritStone = 250;
	public int StoredQi { get; private set; }
	public int Tier { get; private set; }
	public int Stage { get; private set; }
	public int Integrity { get; private set; }
	public bool Active { get; private set; } = true;
	public bool ConnectedToSpiritVein { get; private set; }
	public int NearbySpiritCrystalCount { get; private set; }
	public string OwnerName { get; private set; } = string.Empty;
	public FormationAccessMode AccessMode { get; private set; }
	public PermanentFormationKind FormationKind { get; private set; }
	private byte enabledFormationMask = 1;
	private int updateTimer;
	private int veinScanTimer;
	private int integrityDamageCooldown;
	private int combatTrainingTimer;
	private bool veinInsightGranted;

	public int MaximumStoredQi => 10000 + Tier * 10000 + Stage * 2500;
	public int MaximumIntegrity => 5000 + Tier * 4000 + Stage * 1000;
	public float RadiusPixels => Math.Min(80f,
		40f + Tier * 15f + Stage * 5f) * 16f;
	public float RelayLinkRangePixels =>
		RadiusPixels + FormationRelayFlagEntity.TerritoryRadiusPixels;
	public int LinkedFlagCount
	{
		get
		{
			int count = 0;
			foreach (TileEntity entity in TileEntity.ByID.Values)
			{
				if (entity is FormationRelayFlagEntity flag
					&& flag.LinkedCoreId == ID
					&& IsRelayAuthorized(flag.ID)
					&& Vector2.DistanceSquared(flag.WorldCenter, WorldCenter)
						<= RelayLinkRangePixels * RelayLinkRangePixels)
					count++;
			}
			return count;
		}
	}
	public int MaximumRelayFlags => Tier switch
	{
		0 => 0,
		1 => 1,
		2 => 2,
		3 => 4,
		_ => 6
	};
	public int MaxActiveFormationModes => Tier switch
	{
		0 => 1,
		1 => 2,
		2 => 2,
		3 => 3,
		_ => 4
	};
	public int ActiveFormationModeCount
	{
		get
		{
			int count = 0;
			for (int i = 0; i < 4; i++)
				if ((enabledFormationMask & (1 << i)) != 0)
					count++;
			return count;
		}
	}
	public int QiUpkeepPerSecond
	{
		get
		{
			int upkeep = 0;
			if (IsModeEnabled(PermanentFormationKind.Protection))
				upkeep += 1;
			if (IsModeEnabled(PermanentFormationKind.SpiritGathering))
				upkeep += 2;
			if (IsModeEnabled(PermanentFormationKind.Suppression))
				upkeep += 2;
			if (IsModeEnabled(PermanentFormationKind.Restoration))
				upkeep += 3;
			foreach (TileEntity entity in TileEntity.ByID.Values)
			{
				if (entity is FormationRelayFlagEntity flag
					&& flag.LinkedCoreId == ID
					&& IsRelayAuthorized(flag.ID))
					upkeep += flag.CurrentUpkeepPerSecond;
			}
			return Math.Max(1, upkeep);
		}
	}
	public int MaximumRepairPerSecond => 25 + Tier * 20 + Stage * 5;
	public int SpiritualQiConcentrationLevel =>
		SpiritualQiConcentration.GetLevel(NearbySpiritCrystalCount);
	public int LocalVeinQiGenerationPerSecond =>
		SpiritualQiConcentration.GetFormationQiPerSecond(NearbySpiritCrystalCount);
	public int RelayVeinQiGenerationPerSecond
	{
		get
		{
			int total = 0;
			foreach (TileEntity entity in TileEntity.ByID.Values)
			{
				if (entity is FormationRelayFlagEntity flag
					&& flag.LinkedCoreId == ID
					&& IsRelayAuthorized(flag.ID))
					total += flag.VeinQiGenerationPerSecond;
			}
			return total;
		}
	}
	public int NetworkSpiritCrystalCount
	{
		get
		{
			int total = NearbySpiritCrystalCount;
			foreach (TileEntity entity in TileEntity.ByID.Values)
			{
				if (entity is FormationRelayFlagEntity flag
					&& flag.LinkedCoreId == ID
					&& IsRelayAuthorized(flag.ID))
					total += flag.NearbySpiritCrystalCount;
			}
			return total;
		}
	}
	public int VeinQiGenerationPerSecond =>
		LocalVeinQiGenerationPerSecond + RelayVeinQiGenerationPerSecond;
	public bool IsMaximumRank =>
		Tier == FormationPathPlayer.MaxTier && Stage == FormationPathPlayer.MaxStage;
	public int RankIndex => Tier * (FormationPathPlayer.MaxStage + 1) + Stage;
	public int NextTier => Stage < FormationPathPlayer.MaxStage ? Tier : Tier + 1;
	public int NextStage => Stage < FormationPathPlayer.MaxStage ? Stage + 1 : 0;
	public int NextRankIndex => IsMaximumRank ? RankIndex : RankIndex + 1;
	public int RepairQiPerSecond
	{
		get
		{
			int repair = Math.Min(Math.Max(0, MaximumIntegrity - Integrity),
				MaximumRepairPerSecond);
			return (repair + 9) / 10;
		}
	}
	public Color FormationColor => FormationKind switch
	{
		PermanentFormationKind.Protection => new Color(80, 235, 225),
		PermanentFormationKind.SpiritGathering => new Color(100, 255, 145),
		PermanentFormationKind.Suppression => new Color(195, 105, 255),
		PermanentFormationKind.Restoration => new Color(255, 210, 95),
		_ => new Color(80, 235, 225)
	};
	public Vector2 WorldCenter => new((Position.X + 1.5f) * 16f,
		(Position.Y + 1.5f) * 16f);

	public static bool TryProtectFromTribulation(Player player, int incomingDamage,
		int realmOffset, out int remainingDamage)
	{
		remainingDamage = incomingDamage;
		PermanentFormationCoreEntity selectedCore = null;
		float closestDistanceSquared = float.MaxValue;
		foreach (TileEntity entity in ByID.Values)
		{
			if (entity is not PermanentFormationCoreEntity core
				|| !core.Active || core.Integrity <= 0
				|| !core.ProvidesProtectionAt(player.Center))
				continue;
			float distanceSquared = Vector2.DistanceSquared(player.Center, core.WorldCenter);
			if (!core.ContainsTerritory(player.Center)
				|| distanceSquared >= closestDistanceSquared)
				continue;
			selectedCore = core;
			closestDistanceSquared = distanceSquared;
		}

		if (selectedCore is null)
			return false;

		remainingDamage = selectedCore.AbsorbTribulationStrike(
			player, incomingDamage, realmOffset, showEffect: true);
		if (Main.netMode == NetmodeID.MultiplayerClient)
			Xianxia.SendPermanentFormationTribulationHit(
				selectedCore.ID, incomingDamage, realmOffset);
		return remainingDamage < incomingDamage;
	}

	public int AbsorbTribulationStrike(Player player, int incomingDamage,
		int realmOffset, bool showEffect)
	{
		if (!Active || Integrity <= 0 || incomingDamage <= 0
			|| !ProvidesProtectionAt(player.Center))
			return incomingDamage;

		TryGetTerritoryCenter(player.Center, out _,
			out FormationRelayFlagEntity protectionRelay);
		bool specializedProtection = protectionRelay is not null
			&& protectionRelay.HasSpecialization
			&& protectionRelay.SpecializedMode == PermanentFormationKind.Protection
			&& IsModeEnabled(PermanentFormationKind.Protection);
		float protection = MathHelper.Clamp(
			0.55f + Tier * 0.04f + Stage * 0.01f, 0.55f, 0.80f);
		if (specializedProtection)
			protection = Math.Min(0.90f, protection + 0.10f);
		float integrityPerDamage = 2.6f + Math.Max(0, realmOffset) * 0.4f;
		int desiredAbsorption = Math.Max(1,
			(int)MathF.Ceiling(incomingDamage * protection));
		int affordableAbsorption = Math.Max(0,
			(int)MathF.Floor(Integrity / integrityPerDamage));
		int absorbedDamage = Math.Min(desiredAbsorption, affordableAbsorption);
		if (absorbedDamage <= 0)
			return incomingDamage;

		int integritySpent = Math.Min(Integrity,
			Math.Max(1, (int)MathF.Ceiling(absorbedDamage * integrityPerDamage)));
		Integrity -= integritySpent;
		if (Integrity <= 0)
		{
			Integrity = 0;
			Active = false;
		}
		Sync();
		if (Main.netMode != NetmodeID.MultiplayerClient)
			GetPathStudent(player)?.GetModPlayer<FormationPathPlayer>()
				.RecordTribulationStrike(absorbedDamage);

		if (showEffect && Main.netMode != NetmodeID.Server)
		{
			CombatText.NewText(player.Hitbox,
				Active ? new Color(95, 255, 215) : new Color(255, 175, 85),
				Mod.GetLocalization(Active
					? "PermanentFormation.TribulationBlocked"
					: "PermanentFormation.TribulationShattered")
					.Format(absorbedDamage, integritySpent, Integrity,
						MaximumIntegrity));
			SoundEngine.PlaySound(Active
				? SoundID.Item29 with { Pitch = -0.05f, Volume = 0.8f }
				: SoundID.Shatter with { Pitch = -0.3f, Volume = 1f },
				player.Center);
		}

		return Math.Max(1, incomingDamage - absorbedDamage);
	}

	public override bool IsTileValidForEntity(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		return tile.HasTile
			&& tile.TileType == ModContent.TileType<PermanentFormationCoreTile>()
			&& tile.TileFrameX == 0 && tile.TileFrameY == 0;
	}

	public override int Hook_AfterPlacement(int i, int j, int type, int style,
		int direction, int alternate)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			NetMessage.SendTileSquare(Main.myPlayer, i, j, 3);
			NetMessage.SendData(MessageID.TileEntityPlacement,
				number: i, number2: j, number3: Type);
			return -1;
		}
		int id = Place(i, j);
		if (ByID.TryGetValue(id, out TileEntity entity)
			&& entity is PermanentFormationCoreEntity core)
			core.Integrity = core.MaximumIntegrity;
		return id;
	}

	public override void Update()
	{
		if (integrityDamageCooldown > 0)
			integrityDamageCooldown--;
		if (++veinScanTimer >= 300)
		{
			veinScanTimer = 0;
			bool previous = ConnectedToSpiritVein;
			UpdateSpiritVeinConnection();
			if (previous != ConnectedToSpiritVein)
				Sync();
		}
		if (Main.netMode != NetmodeID.MultiplayerClient
			&& ConnectedToSpiritVein && !veinInsightGranted)
		{
			FormationPathPlayer path = GetPathStudent()?.GetModPlayer<FormationPathPlayer>();
			if (path is not null)
			{
				veinInsightGranted = true;
				path.RecordVeinLink();
				Sync();
			}
		}

		if (Main.netMode != NetmodeID.MultiplayerClient && ++updateTimer >= 60)
		{
			updateTimer = 0;
			ConnectedToSpiritVein = VeinQiGenerationPerSecond > 0;
			if (Active)
				ProcessFormationEnergy();
			else if (VeinQiGenerationPerSecond > 0)
				StoredQi = Math.Min(MaximumStoredQi,
					StoredQi + VeinQiGenerationPerSecond);
			Sync();
		}

		if (!Active)
			return;

		InterceptIncomingProjectiles();
		ApplyTerritoryEffects();
		SpawnVisuals();
	}

	private void ProcessFormationEnergy()
	{
		int veinQiRemaining = VeinQiGenerationPerSecond;
		int upkeep = QiUpkeepPerSecond;
		int veinUpkeep = Math.Min(veinQiRemaining, upkeep);
		veinQiRemaining -= veinUpkeep;
		int storedUpkeep = upkeep - veinUpkeep;
		if (StoredQi < storedUpkeep)
		{
			Active = false;
			return;
		}
		StoredQi -= storedUpkeep;

		int missingIntegrity = MaximumIntegrity - Integrity;
		if (missingIntegrity > 0)
		{
			int desiredRepair = Math.Min(missingIntegrity, MaximumRepairPerSecond);
			int desiredRepairQi = (desiredRepair + 9) / 10;
			int availableRepairQi = veinQiRemaining + StoredQi;
			int repairQi = Math.Min(desiredRepairQi, availableRepairQi);
			int veinRepairQi = Math.Min(veinQiRemaining, repairQi);
			veinQiRemaining -= veinRepairQi;
			StoredQi -= repairQi - veinRepairQi;
			Integrity = Math.Min(MaximumIntegrity,
				Integrity + Math.Min(desiredRepair, repairQi * 10));
		}

		if (veinQiRemaining > 0)
			StoredQi = Math.Min(MaximumStoredQi, StoredQi + veinQiRemaining);
	}

	private void InterceptIncomingProjectiles()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient
			|| Integrity <= 0
			|| !IsModeEnabled(PermanentFormationKind.Protection))
			return;

		for (int i = 0; i < Main.maxProjectiles; i++)
		{
			Projectile projectile = Main.projectile[i];
			if (!projectile.active || projectile.damage <= 0
				|| projectile.GetGlobalProjectile<FormationBarrierProjectileGlobal>()
					.OriginatedInside(ID))
				continue;

			Vector2 current = projectile.Center;
			Vector2 previous = current - projectile.velocity;
			if (!TryGetIncomingBarrierImpact(previous, current,
				out Vector2 impact, out FormationRelayFlagEntity impactRelay))
				continue;

			int integrityDamage = Math.Clamp(
				Math.Max(25, projectile.damage * 2), 25, 2500);
			if (impactRelay is not null)
			{
				float relayStrain = impactRelay.HasSpecialization
					&& impactRelay.SpecializedMode
						== PermanentFormationKind.Protection
					&& IsModeEnabled(PermanentFormationKind.Protection)
						? 0.85f : 1.25f;
				integrityDamage = Math.Min(3000,
					(int)MathF.Ceiling(integrityDamage * relayStrain));
			}
			Integrity = Math.Max(0, Integrity - integrityDamage);
			GetPathStudent()?.GetModPlayer<FormationPathPlayer>()
				.RecordDamageHandled(integrityDamage);
			Projectile.NewProjectile(
				new EntitySource_Misc("PermanentFormationBarrierImpact"),
				impact, Vector2.Zero,
				ModContent.ProjectileType<PermanentFormationBarrierImpactProjectile>(),
				0, 0f, Main.myPlayer, (float)FormationKind);
			projectile.Kill();
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.KillProjectile, -1, -1, null,
					projectile.identity, projectile.owner);
			if (Integrity <= 0)
			{
				Active = false;
				Sync();
				break;
			}
			Sync();
		}
	}

	private bool TryGetIncomingBarrierImpact(Vector2 start, Vector2 end,
		out Vector2 impact, out FormationRelayFlagEntity impactRelay)
	{
		if (SegmentTouchesBarrier(start, end, WorldCenter, RadiusPixels,
			out impact))
		{
			impactRelay = null;
			return true;
		}
		foreach (TileEntity entity in TileEntity.ByID.Values)
		{
			if (entity is FormationRelayFlagEntity flag
				&& flag.LinkedCoreId == ID
				&& IsRelayAuthorized(flag.ID)
				&& (!flag.HasSpecialization
					|| flag.SpecializedMode == PermanentFormationKind.Protection)
				&& IsModeEnabled(PermanentFormationKind.Protection)
				&& SegmentTouchesBarrier(start, end, flag.WorldCenter,
					FormationRelayFlagEntity.TerritoryRadiusPixels,
					out impact))
			{
				impactRelay = flag;
				return true;
			}
		}
		impact = default;
		impactRelay = null;
		return false;
	}

	private static bool SegmentTouchesBarrier(Vector2 start, Vector2 end,
		Vector2 center, float radius, out Vector2 impact)
	{
		float startDistanceSquared = Vector2.DistanceSquared(start, center);
		float endDistanceSquared = Vector2.DistanceSquared(end, center);
		float radiusSquared = radius * radius;
		if (startDistanceSquared <= radiusSquared)
		{
			impact = default;
			return false;
		}
		if (endDistanceSquared <= radiusSquared)
		{
			impact = ClosestPointOnSegment(start, end, center);
			return true;
		}
		impact = ClosestPointOnSegment(start, end, center);
		return Vector2.DistanceSquared(impact, center) <= radiusSquared;
	}

	private static Vector2 ClosestPointOnSegment(
		Vector2 start, Vector2 end, Vector2 point)
	{
		Vector2 segment = end - start;
		float lengthSquared = segment.LengthSquared();
		if (lengthSquared <= 0.0001f)
			return start;
		float amount = MathHelper.Clamp(
			Vector2.Dot(point - start, segment) / lengthSquared, 0f, 1f);
		return start + segment * amount;
	}

	public FormationCoreUpgradeCost GetNextUpgradeCost()
	{
		if (IsMaximumRank)
			return default;
		int targetTier = NextTier;
		int targetStage = NextStage;
		bool tierBreakthrough = targetStage == 0 && targetTier > Tier;
		int specialItemType = tierBreakthrough ? targetTier switch
		{
			1 => ModContent.ItemType<QiGatheringBeastCore>(),
			2 => ModContent.ItemType<FoundationBeastCore>(),
			_ => ModContent.ItemType<CoreFormationBeastCore>()
		} : 0;
		int specialItemCount = tierBreakthrough
			? targetTier == FormationPathPlayer.MaxTier ? 3 : 1
			: 0;
		return new FormationCoreUpgradeCost(
			10 + targetTier * 18 + targetStage * 6 + (tierBreakthrough ? 12 : 0),
			4 + targetTier * 5 + targetStage * 2 + (tierBreakthrough ? 4 : 0),
			2 + targetTier * 4 + targetStage * 2 + (tierBreakthrough ? 3 : 0),
			specialItemType,
			specialItemCount);
	}

	public void HandleInteraction(Player player, bool deposit, bool toggle,
		bool cycle, bool toggleMode, bool upgrade = false,
		bool cycleAccess = false,
		int requestedKind = byte.MaxValue)
	{
		if (Vector2.DistanceSquared(player.Center, WorldCenter) > 8f * 16f * 8f * 16f)
			return;
		if (string.IsNullOrWhiteSpace(OwnerName))
			OwnerName = player.name;
		bool owner = player.name == OwnerName;
		if ((toggle || cycle || toggleMode || cycleAccess) && !owner)
		{
			SendStatus(player, "PermanentFormation.OwnerControlRequired");
			Sync();
			return;
		}
		if (deposit && !CanContribute(player))
		{
			SendStatus(player, "PermanentFormation.ContributionDenied");
			Sync();
			return;
		}

		if (requestedKind >= 0
			&& requestedKind <= Math.Min(3, Tier))
			FormationKind = (PermanentFormationKind)requestedKind;

		if (cycleAccess)
		{
			AccessMode = (FormationAccessMode)(((int)AccessMode + 1) % 3);
			SendStatus(player, "PermanentFormation.AccessChanged",
				GetAccessModeNetworkText());
		}
		else if (upgrade)
		{
			TryUpgrade(player);
		}
		else if (toggleMode)
		{
			ToggleSelectedMode(player);
		}
		else if (cycle)
		{
			int unlockedKinds = Math.Min(3, Tier) + 1;
			FormationKind = (PermanentFormationKind)
				(((int)FormationKind + 1) % unlockedKinds);
			SendStatus(player, "PermanentFormation.ModeSelected",
				GetFormationKindNetworkText(),
				IsModeEnabled(FormationKind)
					? NetworkText.FromKey("Mods.Xianxia.PermanentFormation.ModeOn")
					: NetworkText.FromKey("Mods.Xianxia.PermanentFormation.ModeOff"));
		}
		else if (deposit && player.ConsumeItem(ModContent.ItemType<SpiritStone>()))
		{
			StoredQi = Math.Min(MaximumStoredQi, StoredQi + QiPerSpiritStone);
			Integrity = Math.Min(MaximumIntegrity,
				Math.Max(Integrity, MaximumIntegrity / 2) + QiPerSpiritStone);
			Active = true;
			SendStatus(player, "PermanentFormation.Deposited",
				StoredQi, MaximumStoredQi, Integrity, MaximumIntegrity);
		}
		else if (toggle)
		{
			Active = !Active;
			SendStatus(player, Active
				? "PermanentFormation.Activated"
				: "PermanentFormation.Deactivated",
				StoredQi, MaximumStoredQi, Integrity, MaximumIntegrity);
		}
		else
		{
			SendStatus(player, "PermanentFormation.Status",
				Active
					? NetworkText.FromKey("Mods.Xianxia.PermanentFormation.StateActive")
					: NetworkText.FromKey("Mods.Xianxia.PermanentFormation.StateInactive"),
				GetFormationKindNetworkText(), StoredQi, MaximumStoredQi,
				Integrity, MaximumIntegrity, Tier, Stage,
				(int)MathF.Round(RadiusPixels / 16f), QiUpkeepPerSecond,
				ConnectedToSpiritVein
					? NetworkText.FromKey("Mods.Xianxia.PermanentFormation.VeinConnected")
					: NetworkText.FromKey("Mods.Xianxia.PermanentFormation.VeinDisconnected"));
			SendStatus(player, "PermanentFormation.ModesStatus",
				GetModeStateNetworkText(PermanentFormationKind.Protection),
				GetModeStateNetworkText(PermanentFormationKind.SpiritGathering),
				GetModeStateNetworkText(PermanentFormationKind.Suppression),
				GetModeStateNetworkText(PermanentFormationKind.Restoration),
				ActiveFormationModeCount, MaxActiveFormationModes);
		}
		Sync();
	}

	public bool CanContribute(Player player)
	{
		if (string.IsNullOrWhiteSpace(OwnerName) || player.name == OwnerName
			|| AccessMode == FormationAccessMode.Everyone)
			return true;
		if (AccessMode != FormationAccessMode.Team || player.team <= 0)
			return false;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player candidate = Main.player[i];
			if (candidate.active && candidate.name == OwnerName)
				return candidate.team > 0 && candidate.team == player.team;
		}
		return false;
	}

	public NetworkText GetAccessModeNetworkText() =>
		NetworkText.FromKey(
			$"Mods.Xianxia.PermanentFormation.Access.{AccessMode}");

	private void TryUpgrade(Player player)
	{
		if (player.name != OwnerName)
		{
			SendStatus(player, "PermanentFormation.UpgradeOwnerOnly");
			return;
		}
		if (IsMaximumRank)
		{
			SendStatus(player, "PermanentFormation.UpgradeMaximum");
			return;
		}

		FormationPathPlayer path = player.GetModPlayer<FormationPathPlayer>();
		if (path.RankIndex < NextRankIndex)
		{
			SendStatus(player, "PermanentFormation.UpgradePathRequired",
				NextTier, path.Player.GetModPlayer<AlchemyPlayer>().GetStageName(NextStage));
			return;
		}

		FormationCoreUpgradeCost cost = GetNextUpgradeCost();
		int spiritStoneType = ModContent.ItemType<SpiritStone>();
		int ironType = ModContent.ItemType<ProfoundIronBar>();
		int jadeType = ModContent.ItemType<SpiritJadeBar>();
		if (CountInventoryItem(player, spiritStoneType) < cost.SpiritStones
			|| CountInventoryItem(player, ironType) < cost.ProfoundIronBars
			|| CountInventoryItem(player, jadeType) < cost.SpiritJadeBars)
		{
			SendStatus(player, "PermanentFormation.UpgradeMissingMaterials");
			return;
		}
		if (cost.SpecialItemCount > 0
			&& CountInventoryItem(player, cost.SpecialItemType) < cost.SpecialItemCount)
		{
			SendStatus(player, "PermanentFormation.UpgradeMissingMaterials");
			return;
		}

		bool tierBreakthrough = NextStage == 0 && NextTier > Tier;
		ConsumeInventoryItem(player, spiritStoneType, cost.SpiritStones);
		ConsumeInventoryItem(player, ironType, cost.ProfoundIronBars);
		ConsumeInventoryItem(player, jadeType, cost.SpiritJadeBars);
		if (cost.SpecialItemCount > 0)
			ConsumeInventoryItem(player, cost.SpecialItemType, cost.SpecialItemCount);
		Tier = NextTier;
		Stage = NextStage;
		Integrity = Math.Min(MaximumIntegrity,
			Integrity + Math.Max(500, MaximumIntegrity / 5));
		SendStatus(player, "PermanentFormation.UpgradeSuccess",
			Tier, path.Player.GetModPlayer<AlchemyPlayer>().GetStageName(Stage));
		Projectile.NewProjectile(
			new EntitySource_Misc("PermanentFormationUpgrade"),
			WorldCenter, Vector2.Zero,
			ModContent.ProjectileType<PermanentFormationUpgradeEffectProjectile>(),
			0, 0f, Main.myPlayer, tierBreakthrough ? 1f : 0f, Tier);
	}

	public bool IsRelayAuthorized(int relayEntityId)
	{
		if (MaximumRelayFlags <= 0)
			return false;
		int earlierLinkedFlags = 0;
		foreach (TileEntity entity in TileEntity.ByID.Values)
		{
			if (entity is FormationRelayFlagEntity flag
				&& flag.ID < relayEntityId
				&& flag.LinkedCoreId == ID
				&& Vector2.DistanceSquared(flag.WorldCenter, WorldCenter)
					<= RelayLinkRangePixels * RelayLinkRangePixels)
				earlierLinkedFlags++;
		}
		return earlierLinkedFlags < MaximumRelayFlags;
	}

	public static int CountInventoryItem(Player player, int itemType)
	{
		int total = 0;
		for (int i = 0; i < player.inventory.Length; i++)
			if (player.inventory[i].type == itemType)
				total += player.inventory[i].stack;
		return total;
	}

	private static void ConsumeInventoryItem(Player player, int itemType, int amount)
	{
		for (int i = 0; i < player.inventory.Length && amount > 0; i++)
		{
			Item item = player.inventory[i];
			if (item.type != itemType || item.stack <= 0)
				continue;
			int consumed = Math.Min(amount, item.stack);
			item.stack -= consumed;
			amount -= consumed;
			if (item.stack <= 0)
				item.TurnToAir();
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null,
					player.whoAmI, i, item.stack, item.prefix);
		}
	}

	private void ApplyTerritoryEffects()
	{
		bool hostileInside = false;
		int hostilePressure = 0;
		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (!npc.CanBeChasedBy() || npc.friendly || npc.townNPC)
					continue;
				if (!TryGetTerritoryCenter(npc.Center,
					out Vector2 territoryCenter,
					out FormationRelayFlagEntity relay))
					continue;
				bool normalTerritory = relay is null || !relay.HasSpecialization;
				bool specializedProtection = relay is not null
					&& relay.HasSpecialization
					&& relay.SpecializedMode == PermanentFormationKind.Protection
					&& IsModeEnabled(PermanentFormationKind.Protection);
				bool specializedSuppression = relay is not null
					&& relay.HasSpecialization
					&& relay.SpecializedMode == PermanentFormationKind.Suppression
					&& IsModeEnabled(PermanentFormationKind.Suppression);
				Vector2 away = npc.Center - territoryCenter;
				float distanceSquared = away.LengthSquared();
				if (distanceSquared <= 1f)
					continue;
				hostileInside = true;
				hostilePressure += Math.Max(1, npc.damage);
				float distance = MathF.Sqrt(distanceSquared);
				if ((normalTerritory
						&& IsModeEnabled(PermanentFormationKind.Protection))
					|| specializedProtection)
				{
					float force = specializedProtection
						? npc.boss ? 0.20f : 2.15f
						: npc.boss ? 0.12f : 1.25f;
					npc.velocity += away / distance * force;
				}
				if ((normalTerritory
						&& IsModeEnabled(PermanentFormationKind.Suppression))
					|| specializedSuppression)
				{
					npc.velocity *= specializedSuppression
						? npc.boss ? 0.95f : 0.72f
						: npc.boss ? 0.98f : 0.90f;
					if (!npc.boss)
					{
						npc.AddBuff(BuffID.Slow, 30);
						npc.AddBuff(BuffID.BrokenArmor, 30);
					}
				}
				if (distance < 52f && integrityDamageCooldown <= 0)
				{
					int integrityDamage = Math.Max(10, npc.damage / 2);
					Integrity = Math.Max(0, Integrity - integrityDamage);
					GetPathStudent()?.GetModPlayer<FormationPathPlayer>()
						.RecordDamageHandled(integrityDamage);
					integrityDamageCooldown = 30;
					if (Integrity <= 0)
						Active = false;
					Sync();
				}
				if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 15 == 0)
					npc.netUpdate = true;
			}

			if (hostileInside)
			{
				if (++combatTrainingTimer >= 300)
				{
					combatTrainingTimer = 0;
					FormationPathPlayer path = GetPathStudent()?
						.GetModPlayer<FormationPathPlayer>();
					if (path is not null)
					{
						path.RecordDefenseCycle();
						path.RecordDamageHandled(
							Math.Clamp(hostilePressure * 2, 50, 1000));
					}
				}
			}
			else
			{
				combatTrainingTimer = 0;
			}
		}

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player player = Main.player[i];
			if (player.active && !player.dead
				&& TryGetTerritoryCenter(player.Center, out _,
					out FormationRelayFlagEntity relay))
			{
				if (relay is null || !relay.HasSpecialization)
				{
					if (IsModeEnabled(PermanentFormationKind.Protection))
						player.AddBuff(ModContent.BuffType<
							PermanentFormationProtectionBuff>(), 2);
					if (IsModeEnabled(PermanentFormationKind.SpiritGathering))
						player.AddBuff(ModContent.BuffType<
							PermanentFormationGatheringBuff>(), 2);
					if (IsModeEnabled(PermanentFormationKind.Suppression))
						player.AddBuff(ModContent.BuffType<
							PermanentFormationSuppressionBuff>(), 2);
					if (IsModeEnabled(PermanentFormationKind.Restoration))
						player.AddBuff(ModContent.BuffType<
							PermanentFormationRestorationBuff>(), 2);
				}
				else if (IsModeEnabled(relay.SpecializedMode))
				{
					ClearNormalFormationBuffs(player);
					if (relay.SpecializedMode == PermanentFormationKind.Protection)
						player.AddBuff(ModContent.BuffType<
							PermanentFormationRelayProtectionBuff>(), 2);
					else if (relay.SpecializedMode
						== PermanentFormationKind.SpiritGathering)
						player.AddBuff(ModContent.BuffType<
							PermanentFormationRelayGatheringBuff>(), 2);
					else if (relay.SpecializedMode
						== PermanentFormationKind.Suppression)
						player.AddBuff(ModContent.BuffType<
							PermanentFormationRelaySuppressionBuff>(), 2);
					else if (relay.SpecializedMode
						== PermanentFormationKind.Restoration)
						player.AddBuff(ModContent.BuffType<
							PermanentFormationRelayRestorationBuff>(), 2);
				}
				else
				{
					ClearNormalFormationBuffs(player);
				}
			}
		}
	}

	private static void ClearNormalFormationBuffs(Player player)
	{
		player.ClearBuff(ModContent.BuffType<PermanentFormationProtectionBuff>());
		player.ClearBuff(ModContent.BuffType<PermanentFormationGatheringBuff>());
		player.ClearBuff(ModContent.BuffType<PermanentFormationSuppressionBuff>());
		player.ClearBuff(ModContent.BuffType<PermanentFormationRestorationBuff>());
		player.ClearBuff(ModContent.BuffType<PermanentFormationRelayProtectionBuff>());
		player.ClearBuff(ModContent.BuffType<PermanentFormationRelayGatheringBuff>());
		player.ClearBuff(ModContent.BuffType<PermanentFormationRelaySuppressionBuff>());
		player.ClearBuff(ModContent.BuffType<PermanentFormationRelayRestorationBuff>());
	}

	public bool ContainsTerritory(Vector2 position)
	{
		return TryGetTerritoryCenter(position, out _, out _);
	}

	private bool ProvidesProtectionAt(Vector2 position)
	{
		if (!IsModeEnabled(PermanentFormationKind.Protection)
			|| !TryGetTerritoryCenter(position, out _,
				out FormationRelayFlagEntity relay))
			return false;
		return relay is null
			|| !relay.HasSpecialization
			|| relay.SpecializedMode == PermanentFormationKind.Protection;
	}

	private bool TryGetTerritoryCenter(Vector2 position,
		out Vector2 territoryCenter, out FormationRelayFlagEntity relay)
	{
		float relayRadiusSquared = FormationRelayFlagEntity.TerritoryRadiusPixels
			* FormationRelayFlagEntity.TerritoryRadiusPixels;
		foreach (TileEntity entity in TileEntity.ByID.Values)
		{
			if (entity is FormationRelayFlagEntity flag
				&& flag.LinkedCoreId == ID
				&& IsRelayAuthorized(flag.ID)
				&& Vector2.DistanceSquared(flag.WorldCenter, WorldCenter)
					<= RelayLinkRangePixels * RelayLinkRangePixels
				&& Vector2.DistanceSquared(position, flag.WorldCenter)
					< relayRadiusSquared)
			{
				territoryCenter = flag.WorldCenter;
				relay = flag;
				return true;
			}
		}
		if (Vector2.DistanceSquared(position, WorldCenter)
			< RadiusPixels * RadiusPixels)
		{
			territoryCenter = WorldCenter;
			relay = null;
			return true;
		}
		territoryCenter = default;
		relay = null;
		return false;
	}

	private void SpawnVisuals()
	{
		if (Main.netMode == NetmodeID.Server)
			return;
		if (Main.GameUpdateCount % 30 == 0 && !HasDomeProjectile())
		{
			Projectile.NewProjectile(
				new EntitySource_Misc("PermanentFormationDome"),
				WorldCenter, Vector2.Zero,
				ModContent.ProjectileType<PermanentFormationDomeProjectile>(),
				0, 0f, Main.myPlayer, ID);
		}
		Lighting.AddLight(WorldCenter, 0.08f, 0.42f, 0.48f);
		if (Main.rand.NextBool(14)
			&& Common.Config.CultivationClientConfig.ShouldSpawnParticle())
		{
			float angle = Main.rand.NextFloat(MathHelper.TwoPi);
			Dust dust = Dust.NewDustPerfect(WorldCenter
				+ angle.ToRotationVector2() * Main.rand.NextFloat(18f, 34f),
				DustID.MagicMirror, -angle.ToRotationVector2() * 0.4f,
				50, FormationColor, 0.9f);
			dust.noGravity = true;
		}
		if (Main.GameUpdateCount % 4 == 0
			&& Common.Config.CultivationClientConfig.ShouldSpawnParticle())
		{
			int particleCount = Common.Config.CultivationClientConfig
				.ScaleParticleCount(48, 12);
			float phase = (float)Main.GameUpdateCount * 0.0035f;
			for (int i = 0; i < particleCount; i++)
			{
				float angle = phase + MathHelper.TwoPi * i / particleCount
					+ Main.rand.NextFloat(-0.025f, 0.025f);
				float radialOffset = Main.rand.NextFloat(-5f, 5f);
				Vector2 position = WorldCenter
					+ angle.ToRotationVector2() * (RadiusPixels + radialOffset);
				Vector2 tangent = (angle + MathHelper.PiOver2)
					.ToRotationVector2() * Main.rand.NextFloat(0.15f, 0.45f);
				Dust boundary = Dust.NewDustPerfect(position,
					i % 3 == 0 ? DustID.MagicMirror : DustID.GemSapphire,
					tangent, 95, FormationColor,
					Main.rand.NextFloat(0.7f, 1.05f));
				boundary.noGravity = true;
				boundary.fadeIn = 1.05f;
			}
		}
	}

	private bool HasDomeProjectile()
	{
		int projectileType = ModContent.ProjectileType<PermanentFormationDomeProjectile>();
		for (int i = 0; i < Main.maxProjectiles; i++)
		{
			Projectile projectile = Main.projectile[i];
			if (projectile.active && projectile.type == projectileType
				&& (int)projectile.ai[0] == ID)
				return true;
		}
		return false;
	}

	private void UpdateSpiritVeinConnection()
	{
		Common.Config.CultivationServerConfig config =
			Common.Config.CultivationServerConfig.Instance;
		if (config is null || !config.EnableSpiritualQiZones)
		{
			NearbySpiritCrystalCount = 0;
			ConnectedToSpiritVein = false;
			return;
		}
		NearbySpiritCrystalCount = SpiritualQiConcentration.CountCrystals(
			WorldCenter, config.SpiritualQiZoneRadiusBlocks);
		ConnectedToSpiritVein = VeinQiGenerationPerSecond > 0;
	}

	private Player GetPathStudent(Player fallback = null)
	{
		if (!string.IsNullOrWhiteSpace(OwnerName))
		{
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player candidate = Main.player[i];
				if (candidate.active && candidate.name == OwnerName)
					return candidate;
			}
		}
		return fallback is { active: true } ? fallback : null;
	}

	public NetworkText GetFormationKindNetworkText() =>
		NetworkText.FromKey(
			$"Mods.Xianxia.PermanentFormation.Types.{FormationKind}");

	public bool IsModeEnabled(PermanentFormationKind kind) =>
		(enabledFormationMask & (1 << (int)kind)) != 0;

	private NetworkText GetModeStateNetworkText(PermanentFormationKind kind) =>
		NetworkText.FromKey(IsModeEnabled(kind)
			? "Mods.Xianxia.PermanentFormation.ModeOn"
			: "Mods.Xianxia.PermanentFormation.ModeOff");

	private void ToggleSelectedMode(Player player)
	{
		int selectedIndex = (int)FormationKind;
		if (selectedIndex > Math.Min(3, Tier))
			return;
		int bit = 1 << selectedIndex;
		bool enabled = (enabledFormationMask & bit) != 0;
		if (enabled)
		{
			if (ActiveFormationModeCount <= 1)
			{
				SendStatus(player, "PermanentFormation.LastModeRequired");
				return;
			}
			enabledFormationMask = (byte)(enabledFormationMask & ~bit);
		}
		else
		{
			if (ActiveFormationModeCount >= MaxActiveFormationModes)
			{
				SendStatus(player, "PermanentFormation.NoFreeSlots",
					MaxActiveFormationModes);
				return;
			}
			enabledFormationMask = (byte)(enabledFormationMask | bit);
		}
		SendStatus(player, "PermanentFormation.ModeToggled",
			GetFormationKindNetworkText(),
			GetModeStateNetworkText(FormationKind),
			ActiveFormationModeCount, MaxActiveFormationModes,
			QiUpkeepPerSecond);
	}

	private void SendStatus(Player player, string key, params object[] args)
	{
		if (Main.netMode == NetmodeID.Server)
			ChatHelper.SendChatMessageToClient(
				NetworkText.FromKey($"Mods.Xianxia.{key}", args),
				new Color(80, 225, 255), player.whoAmI);
		else
			Main.NewText(Mod.GetLocalization(key).Format(args),
				new Color(80, 225, 255));
	}

	private void Sync()
	{
		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID);
	}

	public override void SaveData(TagCompound tag)
	{
		tag["storedQi"] = StoredQi;
		tag["tier"] = Tier;
		tag["stage"] = Stage;
		tag["integrity"] = Integrity;
		tag["active"] = Active;
		tag["ownerName"] = OwnerName;
		tag["accessMode"] = (byte)AccessMode;
		tag["veinInsightGranted"] = veinInsightGranted;
		tag["formationKind"] = (byte)FormationKind;
		tag["enabledFormationMask"] = enabledFormationMask;
	}

	public override void LoadData(TagCompound tag)
	{
		Tier = Math.Clamp(tag.GetInt("tier"), 0, FormationPathPlayer.MaxTier);
		Stage = Math.Clamp(tag.GetInt("stage"), 0, FormationPathPlayer.MaxStage);
		StoredQi = Math.Clamp(tag.GetInt("storedQi"), 0, MaximumStoredQi);
		Integrity = Math.Clamp(tag.GetInt("integrity"), 0, MaximumIntegrity);
		Active = tag.GetBool("active");
		OwnerName = tag.GetString("ownerName");
		AccessMode = (FormationAccessMode)Math.Clamp(
			(int)tag.GetByte("accessMode"), 0, 2);
		veinInsightGranted = tag.GetBool("veinInsightGranted");
		FormationKind = (PermanentFormationKind)Math.Clamp(
			(int)tag.GetByte("formationKind"), 0, 3);
		enabledFormationMask = tag.ContainsKey("enabledFormationMask")
			? (byte)(tag.GetByte("enabledFormationMask") & 0b1111)
			: (byte)(1 << (int)FormationKind);
		if (enabledFormationMask == 0)
			enabledFormationMask = 1;
	}

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write(StoredQi);
		writer.Write((byte)Tier);
		writer.Write((byte)Stage);
		writer.Write(Integrity);
		writer.Write(Active);
		writer.Write(ConnectedToSpiritVein);
		writer.Write(NearbySpiritCrystalCount);
		writer.Write(OwnerName ?? string.Empty);
		writer.Write((byte)AccessMode);
		writer.Write(veinInsightGranted);
		writer.Write((byte)FormationKind);
		writer.Write(enabledFormationMask);
	}

	public override void NetReceive(BinaryReader reader)
	{
		StoredQi = reader.ReadInt32();
		Tier = reader.ReadByte();
		Stage = reader.ReadByte();
		Integrity = reader.ReadInt32();
		Active = reader.ReadBoolean();
		ConnectedToSpiritVein = reader.ReadBoolean();
		NearbySpiritCrystalCount = reader.ReadInt32();
		OwnerName = reader.ReadString();
		AccessMode = (FormationAccessMode)Math.Clamp((int)reader.ReadByte(), 0, 2);
		veinInsightGranted = reader.ReadBoolean();
		FormationKind = (PermanentFormationKind)Math.Clamp(
			(int)reader.ReadByte(), 0, 3);
		enabledFormationMask = (byte)(reader.ReadByte() & 0b1111);
		if (enabledFormationMask == 0)
			enabledFormationMask = 1;
	}
}
